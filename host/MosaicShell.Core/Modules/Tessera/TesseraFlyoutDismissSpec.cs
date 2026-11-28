namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// The "Dismiss" link a Tessera flyout shows while the pointer is over it. Clicks outside a flyout pass through
    /// (<see cref="TesseraFocusDimPolicy.LayerPassesClicksThrough"/>), so this is how someone who has used the flyout
    /// sends it away at once. It sits just outside the cluster, under its bottom-right edge like a toast's trailing
    /// action, so it covers none of the flyout; the styles copy OS pop-ups that have nothing like it, so it never shows
    /// at rest.
    /// </summary>
    public static class TesseraFlyoutDismissSpec
    {
        /// <summary>Its text, and its accessible name.</summary>
        public const string Label = "Dismiss";

        /// <summary>Pill height, in layout units (scaled with the flyout scale).</summary>
        public const double HeightDip = 24;

        public const double PaddingXDip = 10;

        public const double FontSizeDip = 12;

        /// <summary>Gap between the cluster's edge and the pill.</summary>
        public const double GapDip = 6;

        /// <summary>
        /// The pill sits on the desktop, so a dark translucent fill under white text reads on any wallpaper without
        /// reading the backdrop. Alphas at rest, hovered and pressed.
        /// </summary>
        public const byte PillAlpha = 0xA0;
        public const byte PillHoverAlpha = 0xC8;
        public const byte PillPressedAlpha = 0xE6;

        /// <summary>Fade in and out as the pointer arrives and leaves.</summary>
        public const int FadeMs = 120;

        /// <summary>
        /// After the pointer leaves the flyout or the link, the link stays (and stays clickable) this long before it
        /// hides, and stays if the pointer comes back. It carries the pointer across the gap to the link, and over the
        /// leave-and-return a flyout can report with the pointer held still (Windows, stacked, 220 to 310 ms apart).
        /// Shorter than <see cref="TesseraFlyoutDismissPolicy.PointerLeaveDismissMs"/>, so it is gone before the flyout.
        /// </summary>
        public const int HideGraceMs = 400;

        /// <summary>
        /// Acts on release inside the link, so a press can be cancelled by sliding off, as OS buttons do. A property, not
        /// a const, so the Host's branch on it is not folded away.
        /// </summary>
        public static bool ActsOnRelease => true;

        /// <summary>Status chips (lock keys, airplane mode) are one-line notices that leave on their own; they get none.</summary>
        public static bool ShowsOn(string? kind)
        {
            return !TesseraStatusFlyoutPolicy.IsStatusKind(kind);
        }

        /// <summary>Shown only while the pointer is over a flyout that is up; never on one that is leaving.</summary>
        public static bool IsVisible(bool hovered, TesseraFlyoutPhase phase)
        {
            return hovered && phase is TesseraFlyoutPhase.Entering or TesseraFlyoutPhase.Shown;
        }

        /// <summary>A flyout window's place on screen, in physical pixels.</summary>
        public readonly record struct PanelBounds(int X, int Y, int Width, int Height);

        /// <summary>The cluster's outline: the union of its panels (one for a single-window flyout); null when there are none.</summary>
        public static PanelBounds? ResolveClusterBounds(IReadOnlyList<PanelBounds> panels)
        {
            if (panels.Count == 0)
            {
                return null;
            }

            int left = panels.Min(static p => p.X);
            int top = panels.Min(static p => p.Y);
            int right = panels.Max(static p => p.X + p.Width);
            int bottom = panels.Max(static p => p.Y + p.Height);
            return new PanelBounds(left, top, right - left, bottom - top);
        }

        /// <summary>
        /// Where the link goes, in physical pixels: right-aligned with the cluster, <paramref name="gapPx"/> below it, or
        /// the same distance above it when the work area has no room below (a pop-up in the bottom-right corner).
        /// </summary>
        public static (int X, int Y) ResolvePlacement(
            PanelBounds cluster,
            int linkWidthPx,
            int linkHeightPx,
            int gapPx,
            int workAreaTopPx,
            int workAreaBottomPx)
        {
            int x = cluster.X + cluster.Width - linkWidthPx;
            int below = cluster.Y + cluster.Height + gapPx;
            int y = below + linkHeightPx <= workAreaBottomPx || cluster.Y - gapPx - linkHeightPx < workAreaTopPx
                ? below
                : cluster.Y - gapPx - linkHeightPx;
            return (x, y);
        }
    }
}
