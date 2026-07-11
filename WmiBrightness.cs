using System.Management;

namespace Working
{
    /// <summary>
    /// 笔记本内置屏亮度控制（WMI root\WMI，System.Management，与 PowerShell CIM 同路径）。
    /// </summary>
    internal sealed class WmiBrightness
    {
        private const string WmiPath = @"root\WMI";
        private byte? _saved;

        public bool IsDimmed => _saved != null;

        public bool IsSupported()
        {
            try
            {
                LogProbe();
                return QueryCurrentBrightness() != null;
            }
            catch (Exception ex)
            {
                AppLog.Print("亮度", $"[内置屏] 支持检测异常：{FormatError(ex)}");
                return false;
            }
        }

        public bool DimToMinimum()
        {
            try
            {
                byte? current = QueryCurrentBrightness(logDetail: true);
                if (current == null)
                {
                    AppLog.Print("亮度", "[内置屏] 调暗跳过：无法读取当前亮度");
                    return false;
                }

                AppLog.Print("亮度", $"[内置屏] 准备调暗，当前亮度 {current}");

                if (current.Value == 0)
                {
                    AppLog.Print("亮度", "[内置屏] 已为最低");
                    return false;
                }

                if (SetBrightness(0, "调暗"))
                {
                    _saved = current;
                    AppLog.Print("亮度", $"[内置屏] {current} -> 0");
                    return true;
                }

                AppLog.Print("亮度", "[内置屏] 调暗失败：WmiSetBrightness 未生效");
            }
            catch (Exception ex)
            {
                AppLog.Print("亮度", $"[内置屏] 调暗失败：{FormatError(ex)}");
            }
            return false;
        }

        public byte? SavedBrightness => _saved;

        public void LoadSaved(byte value) => _saved = value;

        public void Restore()
        {
            if (_saved == null) return;

            bool restored = false;
            try
            {
                if (SetBrightness(_saved.Value, "恢复"))
                {
                    AppLog.Print("亮度", $"[内置屏] 恢复为 {_saved}");
                    restored = true;
                }
            }
            catch (Exception ex)
            {
                AppLog.Print("亮度", $"[内置屏] 恢复失败：{FormatError(ex)}");
            }

            if (restored)
            {
                _saved = null;
            }
            else
            {
                AppLog.Print("亮度", "[内置屏] 本次恢复未成功，将在后续输入/会话恢复时重试");
            }
        }

        private static void LogProbe()
        {
            var scope = TryConnect(logFailure: true);
            if (scope == null) return;

            int brightnessCount = CountInstances(scope, "WmiMonitorBrightness");
            int methodsCount = CountInstances(scope, "WmiMonitorBrightnessMethods");
            AppLog.Print("亮度", $"[内置屏] WMI 探测：Brightness={brightnessCount}，Methods={methodsCount}");
        }

        private static ManagementScope? TryConnect(bool logFailure = false)
        {
            var scope = new ManagementScope($@"\\.\{WmiPath}");
            try
            {
                scope.Connect();
                return scope;
            }
            catch (Exception ex)
            {
                if (logFailure) AppLog.Print("亮度", $"[内置屏] WMI 连接失败：{FormatError(ex)}");
                return null;
            }
        }

        private static byte? QueryCurrentBrightness(bool logDetail = false)
        {
            var scope = TryConnect(logFailure: logDetail);
            if (scope == null) return null;

            using var searcher = new ManagementObjectSearcher(scope,
                new ObjectQuery("SELECT CurrentBrightness FROM WmiMonitorBrightness"));

            int index = 0;
            foreach (ManagementObject obj in searcher.Get())
            {
                using (obj)
                {
                    try
                    {
                        byte level = Convert.ToByte(obj["CurrentBrightness"]);
                        if (logDetail) AppLog.Print("亮度", $"[内置屏] Brightness#{index} CurrentBrightness={level}");
                        return level;
                    }
                    catch (Exception ex)
                    {
                        if (logDetail) AppLog.Print("亮度", $"[内置屏] Brightness#{index} 读取失败：{FormatError(ex)}");
                    }
                }

                index++;
            }

            if (logDetail) AppLog.Print("亮度", "[内置屏] 未读到任何 CurrentBrightness");
            return null;
        }

        private static bool SetBrightness(byte level, string action)
        {
            var scope = TryConnect(logFailure: true);
            if (scope == null) return false;

            byte? before = QueryCurrentBrightness();
            if (before != null)
                AppLog.Print("亮度", $"[内置屏] {action}前亮度 {before}");

            using var searcher = new ManagementObjectSearcher(scope,
                new ObjectQuery("SELECT * FROM WmiMonitorBrightnessMethods"));

            int index = 0;
            bool invoked = false;
            foreach (ManagementObject obj in searcher.Get())
            {
                using (obj)
                {
                    try
                    {
                        string id = obj.Path?.Path ?? $"#{index}";
                        AppLog.Print("亮度", $"[内置屏] Methods#{index} 调用 WmiSetBrightness(Timeout=5, Brightness={level}) {id}");

                        using ManagementBaseObject inParams = obj.GetMethodParameters("WmiSetBrightness");
                        inParams["Timeout"] = (uint)5;
                        inParams["Brightness"] = (byte)level;
                        using ManagementBaseObject? outParams = obj.InvokeMethod("WmiSetBrightness", inParams, null);

                        invoked = true;
                        uint ret = outParams?["ReturnValue"] is uint u ? u : 0;
                        AppLog.Print("亮度", $"[内置屏] Methods#{index} 返回 ReturnValue={ret}");
                    }
                    catch (Exception ex)
                    {
                        AppLog.Print("亮度", $"[内置屏] Methods#{index} 调用失败：{FormatError(ex)}");
                    }
                }

                index++;
            }

            if (!invoked)
            {
                AppLog.Print("亮度", $"[内置屏] {action}失败：未找到 WmiMonitorBrightnessMethods 实例");
                return false;
            }

            byte? after = QueryCurrentBrightness();
            if (after == null)
            {
                AppLog.Print("亮度", $"[内置屏] {action}后无法回读亮度");
                return false;
            }

            AppLog.Print("亮度", $"[内置屏] {action}后亮度 {after}");
            if (level == 0)
                return after.Value == 0;

            return after.Value >= level - 1;
        }

        private static int CountInstances(ManagementScope scope, string className)
        {
            using var searcher = new ManagementObjectSearcher(scope,
                new ObjectQuery($"SELECT * FROM {className}"));
            int count = 0;
            foreach (ManagementObject obj in searcher.Get())
            {
                obj.Dispose();
                count++;
            }
            return count;
        }

        private static string FormatError(Exception ex) =>
            ex.InnerException == null ? ex.Message : $"{ex.Message} -> {ex.InnerException.Message}";
    }
}
