using MosaicShell.Core.Services.WebNowPlaying;

namespace MosaicShell.Core.Services
{
    /// <summary>
    /// Merges Windows SMTC with WebNowPlaying. SMTC drives transport when present;
    /// WNP supplies album art for browser players (YouTube Music) where SMTC Thumbnail is null.
    /// </summary>
    public sealed class CompositeMediaSessionService : IMediaSessionService
    {
        private readonly IMediaSessionService _smtc;
        private readonly IWebNowPlayingService _wnp;

        public CompositeMediaSessionService(IMediaSessionService smtc, IWebNowPlayingService wnp)
        {
            _smtc = smtc;
            _wnp = wnp;
            _smtc.Changed += OnSourceChanged;
            _smtc.ProgressChanged += OnSmtcProgress;
            _wnp.Changed += OnSourceChanged;
            Rebuild(raiseProgress: false);
        }

        public MediaSessionInfo? Current { get; private set; }
        public event EventHandler? Changed;
        public event EventHandler? ProgressChanged;

        public void PumpTimeline()
        {
            _smtc.PumpTimeline();
            // WNP position updates arrive via Changed; still refresh merge in case only SMTC moved
            Rebuild(raiseProgress: true);
        }

        public Task PlayPauseAsync()
        {
            return _smtc.PlayPauseAsync();
        }

        public Task NextAsync()
        {
            return _smtc.NextAsync();
        }

        public Task PreviousAsync()
        {
            return _smtc.PreviousAsync();
        }

        public Task SeekAsync(double positionSeconds)
        {
            return _smtc.SeekAsync(positionSeconds);
        }

        public async Task ToggleShuffleAsync()
        {
            await _smtc.ToggleShuffleAsync();
            if (_wnp is WebNowPlayingReduxHost host)
            {
                await host.TryToggleShuffleAsync();
            }
        }
        public async Task ToggleRepeatAsync()
        {
            await _smtc.ToggleRepeatAsync();
            if (_wnp is WebNowPlayingReduxHost host)
            {
                await host.TryToggleRepeatAsync();
            }
        }
        public async Task ToggleLikeAsync(bool wantLiked)
        {
            await _smtc.ToggleLikeAsync(wantLiked);
            if (_wnp is WebNowPlayingReduxHost host)
            {
                await host.TrySetLikeAsync(wantLiked);
            }
        }

        public async Task ToggleDislikeAsync(bool wantDisliked)
        {
            await _smtc.ToggleDislikeAsync(wantDisliked);
            if (_wnp is WebNowPlayingReduxHost host)
            {
                await host.TrySetDislikeAsync(wantDisliked);
            }
        }

        public void Dispose()
        {
            _smtc.Changed -= OnSourceChanged;
            _smtc.ProgressChanged -= OnSmtcProgress;
            _wnp.Changed -= OnSourceChanged;
            _smtc.Dispose();
            _wnp.Dispose();
        }

        private void OnSourceChanged(object? sender, EventArgs e)
        {
            Rebuild(raiseProgress: false);
        }

        private void OnSmtcProgress(object? sender, EventArgs e)
        {
            Rebuild(raiseProgress: true);
        }

        private void Rebuild(bool raiseProgress)
        {
            MediaSessionInfo? smtc = _smtc.Current;
            WnpPlayerSnapshot? wnp = _wnp.Active;
            MediaSessionInfo? prev = Current;
            MediaSessionInfo? next = Merge(smtc, wnp);
            Current = next;

            if (prev is null && next is null)
            {
                return;
            }

            bool raiseChanged = false;

            if (prev is not null && smtc is not null)
            {
                if (!string.IsNullOrWhiteSpace(smtc.Title)
                    && !string.Equals(prev.Title, smtc.Title, StringComparison.Ordinal))
                {
                    raiseChanged = true;
                }

                if (!raiseChanged
                    && MediaSessionChangePolicy.LooksLikeNewTrackPosition(
                        prev.PositionSeconds, smtc.PositionSeconds))
                {
                    raiseChanged = true;
                }
            }

            if (!raiseChanged
                && prev is not null
                && next is not null
                && MediaSessionChangePolicy.LooksLikeNewTrackPosition(
                    prev.PositionSeconds, next.PositionSeconds))
            {
                raiseChanged = true;
            }

            if (raiseChanged)
            {
                Changed?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (raiseProgress)
            {
                if (prev is not null && next is not null
                    && string.Equals(prev.Title, next.Title, StringComparison.Ordinal)
                    && string.Equals(prev.Artist, next.Artist, StringComparison.Ordinal)
                    && ThumbEqual(prev.ThumbnailPng, next.ThumbnailPng)
                    && prev.IsPlaying == next.IsPlaying
                    && (Math.Abs(prev.PositionSeconds - next.PositionSeconds) >= 0.05
                        || Math.Abs(prev.DurationSeconds - next.DurationSeconds) >= 0.5))
                {
                    ProgressChanged?.Invoke(this, EventArgs.Empty);
                    return;
                }
            }

            if (!SessionEqual(prev, next))
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }
            else if (raiseProgress && prev is not null && next is not null
                     && Math.Abs(prev.PositionSeconds - next.PositionSeconds) >= 0.05)
            {
                ProgressChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        internal static MediaSessionInfo? Merge(MediaSessionInfo? smtc, WnpPlayerSnapshot? wnp)
        {
            if (smtc is null && wnp is null)
            {
                return null;
            }

            if (smtc is null)
            {
                return new MediaSessionInfo(
                    Title: NullIfEmpty(wnp!.Title),
                    Artist: NullIfEmpty(wnp.Artist),
                    AppId: NullIfEmpty(wnp.Name) ?? "WebNowPlaying",
                    IsPlaying: wnp.IsPlaying,
                    ThumbnailPng: wnp.CoverPng,
                    PositionSeconds: wnp.PositionSeconds,
                    DurationSeconds: wnp.DurationSeconds,
                    LikeRating: wnp.Rating);
            }

            // Prefer any WNP cover when SMTC has none (YTM PWA / browser)
            byte[]? thumb = PickCover(smtc.ThumbnailPng, wnp?.CoverPng, smtc.Title, wnp?.Title, smtc.AppId);
            string? title = smtc.Title;
            string? artist = smtc.Artist;
            if (wnp is not null && !string.IsNullOrWhiteSpace(wnp.Title)
                && (LooksLikeBrowserSession(smtc.AppId)
                    || TitlesLooselyMatch(smtc.Title, wnp.Title)))
            {
                // Use WNP title/artist when SMTC is empty or still agrees with WNP.
                // Do not keep a stale WNP title when SMTC already advanced to a new track;
                // that swallowed Media.Changed and blocked Tessera media flyouts.
                if (string.IsNullOrWhiteSpace(smtc.Title) || TitlesLooselyMatch(smtc.Title, wnp.Title))
                {
                    title = wnp.Title;
                    if (!string.IsNullOrWhiteSpace(wnp.Artist))
                    {
                        artist = wnp.Artist;
                    }
                }

                if (!IsUsableCover(thumb) && IsUsableCover(wnp.CoverPng))
                {
                    thumb = wnp.CoverPng;
                }
            }

            // Prefer WNP timeline when SMTC duration is missing / sticky
            double pos = smtc.PositionSeconds;
            double dur = smtc.DurationSeconds;
            if (wnp is not null && wnp.DurationSeconds > 0
                && (dur <= 0.5 || LooksLikeBrowserSession(smtc.AppId)))
            {
                // WNP position often lags a skip; do not mask SMTC restart edges.
                if (!MediaSessionChangePolicy.LooksLikeNewTrackPosition(
                        wnp.PositionSeconds, smtc.PositionSeconds))
                {
                    pos = MediaSessionChangePolicy.ResolvePlayingPosition(
                        smtc.PositionSeconds,
                        wnp.PositionSeconds,
                        smtc.IsPlaying,
                        incomingReportedChange: true);
                    dur = wnp.DurationSeconds;
                }
            }

            return smtc with
            {
                Title = title,
                Artist = artist,
                ThumbnailPng = thumb,
                PositionSeconds = pos,
                DurationSeconds = dur,
                LikeRating = ResolveLikeRating(smtc.AppId, wnp?.Rating),
            };
        }

        private static int? ResolveLikeRating(string? appId, int? wnpRating)
        {
            return !LooksLikeBrowserSession(appId) || wnpRating is null ? null : wnpRating.Value;
        }

        private static bool LooksLikeBrowserSession(string? appId)
        {
            return !string.IsNullOrEmpty(appId) && (appId.Contains("youtube", StringComparison.OrdinalIgnoreCase)
                   || appId.Contains("chrome", StringComparison.OrdinalIgnoreCase)
                   || appId.Contains("msedge", StringComparison.OrdinalIgnoreCase)
                   || appId.Contains("firefox", StringComparison.OrdinalIgnoreCase)
                   || appId.Contains("music.youtube", StringComparison.OrdinalIgnoreCase)
                   || appId.Contains("brave", StringComparison.OrdinalIgnoreCase));
        }

        private static bool TitlesLooselyMatch(string? a, string? b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            {
                return false;
            }

            static string Norm(string s)
            {
                int i = s.IndexOf('|');
                if (i > 0)
                {
                    s = s[..i];
                }

                return s.Trim();
            }
            return string.Equals(Norm(a), Norm(b), StringComparison.OrdinalIgnoreCase);
        }

        private static string? NullIfEmpty(string? s)
        {
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        private static bool ThumbEqual(byte[]? a, byte[]? b)
        {
            return ReferenceEquals(a, b)
                || (a is not null && b is not null && a.Length == b.Length && a.AsSpan().SequenceEqual(b));
        }

        private static bool SessionEqual(MediaSessionInfo? a, MediaSessionInfo? b)
        {
            return ReferenceEquals(a, b) || (a is not null && b is not null && string.Equals(a.Title, b.Title, StringComparison.Ordinal)
                   && string.Equals(a.Artist, b.Artist, StringComparison.Ordinal)
                   && string.Equals(a.AppId, b.AppId, StringComparison.Ordinal)
                   && a.IsPlaying == b.IsPlaying
                   && ThumbEqual(a.ThumbnailPng, b.ThumbnailPng)
                   && Math.Abs(a.DurationSeconds - b.DurationSeconds) < 0.5);
        }

        public static bool IsUsableCover(byte[]? bytes)
        {
            return bytes is not null && bytes.Length >= 32 && (LooksLikePng(bytes) || LooksLikeJpeg(bytes) || LooksLikeWebp(bytes) || bytes.Length >= 256);
        }

        internal static byte[]? PickCover(
            byte[]? smtcThumb, byte[]? wnpCover, string? smtcTitle, string? wnpTitle, string? appId)
        {
            return LooksLikeBrowserSession(appId) && IsUsableCover(wnpCover)
                ? wnpCover
                : IsUsableCover(smtcThumb)
                ? smtcThumb
                : IsUsableCover(wnpCover)
                ? wnpCover
                : WebNowPlayingReduxHost.TryGetCachedCover(smtcTitle, out byte[]? png) && IsUsableCover(png)
                ? png
                : WebNowPlayingReduxHost.TryGetCachedCover(wnpTitle, out byte[]? png2) && IsUsableCover(png2)
                ? png2
                : IsUsableCover(smtcThumb) ? smtcThumb : null;
        }

        private static bool LooksLikePng(byte[] b)
        {
            return b.Length > 8 && b[0] == 137 && b[1] == 80 && b[2] == 78 && b[3] == 71;
        }

        private static bool LooksLikeJpeg(byte[] b)
        {
            return b.Length > 3 && b[0] == 255 && b[1] == 216 && b[2] == 255;
        }

        private static bool LooksLikeWebp(byte[] b)
        {
            return b.Length > 12 && b[0] == 82 && b[1] == 73 && b[2] == 70 && b[3] == 70;
        }
    }
}
