using System.Collections;
using System.Reflection;

namespace Working
{
    /// <summary>
    /// 笔记本内置屏亮度控制（WMI root\WMI）。
    /// 通过 COM（WbemScripting）反射调用，避免 dynamic 与额外 NuGet 依赖。
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

        public byte? SavedBrightness => _saved;

        public void LoadSaved(byte value) => _saved = value;

        public void Restore()
        {
            if (_saved == null) return;

            bool restored = false;
            try
            {
                if (SetBrightness(_saved.Value))
                {
                    AppLog.Print("亮度", $"[内置屏] 恢复为 {_saved}");
                    restored = true;
                }
            }
            catch (Exception ex)
            {
                AppLog.Print("亮度", $"[内置屏] 恢复失败：{ex.Message}");
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

        private static object? ConnectWmi()
        {
            try
            {
                Type? locatorType = Type.GetTypeFromProgID("WbemScripting.SWbemLocator");
                if (locatorType == null) return null;

                object locator = Activator.CreateInstance(locatorType)!;
                return locatorType.InvokeMember("ConnectServer",
                    BindingFlags.InvokeMethod, null, locator, new object?[] { ".", @"root\WMI" });
            }
            catch
            {
                return null;
            }
        }

        private static byte? QueryCurrentBrightness()
        {
            object? services = ConnectWmi();
            if (services == null) return null;

            foreach (object? inst in EnumerateInstances(services, "WmiMonitorBrightness"))
            {
                if (inst == null) continue;

                try
                {
                    object? value = ReadWmiProperty(inst, "CurrentBrightness");
                    if (value != null) return Convert.ToByte(value);
                }
                catch { /* 跳过无效实例 */ }
            }
            return null;
        }

        private static bool SetBrightness(byte level)
        {
            object? services = ConnectWmi();
            if (services == null) return false;

            bool any = false;
            foreach (object? inst in EnumerateInstances(services, "WmiMonitorBrightnessMethods"))
            {
                if (inst == null) continue;

                try
                {
                    inst.GetType().InvokeMember("WmiSetBrightness",
                        BindingFlags.InvokeMethod, null, inst, new object?[] { 5u, level });
                    any = true;
                }
                catch { /* 跳过无效实例 */ }
            }
            return any;
        }

        private static object? ReadWmiProperty(object inst, string name)
        {
            // SWbemObject：优先直接读属性，失败则走 Properties_.Item(name).Value
            try
            {
                return inst.GetType().InvokeMember(name,
                    BindingFlags.GetProperty | BindingFlags.InvokeMethod, null, inst, null);
            }
            catch
            {
                object? props = inst.GetType().InvokeMember("Properties_",
                    BindingFlags.GetProperty, null, inst, null);
                if (props == null) return null;

                object? prop = props.GetType().InvokeMember("Item",
                    BindingFlags.InvokeMethod, null, props, new object?[] { name, 0 });
                if (prop == null) return null;

                return prop.GetType().InvokeMember("Value",
                    BindingFlags.GetProperty, null, prop, null);
            }
        }

        private static IEnumerable EnumerateInstances(object services, string className)
        {
            object? instances;
            try
            {
                instances = services.GetType().InvokeMember("InstancesOf",
                    BindingFlags.InvokeMethod, null, services, new object?[] { className });
            }
            catch
            {
                yield break;
            }

            if (instances is not IEnumerable enumerable) yield break;

            foreach (object? item in enumerable)
            {
                if (item != null) yield return item;
            }
        }
    }
}
