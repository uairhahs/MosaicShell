namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// Hub Motion easing preview. Same phase duration as live flyouts so family
/// and In/Out/InOut are visible without a volume key.
/// </summary>
public static class TesseraEasePreviewSpec
{
    public const bool MustReplayWhenEaseOrStepsChange = true;

    public const double TrackWidthDip = 220;
    public const double TrackHeightDip = 22;
    public const double MarkerSizeDip = 12;
    public const double OvershootPadDip = 28;

    public static double TravelDip => TrackWidthDip - MarkerSizeDip - (OvershootPadDip * 2);

    public static int ResolveDurationMs(int aniSteps) =>
        TesseraFlyoutAnimationPolicy.ResolvePhaseDurationMs(aniSteps);
}
