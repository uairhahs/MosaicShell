using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MosaicShell.Core.Capabilities.Platform
{
    /// <summary>
    /// What the flyout pipeline did for a scripted action, counted from its own trace: the calls that
    /// entered it, the routing decisions, how many presents, patches, rebuilds and revives followed, the
    /// motion sequence and the restart counts. A pure parse, so a canned trace can test it. It is the
    /// recorded "before" for the pipeline rewrite, and <see cref="ToGolden"/> is the text that is compared.
    /// </summary>
    public sealed partial record FlyoutTraceSummary(
        int ShowCalls,
        int UpdateCalls,
        int SoftRefreshCalls,
        IReadOnlyList<string> IngressDecisions,
        int Presents,
        int Patches,
        int Rebuilds,
        int Revives,
        IReadOnlyList<string> MotionSequence,
        int MotionRestarts,
        int MotionSuperseded,
        int MarqueeRestarts,
        int MaxVisibleProcessWindows)
    {
        /// <summary>
        /// Reads lines as they appear in flyout.log (timestamp, run and level first) or as the in-process
        /// <see cref="FlyoutTrace.Sink"/> receives them (message only). Lines it does not know are ignored.
        /// </summary>
        public static FlyoutTraceSummary Parse(IEnumerable<string> lines)
        {
            int show = 0, update = 0, soft = 0, presents = 0, patches = 0, rebuilds = 0, revives = 0;
            int restarts = 0, superseded = 0, marquee = 0, maxWindows = 0;
            List<string> decisions = [];
            List<string> motion = [];

            foreach (string line in lines)
            {
                string message = LogPrefix().Replace(line, string.Empty, 1);

                if (message.StartsWith("Show queued ", StringComparison.Ordinal))
                {
                    show++;
                }
                else if (message.StartsWith("Update queued ", StringComparison.Ordinal))
                {
                    update++;
                }
                else if (message.StartsWith("SoftRefresh queued ", StringComparison.Ordinal))
                {
                    soft++;
                }
                else if (DecisionName(message) is { } decision)
                {
                    decisions.Add(decision);
                }
                else if (message.StartsWith("presented kind=", StringComparison.Ordinal)
                         || message.StartsWith("stacked presented ", StringComparison.Ordinal))
                {
                    presents++;
                }
                else if (message.StartsWith("patch kind=", StringComparison.Ordinal)
                         || message.StartsWith("stacked patch ", StringComparison.Ordinal))
                {
                    patches++;
                }
                else if (message.StartsWith("rebuild path ", StringComparison.Ordinal))
                {
                    rebuilds++;
                }
                else if (message.StartsWith("revive kind=", StringComparison.Ordinal)
                         || message.StartsWith("stacked revive ", StringComparison.Ordinal))
                {
                    revives++;
                }
                else if (message.StartsWith("motion plan entrance=", StringComparison.Ordinal))
                {
                    motion.Add(message.StartsWith("motion plan entrance=True", StringComparison.Ordinal) ? "enter" : "exit");
                }
                else if (message.StartsWith("MOTION RESTART", StringComparison.Ordinal))
                {
                    restarts++;
                }
                else if (message.StartsWith("motion superseded ", StringComparison.Ordinal))
                {
                    superseded++;
                }
                else if (message.StartsWith("marquee restart ", StringComparison.Ordinal))
                {
                    marquee++;
                }
                else if (VisibleWindows().Match(message) is { Success: true } windows)
                {
                    maxWindows = Math.Max(maxWindows, int.Parse(windows.Groups[1].Value, CultureInfo.InvariantCulture));
                }
            }

            return new FlyoutTraceSummary(
                show, update, soft, decisions, presents, patches, rebuilds, revives, motion, restarts, superseded, marquee, maxWindows);
        }

        /// <summary>Stable text for a golden file: one line per group, in a fixed order.</summary>
        public string ToGolden()
        {
            StringBuilder text = new();
            _ = text.Append(CultureInfo.InvariantCulture, $"calls: show={ShowCalls} update={UpdateCalls} softRefresh={SoftRefreshCalls}\n");
            _ = text.Append(CultureInfo.InvariantCulture, $"decisions: {string.Join(",", IngressDecisions)}\n");
            _ = text.Append(CultureInfo.InvariantCulture, $"pipeline: presents={Presents} patches={Patches} rebuilds={Rebuilds} revives={Revives}\n");
            _ = text.Append(CultureInfo.InvariantCulture, $"motion: {string.Join(",", MotionSequence)}\n");
            _ = text.Append(CultureInfo.InvariantCulture, $"restarts: motion={MotionRestarts} superseded={MotionSuperseded} marquee={MarqueeRestarts}\n");
            _ = text.Append(CultureInfo.InvariantCulture, $"maxVisibleProcessWindows: {MaxVisibleProcessWindows}\n");
            return text.ToString();
        }

        private static string? DecisionName(string message)
        {
            const string Prefix = "ingress decision=";
            if (!message.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return null;
            }

            int end = message.IndexOf(' ', Prefix.Length);
            return end < 0 ? message[Prefix.Length..] : message[Prefix.Length..end];
        }

        [GeneratedRegex(@"^\S+ run=\S+ (?:INFO|DEBUG|WARN|ERROR) ")]
        private static partial Regex LogPrefix();

        [GeneratedRegex(@"^visible windows (\d+)")]
        private static partial Regex VisibleWindows();
    }
}
