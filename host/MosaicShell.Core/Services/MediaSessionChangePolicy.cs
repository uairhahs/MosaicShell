namespace MosaicShell.Core.Services;

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

    public static bool LooksLikeNewTrackPosition(double prevPositionSeconds, double nextPositionSeconds)
    {
        if (prevPositionSeconds >= MinPrevSecondsForRestart
            && nextPositionSeconds <= MaxNextSecondsForRestart)
            return true;

        return prevPositionSeconds - nextPositionSeconds >= MinBackwardJumpSeconds;
    }
}
