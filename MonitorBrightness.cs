namespace Working
{
    /// <summary>
    /// 统一的亮度管理：同时驱动外接显示器（DDC/CI）和笔记本内置屏（WMI）。
    /// 不论台式机多显示器，还是笔记本（内置屏 + 任意数量外接屏），都会全部调暗 / 恢复。
    /// </summary>
    internal sealed class MonitorBrightness
    {
        private readonly DdcBrightness _ddc = new();
        private readonly WmiBrightness _wmi = new();

        public bool IsDimmed => _ddc.IsDimmed || _wmi.IsDimmed;

        public bool IsSupported()
        {
            bool ddc = _ddc.IsSupported();
            bool wmi = _wmi.IsSupported();
            AppLog.Print("亮度", $"外接屏(DDC/CI)：{(ddc ? "可用" : "不可用")}，内置屏(WMI)：{(wmi ? "可用" : "不可用")}");
            return ddc || wmi;
        }

        public bool DimToMinimum()
        {
            // 两类都尝试，任一成功即视为已调暗
            bool wmi = _wmi.DimToMinimum();
            bool ddc = _ddc.DimToMinimum();
            return wmi || ddc;
        }

        public void Restore()
        {
            _wmi.Restore();
            _ddc.Restore();
        }
    }
}
