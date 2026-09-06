namespace MosaicShell.Core.Capabilities.Platform
{
    /// <summary>
    /// Present (cold/structural/revive) vs Patch (in-place live bind) for capability flyouts.
    /// </summary>
    public static class FlyoutRefreshPolicy
    {
        public static FlyoutSyncAction ResolvePresentation(
            FlyoutSyncTrigger trigger,
            bool isEffectivelyShowing,
            string openKind,
            string nextKind,
            string? openStyle,
            string? nextStyle,
            bool enableMediaFlyouts)
        {
            if (!isEffectivelyShowing)
            {
                return FlyoutSyncAction.Present;
            }

            if (!string.Equals(openKind, nextKind, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(openStyle ?? "", nextStyle ?? "", StringComparison.OrdinalIgnoreCase))
            {
                return FlyoutSyncAction.Present;
            }

            return trigger == FlyoutSyncTrigger.MediaSession
                && !enableMediaFlyouts
                && MediaFlyoutRouter.IsStripKind(openKind)
                ? FlyoutSyncAction.Patch
                : trigger == FlyoutSyncTrigger.ShellMedia
                && enableMediaFlyouts
                && openKind.Equals("media", StringComparison.OrdinalIgnoreCase)
                ? FlyoutSyncAction.Patch
                : trigger == FlyoutSyncTrigger.MediaSession
                && enableMediaFlyouts
                && openKind.Equals("media", StringComparison.OrdinalIgnoreCase)
                ? FlyoutSyncAction.Patch
                : FlyoutSyncAction.Patch;
        }
    }
}
