namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// Shared volume slider/ring drag semantics for Tessera live flyouts.
    /// </summary>
    public static class TesseraVolumeAdjustPolicy
    {

        /// <summary>Grace period after user input before ApplyLive may overwrite the control.</summary>
        public static TimeSpan UserAdjustGracePeriod { get; } = TimeSpan.FromMilliseconds(350);

        /// <summary>
        /// Radial <c>Arc</c> sweep is assigned in arrange; Host must invalidate the visual when sweep
        /// changes or the progress stroke appears frozen while the percent label updates.
        /// </summary>
        public const bool VolumeRingArcMustInvalidateVisualOnSweepChange = true;
    }
}
