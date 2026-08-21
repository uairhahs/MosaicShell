namespace MosaicShell.Core.Services;

public sealed class FakeAudioService : IAudioService
{
    private double _volume = 0.5;
    private bool _muted;

    public double MasterVolume
    {
        get => _volume;
        set
        {
            _volume = value;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsMuted
    {
        get => _muted;
        set
        {
            _muted = value;
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
    public IReadOnlyList<AppAudioSession> GetSessions() => Sessions;
    public void SetVolume(string sessionId, double volume)
    {
        var i = Sessions.FindIndex(s => s.Id == sessionId);
        if (i < 0) return;
        Sessions[i] = Sessions[i] with { Volume = volume };
    }
    public void SetMuted(string sessionId, bool muted)
    {
        var i = Sessions.FindIndex(s => s.Id == sessionId);
        if (i < 0) return;
        Sessions[i] = Sessions[i] with { IsMuted = muted };
    }
    public void Dispose() { }
}

public sealed class FakeMediaSessionService : IMediaSessionService
{
    public MediaSessionInfo? Current { get; set; }
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
        if (Current is not null) Current = Current with { IsPlaying = !Current.IsPlaying };
        return Task.CompletedTask;
    }
    public Task NextAsync() => Task.CompletedTask;
    public Task PreviousAsync() => Task.CompletedTask;
    public Task SeekAsync(double positionSeconds) => Task.CompletedTask;
    public Task ToggleShuffleAsync() => Task.CompletedTask;
    public Task ToggleRepeatAsync() => Task.CompletedTask;
    public Task ToggleLikeAsync() => Task.CompletedTask;
    public void RaiseProgress() => ProgressChanged?.Invoke(this, EventArgs.Empty);
    public void Dispose() { }
}

public sealed class FakeSystemMetricsService : ISystemMetricsService
{
    public SystemMetricsSnapshot Sample() => new(
        10, 40, 6, 16, [new DiskMetric("C:", 100, 500)], Environment.MachineName);
    public void Dispose() { }
}

public sealed class FakeAudioLevelService : IAudioLevelService
{
    public double Peak => 0.2;
    public IReadOnlyList<double> Bands => Enumerable.Repeat(0.2, 16).ToArray();
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
    public bool Register(string id, ModifierKeys modifiers, int virtualKey, Action callback) => true;
    public void Unregister(string id) { }
    public void Dispose() { }
}

public sealed class FakeAutostartService : IAutostartService
{
    public bool IsEnabled { get; private set; }
    public void SetEnabled(bool enabled) => IsEnabled = enabled;
}

public static class HostServicesFakes
{
    public static HostServices Create() => new()
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
        Idle = new NullIdleService(),
        LockKeys = new NullLockKeysService(),
        Airplane = new NullAirplaneModeService(),
        AudioDevices = new NullAudioDeviceService(),
        ShellFlyoutTriggers = new NullShellFlyoutTriggerSource(),
    };
}
