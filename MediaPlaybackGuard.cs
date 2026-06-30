using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace Working
{
    /// <summary>
    /// 检测系统是否有应用正在输出音频（WASAPI，经 NAudio）。
    /// 某进程近期有过声音输出时，在宽限期内仍视为播放中（覆盖电影静音片段）。
    /// </summary>
    internal static class MediaPlaybackGuard
    {
        private static readonly int OwnPid = Environment.ProcessId;
        private const float PeakThreshold = 0.0001f;

        /// <summary>电影等静音片段的最长保留时间（自上次检测到声音起算）。</summary>
        private static readonly TimeSpan SilentGapHold = TimeSpan.FromMinutes(3);

        private static readonly Dictionary<uint, DateTime> _lastAudibleUtc = new();
        private static DateTime _lastAnyAudibleUtc = DateTime.MinValue;

        public static bool IsActive()
        {
            try
            {
                var now = DateTime.UtcNow;
                bool playing = false;

                var enumerator = new MMDeviceEnumerator();
                foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    if (ScanDevice(device, now))
                        playing = true;
                }

                PruneStale(now);

                if (!playing && now - _lastAnyAudibleUtc < SilentGapHold)
                    playing = true;

                return playing;
            }
            catch (Exception ex)
            {
                AppLog.Print("媒体", $"检测失败：{ex.Message}");
                return false;
            }
        }

        private static bool ScanDevice(MMDevice device, DateTime now)
        {
            bool playing = false;
            var sessions = device.AudioSessionManager.Sessions;

            for (int i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];
                if (session.State != AudioSessionState.AudioSessionStateActive)
                    continue;

                if (session.IsSystemSoundsSession)
                    continue;

                uint pid;
                try { pid = session.GetProcessID; }
                catch { continue; }

                if (pid == 0 || pid == (uint)OwnPid)
                    continue;

                if (session.SimpleAudioVolume is { Mute: true })
                    continue;

                float peak;
                try { peak = session.AudioMeterInformation?.MasterPeakValue ?? 0; }
                catch { continue; }

                if (peak > PeakThreshold)
                {
                    _lastAudibleUtc[pid] = now;
                    _lastAnyAudibleUtc = now;
                    AppLog.Print("媒体", $"检测到音频输出 pid={pid} peak={peak:F3} ({device.FriendlyName})");
                    playing = true;
                    continue;
                }

                // 会话仍活跃但当前静音：若该进程近期有过声音，视为电影静音片段
                if (_lastAudibleUtc.TryGetValue(pid, out var last)
                    && now - last < SilentGapHold)
                {
                    playing = true;
                }
            }

            return playing;
        }

        private static void PruneStale(DateTime now)
        {
            var expire = now - SilentGapHold;
            foreach (var pid in _lastAudibleUtc.Keys.ToList())
            {
                if (_lastAudibleUtc[pid] < expire)
                    _lastAudibleUtc.Remove(pid);
            }
        }
    }
}
