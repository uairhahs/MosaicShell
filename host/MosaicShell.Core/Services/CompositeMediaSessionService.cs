namespace MosaicShell.Core.Services
{
    /// <summary>
    /// Merges Windows SMTC with browser media sources. SMTC drives transport when present; a browser source
    /// supplies artist, album and cover for browser players (YouTube Music) where SMTC has only the page title.
    /// </summary>
    public sealed class CompositeMediaSessionService : IMediaSessionService, IMediaSourceDiagnostics
    {
        private readonly IMediaSessionService _smtc;
        private readonly IBrowserMediaSource[] _browser;

        // Which source's title changed last. On a web app's track change the extension and Windows
        // disagree for 100 to 300 ms, and only the order says which one is current.
        private long _titleSequence;
        private string? _lastSmtcTitle;
        private long _smtcTitleAt;
        private string? _lastBrowserTitle;
        private long _browserTitleAt;

        /// <summary>Browser sources are listed in order of preference; the first with an active player is used.</summary>
        public CompositeMediaSessionService(IMediaSessionService smtc, params IBrowserMediaSource[] browserSources)
        {
            _smtc = smtc;
            _browser = browserSources;
            _smtc.Changed += OnSourceChanged;
            _smtc.ProgressChanged += OnSmtcProgress;
            foreach (IBrowserMediaSource source in _browser)
            {
                source.Changed += OnSourceChanged;
            }

            Rebuild(raiseProgress: false);
        }

        public MediaSessionInfo? Current { get; private set; }
        public event EventHandler? Changed;
        public event EventHandler? ProgressChanged;

        public string DescribeSources()
        {
            return MediaSourceAttribution.Describe(_smtc.Current, ActiveBrowserPlayer(), Current);
        }

        public void PumpTimeline()
        {
            _smtc.PumpTimeline();
            // Browser position updates arrive via Changed; still refresh merge in case only SMTC moved
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
            if (ActiveSourceWith(BrowserMediaCapabilities.Shuffle) is { } source)
            {
                await source.ToggleShuffleAsync();
            }
        }

        public async Task ToggleRepeatAsync()
        {
            await _smtc.ToggleRepeatAsync();
            if (ActiveSourceWith(BrowserMediaCapabilities.Repeat) is { } source)
            {
                await source.ToggleRepeatAsync();
            }
        }

        public async Task ToggleLikeAsync(bool wantLiked)
        {
            await _smtc.ToggleLikeAsync(wantLiked);
            if (ActiveSourceWith(BrowserMediaCapabilities.Rating) is { } source)
            {
                await source.SetLikedAsync(wantLiked);
            }
        }

        public async Task ToggleDislikeAsync(bool wantDisliked)
        {
            await _smtc.ToggleDislikeAsync(wantDisliked);
            if (ActiveSourceWith(BrowserMediaCapabilities.Dislike) is { } source)
            {
                await source.SetDislikedAsync(wantDisliked);
            }
        }

        public void Dispose()
        {
            _smtc.Changed -= OnSourceChanged;
            _smtc.ProgressChanged -= OnSmtcProgress;
            foreach (IBrowserMediaSource source in _browser)
            {
                source.Changed -= OnSourceChanged;
            }

            _smtc.Dispose();
            foreach (IBrowserMediaSource source in _browser)
            {
                source.Dispose();
            }
        }

        private void OnSourceChanged(object? sender, EventArgs e)
        {
            Rebuild(raiseProgress: false);
        }

        private void OnSmtcProgress(object? sender, EventArgs e)
        {
            Rebuild(raiseProgress: true);
        }

        /// <summary>The source whose player is active: the first, in order of preference, that has one.</summary>
        private IBrowserMediaSource? ActiveSource()
        {
            foreach (IBrowserMediaSource source in _browser)
            {
                if (source.Active is not null)
                {
                    return source;
                }
            }

            return null;
        }

        private BrowserPlayerSnapshot? ActiveBrowserPlayer()
        {
            return ActiveSource()?.Active;
        }

        private IBrowserMediaSource? ActiveSourceWith(BrowserMediaCapabilities capability)
        {
            IBrowserMediaSource? source = ActiveSource();
            return source?.Active is { } player && player.Capabilities.HasFlag(capability) ? source : null;
        }

        private void Rebuild(bool raiseProgress)
        {
            MediaSessionInfo? smtc = _smtc.Current;
            BrowserPlayerSnapshot? browser = ActiveBrowserPlayer();
            MediaSessionInfo? prev = Current;
            bool browserNewer = TrackTitleOrder(smtc, browser);
            MediaSessionInfo? next = Merge(smtc, browser, browserNewer);
            Current = next;

            if (prev is null && next is null)
            {
                return;
            }

            bool raiseChanged = false;

            if (prev is not null && smtc is not null && !browserNewer)
            {
                if (!string.IsNullOrWhiteSpace(smtc.Title)
                    && !MediaTitleNormalizer.IsSitePlaceholder(smtc.Title)
                    && !string.Equals(
                        prev.Title, MediaTitleNormalizer.StripSiteSuffix(smtc.Title), StringComparison.Ordinal))
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

        /// <summary>
        /// Records which source's title changed last and returns true when the browser's did. A placeholder from
        /// Windows ("YouTube Music") is not a title, so it changes nothing.
        /// </summary>
        private bool TrackTitleOrder(MediaSessionInfo? smtc, BrowserPlayerSnapshot? browser)
        {
            string? smtcTitle = MediaTitleNormalizer.IsSitePlaceholder(smtc?.Title)
                ? _lastSmtcTitle
                : MediaTitleNormalizer.StripSiteSuffix(smtc?.Title);
            if (!string.Equals(smtcTitle, _lastSmtcTitle, StringComparison.OrdinalIgnoreCase))
            {
                _lastSmtcTitle = smtcTitle;
                _smtcTitleAt = ++_titleSequence;
            }

            string? browserTitle = browser?.Title;
            if (!string.Equals(browserTitle, _lastBrowserTitle, StringComparison.OrdinalIgnoreCase))
            {
                _lastBrowserTitle = browserTitle;
                _browserTitleAt = ++_titleSequence;
            }

            return _browserTitleAt > _smtcTitleAt;
        }

        /// <summary>
        /// Windows' session with the browser player laid over it. <paramref name="browserTitleIsNewer"/>: the
        /// browser's title changed after Windows' did; when Windows' session is the web app of the browser player's
        /// own site, the newer of the two titles is the current track.
        /// </summary>
        internal static MediaSessionInfo? Merge(MediaSessionInfo? smtc, BrowserPlayerSnapshot? browser, bool browserTitleIsNewer = false)
        {
            if (smtc is null && browser is null)
            {
                return null;
            }

            if (smtc is null)
            {
                return new MediaSessionInfo(
                    Title: NullIfEmpty(browser!.Title),
                    Artist: NullIfEmpty(browser.Artist),
                    AppId: NullIfEmpty(browser.Name) ?? "Browser",
                    IsPlaying: browser.IsPlaying,
                    ThumbnailPng: browser.CoverPng,
                    PositionSeconds: browser.PositionSeconds,
                    DurationSeconds: browser.DurationSeconds,
                    LikeRating: MediaLikePolicy.ToLikeRating(browser.Rating),
                    Capabilities: browser.Capabilities);
            }

            // Prefer any browser cover when SMTC has none (YTM PWA / browser)
            byte[]? thumb = PickCover(smtc.ThumbnailPng, browser?.CoverPng, smtc.AppId);
            string? title = MediaTitleNormalizer.StripSiteSuffix(smtc.Title);
            string? artist = smtc.Artist;
            // The bare "YouTube Music" a web app shows between tracks names no track.
            string? smtcTitle = MediaTitleNormalizer.IsSitePlaceholder(smtc.Title) ? null : smtc.Title;
            bool sameSite = browser is not null
                && BrowserSessionPolicy.SiteNameOfApp(smtc.AppId) is { } appSite
                && string.Equals(appSite, browser.Name, StringComparison.OrdinalIgnoreCase);
            if (browser is not null && !string.IsNullOrWhiteSpace(browser.Title)
                && (BrowserSessionPolicy.LooksLikeBrowserSession(smtc.AppId)
                    || MediaTitleNormalizer.LooselyMatch(smtcTitle, browser.Title)))
            {
                // Use the browser title and artist when SMTC has none, still agrees with it, or (same site) is
                // the older of the two. Do not keep a stale browser title when SMTC already advanced to a new
                // track; that swallowed Media.Changed and blocked Tessera media flyouts.
                if (string.IsNullOrWhiteSpace(smtcTitle)
                    || MediaTitleNormalizer.LooselyMatch(smtcTitle, browser.Title)
                    || (sameSite && browserTitleIsNewer))
                {
                    title = browser.Title;
                    if (!string.IsNullOrWhiteSpace(browser.Artist))
                    {
                        artist = browser.Artist;
                    }
                }

                if (!IsUsableCover(thumb) && IsUsableCover(browser.CoverPng))
                {
                    thumb = browser.CoverPng;
                }
            }

            // Prefer the browser timeline when SMTC duration is missing / sticky
            double pos = smtc.PositionSeconds;
            double dur = smtc.DurationSeconds;
            if (browser is not null && browser.DurationSeconds > 0
                && (dur <= 0.5 || BrowserSessionPolicy.LooksLikeBrowserSession(smtc.AppId)))
            {
                // A browser position often lags a skip; do not mask SMTC restart edges.
                if (!MediaSessionChangePolicy.LooksLikeNewTrackPosition(
                        browser.PositionSeconds, smtc.PositionSeconds))
                {
                    pos = MediaSessionChangePolicy.ResolvePlayingPosition(
                        smtc.PositionSeconds,
                        browser.PositionSeconds,
                        smtc.IsPlaying,
                        incomingReportedChange: true);
                    dur = browser.DurationSeconds;
                }
            }

            return smtc with
            {
                Title = title,
                Artist = artist,
                ThumbnailPng = thumb,
                PositionSeconds = pos,
                DurationSeconds = dur,
                LikeRating = ResolveLikeRating(smtc.AppId, browser?.Rating),
                Capabilities = ResolveCapabilities(smtc.AppId, browser),
            };
        }

        private static int? ResolveLikeRating(string? appId, BrowserRating? rating)
        {
            return !BrowserSessionPolicy.LooksLikeBrowserSession(appId) || rating is null ? null : MediaLikePolicy.ToLikeRating(rating.Value);
        }

        private static BrowserMediaCapabilities ResolveCapabilities(string? appId, BrowserPlayerSnapshot? browser)
        {
            return browser is not null && BrowserSessionPolicy.LooksLikeBrowserSession(appId) ? browser.Capabilities : BrowserMediaCapabilities.None;
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

        internal static byte[]? PickCover(byte[]? smtcThumb, byte[]? browserCover, string? appId)
        {
            return BrowserSessionPolicy.LooksLikeBrowserSession(appId) && IsUsableCover(browserCover)
                ? browserCover
                : IsUsableCover(smtcThumb)
                ? smtcThumb
                : IsUsableCover(browserCover) ? browserCover : null;
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
