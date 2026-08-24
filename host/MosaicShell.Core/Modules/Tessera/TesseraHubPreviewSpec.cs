namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// Hub Tessera style preview chrome. Host Viewbox reads these dips; do not hardcode 100.
/// Floor is the tallest rest card so DownOnly scale does not crush CoreUI or Win11.
/// </summary>
public static class TesseraHubPreviewSpec
{
    public static double Win11RestHeightDip =>
        TesseraStackedPlacementSpec.Win11VolumeHeightDip
        + TesseraStackedPlacementSpec.Win11MediaHeightDip;

    public static double CoreUiRestHeightDip => TesseraCoreUiLayoutSpec.RestHeightDip;

    public static double FluentRestHeightDip => TesseraFluentLayoutSpec.HeightDip;

    /// <summary>Viewbox max height; at least the tallest rest card among Hub styles.</summary>
    public static double MaxHeightDip =>
        Math.Max(Win11RestHeightDip, Math.Max(CoreUiRestHeightDip, FluentRestHeightDip));
}
