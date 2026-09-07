using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace MosaicShell.Core.Services
{
    public sealed class WindowsMediaSessionService : IMediaSessionService
    {
        private GlobalSystemMediaTransportControlsSessionManager? _manager;
        private GlobalSystemMediaTransportControlsSession? _session;
        private byte[]? _lastThumb;
        private string? _lastTitle;
        private string? _lastAppId;
        private bool _disposed;
        private int _updateGen;
        private Timer? _timelinePoll;
        private Timer? _nullSessionGrace;

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
                if (MediaSessionChangePolicy.MustPollTimelineIndependentlyOfFlyout)
                {
                    int ms = MediaSessionChangePolicy.TimelinePollMs;
                    _timelinePoll = new Timer(
                        _ => { try { PumpTimeline(); } catch { /* soft-fail */ } },
                        null, ms, ms);
                }
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
                if (_manager is null)
                {
                    return;
                }

                if (_session is not null)
                {
                    _session.MediaPropertiesChanged -= OnProps;
                    _session.PlaybackInfoChanged -= OnProps;
                    _session.TimelinePropertiesChanged -= OnTimeline;
                }

                _session = _manager.GetCurrentSession();
                if (_session is null)
                {
                    _lastThumb = null;
                    _lastTitle = null;
                    _lastAppId = null;
                    ScheduleNullSessionChanged();
                    return;
                }

                CancelPendingNullSessionChanged();
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

        /// <summary>
        /// YTM's browser session closes and reopens on every track change (see
        /// <see cref="MediaSessionChangePolicy.NullSessionGraceMs"/>). Defer the null state
        /// so a session reattaching within the grace window never surfaces as a stop-then-start
        /// blip; a real stop still lands once the window elapses with nothing reattached.
        /// </summary>
        private void ScheduleNullSessionChanged()
        {
            _nullSessionGrace?.Dispose();
            _nullSessionGrace = new Timer(
                _ =>
                {
                    try
                    {
                        Current = null;
                        Changed?.Invoke(this, EventArgs.Empty);
                    }
                    catch (Exception ex)
                    {
                        // An unhandled exception on this ThreadPool timer thread would
                        // otherwise crash the whole process, same as _timelinePoll.
                        System.Diagnostics.Debug.WriteLine($"[WindowsMediaSessionService null-session] {ex}");
                    }
                },
                null,
                MediaSessionChangePolicy.NullSessionGraceMs,
                Timeout.Infinite);
        }

        private void CancelPendingNullSessionChanged()
        {
            _nullSessionGrace?.Dispose();
            _nullSessionGrace = null;
        }

        private void OnProps(GlobalSystemMediaTransportControlsSession sender, object args)
        {
            _ = UpdateFromSessionAsync(sender, raiseProgress: false);
        }

        private void OnTimeline(GlobalSystemMediaTransportControlsSession sender, object args)
        {
            _ = UpdateFromSessionAsync(sender, raiseProgress: true);
        }

        private async Task UpdateFromSessionAsync(
            GlobalSystemMediaTransportControlsSession session, bool raiseProgress)
        {
            int gen = Interlocked.Increment(ref _updateGen);
            try
            {
                GlobalSystemMediaTransportControlsSessionMediaProperties? props = await session.TryGetMediaPropertiesAsync();
                if (gen != _updateGen)
                {
                    return; // superseded
                }

                GlobalSystemMediaTransportControlsSessionPlaybackInfo playback = session.GetPlaybackInfo();
                GlobalSystemMediaTransportControlsSessionTimelineProperties timeline = session.GetTimelineProperties();
                byte[]? thumb = null;
                try
                {
                    if (props?.Thumbnail is not null)
                    {
                        thumb = await ReadThumbnailAsync(props.Thumbnail);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[SMTC] Thumbnail is null for {session.SourceAppUserModelId} / '{props?.Title}' " +
                            "(source did not publish artwork - common for YouTube Music PWA)");
                    }
                }
                catch { /* optional */ }

                if (gen != _updateGen)
                {
                    return;
                }

                string? title = props?.Title;
                string appId = session.SourceAppUserModelId;
                // Drop cached art when track/app changes
                if (!string.Equals(title, _lastTitle, StringComparison.Ordinal)
                    || !string.Equals(appId, _lastAppId, StringComparison.Ordinal))
                {
                    if (thumb is null || thumb.Length == 0)
                    {
                        _lastThumb = null;
                    }

                    _lastTitle = title;
                    _lastAppId = appId;
                }

                if (thumb is { Length: > 0 })
                {
                    _lastThumb = thumb;
                }
                else
                {
                    thumb = _lastThumb;
                }

                double apiPos = timeline.Position.TotalSeconds;
                double dur = timeline.EndTime.TotalSeconds;
                if (dur <= 0)
                {
                    dur = timeline.MaxSeekTime.TotalSeconds;
                }

                bool playing = playback.PlaybackStatus ==
                              GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                MediaSessionInfo? prev = Current;
                double pos = apiPos;
                if (playing && prev is not null && MediaSessionChangePolicy.MustNotRewindPlayingScrubber)
                {
                    bool apiMoved = !_hasLastApi
                        || Math.Abs(apiPos - _lastApiPos) >= MediaSessionChangePolicy.PlayingPositionJitterSeconds;
                    pos = MediaSessionChangePolicy.ResolvePlayingPosition(
                        prev.PositionSeconds, apiPos, playing: true, incomingReportedChange: apiMoved);
                }

                _lastApiPos = apiPos;
                _hasLastApi = true;

                MediaSessionInfo next = new(
                    title,
                    props?.Artist,
                    appId,
                    playing,
                    thumb,
                    pos,
                    dur);

                Current = next;

                if (IsMeaningfulSessionChange(prev, next))
                {
                    _timelineSamplePos = pos;
                    _timelineSampleUtc = DateTimeOffset.UtcNow;
                    _timelinePlaying = next.IsPlaying;
                    Changed?.Invoke(this, EventArgs.Empty);
                }
                else if (raiseProgress
                    || (prev is not null && MediaSessionChangePolicy.IsArtOnlyRefresh(
                        prev.ThumbnailPng?.Length ?? 0, next.ThumbnailPng?.Length ?? 0)))
                {
                    ProgressChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            catch { /* ignore */ }
        }

        private static bool IsMeaningfulSessionChange(MediaSessionInfo? prev, MediaSessionInfo next)
        {
            if (prev is null)
            {
                return true;
            }

            if (!string.Equals(prev.Title, next.Title, StringComparison.Ordinal))
            {
                return true;
            }

            if (!string.Equals(prev.Artist, next.Artist, StringComparison.Ordinal))
            {
                return true;
            }

            if (!string.Equals(prev.AppId, next.AppId, StringComparison.Ordinal))
            {
                return true;
            }

            if (prev.IsPlaying != next.IsPlaying)
            {
                return true;
            }

            // Thumbnail-only differences are deliberately excluded: late-arriving art for a
            // track whose identity already settled must patch in via ProgressChanged, not
            // restart Tessera's entrance animation. See the caller's IsArtOnlyRefresh check.
            return MediaSessionChangePolicy.LooksLikeNewTrackPosition(
                prev.PositionSeconds, next.PositionSeconds);
        }

        private DateTimeOffset _timelineSampleUtc = DateTimeOffset.MinValue;
        private double _timelineSamplePos;
        private bool _timelinePlaying;
        private double _lastApiPos;
        private bool _hasLastApi;

        /// <summary>
        /// Poll timeline + retry thumbnail. YouTube Music / Chrome often never fire TimelinePropertiesChanged
        /// and freeze Position until the next sparse update - we extrapolate while playing.
        /// </summary>
        public void PumpTimeline()
        {
            if (_disposed || _session is null)
            {
                return;
            }

            try
            {
                GlobalSystemMediaTransportControlsSessionTimelineProperties timeline = _session.GetTimelineProperties();
                GlobalSystemMediaTransportControlsSessionPlaybackInfo playback = _session.GetPlaybackInfo();
                double apiPos = timeline.Position.TotalSeconds;
                double dur = timeline.EndTime.TotalSeconds;
                if (dur <= 0)
                {
                    dur = timeline.MaxSeekTime.TotalSeconds;
                }

                bool playing = playback.PlaybackStatus ==
                              GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

                DateTimeOffset now = DateTimeOffset.UtcNow;
                bool apiMoved = !_hasLastApi
                    || Math.Abs(apiPos - _lastApiPos) >= MediaSessionChangePolicy.PlayingPositionJitterSeconds
                    || playing != _timelinePlaying
                    || _timelineSampleUtc == DateTimeOffset.MinValue;
                _lastApiPos = apiPos;
                _hasLastApi = true;

                double pos = apiPos;
                if (playing && _timelineSampleUtc != DateTimeOffset.MinValue)
                {
                    double extrapolated = _timelineSamplePos + (now - _timelineSampleUtc).TotalSeconds;
                    if (dur > 0.5)
                    {
                        extrapolated = Math.Clamp(extrapolated, 0, dur);
                    }

                    pos = MediaSessionChangePolicy.ResolvePlayingPosition(
                        extrapolated, apiPos, playing: true, incomingReportedChange: apiMoved);
                }

                if (apiMoved && Math.Abs(pos - apiPos) < MediaSessionChangePolicy.PlayingPositionJitterSeconds)
                {
                    _timelineSamplePos = apiPos;
                    _timelineSampleUtc = now;
                    _timelinePlaying = playing;
                }
                else if (_timelineSampleUtc == DateTimeOffset.MinValue)
                {
                    _timelineSamplePos = apiPos;
                    _timelineSampleUtc = now;
                    _timelinePlaying = playing;
                }
                else
                {
                    _timelinePlaying = playing;
                }

                MediaSessionInfo? prev = Current;
                if (prev is null)
                {
                    return;
                }

                bool moved = Math.Abs(prev.PositionSeconds - pos) >= 0.05
                            || Math.Abs(prev.DurationSeconds - dur) >= 0.5
                            || prev.IsPlaying != playing;

                if (moved)
                {
                    double priorPos = prev.PositionSeconds;
                    Current = prev with
                    {
                        PositionSeconds = pos,
                        DurationSeconds = dur,
                        IsPlaying = playing
                    };
                    if (MediaSessionChangePolicy.LooksLikeNewTrackPosition(priorPos, pos))
                    {
                        Changed?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        ProgressChanged?.Invoke(this, EventArgs.Empty);
                    }
                }

                MediaSessionInfo? cur = Current;
                if (cur is not null
                    && (cur.ThumbnailPng is null || cur.ThumbnailPng.Length < 32)
                    && !_thumbRetryBusy)
                {
                    _ = RetryThumbnailAsync(_session);
                }
            }
            catch { /* ignore */ }
        }

        private bool _thumbRetryBusy;

        private async Task RetryThumbnailAsync(GlobalSystemMediaTransportControlsSession session)
        {
            if (_thumbRetryBusy)
            {
                return;
            }

            _thumbRetryBusy = true;
            try
            {
                // Prefer current session, then any SMTC session that has a thumbnail (YT Music quirks)
                byte[]? thumb = null;
                GlobalSystemMediaTransportControlsSessionMediaProperties props = await session.TryGetMediaPropertiesAsync();
                if (props?.Thumbnail is not null)
                {
                    thumb = await ReadThumbnailAsync(props.Thumbnail);
                }

                if ((thumb is null || thumb.Length < 32) && _manager is not null)
                {
                    foreach (GlobalSystemMediaTransportControlsSession? s in _manager.GetSessions())
                    {
                        try
                        {
                            GlobalSystemMediaTransportControlsSessionMediaProperties p = await s.TryGetMediaPropertiesAsync();
                            if (p?.Thumbnail is null)
                            {
                                continue;
                            }

                            thumb = await ReadThumbnailAsync(p.Thumbnail);
                            if (thumb is { Length: > 32 })
                            {
                                break;
                            }
                        }
                        catch { /* next */ }
                    }
                }

                if (thumb is not { Length: > 32 } || Current is null)
                {
                    return;
                }

                _lastThumb = thumb;
                Current = Current with { ThumbnailPng = thumb };
                // Only the thumbnail changed here - patch in place, do not restart the
                // entrance animation for a track already shown (see IsArtOnlyRefresh).
                ProgressChanged?.Invoke(this, EventArgs.Empty);
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
                using IRandomAccessStreamWithContentType? ras = await reference.OpenReadAsync();
                if (ras is null)
                {
                    return null;
                }

                // Preferred: re-encode through WinRT so Skia gets clean PNG
                try
                {
                    ras.Seek(0);
                    BitmapDecoder decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(ras);
                    SoftwareBitmap soft = await decoder.GetSoftwareBitmapAsync();
                    // Encoder requires Bgra8 / compatible alpha
                    if (soft.BitmapPixelFormat != Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8
                        || soft.BitmapAlphaMode == Windows.Graphics.Imaging.BitmapAlphaMode.Straight)
                    {
                        soft = Windows.Graphics.Imaging.SoftwareBitmap.Convert(
                            soft,
                            Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
                            Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);
                    }
                    using InMemoryRandomAccessStream outStream = new();
                    BitmapEncoder encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(
                        Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, outStream);
                    encoder.SetSoftwareBitmap(soft);
                    await encoder.FlushAsync();
                    outStream.Seek(0);
                    byte[] png = new byte[outStream.Size];
                    using (DataReader reader = new(outStream))
                    {
                        _ = await reader.LoadAsync((uint)outStream.Size);
                        reader.ReadBytes(png);
                    }
                    if (png.Length >= 32)
                    {
                        return png;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SMTC thumb] BitmapDecoder: {ex.Message}");
                }

                // Raw bytes fallback
                ras.Seek(0);
                ulong size = ras.Size;
                if (size is > 0 and <= 8_000_000)
                {
                    DataReader reader = new(ras);
                    try
                    {
                        _ = await reader.LoadAsync((uint)size);
                        byte[] buf = new byte[size];
                        reader.ReadBytes(buf);
                        if (buf.Length >= 32)
                        {
                            return buf; // don't reject on magic - Skia may still decode
                        }
                    }
                    finally
                    {
                        reader.Dispose();
                    }
                }

                ras.Seek(0);
                await using Stream input = ras.AsStreamForRead();
                await using MemoryStream ms = new();
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
            if (_session is null)
            {
                await RefreshAsync();
            }

            if (_session is not null)
            {
                _ = await _session.TryTogglePlayPauseAsync();
            }
        }

        public async Task NextAsync()
        {
            if (_session is null)
            {
                await RefreshAsync();
            }

            if (_session is not null)
            {
                _ = await _session.TrySkipNextAsync();
            }
        }

        public async Task PreviousAsync()
        {
            if (_session is null)
            {
                await RefreshAsync();
            }

            if (_session is not null)
            {
                _ = await _session.TrySkipPreviousAsync();
            }
        }

        public async Task SeekAsync(double positionSeconds)
        {
            if (_session is null)
            {
                await RefreshAsync();
            }

            if (_session is null)
            {
                return;
            }

            try
            {
                _ = await _session.TryChangePlaybackPositionAsync(TimeSpan.FromSeconds(positionSeconds).Ticks);
            }
            catch { /* seek not supported */ }
        }

        public async Task ToggleShuffleAsync()
        {
            if (_session is null)
            {
                await RefreshAsync();
            }

            if (_session is null)
            {
                return;
            }

            try
            {
                GlobalSystemMediaTransportControlsSessionPlaybackInfo info = _session.GetPlaybackInfo();
                bool next = !(info?.IsShuffleActive ?? false);
                _ = await _session.TryChangeShuffleActiveAsync(next);
            }
            catch { /* shuffle not supported */ }
        }

        public async Task ToggleRepeatAsync()
        {
            if (_session is null)
            {
                await RefreshAsync();
            }

            if (_session is null)
            {
                return;
            }

            try
            {
                MediaPlaybackAutoRepeatMode mode = _session.GetPlaybackInfo()?.AutoRepeatMode
                           ?? Windows.Media.MediaPlaybackAutoRepeatMode.None;
                MediaPlaybackAutoRepeatMode next = mode switch
                {
                    Windows.Media.MediaPlaybackAutoRepeatMode.None =>
                        Windows.Media.MediaPlaybackAutoRepeatMode.List,
                    Windows.Media.MediaPlaybackAutoRepeatMode.List =>
                        Windows.Media.MediaPlaybackAutoRepeatMode.Track,
                    _ => Windows.Media.MediaPlaybackAutoRepeatMode.None
                };
                _ = await _session.TryChangeAutoRepeatModeAsync(next);
            }
            catch { /* repeat not supported */ }
        }

        public Task ToggleLikeAsync(bool wantLiked)
        {
            return Task.CompletedTask; // SMTC has no standard like API
        }

        public Task ToggleDislikeAsync(bool wantDisliked)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _timelinePoll?.Dispose();
            _timelinePoll = null;
            _nullSessionGrace?.Dispose();
            _nullSessionGrace = null;
            if (_session is not null)
            {
                _session.MediaPropertiesChanged -= OnProps;
                _session.PlaybackInfoChanged -= OnProps;
                _session.TimelinePropertiesChanged -= OnTimeline;
            }
        }
    }
}
