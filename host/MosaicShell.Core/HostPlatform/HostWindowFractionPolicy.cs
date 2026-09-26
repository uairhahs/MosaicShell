namespace MosaicShell.Core.HostPlatform
{
    /// <summary>
    /// Retry bound for the Host window's screen-fraction apply. While DesktopScaling disagrees with
    /// the resolved screen's Scaling, the Host re-queues the apply to let DPI settle. On a window
    /// straddling mixed-DPI monitors the two can disagree permanently, so the wait is capped: after
    /// <see cref="MaxScaleSettleRetries"/> re-queues the apply proceeds with the current scaling.
    /// </summary>
    public static class HostWindowFractionPolicy
    {
        public const int MaxScaleSettleRetries = 3;

        public static bool ShouldWaitForScaleSettle(bool scalingSettled, int pass, int settleRetries)
        {
            return !scalingSettled && pass == 0 && settleRetries < MaxScaleSettleRetries;
        }
    }
}
