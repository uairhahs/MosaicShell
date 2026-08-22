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

/// <summary>Host Win11 metrics - quiet horizontal chip.</summary>
public static class TesseraWin11Metrics
{
    public const double Width = 300;
    public const double VolumeHeight = 48;
    public const double MediaHeight = 156;
    public const double Pad = 12;
    public const double CornerRadius = 14;
}

/// <summary>Host Center metrics - round quiet card.</summary>
public static class TesseraCenterMetrics
{
    public const double Size = 128;
    public const double CornerRadius = 24;
    public const double GlyphSize = 28;
    public const double PercentSize = 20;
}
