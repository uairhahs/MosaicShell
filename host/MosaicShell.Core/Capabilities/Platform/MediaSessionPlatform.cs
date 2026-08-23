namespace MosaicShell.Core.Capabilities.Platform;

using MosaicShell.Core.Services;

/// <summary>
/// Shared media signal normalization. SMTC/WNP polling lives in <see cref="IMediaSessionService"/>;
/// this layer classifies Changed vs Progress and track boundaries for capabilities.
/// </summary>
public sealed class MediaSessionPlatform : IDisposable
{
    private readonly IMediaSessionService _media;
    private int _consumers;
    private MediaSessionInfo? _lastIdentity;
    private bool _disposed;

    public MediaSessionPlatform(IMediaSessionService media) => _media = media;

    public event Action<MediaSessionSignal>? Signal;

    public MediaSessionInfo? Current => _media.Current;

    /// <summary>Call from capability Arm when module cares about media flyouts or strip refresh.</summary>
    public void Acquire()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_consumers++ != 0) return;
        _media.Changed += OnChanged;
        _media.ProgressChanged += OnProgress;
        _lastIdentity = _media.Current;
    }

    /// <summary>Call from capability Disarm.</summary>
    public void Release()
    {
        if (_disposed) return;
        if (--_consumers > 0) return;
        _consumers = 0;
        _media.Changed -= OnChanged;
        _media.ProgressChanged -= OnProgress;
        _lastIdentity = null;
    }

    public void PumpTimeline()
    {
        try { _media.PumpTimeline(); }
        catch { /* soft-fail */ }
    }

    private void OnChanged(object? sender, EventArgs e)
    {
        var current = _media.Current;
        var trackBoundary = MediaFlyoutRouter.IsTrackBoundary(_lastIdentity, current);
        if (trackBoundary)
            _lastIdentity = current;
        Raise(new MediaSessionSignal(current, MediaSessionSignalKind.SessionChanged, trackBoundary));
    }

    private void OnProgress(object? sender, EventArgs e) =>
        Raise(new MediaSessionSignal(_media.Current, MediaSessionSignalKind.Progress, false));

    private void Raise(MediaSessionSignal signal)
    {
        try { Signal?.Invoke(signal); }
        catch { /* soft-fail */ }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        while (_consumers > 0)
            Release();
    }
}

public enum MediaSessionSignalKind
{
    SessionChanged,
    Progress,
}

public sealed record MediaSessionSignal(
    MediaSessionInfo? Current,
    MediaSessionSignalKind Kind,
    bool IsTrackBoundary);
