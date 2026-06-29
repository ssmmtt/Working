namespace Working
{
    /// <summary>
    /// 笔记本内置屏亮度控制（WMI root\WMI）。
    /// 通过后期绑定 COM（WbemScripting）调用，避免额外 NuGet 依赖。
    /// </summary>
    internal sealed class WmiBrightness
    {
        private byte? _saved;

        public bool IsDimmed => _saved != null;

        public bool IsSupported()
        {
            try { return QueryCurrentBrightness() != null; }
            catch { return false; }
        }

        public bool DimToMinimum()
        {
            try
            {
                byte? current = QueryCurrentBrightness();
                if (current == null) return false;

                if (current.Value == 0)
                {
                    AppLog.Print("亮度", "[内置屏] 已为最低");
                    return false;
                }

                if (SetBrightness(0))
                {
                    _saved = current;
                    AppLog.Print("亮度", $"[内置屏] {current} -> 0");
                    return true;
                }
            }
            catch (Exception ex)
            {
                AppLog.Print("亮度", $"[内置屏] 调暗失败：{ex.Message}");
            }
            return false;
        }

        public void Restore()
        {
            if (_saved == null) return;

            try
            {
                if (SetBrightness(_saved.Value))
                    AppLog.Print("亮度", $"[内置屏] 恢复为 {_saved}");
            }
            catch (Exception ex)
            {
                AppLog.Print("亮度", $"[内置屏] 恢复失败：{ex.Message}");
            }
            finally
            {
                _saved = null;
            }
        }

        private static dynamic? ConnectWmi()
        {
            Type? locatorType = Type.GetTypeFromProgID("WbemScripting.SWbemLocator");
            if (locatorType == null) return null;

            dynamic locator = Activator.CreateInstance(locatorType)!;
            return locator.ConnectServer(".", @"root\WMI");
        }

        private static byte? QueryCurrentBrightness()
        {
            dynamic? services = ConnectWmi();
            if (services == null) return null;

            foreach (dynamic inst in services.InstancesOf("WmiMonitorBrightness"))
                return (byte)inst.CurrentBrightness;

            return null;
        }

        private static bool SetBrightness(byte level)
        {
            dynamic? services = ConnectWmi();
            if (services == null) return false;

            bool any = false;
            foreach (dynamic inst in services.InstancesOf("WmiMonitorBrightnessMethods"))
            {
                inst.WmiSetBrightness(5u, level);
                any = true;
            }
            return any;
        }
    }
}
