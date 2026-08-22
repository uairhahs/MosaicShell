namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>Host Fluent metrics - compact HUD (not full YourFlyouts media width).</summary>
public static class TesseraFluentMetrics
{
    public const double VolumeWidth = 72;
    public const double Height = 176;
    public const double MediaWidth = 340;
    public const double Pad = 14;
    public const double LocksWidth = 220;
    public const double LocksHeight = 44;
    /// <summary>Hard cap so media strip stays a small fraction of a typical display.</summary>
    public const double MaxShellWidth = 420;
}

/// <summary>Host Win11 metrics — YourFlyouts Win11.inc at scale 1.</summary>
public static class TesseraWin11Metrics
{
    public const double Width = 320;
    public const double VolumeHeight = 50;
    public const double MediaHeight = 175;
    public const double Pad = 15;
    public const double CornerRadius = 12;
}

/// <summary>Host Center metrics - round quiet card.</summary>
public static class TesseraCenterMetrics
{
    public const double Size = 128;
    public const double CornerRadius = 24;
    public const double GlyphSize = 28;
    public const double PercentSize = 20;
}
