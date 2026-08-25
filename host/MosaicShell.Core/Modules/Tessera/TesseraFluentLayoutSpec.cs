namespace MosaicShell.Core.Modules.Tessera;

/// <summary>Fluent HUD dips. Host TesseraFluentMetrics aliases these constants.</summary>
public static class TesseraFluentLayoutSpec
{
    public const double VolumeWidthDip = 72;
    public const double HeightDip = 176;
    public const double MediaWidthDip = 340;
    public const double PadDip = 14;
    public const double MaxShellWidthDip = 420;

    /// <summary>
    /// MediaB column: layout spacing + 1px line. Collapsed shell must include this so
    /// show can wipe the divider in place like hide, not clip it then pop a full line.
    /// </summary>
    public static double DividerColumnWidthDip => TesseraStackedPlacementPolicy.FluentDividerDip;
}
