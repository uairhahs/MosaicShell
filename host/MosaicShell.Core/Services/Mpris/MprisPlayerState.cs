namespace MosaicShell.Core.Services.Mpris
{
    public enum MprisPlaybackStatus
    {
        Stopped,
        Paused,
        Playing,
    }

    /// <summary>
    /// One MPRIS player as read from the bus (org.mpris.MediaPlayer2 and its .Player interface), already converted
    /// from D-Bus variants. Fields a player does not publish stay at their defaults.
    /// </summary>
    /// <param name="BusName">The well-known name, for example <c>org.mpris.MediaPlayer2.spotify</c>.</param>
    public sealed record MprisPlayerState(string BusName)
    {
        public string? Identity { get; init; }
        public string? DesktopEntry { get; init; }
        public MprisPlaybackStatus PlaybackStatus { get; init; }

        /// <summary><c>mpris:trackid</c>; SetPosition needs it.</summary>
        public string? TrackId { get; init; }

        public string? Title { get; init; }
        public IReadOnlyList<string> Artists { get; init; } = [];

        /// <summary><c>mpris:artUrl</c>: usually file:// or https://.</summary>
        public string? ArtUrl { get; init; }

        public long LengthMicros { get; init; }
        public long PositionMicros { get; init; }
        public bool? Shuffle { get; init; }

        /// <summary>None, Track or Playlist; null when the player has no loop support.</summary>
        public string? LoopStatus { get; init; }

        public bool CanSeek { get; init; }

        /// <summary>Process that owns the bus name, when the bus would say.</summary>
        public int? OwnerPid { get; init; }

        /// <summary>
        /// <c>kde:pid</c>: plasma-browser-integration publishes a cleaner copy of a browser tab's player and names the
        /// browser's process here.
        /// </summary>
        public int? MirroredPid { get; init; }
    }
}
