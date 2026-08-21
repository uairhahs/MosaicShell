using Windows.Media.Control;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace MosaicShell.Core.Services;

public sealed class WindowsMediaSessionService : IMediaSessionService
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;
    private byte[]? _lastThumb;
    private bool _disposed;

    public WindowsMediaSessionService()
    {
        _ = InitAsync();
    }

    public MediaSessionInfo? Current { get; private set; }
    public event EventHandler? Changed;

    private async Task InitAsync()
    {
        try
        {
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _manager.CurrentSessionChanged += (_, _) => _ = RefreshAsync();
            _manager.SessionsChanged += (_, _) => _ = RefreshAsync();
            await RefreshAsync();
        }
        catch
        {
            Current = null;
        }
    }

    private async Task RefreshAsync()
    {
        try
        {
            if (_manager is null) return;
            if (_session is not null)
            {
                _session.MediaPropertiesChanged -= OnProps;
                _session.PlaybackInfoChanged -= OnProps;
                _session.TimelinePropertiesChanged -= OnProps;
            }

            _session = _manager.GetCurrentSession();
            if (_session is null)
            {
                Current = null;
                _lastThumb = null;
                Changed?.Invoke(this, EventArgs.Empty);
                return;
            }

            _session.MediaPropertiesChanged += OnProps;
            _session.PlaybackInfoChanged += OnProps;
            _session.TimelinePropertiesChanged += OnProps;
            await UpdateFromSessionAsync(_session);
        }
        catch
        {
            Current = null;
        }
    }

    private void OnProps(GlobalSystemMediaTransportControlsSession sender, object args) =>
        _ = UpdateFromSessionAsync(sender);

    private async Task UpdateFromSessionAsync(GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            var props = await session.TryGetMediaPropertiesAsync();
            var playback = session.GetPlaybackInfo();
            var timeline = session.GetTimelineProperties();
            byte[]? thumb = null;
            try
            {
                if (props?.Thumbnail is not null)
                    thumb = await ReadThumbnailAsync(props.Thumbnail);
            }
            catch { /* optional */ }

            // Keep last good art across timeline-only refreshes
            if (thumb is { Length: > 0 })
                _lastThumb = thumb;
            else
                thumb = _lastThumb;

            var pos = timeline.Position.TotalSeconds;
            var dur = timeline.EndTime.TotalSeconds;
            if (dur <= 0) dur = timeline.MaxSeekTime.TotalSeconds;

            var next = new MediaSessionInfo(
                props?.Title,
                props?.Artist,
                session.SourceAppUserModelId,
                playback.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
                thumb,
                pos,
                dur);

            // TimelinePropertiesChanged fires continuously while a track plays. Raising Changed
            // on every tick makes Tessera (and other consumers) pop the media flyout periodically.
            var prev = Current;
            Current = next;
            if (IsMeaningfulSessionChange(prev, next))
                Changed?.Invoke(this, EventArgs.Empty);
        }
        catch { /* ignore */ }
    }

    private static bool IsMeaningfulSessionChange(MediaSessionInfo? prev, MediaSessionInfo next)
    {
        if (prev is null) return true;
        if (!string.Equals(prev.Title, next.Title, StringComparison.Ordinal)) return true;
        if (!string.Equals(prev.Artist, next.Artist, StringComparison.Ordinal)) return true;
        if (!string.Equals(prev.AppId, next.AppId, StringComparison.Ordinal)) return true;
        if (prev.IsPlaying != next.IsPlaying) return true;
        // Thumb byte identity — only treat as change when presence flips or length changes
        var prevLen = prev.ThumbnailPng?.Length ?? 0;
        var nextLen = next.ThumbnailPng?.Length ?? 0;
        if (prevLen != nextLen) return true;
        return false;
    }

    private static async Task<byte[]?> ReadThumbnailAsync(IRandomAccessStreamReference reference)
    {
        using var ras = await reference.OpenReadAsync();
        if (ras.Size == 0 || ras.Size > 8_000_000) return null;

        // Copy to managed MemoryStream — WinRT streams are often JPEG/PNG; Avalonia/Skia decodes both.
        await using var input = ras.AsStreamForRead();
        await using var ms = new MemoryStream();
        await input.CopyToAsync(ms);
        if (ms.Length < 32) return null;
        return ms.ToArray();
    }

    public async Task PlayPauseAsync()
    {
        if (_session is null) await RefreshAsync();
        if (_session is not null) await _session.TryTogglePlayPauseAsync();
    }

    public async Task NextAsync()
    {
        if (_session is null) await RefreshAsync();
        if (_session is not null) await _session.TrySkipNextAsync();
    }

    public async Task PreviousAsync()
    {
        if (_session is null) await RefreshAsync();
        if (_session is not null) await _session.TrySkipPreviousAsync();
    }

    public async Task SeekAsync(double positionSeconds)
    {
        if (_session is null) await RefreshAsync();
        if (_session is null) return;
        try
        {
            await _session.TryChangePlaybackPositionAsync(TimeSpan.FromSeconds(positionSeconds).Ticks);
        }
        catch { /* seek not supported */ }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_session is not null)
        {
            _session.MediaPropertiesChanged -= OnProps;
            _session.PlaybackInfoChanged -= OnProps;
            _session.TimelinePropertiesChanged -= OnProps;
        }
    }
}
