using System.Runtime.InteropServices;

namespace Working
{
    internal sealed class MonitorBrightness
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

        private readonly Dictionary<string, uint> _saved = new();

        public bool IsDimmed { get; private set; }

        public bool IsSupported()
        {
            bool found = false;
            ForEach((_, _) => found = true);
            return found;
        }

        public bool DimToMinimum()
        {
            if (IsDimmed) return true;

            ForEach((name, handle) =>
            {
                uint min = 0, cur = 0, max = 0;
                if (!GetMonitorBrightness(handle, ref min, ref cur, ref max) || cur <= min)
                    return;

                if (SetMonitorBrightness(handle, min))
                {
                    _saved[name] = cur;
                    AppLog.Print("亮度", $"[{name}] {cur} -> {min}");
                }
            });

            IsDimmed = _saved.Count > 0;
            return IsDimmed;
        }

        public void Restore()
        {
            if (!IsDimmed) return;

            ForEach((name, handle) =>
            {
                if (_saved.TryGetValue(name, out uint saved) && SetMonitorBrightness(handle, saved))
                    AppLog.Print("亮度", $"[{name}] 恢复为 {saved}");
            });

            _saved.Clear();
            IsDimmed = false;
        }

        private void ForEach(Action<string, IntPtr> action)
        {
            var list = new List<(string Name, IntPtr Handle)>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, EnumCallback, IntPtr.Zero);

            foreach (var (name, handle) in list)
            {
                try { action(name, handle); }
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
                    list.Add((pm.szPhysicalMonitorDescription, pm.hPhysicalMonitor));
                return true;
            }
        }
    }
}
