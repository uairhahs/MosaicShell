namespace MosaicShell.Core.Services
{
    public enum BrowserPlaybackState
    {
        Playing = 0,
        Paused = 1,
        Stopped = 2,
    }

    /// <summary>Like and dislike are one state, so a track is never both.</summary>
    public enum BrowserRating
    {
        None = 0,
        Liked = 1,
        Disliked = 2,
    }

    /// <summary>What the player behind a browser source can be asked to do. Declared per player, not per source.</summary>
    [Flags]
    public enum BrowserMediaCapabilities
    {
        None = 0,
        Rating = 1,
        Dislike = 2,
        Shuffle = 4,
        Repeat = 8,
    }

    /// <summary>The media a browser reports, independent of how it got to the Host.</summary>
    public sealed class BrowserPlayerSnapshot
    {
        public string Name { get; init; } = "";
        public string Title { get; init; } = "";
        public string Artist { get; init; } = "";
        public string Album { get; init; } = "";
        public BrowserPlaybackState State { get; init; }
        public double PositionSeconds { get; init; }
        public double DurationSeconds { get; init; }
        public byte[]? CoverPng { get; init; }
        public BrowserRating Rating { get; init; }
        public BrowserMediaCapabilities Capabilities { get; init; }

        public bool IsPlaying => State == BrowserPlaybackState.Playing;
    }

    /// <summary>
    /// A provider of browser media: title, artist, album and cover for whatever the browser is playing, plus the
    /// commands its capabilities allow. The composite media service depends on this and on nothing behind it.
    /// </summary>
    public interface IBrowserMediaSource : IDisposable
    {
        /// <summary>The player the source considers active, or null when nothing is playing.</summary>
        BrowserPlayerSnapshot? Active { get; }

        event EventHandler? Changed;

        /// <summary>Like or clear the like. Only called when the active player declares <see cref="BrowserMediaCapabilities.Rating"/>.</summary>
        Task SetLikedAsync(bool liked);

        /// <summary>Dislike or clear the dislike. Only called when the active player declares <see cref="BrowserMediaCapabilities.Dislike"/>.</summary>
        Task SetDislikedAsync(bool disliked);

        Task ToggleShuffleAsync();

        Task ToggleRepeatAsync();
    }
}
