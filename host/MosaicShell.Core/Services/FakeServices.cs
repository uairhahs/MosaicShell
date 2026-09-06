namespace MosaicShell.Core.Services
{
    public sealed class FakeAudioService : IAudioService
    {
        public double MasterVolume
        {
            get;
            set
            {
                field = value;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        } = 0.5;

        public bool IsMuted
        {
            get;
            set
            {
                field = value;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? Changed;
        public void Dispose() { }
    }

    public sealed class FakeAppAudioService : IAppAudioService
    {
        public List<AppAudioSession> Sessions { get; } = [];
        public event EventHandler? SessionsChanged;
        public IReadOnlyList<AppAudioSession> GetSessions()
        {
            return Sessions;
        }

        public void SetVolume(string sessionId, double volume)
        {
            int i = Sessions.FindIndex(s => s.Id == sessionId);
            if (i < 0)
            {
                return;
            }

            Sessions[i] = Sessions[i] with { Volume = volume };
            SessionsChanged?.Invoke(this, EventArgs.Empty);
        }
        public void SetMuted(string sessionId, bool muted)
        {
            int i = Sessions.FindIndex(s => s.Id == sessionId);
            if (i < 0)
            {
                return;
            }

            Sessions[i] = Sessions[i] with { IsMuted = muted };
            SessionsChanged?.Invoke(this, EventArgs.Empty);
        }
        public void Dispose() { }
    }

    public sealed class FakeMediaSessionService : IMediaSessionService
    {
        public MediaSessionInfo? Current
        {
            get;
            set
            {
                field = value;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? Changed;
        public event EventHandler? ProgressChanged;
        public void PumpTimeline()
        {
            if (Current is { IsPlaying: true, DurationSeconds: > 0 })
            {
                Current = Current with
                {
                    PositionSeconds = Math.Min(Current.DurationSeconds, Current.PositionSeconds + 0.2)
                };
                ProgressChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public Task PlayPauseAsync()
        {
            if (Current is not null)
            {
                Current = Current with { IsPlaying = !Current.IsPlaying };
            }

            return Task.CompletedTask;
        }
        public Task NextAsync()
        {
            return Task.CompletedTask;
        }

        public Task PreviousAsync()
        {
            return Task.CompletedTask;
        }

        public Task SeekAsync(double positionSeconds)
        {
            return Task.CompletedTask;
        }

        public Task ToggleShuffleAsync()
        {
            return Task.CompletedTask;
        }

        public Task ToggleRepeatAsync()
        {
            return Task.CompletedTask;
        }

        public Task ToggleLikeAsync(bool wantLiked)
        {
            return Task.CompletedTask;
        }

        public Task ToggleDislikeAsync(bool wantDisliked)
        {
            return Task.CompletedTask;
        }

        public void RaiseProgress()
        {
            ProgressChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose() { }
    }

    public sealed class FakeSystemMetricsService : ISystemMetricsService
    {
        public SystemMetricsSnapshot Sample()
        {
            return new(
            10, 40, 6, 16, [new DiskMetric("C:", 100, 500)], Environment.MachineName);
        }

        public void Dispose() { }
    }

    public sealed class FakeAudioLevelService : IAudioLevelService
    {
        public double Peak => 0.2;
        public IReadOnlyList<double> Bands => [.. Enumerable.Repeat(0.2, 16)];
        public void Start() { }
        public void Stop() { }
        public void Dispose() { }
    }

    public sealed class FakeBrightnessService : IBrightnessService
    {
        public bool IsSupported => true;
        public double Brightness { get; set; } = 0.5;
    }

    public sealed class FakeHotkeyService : IHotkeyService
    {
        private readonly Dictionary<string, Action> _callbacks = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyCollection<string> RegisteredIds => [.. _callbacks.Keys];

        public bool Register(string id, ModifierKeys modifiers, int virtualKey, Action callback)
        {
            _callbacks[id] = callback;
            return true;
        }

        public void Unregister(string id)
        {
            _ = _callbacks.Remove(id);
        }

        public bool TryInvoke(string id)
        {
            if (!_callbacks.TryGetValue(id, out Action? cb))
            {
                return false;
            }

            cb();
            return true;
        }

        public void Dispose()
        {
            _callbacks.Clear();
        }
    }

    public sealed class FakeIdleService : IIdleService
    {
        public TimeSpan IdleTime { get; set; }
        public TimeSpan Threshold { get; set; } = TimeSpan.FromMinutes(5);
        public event EventHandler? IdleThresholdReached;
        public bool IsStarted { get; private set; }

        public void Start()
        {
            IsStarted = true;
        }

        public void Stop()
        {
            IsStarted = false;
        }

        public void RaiseIdle()
        {
            IdleThresholdReached?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            Stop();
        }
    }

    public sealed class FakeFullscreenProbe : IFullscreenProbe
    {
        public bool IsForegroundFullscreen { get; set; }
    }

    public sealed class FakeAutostartService : IAutostartService
    {
        public bool IsEnabled { get; private set; }
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
        }
    }

    public static class HostServicesFakes
    {
        public static HostServices Create()
        {
            return new()
            {
                Audio = new FakeAudioService(),
                AppAudio = new FakeAppAudioService(),
                Brightness = new FakeBrightnessService(),
                Media = new FakeMediaSessionService(),
                Hotkeys = new FakeHotkeyService(),
                Metrics = new FakeSystemMetricsService(),
                AudioLevels = new FakeAudioLevelService(),
                Autostart = new FakeAutostartService(),
                BrightnessChanges = new NullBrightnessChangeSource(),
                OsdSuppressor = new NullNativeOsdSuppressor(),
                LegacyVolumeKeys = new NullLegacyMediaKeyHook(),
                Idle = new FakeIdleService(),
                Fullscreen = new FakeFullscreenProbe(),
                LockKeys = new NullLockKeysService(),
                Airplane = new NullAirplaneModeService(),
                AudioDevices = new NullAudioDeviceService(),
                ShellFlyoutTriggers = new NullShellFlyoutTriggerSource(),
            };
        }
    }
}
