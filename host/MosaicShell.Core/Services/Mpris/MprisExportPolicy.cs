using System.Security.Cryptography;
using System.Text;

namespace MosaicShell.Core.Services.Mpris
{
    /// <summary>One track as the published player describes it in <c>Metadata</c>.</summary>
    public sealed record MprisExportedTrack(
        string TrackId,
        string Title,
        IReadOnlyList<string> Artists,
        string? Album,
        long? LengthMicros,
        string? ArtUrl,
        int? KdePid = null);

    /// <summary>Everything the published player answers, as of one report.</summary>
    public sealed record MprisExportedState(
        string PlaybackStatus,
        MprisExportedTrack Track,
        long PositionMicros,
        string Identity,
        string? DesktopEntry);

    /// <summary>
    /// The MPRIS player the Host publishes on Linux from Grout's report. The browser's own players carry what the
    /// browser passes on, and plasma-browser-integration drops its cover partway through a track; Grout reads the page
    /// itself, so publishing its report gives Plasma's media controls, the lock screen and any other MPRIS client the
    /// right title, artist and cover for the whole track. Pure decisions only; the D-Bus server translates them.
    /// </summary>
    public static class MprisExportPolicy
    {
        public const string BusName = "org.mpris.MediaPlayer2.mosaicshell";
        public const string ObjectPath = "/org/mpris/MediaPlayer2";

        /// <summary>How far the position may stray from where playback would be before it counts as a seek.</summary>
        public const long SeekToleranceMicros = 1_500_000;

        private const string TrackIdPrefix = "/org/mosaicshell/track/t";

        /// <summary>Whether <paramref name="busName"/> is the Host's own player, which its MPRIS reader must skip.</summary>
        public static bool IsOwnPlayer(string busName)
        {
            return string.Equals(busName, BusName, StringComparison.Ordinal)
                || busName.StartsWith(BusName + ".", StringComparison.Ordinal);
        }

        public static string PlaybackStatus(BrowserPlaybackState state)
        {
            return state switch
            {
                BrowserPlaybackState.Playing => "Playing",
                BrowserPlaybackState.Paused => "Paused",
                _ => "Stopped",
            };
        }

        /// <summary>An object path that names the track, the same for the same title and artist.</summary>
        public static string TrackId(string title, string artist)
        {
            return TrackIdPrefix + ShortHash(Encoding.UTF8.GetBytes(title + "\0" + artist));
        }

        public static long? LengthMicros(double durationSeconds)
        {
            return durationSeconds > 0 ? (long)Math.Round(durationSeconds * 1_000_000) : null;
        }

        /// <summary>Where published covers are written: <c>$XDG_CACHE_HOME</c> when absolute, else <c>~/.cache</c>.</summary>
        public static string ArtCacheDirectory(string? xdgCacheHome, string home)
        {
            string cache = !string.IsNullOrEmpty(xdgCacheHome) && xdgCacheHome.StartsWith('/')
                ? xdgCacheHome.TrimEnd('/')
                : home.TrimEnd('/') + "/.cache";
            return cache + "/MosaicShell/mpris-art";
        }

        public static string DefaultArtCacheDirectory()
        {
            return ArtCacheDirectory(
                Environment.GetEnvironmentVariable("XDG_CACHE_HOME"),
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }

        /// <summary>The cover's file name in the art cache: its content hash, so the same cover keeps the same file.</summary>
        public static string ArtFileName(byte[] cover)
        {
            string extension = cover.Length >= 3 && cover[0] == 0xFF && cover[1] == 0xD8 && cover[2] == 0xFF
                ? ".jpg"
                : cover.Length >= 12 && cover[0] == 0x52 && cover[1] == 0x49 && cover[8] == 0x57 && cover[9] == 0x45
                    ? ".webp"
                    : ".png";
            return "cover-" + ShortHash(cover) + extension;
        }

        /// <summary>
        /// The state to publish for <paramref name="report"/>, or null to publish nothing: no report, no title, or
        /// stopped. <paramref name="artUrl"/> is where the cover was written; <paramref name="desktopEntry"/> the
        /// browser's, so clients show its icon. Grout reports no timing, so position and length come from
        /// <paramref name="session"/>, the platform's own session, when it plays the same track (as SMTC supplies them on
        /// Windows). While the session is the browser (<paramref name="desktopEntry"/> set), the track claims
        /// <paramref name="browserProcessId"/> in <c>kde:pid</c>, so Plasma hides the browser's own player behind this one.
        /// </summary>
        public static MprisExportedState? From(
            BrowserPlayerSnapshot? report,
            string? artUrl,
            string? desktopEntry,
            MediaSessionInfo? session = null,
            int? browserProcessId = null)
        {
            if (report is null || string.IsNullOrWhiteSpace(report.Title) || report.State == BrowserPlaybackState.Stopped)
            {
                return null;
            }

            bool sameTrack = session is not null && MediaTitleNormalizer.LooselyMatch(session.Title, report.Title);
            double position = sameTrack && report.PositionSeconds <= 0 ? session!.PositionSeconds : report.PositionSeconds;
            double duration = sameTrack && report.DurationSeconds <= 0 ? session!.DurationSeconds : report.DurationSeconds;
            string artist = report.Artist.Trim();
            MprisExportedTrack track = new(
                TrackId(report.Title, artist),
                report.Title,
                artist.Length > 0 ? [artist] : [],
                string.IsNullOrWhiteSpace(report.Album) ? null : report.Album,
                LengthMicros(duration),
                artUrl,
                desktopEntry is null ? null : browserProcessId);
            return new MprisExportedState(
                PlaybackStatus(report.State),
                track,
                PositionMicros(position),
                string.IsNullOrWhiteSpace(report.Name) ? "MosaicShell" : report.Name,
                desktopEntry);
        }

        /// <summary>
        /// The Player properties to announce in PropertiesChanged. Position is never announced (MPRIS clients read it
        /// and extrapolate); a jump is a Seeked signal instead (<see cref="IsSeek"/>).
        /// </summary>
        public static IReadOnlyList<string> ChangedPlayerProperties(MprisExportedState? previous, MprisExportedState next)
        {
            List<string> changed = [];
            if (previous is null || previous.PlaybackStatus != next.PlaybackStatus)
            {
                changed.Add("PlaybackStatus");
            }

            if (previous is null || !SameTrack(previous.Track, next.Track))
            {
                changed.Add("Metadata");
            }

            return changed;
        }

        /// <summary>Whether the reported position left where playback would have carried it since the last report.</summary>
        public static bool IsSeek(long previousMicros, double elapsedSeconds, bool wasPlaying, long nextMicros)
        {
            long expected = previousMicros + (wasPlaying ? (long)(Math.Max(0, elapsedSeconds) * 1_000_000) : 0);
            return Math.Abs(nextMicros - expected) > SeekToleranceMicros;
        }

        /// <summary>The position to answer <paramref name="secondsSinceReport"/> after the report: running on while playing, never past the end.</summary>
        public static long PositionAt(MprisExportedState state, double secondsSinceReport)
        {
            long position = state.PositionMicros;
            if (state.PlaybackStatus == "Playing")
            {
                position += (long)(Math.Max(0, secondsSinceReport) * 1_000_000);
            }

            return state.Track.LengthMicros is long length ? Math.Min(position, length) : position;
        }

        private static long PositionMicros(double seconds)
        {
            return (long)Math.Round(Math.Max(0, seconds) * 1_000_000);
        }

        private static bool SameTrack(MprisExportedTrack a, MprisExportedTrack b)
        {
            return a.TrackId == b.TrackId
                && a.Title == b.Title
                && a.Artists.SequenceEqual(b.Artists)
                && a.Album == b.Album
                && a.LengthMicros == b.LengthMicros
                && a.ArtUrl == b.ArtUrl
                && a.KdePid == b.KdePid;
        }

        private static string ShortHash(byte[] bytes)
        {
            return Convert.ToHexStringLower(SHA256.HashData(bytes))[..16];
        }
    }
}
