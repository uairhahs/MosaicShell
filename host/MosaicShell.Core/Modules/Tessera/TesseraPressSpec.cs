namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// How a flyout button answers a press (audit U31, research 6.5): it sinks by a fixed inset toward the press point
    /// and takes a stronger fill than hover, and it acts when released inside itself, so sliding off cancels. The volume
    /// track is not a button and keeps acting on press.
    /// </summary>
    public static class TesseraPressSpec
    {
        /// <summary>How far each edge moves in when pressed. A ratio would be invisible on a 22 dip icon.</summary>
        public const double InsetDip = 1.25;

        public const double MinScale = 0.94;
        public const double MaxScale = 0.99;

        /// <summary>The scale origin sits this share of the way from the centre toward the press point.</summary>
        public const double AnchorShare = 0.3;

        public const int SinkMs = 80;
        public const int RecoverMs = 160;

        /// <summary>A hover fill closer than this to the normal fill, on every channel, is raised to it.</summary>
        public const int MinHoverStep = 14;

        /// <summary>Pressed goes past hover by this share of the hover step, so pressed never looks lighter than hover.</summary>
        public const double PressedExtra = 0.8;

        /// <summary>The scale that moves each edge in by <see cref="InsetDip"/>, kept inside the clamp.</summary>
        public static double SinkScale(double width, double height)
        {
            double side = Math.Min(width, height);
            return side <= 0 ? MaxScale : Math.Clamp(1 - (2 * InsetDip / side), MinScale, MaxScale);
        }

        /// <summary>The scale origin, relative to the control (0..1 each way), for a press at the given point.</summary>
        public static (double X, double Y) Anchor(double pressX, double pressY, double width, double height)
        {
            if (width <= 0 || height <= 0)
            {
                return (0.5, 0.5);
            }

            double rx = Math.Clamp(pressX / width, 0, 1);
            double ry = Math.Clamp(pressY / height, 0, 1);
            return (0.5 + (AnchorShare * (rx - 0.5)), 0.5 + (AnchorShare * (ry - 0.5)));
        }

        /// <summary>Whether a release at the given point, in the control's own coordinates, acts.</summary>
        public static bool ReleasedInside(double x, double y, double width, double height)
        {
            return x >= 0 && y >= 0 && x <= width && y <= height;
        }

        /// <summary>
        /// The hover fill to draw: the style's own, unless it is so close to an opaque normal fill that it does not read
        /// (white to #F5F5FA on Core UI's play button); then it is moved <see cref="MinHoverStep"/> away from normal,
        /// darker on a light fill and lighter on a dark one.
        /// </summary>
        public static uint VisibleHover(uint normalArgb, uint hoverArgb)
        {
            if (Alpha(normalArgb) != 255 || Alpha(hoverArgb) != 255 || MaxStep(normalArgb, hoverArgb) >= MinHoverStep)
            {
                return hoverArgb;
            }

            int lum = (Red(normalArgb) + Green(normalArgb) + Blue(normalArgb)) / 3;
            int step = lum >= 128 ? -MinHoverStep : MinHoverStep;
            return Argb(255, Red(normalArgb) + step, Green(normalArgb) + step, Blue(normalArgb) + step);
        }

        /// <summary>
        /// The pressed fill: past hover, away from normal, by <see cref="PressedExtra"/> of the hover step on every
        /// channel and alpha. A transparent normal fill has no colour of its own, so only alpha moves then.
        /// </summary>
        public static uint PressedFill(uint normalArgb, uint hoverArgb)
        {
            uint normal = Alpha(normalArgb) == 0 ? (hoverArgb & 0x00FFFFFF) : normalArgb;
            return Argb(
                Push(Alpha(normal), Alpha(hoverArgb)),
                Push(Red(normal), Red(hoverArgb)),
                Push(Green(normal), Green(hoverArgb)),
                Push(Blue(normal), Blue(hoverArgb)));
        }

        private static int Push(int from, int to)
        {
            return (int)Math.Round(to + ((to - from) * PressedExtra));
        }

        private static int MaxStep(uint a, uint b)
        {
            return Math.Max(Math.Abs(Red(a) - Red(b)), Math.Max(Math.Abs(Green(a) - Green(b)), Math.Abs(Blue(a) - Blue(b))));
        }

        private static int Alpha(uint argb)
        {
            return (int)(argb >> 24);
        }

        private static int Red(uint argb)
        {
            return (int)((argb >> 16) & 0xFF);
        }

        private static int Green(uint argb)
        {
            return (int)((argb >> 8) & 0xFF);
        }

        private static int Blue(uint argb)
        {
            return (int)(argb & 0xFF);
        }

        private static uint Argb(int a, int r, int g, int b)
        {
            return ((uint)Math.Clamp(a, 0, 255) << 24)
                   | ((uint)Math.Clamp(r, 0, 255) << 16)
                   | ((uint)Math.Clamp(g, 0, 255) << 8)
                   | (uint)Math.Clamp(b, 0, 255);
        }
    }

    /// <summary>
    /// A toggle that shows its new state as soon as it is pressed (play/pause, audit U31) and holds it against reports
    /// of the old state for <see cref="HoldMs"/>: a slow player reports the old state once or twice before it flips. A
    /// report of the expected state, or the hold running out, hands the icon back to the player.
    /// </summary>
    public sealed class OptimisticToggle
    {
        public const int HoldMs = 1500;

        private bool? _expected;
        private long _pressedAtMs;

        /// <summary>Records a press on a toggle that showed <paramref name="shownOn"/>; returns the state to show now.</summary>
        public bool Press(bool shownOn, long nowMs)
        {
            _expected = !shownOn;
            _pressedAtMs = nowMs;
            return !shownOn;
        }

        /// <summary>The state to show for a report from the player.</summary>
        public bool Resolve(bool reportedOn, long nowMs)
        {
            if (_expected is not bool expected)
            {
                return reportedOn;
            }

            if (reportedOn == expected || nowMs - _pressedAtMs >= HoldMs)
            {
                _expected = null;
                return reportedOn;
            }

            return expected;
        }
    }
}
