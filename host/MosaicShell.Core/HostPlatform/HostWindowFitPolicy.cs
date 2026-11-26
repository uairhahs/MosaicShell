namespace MosaicShell.Core.HostPlatform
{
    /// <summary>
    /// How the Hub window's size follows a move to another monitor: it does not, unless it no longer fits. A window keeps
    /// the size the user gave it (in device-independent pixels, so the OS handles a scaling change) and is only shrunk to
    /// the destination's work area, never grown.
    /// </summary>
    /// <remarks>
    /// It used to resize to a fixed share of each monitor's work area on every position event. During a drag that fought
    /// the OS move loop: the window shrank on the smaller monitor, the move loop put the drag-start size back on release,
    /// and the restored size was then remembered as the user's choice (seen 2026-11-26 moving between a 2560x1440 and a
    /// 1920x1200 monitor).
    /// </remarks>
    public static class HostWindowFitPolicy
    {
        /// <summary>Kept clear of the work area's edges when the window has to shrink.</summary>
        public const double EdgeMarginDip = 8;

        /// <summary>A move or screen change must be quiet this long before the fit is checked, so a drag is never resized.</summary>
        public const int SettleMs = 300;

        /// <summary>
        /// The size to give the window: its own size when it fits the work area, otherwise shrunk to fit (each side on its
        /// own), but not below the window's minimum unless the work area itself is smaller.
        /// </summary>
        public static (double Width, double Height) Fit(
            double widthDip, double heightDip, double workWidthDip, double workHeightDip, double minWidthDip, double minHeightDip)
        {
            return (FitSide(widthDip, workWidthDip, minWidthDip), FitSide(heightDip, workHeightDip, minHeightDip));
        }

        /// <summary>Whether <see cref="Fit"/> would change the size.</summary>
        public static bool NeedsFit(
            double widthDip, double heightDip, double workWidthDip, double workHeightDip, double minWidthDip, double minHeightDip)
        {
            (double w, double h) = Fit(widthDip, heightDip, workWidthDip, workHeightDip, minWidthDip, minHeightDip);
            return Math.Abs(w - widthDip) > 0.5 || Math.Abs(h - heightDip) > 0.5;
        }

        private static double FitSide(double size, double work, double min)
        {
            double room = Math.Max(1, work - (2 * EdgeMarginDip));
            return size <= room ? size : Math.Max(Math.Min(min, room), room);
        }
    }
}
