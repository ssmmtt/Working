using Microsoft.Win32;

namespace Working
{
    /// <summary>
    /// 读取 Windows 电源方案中的屏幕空闲超时，用作自动调暗阈值。
    /// </summary>
    internal static class SystemIdleTimeout
    {
        private static readonly (Guid Sub, Guid Idle)[] Guids =
        [
            (new("7516b95f-f776-4464-8c53-06167f40cc99"), new("3c0bc021-c8a8-4e07-a973-6b14cbcb2b7e")),
            (new("7516b95f-f776-4464-8c53-06167f40cccf"), new("3c0bc021-c8a8-4e07-a973-6d714efb8bb3")),
        ];

        private static TimeSpan? _last;
        private static bool? _lastOnAc;

        public static TimeSpan? GetIdleThreshold()
        {
            bool onAc = SystemInformation.PowerStatus.PowerLineStatus != PowerLineStatus.Offline;
            string src = onAc ? "交流电源" : "电池";

            using var schemes = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes");
            if (schemes?.GetValue("ActivePowerScheme") is not string schemeText
                || !Guid.TryParse(schemeText, out Guid scheme))
            {
                LogChange(null, onAc, "读取活动电源方案失败");
                return null;
            }

            string key = onAc ? "ACSettingIndex" : "DCSettingIndex";
            TimeSpan? timeout = null;
            foreach (var (sub, idle) in Guids)
            {
                using var k = Registry.LocalMachine.OpenSubKey(
                    $@"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes\{scheme:D}\{sub:D}\{idle:D}");
                if (k?.GetValue(key) is int sec && sec > 0)
                {
                    timeout = TimeSpan.FromSeconds(sec);
                    break;
                }
            }

            if (timeout == null)
                LogChange(null, onAc, $"空闲超时=未设置或无法读取（{src}）");
            else
                LogChange(timeout, onAc, $"空闲超时={timeout.Value.TotalSeconds:F0}s（{timeout.Value:mm\\:ss}，{src}）");

            return timeout;
        }

        private static void LogChange(TimeSpan? t, bool onAc, string msg)
        {
            if (_last == t && _lastOnAc == onAc) return;

            if (_last != null || _lastOnAc != null)
            {
                string old = _last == null ? "未设置" : $"{_last.Value.TotalSeconds:F0}s";
                string neu = t == null ? "未设置" : $"{t.Value.TotalSeconds:F0}s";
                AppLog.Print("电源", $"空闲超时变更：{old} -> {neu}");
            }
            else
            {
                AppLog.Print("电源", msg);
            }

            _last = t;
            _lastOnAc = onAc;
        }
    }
}
