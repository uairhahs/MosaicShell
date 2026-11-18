namespace MosaicShell.Core.Services
{
    // Platform-neutral no-op implementations. They live outside the Windows*.cs files so the
    // portable (non-Windows) build and any platform without a given feature can
    // use them.

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
