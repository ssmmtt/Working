using System.Runtime.InteropServices;

namespace Working
{
    /// <summary>
    /// 外接显示器亮度控制（DDC/CI，dxva2）。支持任意数量的物理显示器。
    /// </summary>
    internal sealed class DdcBrightness
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Rect { public int left, top, right, bottom; }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct PhysicalMonitor
        {
            public IntPtr hPhysicalMonitor;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szPhysicalMonitorDescription;
        }

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref Rect rect, IntPtr data);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc proc, IntPtr data);

        [DllImport("dxva2.dll", SetLastError = true)]
        private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, ref uint count);

        [DllImport("dxva2.dll", SetLastError = true)]
        private static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint count, [Out] PhysicalMonitor[] arr);

        [DllImport("dxva2.dll", SetLastError = true)]
        private static extern bool DestroyPhysicalMonitor(IntPtr hPhysicalMonitor);

        [DllImport("dxva2.dll", SetLastError = true)]
        private static extern bool GetMonitorBrightness(IntPtr h, ref uint min, ref uint current, ref uint max);

        [DllImport("dxva2.dll", SetLastError = true)]
        private static extern bool SetMonitorBrightness(IntPtr h, uint brightness);

        // key: 显示器序号+描述（区分多台同名显示器）
        private readonly Dictionary<string, uint> _saved = new();

        public bool IsDimmed { get; private set; }

        public bool IsSupported()
        {
            bool found = false;
            ForEach((_, handle) =>
            {
                uint min = 0, cur = 0, max = 0;
                if (GetMonitorBrightness(handle, ref min, ref cur, ref max))
                    found = true;
            });
            return found;
        }

        public bool DimToMinimum()
        {
            if (IsDimmed) return true;

            ForEach((key, handle) =>
            {
                uint min = 0, cur = 0, max = 0;
                if (!GetMonitorBrightness(handle, ref min, ref cur, ref max) || cur <= min)
                    return;

                if (SetMonitorBrightness(handle, min))
                {
                    _saved[key] = cur;
                    AppLog.Print("亮度", $"[{key}] {cur} -> {min}");
                }
            });

            IsDimmed = _saved.Count > 0;
            return IsDimmed;
        }

        public void Restore()
        {
            if (!IsDimmed) return;

            ForEach((key, handle) =>
            {
                if (_saved.TryGetValue(key, out uint saved) && SetMonitorBrightness(handle, saved))
                    AppLog.Print("亮度", $"[{key}] 恢复为 {saved}");
            });

            _saved.Clear();
            IsDimmed = false;
        }

        private void ForEach(Action<string, IntPtr> action)
        {
            var list = new List<(string Key, IntPtr Handle)>();
            int index = 0;
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, EnumCallback, IntPtr.Zero);

            foreach (var (key, handle) in list)
            {
                try { action(key, handle); }
                finally { DestroyPhysicalMonitor(handle); }
            }

            bool EnumCallback(IntPtr hMon, IntPtr _, ref Rect __, IntPtr ___)
            {
                uint n = 0;
                if (!GetNumberOfPhysicalMonitorsFromHMONITOR(hMon, ref n) || n == 0)
                    return true;

                var arr = new PhysicalMonitor[n];
                if (!GetPhysicalMonitorsFromHMONITOR(hMon, n, arr))
                    return true;

                foreach (var pm in arr)
                    list.Add(($"外接#{index++} {pm.szPhysicalMonitorDescription}", pm.hPhysicalMonitor));
                return true;
            }
        }
    }
}
