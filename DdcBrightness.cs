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

        public IReadOnlyDictionary<string, uint> SavedBrightness => _saved;

        public void LoadSaved(IReadOnlyDictionary<string, uint> saved)
        {
            _saved.Clear();
            foreach (var kv in saved)
                _saved[kv.Key] = kv.Value;
            IsDimmed = _saved.Count > 0;
        }

        public void Restore()
        {
            if (!IsDimmed) return;

            // 远程桌面连接/断开后，显示器枚举顺序可能变化。
            // 除精确 key 匹配外，使用剩余亮度值兜底恢复，避免卡在暗屏。
            var pending = new Dictionary<string, uint>(_saved);
            var remaining = new Queue<uint>(_saved.Values);
            int success = 0;
            ForEach((key, handle) =>
            {
                if (pending.TryGetValue(key, out uint saved))
                {
                    if (TryRestoreWithVerify(handle, saved))
                    {
                        AppLog.Print("亮度", $"[{key}] 恢复为 {saved}");
                        pending.Remove(key);
                        success++;
                    }
                    else
                    {
                        AppLog.Print("亮度", $"[{key}] 恢复失败（目标 {saved}）");
                    }
                    return;
                }

                if (remaining.Count > 0)
                {
                    uint fallback = remaining.Dequeue();
                    if (TryRestoreWithVerify(handle, fallback))
                    {
                        AppLog.Print("亮度", $"[{key}] 使用兜底亮度恢复为 {fallback}");
                        success++;
                    }
                    else
                    {
                        AppLog.Print("亮度", $"[{key}] 兜底恢复失败（目标 {fallback}）");
                    }
                }
            });

            if (success == 0)
            {
                AppLog.Print("亮度", "外接屏恢复失败，将在后续输入/会话恢复时重试");
                IsDimmed = true;
                return;
            }

            _saved.Clear();
            foreach (var kv in pending)
                _saved[kv.Key] = kv.Value;
            IsDimmed = _saved.Count > 0;
        }

        private static bool TryRestoreWithVerify(IntPtr handle, uint target)
        {
            if (!SetMonitorBrightness(handle, target))
                return false;

            uint min = 0, cur = 0, max = 0;
            if (!GetMonitorBrightness(handle, ref min, ref cur, ref max))
            {
                // 无法回读时保守认为成功（部分显示器回读不稳定）
                return true;
            }

            if (target <= min)
                return cur <= min;

            // 允许显示器步进差异（目标附近 ±1）
            return cur >= target - 1;
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
