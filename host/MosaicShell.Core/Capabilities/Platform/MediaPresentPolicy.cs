namespace MosaicShell.Core.Capabilities.Platform
{
    /// <summary>
    /// Shell-hook and early SMTC ticks can present before title/art catch up; one-shot settle refresh.
    /// </summary>
    public static class MediaPresentPolicy
    {
        public const bool MustPumpBeforeMediaPresent = true;
        public const int PresentSettleMs = 450;

        public static bool ShouldSchedulePresentSettle(string kind, FlyoutSyncAction action)
        {
            return kind.Equals("media", StringComparison.OrdinalIgnoreCase)
            && action == FlyoutSyncAction.Present;
        }

        public static bool ShouldPumpBeforeShellMediaPresent => MustPumpBeforeMediaPresent;
    }
}
