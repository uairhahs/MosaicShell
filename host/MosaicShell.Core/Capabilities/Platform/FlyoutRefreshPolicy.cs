namespace MosaicShell.Core.Capabilities.Platform
{
    /// <summary>
    /// Present (cold/structural/revive) vs Patch (in-place live bind) for capability flyouts.
    /// </summary>
    public static class FlyoutRefreshPolicy
    {
        public static FlyoutSyncAction ResolvePresentation(
            bool isEffectivelyShowing,
            string openKind,
            string nextKind,
            string? openStyle,
            string? nextStyle)
        {
            return !isEffectivelyShowing
                ? FlyoutSyncAction.Present
                : !string.Equals(openKind, nextKind, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(openStyle ?? "", nextStyle ?? "", StringComparison.OrdinalIgnoreCase)
                ? FlyoutSyncAction.Present
                : FlyoutSyncAction.Patch;
        }
    }
}
