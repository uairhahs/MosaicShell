using Windows.Media.Control;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace MosaicShell.Core.Services;

public sealed class WindowsMediaSessionService : IMediaSessionService
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;
    private byte[]? _lastThumb;
    private string? _lastTitle;
    private string? _lastAppId;
    private bool _disposed;
    private int _updateGen;

    public WindowsMediaSessionService()
    {
        _ = InitAsync();
    }

    public MediaSessionInfo? Current { get; private set; }
    public event EventHandler? Changed;
    /// <summary>Timeline / position only - does not open flyouts; consumers refresh visible UI.</summary>
    public event EventHandler? ProgressChanged;

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
                _session.TimelinePropertiesChanged -= OnTimeline;
            }

            _session = _manager.GetCurrentSession();
            if (_session is null)
            {
                Current = null;
                _lastThumb = null;
                _lastTitle = null;
                _lastAppId = null;
                Changed?.Invoke(this, EventArgs.Empty);
                return;
            }

            _session.MediaPropertiesChanged += OnProps;
            _session.PlaybackInfoChanged += OnProps;
            _session.TimelinePropertiesChanged += OnTimeline;
            await UpdateFromSessionAsync(_session, raiseProgress: false);
        }
        catch
        {
            Current = null;
        }
    }

    private void OnProps(GlobalSystemMediaTransportControlsSession sender, object args) =>
        _ = UpdateFromSessionAsync(sender, raiseProgress: false);

    private void OnTimeline(GlobalSystemMediaTransportControlsSession sender, object args) =>
        _ = UpdateFromSessionAsync(sender, raiseProgress: true);

    private async Task UpdateFromSessionAsync(
        GlobalSystemMediaTransportControlsSession session, bool raiseProgress)
    {
        var gen = Interlocked.Increment(ref _updateGen);
        try
        {
            var props = await session.TryGetMediaPropertiesAsync();
            if (gen != _updateGen) return; // superseded

            var playback = session.GetPlaybackInfo();
            var timeline = session.GetTimelineProperties();
            byte[]? thumb = null;
            try
            {
                if (props?.Thumbnail is not null)
                    thumb = await ReadThumbnailAsync(props.Thumbnail);
                else
                    System.Diagnostics.Debug.WriteLine(
                        $"[SMTC] Thumbnail is null for {session.SourceAppUserModelId} / '{props?.Title}' " +
                        "(source did not publish artwork - common for YouTube Music PWA)");
            }
            catch { /* optional */ }

            if (gen != _updateGen) return;

            var title = props?.Title;
            var appId = session.SourceAppUserModelId;
            // Drop cached art when track/app changes
            if (!string.Equals(title, _lastTitle, StringComparison.Ordinal)
                || !string.Equals(appId, _lastAppId, StringComparison.Ordinal))
            {
                if (thumb is null || thumb.Length == 0)
                    _lastThumb = null;
                _lastTitle = title;
                _lastAppId = appId;
            }

            if (thumb is { Length: > 0 })
                _lastThumb = thumb;
            else
                thumb = _lastThumb;

            var pos = timeline.Position.TotalSeconds;
            var dur = timeline.EndTime.TotalSeconds;
            if (dur <= 0) dur = timeline.MaxSeekTime.TotalSeconds;

            var next = new MediaSessionInfo(
                title,
                props?.Artist,
                appId,
                playback.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
                thumb,
                pos,
                dur);

            var prev = Current;
            Current = next;

            if (IsMeaningfulSessionChange(prev, next))
            {
                _timelineSamplePos = pos;
                _timelineSampleUtc = DateTimeOffset.UtcNow;
                _timelinePlaying = next.IsPlaying;
                Changed?.Invoke(this, EventArgs.Empty);
            }
            else if (raiseProgress)
                ProgressChanged?.Invoke(this, EventArgs.Empty);
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
        var prevLen = prev.ThumbnailPng?.Length ?? 0;
        var nextLen = next.ThumbnailPng?.Length ?? 0;
        if (prevLen != nextLen) return true;
        // First time art appears with same length is rare; also detect null→bytes
        if (prevLen == 0 && nextLen > 0) return true;
        return false;
    }

    private DateTimeOffset _timelineSampleUtc = DateTimeOffset.MinValue;
    private double _timelineSamplePos;
    private bool _timelinePlaying;

    /// <summary>
    /// Poll timeline + retry thumbnail. YouTube Music / Chrome often never fire TimelinePropertiesChanged
    /// and freeze Position until the next sparse update - we extrapolate while playing.
    /// </summary>
    public void PumpTimeline()
    {
        if (_disposed || _session is null) return;
        try
        {
            var timeline = _session.GetTimelineProperties();
            var playback = _session.GetPlaybackInfo();
            var apiPos = timeline.Position.TotalSeconds;
            var dur = timeline.EndTime.TotalSeconds;
            if (dur <= 0) dur = timeline.MaxSeekTime.TotalSeconds;
            var playing = playback.PlaybackStatus ==
                          GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

            var now = DateTimeOffset.UtcNow;
            // Detect API movement (seek / sparse SMTC tick)
            if (Math.Abs(apiPos - _timelineSamplePos) >= 0.4
                || playing != _timelinePlaying
                || _timelineSampleUtc == DateTimeOffset.MinValue)
            {
                _timelineSamplePos = apiPos;
                _timelineSampleUtc = now;
                _timelinePlaying = playing;
            }

            var pos = apiPos;
            if (playing && _timelineSampleUtc != DateTimeOffset.MinValue)
            {
                var extrapolated = _timelineSamplePos + (now - _timelineSampleUtc).TotalSeconds;
                if (dur > 0.5) extrapolated = Math.Clamp(extrapolated, 0, dur);
                // Prefer extrapolation when API is sticky (common for YT Music)
                if (Math.Abs(extrapolated - apiPos) >= 0.15)
                    pos = extrapolated;
            }

            var prev = Current;
            if (prev is null) return;

            var moved = Math.Abs(prev.PositionSeconds - pos) >= 0.05
                        || Math.Abs(prev.DurationSeconds - dur) >= 0.5
                        || prev.IsPlaying != playing;

            if (moved)
            {
                Current = prev with
                {
                    PositionSeconds = pos,
                    DurationSeconds = dur,
                    IsPlaying = playing
                };
                ProgressChanged?.Invoke(this, EventArgs.Empty);
            }

            var cur = Current;
            if (cur is not null
                && (cur.ThumbnailPng is null || cur.ThumbnailPng.Length < 32)
                && !_thumbRetryBusy)
                _ = RetryThumbnailAsync(_session);
        }
        catch { /* ignore */ }
    }

    private bool _thumbRetryBusy;

    private async Task RetryThumbnailAsync(GlobalSystemMediaTransportControlsSession session)
    {
        if (_thumbRetryBusy) return;
        _thumbRetryBusy = true;
        try
        {
            // Prefer current session, then any SMTC session that has a thumbnail (YT Music quirks)
            byte[]? thumb = null;
            var props = await session.TryGetMediaPropertiesAsync();
            if (props?.Thumbnail is not null)
                thumb = await ReadThumbnailAsync(props.Thumbnail);

            if ((thumb is null || thumb.Length < 32) && _manager is not null)
            {
                foreach (var s in _manager.GetSessions())
                {
                    try
                    {
                        var p = await s.TryGetMediaPropertiesAsync();
                        if (p?.Thumbnail is null) continue;
                        thumb = await ReadThumbnailAsync(p.Thumbnail);
                        if (thumb is { Length: > 32 }) break;
                    }
                    catch { /* next */ }
                }
            }

            if (thumb is not { Length: > 32 } || Current is null) return;
            _lastThumb = thumb;
            Current = Current with { ThumbnailPng = thumb };
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch { /* ignore */ }
        finally
        {
            _thumbRetryBusy = false;
        }
    }

    /// <summary>
    /// WinRT SMTC thumbs: decode via BitmapDecoder → PNG so Avalonia/Skia always accepts the bytes
    /// (browser/YouTube Music streams are often odd JPEG variants).
    /// </summary>
    private static async Task<byte[]?> ReadThumbnailAsync(IRandomAccessStreamReference reference)
    {
        try
        {
            using var ras = await reference.OpenReadAsync();
            if (ras is null) return null;

            // Preferred: re-encode through WinRT so Skia gets clean PNG
            try
            {
                ras.Seek(0);
                var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(ras);
                var soft = await decoder.GetSoftwareBitmapAsync();
                // Encoder requires Bgra8 / compatible alpha
                if (soft.BitmapPixelFormat != Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8
                    || soft.BitmapAlphaMode == Windows.Graphics.Imaging.BitmapAlphaMode.Straight)
                {
                    soft = Windows.Graphics.Imaging.SoftwareBitmap.Convert(
                        soft,
                        Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
                        Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);
                }
                using var outStream = new InMemoryRandomAccessStream();
                var encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(
                    Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, outStream);
                encoder.SetSoftwareBitmap(soft);
                await encoder.FlushAsync();
                outStream.Seek(0);
                var png = new byte[outStream.Size];
                using (var reader = new DataReader(outStream))
                {
                    await reader.LoadAsync((uint)outStream.Size);
                    reader.ReadBytes(png);
                }
                if (png.Length >= 32) return png;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SMTC thumb] BitmapDecoder: {ex.Message}");
            }

            // Raw bytes fallback
            ras.Seek(0);
            var size = ras.Size;
            if (size is > 0 and <= 8_000_000)
            {
                var reader = new DataReader(ras);
                try
                {
                    await reader.LoadAsync((uint)size);
                    var buf = new byte[size];
                    reader.ReadBytes(buf);
                    if (buf.Length >= 32) return buf; // don't reject on magic - Skia may still decode
                }
                finally
                {
                    reader.Dispose();
                }
            }

            ras.Seek(0);
            await using var input = ras.AsStreamForRead();
            await using var ms = new MemoryStream();
            await input.CopyToAsync(ms);
            return ms.Length >= 32 ? ms.ToArray() : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SMTC thumb] {ex.Message}");
            return null;
        }
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

    public async Task ToggleShuffleAsync()
    {
        if (_session is null) await RefreshAsync();
        if (_session is null) return;
        try
        {
            var info = _session.GetPlaybackInfo();
            var next = !(info?.IsShuffleActive ?? false);
            await _session.TryChangeShuffleActiveAsync(next);
        }
        catch { /* shuffle not supported */ }
    }

    public async Task ToggleRepeatAsync()
    {
        if (_session is null) await RefreshAsync();
        if (_session is null) return;
        try
        {
            var mode = _session.GetPlaybackInfo()?.AutoRepeatMode
                       ?? global::Windows.Media.MediaPlaybackAutoRepeatMode.None;
            var next = mode switch
            {
                global::Windows.Media.MediaPlaybackAutoRepeatMode.None =>
                    global::Windows.Media.MediaPlaybackAutoRepeatMode.List,
                global::Windows.Media.MediaPlaybackAutoRepeatMode.List =>
                    global::Windows.Media.MediaPlaybackAutoRepeatMode.Track,
                _ => global::Windows.Media.MediaPlaybackAutoRepeatMode.None
            };
            await _session.TryChangeAutoRepeatModeAsync(next);
        }
        catch { /* repeat not supported */ }
    }

    public Task ToggleLikeAsync() => Task.CompletedTask; // SMTC has no standard like API

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_session is not null)
        {
            _session.MediaPropertiesChanged -= OnProps;
            _session.PlaybackInfoChanged -= OnProps;
            _session.TimelinePropertiesChanged -= OnTimeline;
        }
    }
}
