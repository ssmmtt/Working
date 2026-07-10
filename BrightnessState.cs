namespace Working
{
    /// <summary>
    /// 将调暗前的亮度写入 config.ini 的 [brightness] 节，避免异常关机后重启无法恢复。
    /// </summary>
    internal static class BrightnessState
    {
        private const string Section = "brightness";

        private static IniConfig Config => new(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini"));

        public static void Save(IReadOnlyDictionary<string, uint>? ddc, byte? wmi)
        {
            try
            {
                if ((ddc == null || ddc.Count == 0) && wmi == null)
                {
                    Clear();
                    return;
                }

                var ini = Config;
                if (wmi != null)
                    ini.WriteKey(Section, "wmi", wmi.Value.ToString());
                else
                    ini.DeleteKey(Section, "wmi");

                if (ddc != null && ddc.Count > 0)
                {
                    string encoded = string.Join(";",
                        ddc.Select(kv => $"{kv.Key}|{kv.Value}"));
                    ini.WriteKey(Section, "ddc", encoded);
                }
                else
                {
                    ini.DeleteKey(Section, "ddc");
                }
            }
            catch (Exception ex)
            {
                AppLog.Print("亮度", $"保存调暗状态失败：{ex.Message}");
            }
        }

        public static bool TryLoad(out Dictionary<string, uint> ddc, out byte? wmi)
        {
            ddc = new Dictionary<string, uint>();
            wmi = null;

            try
            {
                var ini = Config;
                if (!ini.FileExist()) return false;

                string wmiText = ini.ReadKey(Section, "wmi");
                if (!string.IsNullOrEmpty(wmiText) && byte.TryParse(wmiText, out byte wmiValue))
                    wmi = wmiValue;

                string ddcText = ini.ReadKey(Section, "ddc");
                if (!string.IsNullOrEmpty(ddcText))
                {
                    foreach (string part in ddcText.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        int sep = part.LastIndexOf('|');
                        if (sep <= 0) continue;
                        string key = part[..sep];
                        if (uint.TryParse(part[(sep + 1)..], out uint value))
                            ddc[key] = value;
                    }
                }

                return ddc.Count > 0 || wmi != null;
            }
            catch (Exception ex)
            {
                AppLog.Print("亮度", $"读取调暗状态失败：{ex.Message}");
                return false;
            }
        }

        public static void Clear()
        {
            try
            {
                var ini = Config;
                if (!ini.FileExist()) return;
                ini.DeleteSection(Section);
            }
            catch (Exception ex)
            {
                AppLog.Print("亮度", $"清除调暗状态失败：{ex.Message}");
            }
        }
    }
}
