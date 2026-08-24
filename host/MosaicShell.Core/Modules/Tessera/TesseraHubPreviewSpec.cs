namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// Hub Tessera style preview chrome. Host Viewbox reads these dips; do not hardcode 100.
/// Floor is Windows 11 rest card height so DownOnly scale does not crush the layout.
/// </summary>
public static class TesseraHubPreviewSpec
{
    public static double Win11RestHeightDip =>
        TesseraStackedPlacementSpec.Win11VolumeHeightDip
        + TesseraStackedPlacementSpec.Win11MediaHeightDip;

    /// <summary>Viewbox max height; at least the Win11 rest card.</summary>
    public static double MaxHeightDip => Win11RestHeightDip;
}
