namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// Scroll distance, speed and dwell for the Tessera title marquee. The Host measures two numbers
    /// (the new title's width and the width its layout grants) and reads everything else here.
    /// <para>
    /// The distance is a function of those two numbers only. It must never use the width of the
    /// previous content: after a track skip the viewport can still hold an empty title, and
    /// measuring that made a new title scroll almost entirely out of view (audit F01).
    /// </para>
    /// <para>
    /// Speed is in characters per second, scaled by how long the flyout stays open. The slow end
    /// matches Android's TextView marquee (30 dp/s, about 4 cps for UI text) for long-lived or
    /// persistent flyouts; the fast end, for the shortest dismiss window, stays inside the
    /// comfortable 12 to 20 cps subtitle band. A title whose overflow would take longer than
    /// <see cref="MaxScrollDurationMs"/> speeds up just enough to fit, capped at
    /// <see cref="LengthDrivenMaxCps"/>. Dwell before and after the scroll also shortens for short
    /// windows, within fixed bounds.
    /// </para>
    /// </summary>
    public static class TesseraMarqueeSpec
    {
        public const int TickMs = 30;

        public const double MinCps = 4;
        public const double MaxCps = 14;

        /// <summary>Target ceiling for one-way reveal time; only long overflows reach it.</summary>
        public const double MaxScrollDurationMs = 6000;

        /// <summary>Fastest the length-driven speed-up may go: the top of the comfortable band.</summary>
        public const double LengthDrivenMaxCps = 20;

        /// <summary>Average glyph width as a fraction of font size, for proportional Latin text.</summary>
        public const double AvgCharWidthEm = 0.55;

        public const double MinStartDwellMs = 300;
        public const double MaxStartDwellMs = 1200;
        public const double MinEndDwellMs = 250;
        public const double MaxEndDwellMs = 900;

        /// <summary>Settings floor for the auto-dismiss window (see TesseraAutoDismissSeconds).</summary>
        public const double ShortWindowMs = 500;

        /// <summary>
        /// Window at or above which speed and dwell are at their relaxed end; also the budget assumed
        /// for a persistent flyout (no auto-dismiss).
        /// </summary>
        public const double RelaxedWindowMs = 4000;

        /// <summary>Granted-width changes smaller than this do not restart the marquee.</summary>
        public const double WidthToleranceDip = 0.5;

        /// <summary>
        /// The width the title may occupy: what the layout grants, capped at the caller's
        /// <paramref name="maxWidth"/>. An unknown (not yet measured), unbounded or non-positive
        /// grant falls back to the cap.
        /// </summary>
        public static double AvailableWidth(double maxWidth, double grantedWidth)
        {
            return double.IsFinite(grantedWidth) && grantedWidth > 0
                ? Math.Min(maxWidth, grantedWidth)
                : maxWidth;
        }

        /// <summary>How far the full title must slide to show its end; 0 when it fits.</summary>
        public static double ScrollDistance(double textWidth, double availableWidth)
        {
            double distance = textWidth - availableWidth;
            return distance > 0 ? distance : 0;
        }

        /// <summary>True when the available width moved enough that the distance must be re-derived.</summary>
        public static bool NeedsRederive(double previousAvailable, double currentAvailable)
        {
            return !double.IsFinite(previousAvailable)
                || Math.Abs(currentAvailable - previousAvailable) >= WidthToleranceDip;
        }

        public readonly record struct Timing(int StartDwellTicks, int EndDwellTicks, double PixelsPerTick);

        public static Timing ResolveTiming(int autoDismissMs, double fontSize, double distance)
        {
            double budgetMs = autoDismissMs > 0 ? autoDismissMs : RelaxedWindowMs;
            double urgency = Math.Clamp((RelaxedWindowMs - budgetMs) / (RelaxedWindowMs - ShortWindowMs), 0, 1);
            int startDwellTicks = (int)Math.Round(Math.Clamp(budgetMs * 0.3, MinStartDwellMs, MaxStartDwellMs) / TickMs);
            int endDwellTicks = (int)Math.Round(Math.Clamp(budgetMs * 0.2, MinEndDwellMs, MaxEndDwellMs) / TickMs);

            double pxPerChar = fontSize * AvgCharWidthEm;
            double urgencyPxPerSecond = (MinCps + ((MaxCps - MinCps) * urgency)) * pxPerChar;
            double urgencyDurationMs = distance / urgencyPxPerSecond * 1000.0;
            double pxPerSecond = urgencyDurationMs > MaxScrollDurationMs
                ? Math.Min(distance / (MaxScrollDurationMs / 1000.0), LengthDrivenMaxCps * pxPerChar)
                : urgencyPxPerSecond;

            return new Timing(startDwellTicks, endDwellTicks, pxPerSecond * TickMs / 1000.0);
        }
    }
}
