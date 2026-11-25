namespace MosaicShell.Core.Services
{
    // Platform-neutral no-op implementations. They live outside the Windows*.cs files so the
    // portable (non-Windows) build and any platform without a given feature can
    // use them.

    /// <summary>
    /// No mixer to talk to: keeps the last value it was given, so a slider the user moves does not snap back,
    /// and never raises Changed on its own.
    /// </summary>
    public sealed class NullAudioService : IAudioService
    {
        public double MasterVolume { get; set; }
        public bool IsMuted { get; set; }
        public event EventHandler? Changed { add { } remove { } }
        public void Dispose() { }
    }

    public sealed class NullAppAudioService : IAppAudioService
    {
        public IReadOnlyList<AppAudioSession> GetSessions()
        {
            return [];
        }

        public void SetVolume(string sessionId, double volume) { }
        public void SetMuted(string sessionId, bool muted) { }
        public event EventHandler? SessionsChanged { add { } remove { } }
        public void Dispose() { }
    }

    public sealed class NullBrightnessService : IBrightnessService
    {
        public bool IsSupported => false;
        public double Brightness { get => 0; set { } }
    }

    /// <summary>No media session source: nothing is playing and every command completes at once.</summary>
    public sealed class NullMediaSessionService : IMediaSessionService
    {
        public MediaSessionInfo? Current => null;
        public event EventHandler? Changed { add { } remove { } }
        public event EventHandler? ProgressChanged { add { } remove { } }
        public void PumpTimeline() { }
        public Task PlayPauseAsync() { return Task.CompletedTask; }
        public Task NextAsync() { return Task.CompletedTask; }
        public Task PreviousAsync() { return Task.CompletedTask; }
        public Task SeekAsync(double positionSeconds) { return Task.CompletedTask; }
        public Task ToggleShuffleAsync() { return Task.CompletedTask; }
        public Task ToggleRepeatAsync() { return Task.CompletedTask; }
        public Task ToggleLikeAsync(bool wantLiked) { return Task.CompletedTask; }
        public Task ToggleDislikeAsync(bool wantDisliked) { return Task.CompletedTask; }
        public void Dispose() { }
    }

    /// <summary>No global hotkeys: every registration is refused, so callers can tell the user it did not bind.</summary>
    public sealed class NullHotkeyService : IHotkeyService
    {
        public bool Register(string id, ModifierKeys modifiers, int virtualKey, Action callback)
        {
            return false;
        }

        public void Unregister(string id) { }
        public void Dispose() { }
    }

    /// <summary>No metrics source: zero load and no disks, but the real machine name.</summary>
    public sealed class NullSystemMetricsService : ISystemMetricsService
    {
        public SystemMetricsSnapshot Sample()
        {
            return new SystemMetricsSnapshot(0, 0, 0, 0, [], Environment.MachineName);
        }

        public void Dispose() { }
    }

    /// <summary>No capture: silence, with the meter's band count so band-drawing code needs no special case.</summary>
    public sealed class NullAudioLevelService : IAudioLevelService
    {
        private static readonly double[] Silent = new double[AudioLevelMeter.BandCount];

        public double Peak => 0;
        public IReadOnlyList<double> Bands => Silent;
        public void Start() { }
        public void Stop() { }
        public void Dispose() { }
    }

    public sealed class NullAutostartService : IAutostartService
    {
        public bool IsEnabled => false;
        public void SetEnabled(bool enabled) { }
    }

    public sealed class NullNativeOsdSuppressor : INativeOsdSuppressor
    {
        public bool IsActive { get; private set; }
        public void Start()
        {
            IsActive = true;
        }

        public void Stop()
        {
            IsActive = false;
        }

        public void SuppressOnce() { }
        public void SuppressBurst(int durationMs = 1500) { }
        public void Dispose()
        {
            Stop();
        }
    }

    public sealed class NullLegacyMediaKeyHook : ILegacyMediaKeyHook
    {
        public bool IsActive => false;
        public event EventHandler<LegacyVolumeKey>? Pressed { add { } remove { } }
        public void Start() { }
        public void Stop() { }
        public void Dispose() { }
    }

    public sealed class NullBrightnessChangeSource : IBrightnessChangeSource
    {
        public event EventHandler? Changed { add { } remove { } }
        public void Start() { }
        public void Stop() { }
        public void Dispose() { }
    }

    public sealed class NullIdleService : IIdleService
    {
        public TimeSpan IdleTime => TimeSpan.Zero;
        public TimeSpan Threshold { get; set; } = TimeSpan.FromMinutes(5);
        public event EventHandler? IdleThresholdReached { add { } remove { } }
        public void Start() { }
        public void Stop() { }
        public void Dispose() { }
    }

    public sealed class NullFullscreenProbe : IFullscreenProbe
    {
        public bool IsForegroundFullscreen => false;
    }

    public sealed class NullLockKeysService : ILockKeysService
    {
        public bool IsActive { get; private set; }
        public LockKeyState Caps => new(LockKeyKind.CapsLock, false);
        public LockKeyState Num => new(LockKeyKind.NumLock, false);
        public LockKeyState Scroll => new(LockKeyKind.ScrollLock, false);
        public event EventHandler<LockKeyState>? Changed { add { } remove { } }
        public void Start()
        {
            IsActive = true;
        }

        public void Stop()
        {
            IsActive = false;
        }

        public void Dispose() { }
    }

    public sealed class NullAirplaneModeService : IAirplaneModeService
    {
        public bool IsSupported => false;
        public bool IsEnabled => false;
        public event EventHandler? Changed { add { } remove { } }
        public void Start() { }
        public void Stop() { }
        public void Dispose() { }
    }

    public sealed class NullAudioDeviceService : IAudioDeviceService
    {
        public IReadOnlyList<AudioOutputDevice> GetOutputDevices()
        {
            return [];
        }

        public void SetDefaultOutput(string deviceId) { }
        public void Dispose() { }
    }

    public sealed class NullShellFlyoutTriggerSource : IShellFlyoutTriggerSource
    {
        public bool IsActive => false;
        public event EventHandler<ShellFlyoutKind>? Triggered { add { } remove { } }
        public void Start() { }
        public void Stop() { }
        public void Dispose() { }
    }
}
