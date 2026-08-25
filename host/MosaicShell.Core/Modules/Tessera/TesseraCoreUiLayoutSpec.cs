namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// CoreUI single-HWND dips. YourFlyouts CoreUI.inc uses full skin W for MediaC;
/// Host extra transport tile sits in the same wrapped row.
/// </summary>
public static class TesseraCoreUiLayoutSpec
{
    public const double WidthDip = 400;
    public const double GapDip = 6;
    public const double VolumeHeightDip = 58;
    public const double MediaHeightDip = 150;
    public const double PadDip = 6;
    public const double DeviceDip = 58;
    public const double TransportWidthDip = 58;

    /// <summary>Host must pass this into WrapCoreUi, not the art column alone.</summary>
    public const bool MediaLayoutMustUseWrappedRowWidth = true;

    /// <summary>Signed CoreUI pause control is a circle, not a rounded square.</summary>
    public const double PlayButtonDip = 24;

    public static double PlayButtonCornerRadiusDip => PlayButtonDip / 2;

    public static double InnerRowWidthDip => WidthDip - 2 * GapDip;

    public static double ArtColumnWidthDip =>
        InnerRowWidthDip - GapDip - TransportWidthDip;

    public static double RestHeightDip =>
        PadDip + VolumeHeightDip + GapDip + MediaHeightDip + PadDip;
}
