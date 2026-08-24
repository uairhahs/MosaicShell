using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>Host Fluent metrics - compact HUD (not full YourFlyouts media width).</summary>
public static class TesseraFluentMetrics
{
    public const double VolumeWidth = TesseraFluentLayoutSpec.VolumeWidthDip;
    public const double Height = TesseraFluentLayoutSpec.HeightDip;
    public const double MediaWidth = TesseraFluentLayoutSpec.MediaWidthDip;
    public const double Pad = TesseraFluentLayoutSpec.PadDip;
    public const double LocksWidth = 220;
    public const double LocksHeight = 44;
    /// <summary>Hard cap so media strip stays a small fraction of a typical display.</summary>
    public const double MaxShellWidth = TesseraFluentLayoutSpec.MaxShellWidthDip;
}

/// <summary>Host Win11 metrics; values alias Core TesseraStackedPlacementSpec.</summary>
public static class TesseraWindows11Metrics
{
    public const double Width = TesseraStackedPlacementSpec.Win11WidthDip;
    public const double VolumeHeight = TesseraStackedPlacementSpec.Win11VolumeHeightDip;
    public const double MediaHeight = TesseraStackedPlacementSpec.Win11MediaHeightDip;
    public const double Pad = TesseraStackedPlacementSpec.Win11PadDip;
    public const double CornerRadius = TesseraStackedPlacementSpec.Win11CornerRadiusDip;
}

/// <summary>Host Center metrics - round quiet card.</summary>
public static class TesseraSquareMetrics
{
    public const double Size = 128;
    public const double CornerRadius = 24;
    public const double GlyphSize = 28;
    public const double PercentSize = 20;
}
