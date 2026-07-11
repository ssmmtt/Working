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
            bool wmi = _wmi.DimToMinimum();
            bool ddc = _ddc.DimToMinimum();
            if (!wmi && !ddc)
                AppLog.Print("亮度", "本次调暗：外接屏与内置屏均未成功");
            if (wmi || ddc)
                Persist();
            return wmi || ddc;
        }

        public void Restore()
        {
            _wmi.Restore();
            _ddc.Restore();
            Persist();
        }

        /// <summary>
        /// 启动时恢复上次异常关机前未恢复的亮度。
        /// </summary>
        public bool RestoreFromPreviousSession()
        {
            if (!BrightnessState.TryLoad(out var ddc, out byte? wmi))
                return false;

            if (ddc.Count > 0)
                _ddc.LoadSaved(ddc);
            if (wmi != null)
                _wmi.LoadSaved(wmi.Value);

            AppLog.Print("亮度", "检测到上次未恢复的调暗状态，正在恢复");
            Restore();
            return true;
        }

        private void Persist()
        {
            if (!IsDimmed)
            {
                BrightnessState.Clear();
                return;
            }

            BrightnessState.Save(
                _ddc.IsDimmed ? _ddc.SavedBrightness : null,
                _wmi.SavedBrightness);
        }
    }
}
