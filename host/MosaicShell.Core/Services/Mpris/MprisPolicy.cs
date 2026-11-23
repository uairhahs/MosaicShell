namespace MosaicShell.Core.Services.Mpris
{
    /// <summary>
    /// Decisions about MPRIS players that need no bus: which names are players, which player is "the" session, and how
    /// one maps onto <see cref="MediaSessionInfo"/>. Kept pure so they are tested on every OS.
    /// </summary>
    public static class MprisPolicy
    {
        public const string BusNamePrefix = "org.mpris.MediaPlayer2.";
        public const string ObjectPath = "/org/mpris/MediaPlayer2";
        public const string RootInterface = "org.mpris.MediaPlayer2";
        public const string PlayerInterface = "org.mpris.MediaPlayer2.Player";

        /// <summary>The MPRIS spec's "no track" id; SetPosition with it is ignored by players.</summary>
        public const string NoTrackId = "/org/mpris/MediaPlayer2/TrackList/NoTrack";

        private const double MicrosPerSecond = 1_000_000.0;

        /// <summary>An MPRIS player to read; never the Host's own published player (<see cref="MprisExportPolicy"/>).</summary>
        public static bool IsPlayerBusName(string? name)
        {
            return name is not null
                && name.Length > BusNamePrefix.Length
                && name.StartsWith(BusNamePrefix, StringComparison.Ordinal)
                && !MprisExportPolicy.IsOwnPlayer(name);
        }

        /// <summary>
        /// The desktop entry when the player names one ("microsoft-edge"), otherwise the bus name's player part with any
        /// per-process instance suffix removed (".instance66176", ".instance_1_23"), so one app keeps one id.
        /// </summary>
        public static string AppIdOf(MprisPlayerState player)
        {
            if (!string.IsNullOrWhiteSpace(player.DesktopEntry))
            {
                return player.DesktopEntry.Trim();
            }

            string id = IsPlayerBusName(player.BusName) ? player.BusName[BusNamePrefix.Length..] : player.BusName;
            int instance = id.IndexOf(".instance", StringComparison.Ordinal);
            return instance > 0 ? id[..instance] : id;
        }

        /// <summary>Non-blank artists joined with ", "; null when there are none (Chromium publishes [""]).</summary>
        public static string? JoinArtists(IReadOnlyList<string> artists)
        {
            string[] named = [.. artists.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim())];
            return named.Length == 0 ? null : string.Join(", ", named);
        }

        public static MediaSessionInfo ToSessionInfo(MprisPlayerState player, byte[]? thumbnail)
        {
            return new MediaSessionInfo(
                string.IsNullOrWhiteSpace(player.Title) ? null : player.Title,
                JoinArtists(player.Artists),
                AppIdOf(player),
                player.PlaybackStatus == MprisPlaybackStatus.Playing,
                thumbnail,
                Math.Max(0, player.PositionMicros) / MicrosPerSecond,
                Math.Max(0, player.LengthMicros) / MicrosPerSecond);
        }

        /// <summary>
        /// Drops a player when another player says it mirrors that player's process (plasma-browser-integration's
        /// <c>kde:pid</c>). The mirror carries the cleaner title, the artist and the art, so showing both would offer the
        /// same tab twice and the worse copy could win.
        /// </summary>
        public static IReadOnlyList<MprisPlayerState> WithoutMirrored(IReadOnlyList<MprisPlayerState> players)
        {
            HashSet<int> mirrored = [.. players.Where(p => p.MirroredPid is not null).Select(p => p.MirroredPid!.Value)];
            return mirrored.Count == 0
                ? players
                : [.. players.Where(p => p.MirroredPid is not null || p.OwnerPid is not { } pid || !mirrored.Contains(pid))];
        }

        /// <summary>
        /// The player to show, in the spirit of Windows' "current session":
        /// the player that most recently started playing; else, when nothing plays, the one already shown; else the one
        /// that played last. A stopped player with no track is not a session at all.
        /// </summary>
        /// <param name="players">Players to choose from, mirrors already removed.</param>
        /// <param name="previous">Bus name of the player shown until now, if any.</param>
        /// <param name="playingSince">When each player last went to Playing; players never seen playing are absent.</param>
        public static MprisPlayerState? PickActive(
            IReadOnlyList<MprisPlayerState> players,
            string? previous,
            IReadOnlyDictionary<string, DateTimeOffset> playingSince)
        {
            MprisPlayerState[] eligible = [.. players.Where(IsSession)];
            if (eligible.Length == 0)
            {
                return null;
            }

            MprisPlayerState[] playing = [.. eligible.Where(p => p.PlaybackStatus == MprisPlaybackStatus.Playing)];
            if (playing.Length > 0)
            {
                return playing.OrderByDescending(p => SinceOrMin(p, playingSince)).ThenBy(p => p.BusName, StringComparer.Ordinal).First();
            }

            MprisPlayerState? shown = eligible.FirstOrDefault(p => string.Equals(p.BusName, previous, StringComparison.Ordinal));
            return shown ?? eligible.OrderByDescending(p => SinceOrMin(p, playingSince)).ThenBy(p => p.BusName, StringComparer.Ordinal).First();
        }

        /// <summary>None, then Playlist, then Track, then None: the order Windows' repeat button cycles.</summary>
        public static string NextLoopStatus(string? current)
        {
            return current switch
            {
                null or "None" => "Playlist",
                "Playlist" => "Track",
                _ => "None",
            };
        }

        public static bool CanSeekAbsolute(string? trackId)
        {
            return !string.IsNullOrEmpty(trackId) && !string.Equals(trackId, NoTrackId, StringComparison.Ordinal);
        }

        private static bool IsSession(MprisPlayerState player)
        {
            return player.PlaybackStatus != MprisPlaybackStatus.Stopped || !string.IsNullOrWhiteSpace(player.Title);
        }

        private static DateTimeOffset SinceOrMin(MprisPlayerState player, IReadOnlyDictionary<string, DateTimeOffset> playingSince)
        {
            return playingSince.TryGetValue(player.BusName, out DateTimeOffset since) ? since : DateTimeOffset.MinValue;
        }
    }
}
