namespace MosaicShell.Core.HubShell
{
    /// <summary>A rectangle in physical pixels: left, top, right and bottom edges.</summary>
    public readonly record struct PixelBox(int Left, int Top, int Right, int Bottom);

    /// <summary>
    /// Where the tray's quick panel opens: centred over the pointer that clicked the icon and just above it, then kept
    /// inside the work area. That puts it above a bottom taskbar, below a top one and beside a side one without knowing
    /// where the taskbar is. Without a pointer position (Wayland gives none) it takes the work area's bottom right.
    /// </summary>
    public static class TrayPanelPlacement
    {
        /// <summary>The gap between the panel and the work area's edge, and between the panel and the pointer.</summary>
        public const int MarginPx = 12;

        public static (int X, int Y) TopLeft(int? pointerX, int? pointerY, PixelBox work, int widthPx, int heightPx)
        {
            int maxX = work.Right - widthPx - MarginPx;
            int maxY = work.Bottom - heightPx - MarginPx;
            int minX = work.Left + MarginPx;
            int minY = work.Top + MarginPx;
            if (pointerX is not int px || pointerY is not int py)
            {
                return (Math.Max(minX, maxX), Math.Max(minY, maxY));
            }

            int x = Math.Clamp(px - (widthPx / 2), minX, Math.Max(minX, maxX));
            int y = Math.Clamp(py - heightPx - MarginPx, minY, Math.Max(minY, maxY));
            return (x, y);
        }
    }
}
