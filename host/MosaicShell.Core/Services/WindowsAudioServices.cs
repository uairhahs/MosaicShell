using NAudio.CoreAudioApi;

namespace MosaicShell.Core.Services
{
    public sealed class WindowsAudioService : IAudioService
    {
        private readonly MMDeviceEnumerator _enum = new();
        private readonly MMDeviceNotificationClient _deviceEvents;
        private readonly DefaultEndpointBinding<MMDevice> _endpoint;
        private bool _disposed;

        public WindowsAudioService()
        {
            // The default device is not fixed for the process: at log-on it may not exist yet, and it can change later
            _endpoint = new DefaultEndpointBinding<MMDevice>(ResolveDefault, OnBound, OnUnbound);
            _deviceEvents = _enum.CreateNotificationClient(useSynchronizationContext: false);
            _deviceEvents.DefaultDeviceChanged += (_, _) => Rebind();
            _deviceEvents.DeviceAdded += (_, _) => Rebind();
            _deviceEvents.DeviceRemoved += (_, _) => Rebind();
            _deviceEvents.DeviceStateChanged += (_, _) => Rebind();
            _ = _endpoint.Run(_ => 0, 0);
        }

        public double MasterVolume
        {
            get => _endpoint.Run(d => (double)d.AudioEndpointVolume.MasterVolumeLevelScalar, 0d);
            set
            {
                float v = (float)VolumePercent.Quantize(value);
                _ = _endpoint.Run(d =>
                {
                    float cur = d.AudioEndpointVolume.MasterVolumeLevelScalar;
                    if (Math.Abs(cur - v) < 0.004f) // <0.5% - already there
                    {
                        return 0;
                    }
                    // Pre-arm filter so our own write's notification doesn't look like an external change
                    _lastVol = v;
                    d.AudioEndpointVolume.MasterVolumeLevelScalar = v;
                    return 0;
                }, 0);
            }
        }

        public bool IsMuted
        {
            get => _endpoint.Run(d => d.AudioEndpointVolume.Mute, false);
            set => _ = _endpoint.Run(d =>
            {
                d.AudioEndpointVolume.Mute = value;
                return 0;
            }, 0);
        }

        private MMDevice? ResolveDefault()
        {
            return _enum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }

        private void OnBound(MMDevice device)
        {
            _lastVol = device.AudioEndpointVolume.MasterVolumeLevelScalar;
            _lastMute = device.AudioEndpointVolume.Mute;
            device.AudioEndpointVolume.OnVolumeNotification += OnVol;
        }

        private void OnUnbound(MMDevice device)
        {
            try { device.AudioEndpointVolume.OnVolumeNotification -= OnVol; } catch { /* ignore */ }
            device.Dispose();
        }

        // Runs on the Windows audio worker thread, which must not call back into the audio stack: flag, then rebind off-thread
        private void Rebind()
        {
            if (_disposed)
            {
                return;
            }

            _endpoint.MarkStale();
            _ = Task.Run(() => _endpoint.Run(_ => 0, 0));
        }

        public event EventHandler? Changed;

        private float _lastVol = float.NaN;
        private bool _lastMute;

        private void OnVol(AudioVolumeNotificationData data)
        {
            // Ignore no-op notifications some drivers emit without a real change
            float vol = data.MasterVolume;
            bool mute = data.Muted;
            if (!float.IsNaN(_lastVol)
                && Math.Abs(vol - _lastVol) < 0.004f // ignore sub-percent endpoint noise
                && mute == _lastMute)
            {
                return;
            }

            _lastVol = vol;
            _lastMute = mute;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _deviceEvents.Dispose();
            _endpoint.Dispose();
            _enum.Dispose();
        }
    }

    public sealed class WindowsAppAudioService : IAppAudioService
    {
        private readonly MMDeviceEnumerator _enum = new();
        private readonly MMDeviceNotificationClient _deviceEvents;
        private readonly DefaultEndpointBinding<MMDevice> _endpoint;

        public WindowsAppAudioService()
        {
            _endpoint = new DefaultEndpointBinding<MMDevice>(
                () => _enum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia),
                null,
                device => device.Dispose());
            _deviceEvents = _enum.CreateNotificationClient(useSynchronizationContext: false);
            _deviceEvents.DefaultDeviceChanged += (_, _) => _endpoint.MarkStale();
            _deviceEvents.DeviceAdded += (_, _) => _endpoint.MarkStale();
            _deviceEvents.DeviceRemoved += (_, _) => _endpoint.MarkStale();
            _deviceEvents.DeviceStateChanged += (_, _) => _endpoint.MarkStale();
        }

        public event EventHandler? SessionsChanged;

        private SessionCollection? Sessions()
        {
            return _endpoint.Run(d => d.AudioSessionManager.Sessions, null);
        }

        public IReadOnlyList<AppAudioSession> GetSessions()
        {
            List<AppAudioSession> list = [];
            SessionCollection? managers = Sessions();
            if (managers is null)
            {
                return list;
            }

            for (int i = 0; i < managers.Count; i++)
            {
                using AudioSessionControl s = managers[i];
                if (s.State == NAudio.CoreAudioApi.Interfaces.AudioSessionState.AudioSessionStateExpired)
                {
                    continue;
                }

                string name = s.DisplayName;
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = $"Session {i}";
                }

                string id = s.GetSessionIdentifier ?? $"{i}";
                list.Add(new AppAudioSession(id, name, s.SimpleAudioVolume.Volume, s.SimpleAudioVolume.Mute));
            }

            return list;
        }

        public void SetVolume(string sessionId, double volume)
        {
            foreach (AudioSessionControl s in Enumerate())
            {
                if (!string.Equals(s.GetSessionIdentifier, sessionId, StringComparison.Ordinal))
                {
                    s.Dispose();
                    continue;
                }

                s.SimpleAudioVolume.Volume = Math.Clamp((float)volume, 0f, 1f);
                s.Dispose();
                SessionsChanged?.Invoke(this, EventArgs.Empty);
                return;
            }
        }

        public void SetMuted(string sessionId, bool muted)
        {
            foreach (AudioSessionControl s in Enumerate())
            {
                if (!string.Equals(s.GetSessionIdentifier, sessionId, StringComparison.Ordinal))
                {
                    s.Dispose();
                    continue;
                }

                s.SimpleAudioVolume.Mute = muted;
                s.Dispose();
                SessionsChanged?.Invoke(this, EventArgs.Empty);
                return;
            }
        }

        private IEnumerable<AudioSessionControl> Enumerate()
        {
            SessionCollection? managers = Sessions();
            if (managers is null)
            {
                yield break;
            }

            for (int i = 0; i < managers.Count; i++)
            {
                yield return managers[i];
            }
        }

        public void Dispose()
        {
            _deviceEvents.Dispose();
            _endpoint.Dispose();
            _enum.Dispose();
        }
    }
}
