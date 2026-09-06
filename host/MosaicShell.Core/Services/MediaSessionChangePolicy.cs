namespace MosaicShell.Core.Services
{
    /// <summary>
    /// When SMTC lags on title but resets the timeline on skip, Tessera must still raise
    /// <c>Media.Changed</c> (present media flyout), not only ProgressChanged.
    /// </summary>
    public static class MediaSessionChangePolicy
    {
        /// <summary>Prior position high enough that a drop to near-zero looks like a skip.</summary>
        public const double MinPrevSecondsForRestart = 5.0;

        /// <summary>New position at/under this after a long prior play ⇒ treat as new track.</summary>
        public const double MaxNextSecondsForRestart = 2.5;

        /// <summary>Large backward seek without title change still counts as a track boundary.</summary>
        public const double MinBackwardJumpSeconds = 8.0;

        /// <summary>
        /// SMTC often skips MediaPropertiesChanged on skip (YTM/Chrome). Poll timeline even
        /// when no Tessera flyout is pumping.
        /// </summary>
        public const bool MustPollTimelineIndependentlyOfFlyout = true;

        public const int TimelinePollMs = 500;

        /// <summary>
        /// Sticky SMTC/WNP repeats must not pull a playing scrubber backward.
        /// Hide/show tween overlap is a separate Host cancel; this is the timeline clock.
        /// </summary>
        public const bool MustNotRewindPlayingScrubber = true;

        /// <summary>Ignore source jitter smaller than this when deciding whether the API moved.</summary>
        public const double PlayingPositionJitterSeconds = 0.35;

        public static bool LooksLikeNewTrackPosition(double prevPositionSeconds, double nextPositionSeconds)
        {
            return (prevPositionSeconds >= MinPrevSecondsForRestart
                && nextPositionSeconds <= MaxNextSecondsForRestart) || prevPositionSeconds - nextPositionSeconds >= MinBackwardJumpSeconds;
        }

        /// <summary>
        /// Commit a playing position. Sticky repeats keep the ahead clock; real skips
        /// and forward seeks still land. Small lagging ticks (WNP vs extrapolated SMTC)
        /// must not jump the scrubber backward.
        /// </summary>
        public static double ResolvePlayingPosition(
            double committedSeconds,
            double incomingSeconds,
            bool playing,
            bool incomingReportedChange)
        {
            if (!MustNotRewindPlayingScrubber || !playing)
            {
                return incomingSeconds;
            }

            return !incomingReportedChange
                ? incomingSeconds + PlayingPositionJitterSeconds < committedSeconds ? committedSeconds : incomingSeconds
                : LooksLikeNewTrackPosition(committedSeconds, incomingSeconds)
                ? incomingSeconds
                : incomingSeconds + PlayingPositionJitterSeconds >= committedSeconds ? incomingSeconds : committedSeconds;
        }
    }
}
