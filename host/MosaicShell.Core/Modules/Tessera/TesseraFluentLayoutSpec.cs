namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>Fluent HUD dips. Host TesseraFluentMetrics aliases these constants.</summary>
    public static class TesseraFluentLayoutSpec
    {
        public const double VolumeWidthDip = 72;
        public const double HeightDip = 176;
        /// <summary>
        /// Width of the media card itself, and so of the reveal host layout, the window sized to it, the
        /// HWND region cut from that window and the stacked media panel. The compact HUD is narrower than
        /// the full YourFlyouts media column (340); a panel narrower than this value leaves acrylic showing
        /// to its right, so Host panels must use it unmodified (pinned by FluentMediaCardWidthTests).
        /// </summary>
        public const double MediaWidthDip = 324;
        public const double PadDip = 14;
        public const double MaxShellWidthDip = 420;

        /// <summary>
        /// MediaB column: layout spacing + 1px line. Collapsed shell must include this so
        /// show can wipe the divider in place like hide, not clip it then pop a full line.
        /// </summary>
        public static double DividerColumnWidthDip => TesseraStackedPlacementPolicy.FluentDividerDip;
    }
}
