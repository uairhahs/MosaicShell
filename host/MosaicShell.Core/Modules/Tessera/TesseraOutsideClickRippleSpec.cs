namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// The glint at the point of an outside click that dismissed Tessera: a soft holographic ring in the logo's colours
    /// that contracts onto the click while a sheen sweeps round it, like light on iridescent glass mosaic. The ring is one blurred stroke whose colour changes gradually round it; separate
    /// tiles read as harshly segmented. On every platform the layer behind the flyout
    /// swallows that click (popup-menu style), so the glint is the sign that the click did something. It is drawn in its
    /// own small click-through window, shown at the click, because the layer closes as the dismiss begins.
    /// </summary>
    public static class TesseraOutsideClickRippleSpec
    {
        public const int DurationMs = 520;

        /// <summary>Redraw interval while the glint plays (about 60 frames a second).</summary>
        public const int FrameIntervalMs = 16;

        /// <summary>The ring starts wide and contracts onto the click point.</summary>
        public const double StartDiameterDip = 64;
        public const double EndDiameterDip = 10;

        /// <summary>Ring stroke, thinning as the ring closes.</summary>
        public const double StrokeStartDip = 5;
        public const double StrokeEndDip = 3;

        /// <summary>Blur on the ring, so its edges read as a glimmer rather than a line.</summary>
        public const double BlurRadiusDip = 2.5;

        /// <summary>Colour samples round the ring for its conic gradient.</summary>
        public const int GradientStops = 48;

        /// <summary>Overall opacity at the click; it falls to 0 by the end.</summary>
        public const double PeakOpacity = 0.9;

        /// <summary>Square window side: the widest ring, its stroke and its blur, with a little to spare.</summary>
        public const double WindowSizeDip = StartDiameterDip + StrokeStartDip + (4 * BlurRadiusDip) + 4;

        /// <summary>Where the glint starts (degrees clockwise from the right, screen y down): upper left, the sun.</summary>
        public const double GlintStartDeg = 225;

        /// <summary>How far the glint travels round the ring over the whole duration.</summary>
        public const double GlintSweepDeg = 300;

        /// <summary>Angular half-width of the sheen's light (Gaussian falloff); wide, so it reads as a soft sheen.</summary>
        public const double GlintWidthDeg = 56;

        /// <summary>A gentle wave of light travelling along the ring on top of the sweep.</summary>
        public const double ShimmerAmount = 0.2;
        public const double ShimmerWaves = 2;
        public const double ShimmerCycles = 1.5;

        /// <summary>How far each logo colour is lightened toward white: holographic pastel, still clearly the hue.</summary>
        public const double PastelShare = 0.3;

        /// <summary>How far the colours flow round the ring over the whole duration, as iridescence shifts with angle.</summary>
        public const double IridescenceDeg = 140;

        /// <summary>The glow inside the ring, as a share of the ring's opacity.</summary>
        public const double GlowShare = 0.45;

        /// <summary>The logo's tile colours: violet, amber, olive, green, sky blue, indigo.</summary>
        public static readonly IReadOnlyList<(byte R, byte G, byte B)> Palette =
        [
            (0x9D, 0x14, 0xE6),
            (0xB4, 0x83, 0x31),
            (0xBB, 0xB5, 0x55),
            (0x5B, 0xE0, 0x77),
            (0x66, 0xB6, 0xE8),
            (0x5B, 0x62, 0xE6),
        ];

        /// <summary>Where each <see cref="Palette"/> colour sits round the ring: where its tile sits in the logo.</summary>
        private static readonly double[] AnchorAnglesDeg = [315, 45, 90, 135, 225, 270];

        /// <summary>Cool white the ring turns toward as the sheen catches it.</summary>
        public static readonly (byte R, byte G, byte B) Highlight = (0xF7, 0xF5, 0xFF);

        /// <summary>The glow's colour: the logo's indigo, softened well toward white.</summary>
        public static (byte R, byte G, byte B) GlowColor => Lighten(Palette[5], 0.45);

        /// <summary>Wherever the focus dim layer can be drawn translucent, the glint window can be too.</summary>
        public static bool ShouldPlay(FocusDimRoute route)
        {
            return route != FocusDimRoute.None;
        }

        public static double ProgressAt(double elapsedMs)
        {
            return Math.Clamp(elapsedMs / DurationMs, 0, 1);
        }

        /// <summary>
        /// Progress counted from the first frame that reached the screen (<paramref name="firstFrameMs"/>, null until
        /// then), not from Show: a cold-shown composited window first appeared 100 to 150 ms after Show (Windows
        /// probe, 2026-11-24), a quarter of the glint.
        /// </summary>
        public static double ProgressSinceFirstFrame(double? firstFrameMs, double nowMs)
        {
            if (firstFrameMs is double start)
            {
                return ProgressAt(nowMs - start);
            }

            // No frame reported: count from Show after a wait, so the window still finishes and closes.
            return nowMs >= FirstFrameWaitMs ? ProgressAt(nowMs - FirstFrameWaitMs) : 0;
        }

        /// <summary>How long the glint waits for its first frame before it counts from Show anyway.</summary>
        public const double FirstFrameWaitMs = 250;

        /// <summary>Ring diameter, easing out (cubic) from <see cref="StartDiameterDip"/> down to <see cref="EndDiameterDip"/>.</summary>
        public static double DiameterAt(double progress)
        {
            return StartDiameterDip + ((EndDiameterDip - StartDiameterDip) * EaseOut(progress));
        }

        /// <summary>Overall opacity, from <see cref="PeakOpacity"/> to 0 (quadratic, so most of the fade is early).</summary>
        public static double OpacityAt(double progress)
        {
            double p = Math.Clamp(progress, 0, 1);
            return PeakOpacity * (1 - p) * (1 - p);
        }

        public static double GlowOpacityAt(double progress)
        {
            return OpacityAt(progress) * GlowShare;
        }

        public static double StrokeAt(double progress)
        {
            return StrokeStartDip + ((StrokeEndDip - StrokeStartDip) * EaseOut(progress));
        }

        public static double AnchorAngleDeg(int paletteIndex)
        {
            return AnchorAnglesDeg[paletteIndex];
        }

        public static (byte R, byte G, byte B) Pastel((byte R, byte G, byte B) color)
        {
            return Lighten(color, PastelShare);
        }

        /// <summary>
        /// The ring's own colour at <paramref name="angleDeg"/> (clockwise from the right, screen y down): the pastel
        /// logo colours blended linearly between their anchors, so it has no edges, flowing round by
        /// <see cref="IridescenceDeg"/> over the duration.
        /// </summary>
        public static (byte R, byte G, byte B) BaseColorAt(double angleDeg, double progress)
        {
            double angle = Normalize(angleDeg - (IridescenceDeg * Math.Clamp(progress, 0, 1)));
            int count = AnchorAnglesDeg.Length;
            int[] order = [.. Enumerable.Range(0, count).OrderBy(AnchorAngleDeg)];
            for (int n = 0; n < count; n++)
            {
                int from = order[n];
                int to = order[(n + 1) % count];
                double start = AnchorAngleDeg(from);
                double span = Normalize(AnchorAngleDeg(to) - start);
                double into = Normalize(angle - start);
                if (into <= span)
                {
                    double t = span > 0 ? into / span : 0;
                    (byte r0, byte g0, byte b0) = Pastel(Palette[from]);
                    (byte r1, byte g1, byte b1) = Pastel(Palette[to]);
                    return (Mix(r0, r1, t), Mix(g0, g1, t), Mix(b0, b1, t));
                }
            }

            return Pastel(Palette[order[0]]);
        }

        /// <summary>The sheen's position round the ring, easing in and out so it reads as a passing light.</summary>
        public static double GlintAngleDeg(double progress)
        {
            double p = Math.Clamp(progress, 0, 1);
            double eased = p * p * (3 - (2 * p));
            return GlintStartDeg + (GlintSweepDeg * eased);
        }

        /// <summary>Shortest distance between two angles, 0 to 180.</summary>
        public static double AngularDistanceDeg(double a, double b)
        {
            double d = Math.Abs((a - b) % 360);
            return d > 180 ? 360 - d : d;
        }

        /// <summary>How much light the ring catches at <paramref name="angleDeg"/>, 0 to 1: the sweep plus the shimmer.</summary>
        public static double SheenAt(double angleDeg, double progress)
        {
            double p = Math.Clamp(progress, 0, 1);
            double d = AngularDistanceDeg(angleDeg, GlintAngleDeg(p)) / GlintWidthDeg;
            double sweep = Math.Exp(-(d * d));
            double wave = (ShimmerWaves * angleDeg * Math.PI / 180) + (2 * Math.PI * ShimmerCycles * p);
            double shimmer = ShimmerAmount * (0.5 + (0.5 * Math.Sin(wave)));
            return Math.Clamp(sweep + shimmer, 0, 1);
        }

        /// <summary>The ring's colour at <paramref name="angleDeg"/> as the sheen lights it.</summary>
        public static (byte R, byte G, byte B) RingColorAt(double angleDeg, double progress)
        {
            return MixToHighlight(BaseColorAt(angleDeg, progress), SheenAt(angleDeg, progress));
        }

        /// <summary><paramref name="color"/> moved <paramref name="shine"/> (0 to 1) of the way toward <see cref="Highlight"/>.</summary>
        public static (byte R, byte G, byte B) MixToHighlight((byte R, byte G, byte B) color, double shine)
        {
            double t = Math.Clamp(shine, 0, 1);
            return (Mix(color.R, Highlight.R, t), Mix(color.G, Highlight.G, t), Mix(color.B, Highlight.B, t));
        }

        /// <summary>Window side in pixels on a screen with <paramref name="scaling"/>, rounded up to even so it centers.</summary>
        public static int WindowSizePx(double scaling)
        {
            double scale = scaling > 0.1 ? scaling : 1.0;
            int size = (int)Math.Ceiling(WindowSizeDip * scale);
            return size % 2 == 0 ? size : size + 1;
        }

        /// <summary>Top-left pixel of the window centered on the click.</summary>
        public static (int X, int Y) WindowOrigin(int clickX, int clickY, double scaling)
        {
            int half = WindowSizePx(scaling) / 2;
            return (clickX - half, clickY - half);
        }

        private static double EaseOut(double progress)
        {
            double p = Math.Clamp(progress, 0, 1);
            return 1 - Math.Pow(1 - p, 3);
        }

        private static (byte R, byte G, byte B) Lighten((byte R, byte G, byte B) color, double share)
        {
            return (Mix(color.R, 255, share), Mix(color.G, 255, share), Mix(color.B, 255, share));
        }

        private static double Normalize(double angleDeg)
        {
            return ((angleDeg % 360) + 360) % 360;
        }

        private static byte Mix(byte from, byte to, double t)
        {
            return (byte)Math.Round(from + ((to - from) * t));
        }
    }
}
