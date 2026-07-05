using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Working
{
    /// <summary>
    /// 空闲检测、DDC 调暗/恢复亮度、F15 防离开。
    /// </summary>
    internal sealed class IdleManager : IDisposable
    {
        private const uint ES_CONTINUOUS = 0x80000000;
        private const uint ES_SYSTEM_REQUIRED = 0x00000001;
        private const uint ES_DISPLAY_REQUIRED = 0x00000002;
        private const byte VK_F15 = 0x7E;
        private const byte ScanF15 = 0x68;
        private const int KeyeventfExtendedkey = 0x0001;
        private const int KeyeventfKeyup = 0x0002;

        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan DimmedPollInterval = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan DefaultThreshold = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan KeepAliveInputGrace = TimeSpan.FromSeconds(45);

        [StructLayout(LayoutKind.Sequential)]
        private struct LastInputInfo { public uint cbSize; public uint dwTime; }

        private readonly MonitorBrightness _brightness = new();
        private readonly System.Windows.Forms.Timer _timer = new();

        private DateTime _lastActivity;
        private uint _lastInputTick;
        private uint _syntheticTick;
        private bool _hasSynthetic;
        private bool _enabled;
        private bool _canDim;
        private Func<bool>? _inWorkHours;
        private bool _wasInWorkHours;
        private bool _wasMediaPlaying;
        private DateTime _lastDimmedLog;
        private DateTime _lastMediaLog;
        private bool _pendingLocalRestore;
        private bool _suppressRestore;
        private bool _remoteSessionActive;

        [DllImport("user32.dll")] private static extern bool GetLastInputInfo(ref LastInputInfo i);
        [DllImport("user32.dll")] private static extern void keybd_event(byte vk, byte scan, int flags, int extra);
        [DllImport("kernel32.dll")] private static extern uint SetThreadExecutionState(uint flags);

        public IdleManager()
        {
            _timer.Tick += (_, _) => OnTick();
            SystemEvents.SessionSwitch += OnSessionSwitch;
        }

        public void Enable(Func<bool> inWorkHours)
        {
            _inWorkHours = inWorkHours;
            _enabled = true;
            _hasSynthetic = false;
            _wasInWorkHours = false;
            _wasMediaPlaying = false;
            _pendingLocalRestore = false;
            _suppressRestore = false;
            _remoteSessionActive = false;
            _lastInputTick = QueryInputTick();
            _lastActivity = DateTime.UtcNow;

            _canDim = _brightness.IsSupported();

            var sys = SystemIdleTimeout.GetIdleThreshold();
            AppLog.Print("空闲", sys == null
                ? $"调暗阈值默认 {DefaultThreshold.TotalMinutes:F0} 分钟"
                : $"调暗阈值 {sys.Value.TotalSeconds:F0}s（跟随系统电源）");

            ApplyPowerState();
            SetPollInterval();
            _timer.Start();
            AppLog.Print("空闲", $"已启用，检测间隔 {_timer.Interval} ms");
        }

        public void Disable()
        {
            _enabled = false;
            _timer.Stop();
            _brightness.Restore();
            SetThreadExecutionState(ES_CONTINUOUS);
        }

        public void Dispose()
        {
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            Disable();
        }

        public void KeepAlive()
        {
            if (!_enabled) return;

            ApplyPowerState();

            // 用户近期有真实键鼠操作（打游戏、办公等）时不模拟按键，避免干扰前台应用
            if (HasRecentRealInput(KeepAliveInputGrace))
                return;

            // 必须带扩展键扫描码，否则 scan=0 时部分终端（如 Xshell）会把 0x7E 当成 ~ 字符
            keybd_event(VK_F15, ScanF15, KeyeventfExtendedkey, 0);
            keybd_event(VK_F15, ScanF15, KeyeventfExtendedkey | KeyeventfKeyup, 0);
            MarkSynthetic();
            AppLog.Print("防休眠", "F15");
        }

        private void OnTick()
        {
            if (!_enabled) return;

            if (_pendingLocalRestore)
            {
                _pendingLocalRestore = false;
                _lastInputTick = QueryInputTick();
                _lastActivity = DateTime.UtcNow;
                if (_brightness.IsDimmed)
                {
                    _brightness.Restore();
                    ApplyPowerState();
                    if (_brightness.IsDimmed)
                    {
                        AppLog.Print("空闲", "本机解锁，恢复亮度失败，将重试");
                        _pendingLocalRestore = true;
                    }
                    else
                    {
                        AppLog.Print("空闲", "本机解锁，已恢复亮度");
                    }
                }
            }

            bool inWorkHours = _inWorkHours?.Invoke() == true;
            if (!inWorkHours)
            {
                if (_brightness.IsDimmed)
                {
                    _brightness.Restore();
                    ApplyPowerState();
                    AppLog.Print("空闲", "已离开工作时间段，恢复亮度");
                }
                else if (_wasInWorkHours)
                {
                    AppLog.Print("空闲", "已离开工作时间段");
                }

                _wasInWorkHours = false;
                SetPollInterval();
                return;
            }

            _wasInWorkHours = true;
            PollInput();

            if (HandleMediaPlayback())
            {
                SetPollInterval();
                return;
            }

            if (_brightness.IsDimmed)
            {
                if (DateTime.UtcNow - _lastDimmedLog >= TimeSpan.FromSeconds(5))
                {
                    AppLog.Print("空闲", "检测：已调暗，等待用户输入恢复亮度");
                    _lastDimmedLog = DateTime.UtcNow;
                }
                SetPollInterval();
                return;
            }

            var threshold = SystemIdleTimeout.GetIdleThreshold() ?? DefaultThreshold;
            var idle = DateTime.UtcNow - _lastActivity;
            AppLog.Print("空闲", $"检测：空闲 {idle.TotalSeconds:F0}s / 阈值 {threshold.TotalSeconds:F0}s");

            if (idle >= threshold && _brightness.DimToMinimum())
                ApplyPowerState();

            SetPollInterval();
        }

        /// <summary>播放中跳过调暗；若已调暗则恢复。返回 true 表示本轮不再继续空闲调暗逻辑。</summary>
        private bool HandleMediaPlayback()
        {
            if (!MediaPlaybackGuard.IsActive())
            {
                _wasMediaPlaying = false;
                return false;
            }

            if (_brightness.IsDimmed && !_suppressRestore)
            {
                _brightness.Restore();
                ApplyPowerState();
                AppLog.Print("媒体", "检测到播放中，恢复亮度");
            }
            else if (!_wasMediaPlaying || DateTime.UtcNow - _lastMediaLog >= TimeSpan.FromSeconds(30))
            {
                AppLog.Print("媒体", "检测到播放中，跳过调暗");
                _lastMediaLog = DateTime.UtcNow;
            }

            _wasMediaPlaying = true;
            return true;
        }

        private void PollInput()
        {
            uint tick = QueryInputTick();
            if (tick == 0 || tick == _lastInputTick) return;

            _lastInputTick = tick;
            if (_hasSynthetic && tick == _syntheticTick)
            {
                AppLog.Print("用户活动", $"模拟输入 tick={tick}，不计入活动");
                return;
            }

            _lastActivity = DateTime.UtcNow;
            if (_brightness.IsDimmed && !_suppressRestore)
            {
                _brightness.Restore();
                ApplyPowerState();
                AppLog.Print("用户活动", _brightness.IsDimmed ? "恢复亮度失败，将重试" : "恢复亮度");
            }
        }

        private void MarkSynthetic()
        {
            _syntheticTick = QueryInputTick();
            _hasSynthetic = true;
            _lastInputTick = _syntheticTick;
        }

        /// <summary>近期是否有真实用户键鼠输入（排除本程序模拟的 F15）。</summary>
        private bool HasRecentRealInput(TimeSpan threshold)
        {
            uint tick = QueryInputTick();
            if (tick == 0) return false;

            if (_hasSynthetic && tick == _syntheticTick)
                return false;

            return ElapsedMs(tick) < (uint)threshold.TotalMilliseconds;
        }

        private static uint ElapsedMs(uint fromTick) => unchecked((uint)Environment.TickCount - fromTick);

        private static uint QueryInputTick()
        {
            var i = new LastInputInfo { cbSize = (uint)Marshal.SizeOf<LastInputInfo>() };
            return GetLastInputInfo(ref i) ? i.dwTime : 0;
        }

        private void ApplyPowerState()
        {
            uint f = ES_CONTINUOUS | ES_SYSTEM_REQUIRED;
            // DDC 可用时保持显示输出，由程序调暗亮度而非交给系统处理
            if (_canDim) f |= ES_DISPLAY_REQUIRED;
            SetThreadExecutionState(f);
        }

        private void SetPollInterval()
        {
            int ms = (int)(_brightness.IsDimmed ? DimmedPollInterval : PollInterval).TotalMilliseconds;
            if (_timer.Interval != ms)
            {
                string mode = _brightness.IsDimmed ? "恢复监视" : "空闲检测";
                AppLog.Print("空闲", $"{mode}间隔：{_timer.Interval} ms -> {ms} ms");
                _timer.Interval = ms;
            }
        }

        private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (!_enabled) return;
            switch (e.Reason)
            {
                case SessionSwitchReason.RemoteConnect:
                    _remoteSessionActive = true;
                    _suppressRestore = true;
                    AppLog.Print("空闲", $"会话切换：{e.Reason}，保持调暗状态");
                    break;

                case SessionSwitchReason.SessionLock:
                    _suppressRestore = true;
                    AppLog.Print("空闲", $"会话切换：{e.Reason}，保持调暗状态");
                    break;

                case SessionSwitchReason.RemoteDisconnect:
                    // 远程已结束；若仍处于锁屏则继续抑制键鼠恢复，等待本机解锁
                    _remoteSessionActive = false;
                    _suppressRestore = true;
                    AppLog.Print("空闲", $"会话切换：{e.Reason}，保持调暗，等待本机解锁");
                    break;

                case SessionSwitchReason.ConsoleConnect:
                    _remoteSessionActive = false;
                    _suppressRestore = false;
                    _pendingLocalRestore = true;
                    AppLog.Print("空闲", $"会话切换：{e.Reason}，本机控制台恢复亮度");
                    break;

                case SessionSwitchReason.SessionLogon:
                    if (!_remoteSessionActive)
                    {
                        _suppressRestore = false;
                        _pendingLocalRestore = true;
                        AppLog.Print("空闲", $"会话切换：{e.Reason}，登录后恢复亮度");
                    }
                    break;

                case SessionSwitchReason.SessionUnlock:
                    if (_remoteSessionActive)
                    {
                        AppLog.Print("空闲", $"会话切换：{e.Reason}，远程会话中，保持调暗");
                    }
                    else
                    {
                        _suppressRestore = false;
                        _pendingLocalRestore = true;
                        AppLog.Print("空闲", $"会话切换：{e.Reason}，本地解锁后恢复亮度");
                    }
                    break;
            }
        }
    }
}
