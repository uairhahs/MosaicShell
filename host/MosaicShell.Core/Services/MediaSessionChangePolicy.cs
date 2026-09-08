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

        /// <summary>
        /// YouTube Music's browser SMTC session closes and a brand-new session object is
        /// created on every track change, rather than updating in place. Measured empirically
        /// (one-off local probe): the gap between the old session closing and the new one
        /// attaching is consistently ~650-950ms. Treating that transient null as a real
        /// "media stopped" event flickers consumers off then straight back on for what the
        /// user experiences as one skip. A real stop (no new session attaches) still surfaces
        /// once this grace window elapses.
        /// </summary>
        public const int NullSessionGraceMs = 1000;

        /// <summary>
        /// YTM/Chrome often re-fire session-changed once album art finishes loading, moments
        /// after the track's identity (title/artist/app/playing/position) already settled and
        /// was raised. That should patch the art in place (ProgressChanged), not restart
        /// Tessera's entrance animation for a track already shown (Changed).
        /// </summary>
        public static bool IsArtOnlyRefresh(int prevThumbnailLength, int nextThumbnailLength)
        {
            return prevThumbnailLength != nextThumbnailLength;
        }

        /// <summary>
        /// A track skip is not one event. The player re-attaches its SMTC session and the new
        /// track's metadata arrives in stages, each stage raising a session-changed event with a
        /// different title:
        /// <code>
        /// "YouTube Music"            -> app placeholder, session re-attaching
        /// "no hesi! | YouTube Music" -> browser tab title
        /// "no hesi!"                 -> settled track metadata
        /// </code>
        /// Classifying each of those as a track boundary rebinds the flyout's title/artist/art
        /// two or three times while its entrance animation is still running, so the card visibly
        /// churns through placeholder text before landing on the real track.
        /// <para>
        /// The discriminator is the artist, not the title: every transitional snapshot measured
        /// (2026-08-29, YouTube Music, 14 boundaries over 6 skips) had an empty artist and every
        /// settled one had a real artist, with no counterexample in either direction. Matching on
        /// the title would mean hardcoding one player's placeholder strings; the artist test is
        /// player-agnostic.
        /// </para>
        /// Callers must treat this as "not settled <i>yet</i>", never as "never a boundary" -
        /// a track genuinely lacking an artist still has to present once
        /// <see cref="MetadataSettleMs"/> elapses.
        /// </summary>
        public static bool IsMetadataSettling(string? title, string? artist)
        {
            return !string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(artist);
        }

        /// <summary>
        /// How long to wait for <see cref="IsMetadataSettling"/> to clear before accepting a
        /// title as final anyway. Measured settle latency was 50-122ms across six skips; this
        /// clears that with headroom while staying well inside the flyout's ~420ms entrance, so
        /// deferring a present by this much is not perceptible as lag.
        /// </summary>
        public const int MetadataSettleMs = 250;

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
            return !MustNotRewindPlayingScrubber || !playing
                ? incomingSeconds
                : !incomingReportedChange
                ? incomingSeconds + PlayingPositionJitterSeconds < committedSeconds ? committedSeconds : incomingSeconds
                : LooksLikeNewTrackPosition(committedSeconds, incomingSeconds)
                ? incomingSeconds
                : incomingSeconds + PlayingPositionJitterSeconds >= committedSeconds ? incomingSeconds : committedSeconds;
        }
    }
}
