using System.Globalization;

namespace MosaicShell.Core.Services.Kde
{
    /// <summary>
    /// Whether the active window is fullscreen, on KDE. The Host is an XWayland client and cannot see native Wayland
    /// windows, so each query loads <see cref="Script"/> into KWin once: it reports the active window's facts to
    /// <see cref="ProbeBusName"/>, which the Host owns, and does nothing else. The decision is made here, by the rule
    /// Windows' probe uses: the window covers its screen (within 2 px). Kept pure so it is tested on every OS.
    /// </summary>
    public static class KWinFullscreenPolicy
    {
        /// <summary>Stable, so the Host can unload a copy a crashed query left loaded.</summary>
        public const string ScriptName = "mosaicshell-fullscreen-probe";

        public const string ProbeBusName = "org.mosaicshell.FullscreenProbe";
        public const string ProbePath = "/org/mosaicshell/FullscreenProbe";
        public const string ProbeInterface = "org.mosaicshell.FullscreenProbe";

        /// <summary>One string argument: the facts (see <see cref="IsFullscreen"/>) or <c>none</c>.</summary>
        public const string ReportMethod = "Report";

        /// <summary>How far inside its screen's edges a window may stop and still cover it, as on Windows.</summary>
        private const double TolerancePx = 2;

        public static string Script { get; } = $$"""
            // MosaicShell: reports the active window once, for the Host's fullscreen check, then does nothing.
            (function () {
                var w = workspace.activeWindow;
                var report = "none";
                if (w && w.output) {
                    var f = w.frameGeometry;
                    var o = w.output.geometry;
                    report = "full=" + (w.fullScreen ? 1 : 0) + ";desktop=" + (w.desktopWindow ? 1 : 0)
                        + ";frame=" + f.x + "," + f.y + "," + f.width + "," + f.height
                        + ";screen=" + o.x + "," + o.y + "," + o.width + "," + o.height;
                }

                callDBus("{{ProbeBusName}}", "{{ProbePath}}", "{{ProbeInterface}}", "{{ReportMethod}}", report);
            })();
            """;

        /// <summary>
        /// Reads <c>full=0|1;desktop=0|1;frame=x,y,w,h;screen=x,y,w,h</c>: fullscreen when KWin says so or the frame covers
        /// its screen, never for the desktop. No active window, or anything unreadable, is not fullscreen.
        /// </summary>
        public static bool IsFullscreen(string? report)
        {
            if (string.IsNullOrEmpty(report))
            {
                return false;
            }

            Dictionary<string, string> facts = [];
            foreach (string part in report.Split(';'))
            {
                int equals = part.IndexOf('=');
                if (equals > 0)
                {
                    facts[part[..equals]] = part[(equals + 1)..];
                }
            }

            return facts.TryGetValue("full", out string? full)
                && facts.TryGetValue("desktop", out string? desktop)
                && TryReadRect(facts.GetValueOrDefault("frame"), out (double X, double Y, double W, double H) frame)
                && TryReadRect(facts.GetValueOrDefault("screen"), out (double X, double Y, double W, double H) screen)
                && desktop != "1"
                && (full == "1" || Covers(frame, screen));
        }

        private static bool Covers((double X, double Y, double W, double H) frame, (double X, double Y, double W, double H) screen)
        {
            return frame.X <= screen.X + TolerancePx
                && frame.Y <= screen.Y + TolerancePx
                && frame.X + frame.W >= screen.X + screen.W - TolerancePx
                && frame.Y + frame.H >= screen.Y + screen.H - TolerancePx;
        }

        private static bool TryReadRect(string? text, out (double X, double Y, double W, double H) rect)
        {
            rect = default;
            string[] parts = text?.Split(',') ?? [];
            double[] values = new double[4];
            if (parts.Length != 4)
            {
                return false;
            }

            for (int i = 0; i < 4; i++)
            {
                if (!double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]))
                {
                    return false;
                }
            }

            rect = (values[0], values[1], values[2], values[3]);
            return true;
        }
    }
}
