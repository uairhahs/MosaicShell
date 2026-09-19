namespace MosaicShell.Core.Services.WebNowPlaying
{
    /// <summary>
    /// Presents the WebNowPlaying host as an <see cref="IBrowserMediaSource"/>, so the rest of the app never sees
    /// WebNowPlaying types. This whole folder is deleted when WebNowPlaying is removed.
    /// </summary>
    public sealed class WebNowPlayingSource : IBrowserMediaSource
    {
        private readonly WebNowPlayingReduxHost _host;

        public WebNowPlayingSource(WebNowPlayingReduxHost host)
        {
            _host = host;
            _host.Changed += OnHostChanged;
        }

        public BrowserPlayerSnapshot? Active => ToBrowser(_host.Active);

        public event EventHandler? Changed;

        public Task SetLikedAsync(bool liked)
        {
            return _host.TrySetLikeAsync(liked);
        }

        public Task SetDislikedAsync(bool disliked)
        {
            return _host.TrySetDislikeAsync(disliked);
        }

        public Task ToggleShuffleAsync()
        {
            return _host.TryToggleShuffleAsync();
        }

        public Task ToggleRepeatAsync()
        {
            return _host.TryToggleRepeatAsync();
        }

        public void Dispose()
        {
            _host.Changed -= OnHostChanged;
            _host.Dispose();
        }

        internal static BrowserPlayerSnapshot? ToBrowser(WnpPlayerSnapshot? wnp)
        {
            return wnp is null ? null : Convert(wnp);
        }

        private static BrowserPlayerSnapshot Convert(WnpPlayerSnapshot wnp)
        {
            BrowserMediaCapabilities capabilities = BrowserMediaCapabilities.Rating
                | BrowserMediaCapabilities.Shuffle
                | BrowserMediaCapabilities.Repeat;
            if (MediaLikePolicy.SupportsDislike(appId: null, wnp.Name))
            {
                capabilities |= BrowserMediaCapabilities.Dislike;
            }

            return new BrowserPlayerSnapshot
            {
                Name = wnp.Name,
                Title = wnp.Title,
                Artist = wnp.Artist,
                Album = wnp.Album,
                State = wnp.State switch
                {
                    WnpState.Playing => BrowserPlaybackState.Playing,
                    WnpState.Paused => BrowserPlaybackState.Paused,
                    _ => BrowserPlaybackState.Stopped,
                },
                PositionSeconds = wnp.PositionSeconds,
                DurationSeconds = wnp.DurationSeconds,
                CoverPng = wnp.CoverPng,
                Rating = MediaLikePolicy.IsLiked(wnp.Rating)
                    ? BrowserRating.Liked
                    : MediaLikePolicy.IsDisliked(wnp.Rating) ? BrowserRating.Disliked : BrowserRating.None,
                Capabilities = capabilities,
            };
        }

        private void OnHostChanged(object? sender, EventArgs e)
        {
            Changed?.Invoke(this, e);
        }
    }
}
