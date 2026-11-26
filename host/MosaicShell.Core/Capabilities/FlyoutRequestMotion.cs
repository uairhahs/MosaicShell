namespace MosaicShell.Core.Capabilities
{
    /// <summary>
    /// The motion half of a <see cref="FlyoutRequest"/>: how it enters and leaves (<c>Ani</c>, <c>AniDir</c>,
    /// <c>AniEase</c>, <c>AniSteps</c>, <c>AnimationDisplacement</c>). Each stacked flyout window animates from its own
    /// request, and a live patch or revive used to hand the new request to the Volume window only, so the Media window
    /// kept playing the look it was built with (a Spring wobble after the user had picked a plain fade).
    /// </summary>
    public static class FlyoutRequestMotion
    {
        /// <summary><paramref name="target"/> with the motion of <paramref name="source"/>; everything else is kept.</summary>
        public static FlyoutRequest Adopt(FlyoutRequest target, FlyoutRequest source)
        {
            return target with
            {
                Ani = source.Ani,
                AniDir = source.AniDir,
                AniEase = source.AniEase,
                AniSteps = source.AniSteps,
                AnimationDisplacement = source.AnimationDisplacement,
            };
        }

        /// <summary>Whether two requests would play the same motion.</summary>
        public static bool SameMotion(FlyoutRequest a, FlyoutRequest b)
        {
            return a.Ani == b.Ani
                   && string.Equals(a.AniDir, b.AniDir, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(a.AniEase, b.AniEase, StringComparison.OrdinalIgnoreCase)
                   && a.AniSteps == b.AniSteps
                   && a.AnimationDisplacement == b.AnimationDisplacement;
        }
    }
}
