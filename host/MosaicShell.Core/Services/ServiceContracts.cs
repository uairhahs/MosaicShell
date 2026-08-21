namespace MosaicShell.Core.Services;

public interface IAudioService : IDisposable
{
    double MasterVolume { get; set; }
    bool IsMuted { get; set; }
    event EventHandler? Changed;
}

public sealed record AppAudioSession(string Id, string DisplayName, double Volume, bool IsMuted);

public interface IAppAudioService : IDisposable
{
    IReadOnlyList<AppAudioSession> GetSessions();
    void SetVolume(string sessionId, double volume);
    void SetMuted(string sessionId, bool muted);
    event EventHandler? SessionsChanged;
}

public interface IBrightnessService
{
    bool IsSupported { get; }
    double Brightness { get; set; }
}

public sealed record MediaSessionInfo(
    string? Title,
    string? Artist,
    string? AppId,
    bool IsPlaying);

public interface IMediaSessionService : IDisposable
{
    MediaSessionInfo? Current { get; }
    event EventHandler? Changed;
    Task PlayPauseAsync();
    Task NextAsync();
    Task PreviousAsync();
}

public sealed record HotkeyBinding(string Id, string Gesture);

public interface IHotkeyService : IDisposable
{
    bool Register(string id, ModifierKeys modifiers, int virtualKey, Action callback);
    void Unregister(string id);
}

[Flags]
public enum ModifierKeys
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8,
}

public sealed record DiskMetric(string Name, double FreeGb, double TotalGb);
public sealed record SystemMetricsSnapshot(
    double CpuPercent,
    double RamUsedPercent,
    double RamUsedGb,
    double RamTotalGb,
    IReadOnlyList<DiskMetric> Disks,
    string MachineName);

public interface ISystemMetricsService : IDisposable
{
    SystemMetricsSnapshot Sample();
}

public interface IAudioLevelService : IDisposable
{
    /// <summary>0..1 peak level.</summary>
    double Peak { get; }
    /// <summary>Optional FFT/band levels 0..1.</summary>
    IReadOnlyList<double> Bands { get; }
    void Start();
    void Stop();
}

public interface IAutostartService
{
    bool IsEnabled { get; }
    void SetEnabled(bool enabled);
}

/// <summary>Composition root for OS services used by tiles and hub.</summary>
public sealed class HostServices : IDisposable
{
    public required IAudioService Audio { get; init; }
    public required IAppAudioService AppAudio { get; init; }
    public required IBrightnessService Brightness { get; init; }
    public required IMediaSessionService Media { get; init; }
    public required IHotkeyService Hotkeys { get; init; }
    public required ISystemMetricsService Metrics { get; init; }
    public required IAudioLevelService AudioLevels { get; init; }
    public required IAutostartService Autostart { get; init; }
    public required IBrightnessChangeSource BrightnessChanges { get; init; }
    public required INativeOsdSuppressor OsdSuppressor { get; init; }
    public required ILegacyMediaKeyHook LegacyVolumeKeys { get; init; }
    public required IIdleService Idle { get; init; }

    public void Dispose()
    {
        Audio.Dispose();
        AppAudio.Dispose();
        Media.Dispose();
        Hotkeys.Dispose();
        Metrics.Dispose();
        AudioLevels.Dispose();
        BrightnessChanges.Dispose();
        OsdSuppressor.Dispose();
        LegacyVolumeKeys.Dispose();
        Idle.Dispose();
    }

    public static HostServices CreateWindowsDefaults()
    {
        var brightness = new WindowsBrightnessService();
        return new HostServices
        {
            Audio = new WindowsAudioService(),
            AppAudio = new WindowsAppAudioService(),
            Brightness = brightness,
            Media = new WindowsMediaSessionService(),
            Hotkeys = new WindowsHotkeyService(),
            Metrics = new WindowsSystemMetricsService(),
            AudioLevels = new WindowsAudioLevelService(),
            Autostart = new WindowsAutostartService(),
            BrightnessChanges = new WindowsBrightnessChangeSource(brightness),
            OsdSuppressor = new WindowsNativeOsdSuppressor(),
            LegacyVolumeKeys = new WindowsLegacyMediaKeyHook(),
            Idle = new WindowsIdleService(),
        };
    }
}
