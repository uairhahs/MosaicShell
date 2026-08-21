namespace MosaicShell.Core.Services;

/// <summary>Notifies when display brightness changes (including OS/hardware changes).</summary>
public interface IBrightnessChangeSource : IDisposable
{
    event EventHandler? Changed;
    void Start();
    void Stop();
}

/// <summary>Best-effort hide of the Windows volume/brightness OSD while armed.</summary>
public interface INativeOsdSuppressor : IDisposable
{
    bool IsActive { get; }
    void Start();
    void Stop();
    /// <summary>Attempt a one-shot hide of currently visible OS flyouts.</summary>
    void SuppressOnce();
    /// <summary>High-rate hide burst for ~durationMs after a volume/brightness event.</summary>
    void SuppressBurst(int durationMs = 1500);
}

/// <summary>Optional steal of Volume_Up/Down/Mute (LegacyVol path).</summary>
public interface ILegacyMediaKeyHook : IDisposable
{
    bool IsActive { get; }
    event EventHandler<LegacyVolumeKey>? Pressed;
    void Start();
    void Stop();
}

public enum LegacyVolumeKey
{
    Up,
    Down,
    Mute
}

public interface IIdleService : IDisposable
{
    TimeSpan IdleTime { get; }
    event EventHandler? IdleThresholdReached;
    TimeSpan Threshold { get; set; }
    void Start();
    void Stop();
}

/// <summary>Best-effort: true when the foreground window looks fullscreen (Slate HideOnFullscreen).</summary>
public interface IFullscreenProbe
{
    bool IsForegroundFullscreen { get; }
}
