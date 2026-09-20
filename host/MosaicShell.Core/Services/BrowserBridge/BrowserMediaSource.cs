namespace MosaicShell.Core.Services.BrowserBridge
{
    /// <summary>
    /// The browser source fed by the MosaicShell extension: presents the tab the extension reports as the player the
    /// flyout shows, fetches its cover in the background, and turns like and dislike into commands for the extension.
    /// </summary>
    public sealed class BrowserMediaSource : IBrowserMediaSource
    {
        private readonly BrowserSessionHub _hub;
        private readonly BrowserArtworkFetcher _fetcher;
        private readonly Func<string?> _smtcTitle;
        private readonly IDisposable[] _owned;

        /// <summary>
        /// <paramref name="smtcTitle"/> is the title Windows currently reports; when several tabs play, the one that
        /// matches it is shown. Anything in <paramref name="owned"/> is disposed with the source.
        /// </summary>
        public BrowserMediaSource(BrowserSessionHub hub, BrowserArtworkFetcher fetcher, Func<string?> smtcTitle, params IDisposable[] owned)
        {
            _hub = hub;
            _fetcher = fetcher;
            _smtcTitle = smtcTitle;
            _owned = owned;
            _hub.Changed += OnChanged;
            _fetcher.Fetched += OnChanged;
        }

        public BrowserPlayerSnapshot? Active
        {
            get
            {
                BrowserSessionEntry? entry = _hub.Select(_smtcTitle());
                return entry is null ? null : ToSnapshot(entry.Report, CoverFor(entry.Report));
            }
        }

        public event EventHandler? Changed;

        /// <summary>The source, its pipe and its artwork cache, listening for the extension's relay.</summary>
        public static BrowserMediaSource Create(Func<string?> smtcTitle)
        {
            BrowserSessionHub hub = new(TimeProvider.System);
            BrowserArtworkFetcher fetcher = new();
            BrowserPipeServer server = new(BrowserPipeServer.PipeNameForCurrentUser(), hub, BrowserSessionHub.DefaultMaxConnections + 1);
            server.Start();
            return new BrowserMediaSource(hub, fetcher, smtcTitle, server, fetcher);
        }

        public Task SetLikedAsync(bool liked)
        {
            return Send(liked ? BrowserCommandAction.Like : BrowserCommandAction.Clear);
        }

        public Task SetDislikedAsync(bool disliked)
        {
            return Send(disliked ? BrowserCommandAction.Dislike : BrowserCommandAction.Clear);
        }

        /// <summary>Not offered: no player declares the capability, so the composite never asks.</summary>
        public Task ToggleShuffleAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>Not offered: no player declares the capability, so the composite never asks.</summary>
        public Task ToggleRepeatAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _hub.Changed -= OnChanged;
            _fetcher.Fetched -= OnChanged;
            foreach (IDisposable owned in _owned)
            {
                owned.Dispose();
            }
        }

        /// <summary>The player as the flyout sees it. Position and duration stay zero: Windows supplies the timeline.</summary>
        internal static BrowserPlayerSnapshot ToSnapshot(BrowserSessionReport report, byte[]? cover)
        {
            return new BrowserPlayerSnapshot
            {
                Name = BrowserSiteNames.FromOrigin(report.Origin),
                Title = report.Title,
                Artist = report.Artist,
                Album = report.Album,
                State = report.PlaybackState,
                CoverPng = cover,
                Rating = report.Rating,
                Capabilities = report.Capabilities,
            };
        }

        /// <summary>The cached cover, or null while it downloads; asks for the download when it is not cached.</summary>
        private byte[]? CoverFor(BrowserSessionReport report)
        {
            if (BrowserArtworkPolicy.Choose(report.Artwork) is not { } artwork)
            {
                return null;
            }

            byte[]? cached = _fetcher.TryGet(artwork.Src);
            if (cached is null)
            {
                _fetcher.Request(artwork.Src);
            }

            return cached;
        }

        private Task Send(BrowserCommandAction action)
        {
            if (_hub.Select(_smtcTitle()) is { } entry)
            {
                _ = _hub.SendCommand(entry, action);
            }

            return Task.CompletedTask;
        }

        private void OnChanged(object? sender, EventArgs e)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
