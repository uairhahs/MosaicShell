using MosaicShell.Core.Modules;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Capabilities.Platform
{
    public enum FlyoutScenarioStepKind
    {
        Show,
        Update,
        SoftRefresh,
        Hide,

        /// <summary>Wait until the flyout is effectively showing.</summary>
        WaitShown,

        /// <summary>Wait until the session reaches <see cref="FlyoutScenarioStep.Phase"/>.</summary>
        WaitPhase,

        /// <summary>Wait until nothing is showing.</summary>
        WaitHidden,

        /// <summary>
        /// Wait until the presenter's own trace has recorded one more line starting with
        /// <see cref="FlyoutScenarioStep.TracePrefix"/> than earlier waits on it needed. For the moments
        /// no public state shows, such as the exit motion having begun or ended.
        /// </summary>
        WaitTrace,
    }

    /// <summary>One scripted action. Waits name a state to reach, never a time to pass.</summary>
    public sealed record FlyoutScenarioStep(
        FlyoutScenarioStepKind Kind,
        FlyoutRequest? Request = null,
        TesseraFlyoutPhase? Phase = null,
        string? TracePrefix = null)
    {
        public static FlyoutScenarioStep Show(FlyoutRequest request) => new(FlyoutScenarioStepKind.Show, request);

        public static FlyoutScenarioStep Update(FlyoutRequest request) => new(FlyoutScenarioStepKind.Update, request);

        public static FlyoutScenarioStep SoftRefresh(FlyoutRequest request) => new(FlyoutScenarioStepKind.SoftRefresh, request);

        public static FlyoutScenarioStep Hide() => new(FlyoutScenarioStepKind.Hide);

        public static FlyoutScenarioStep WaitShown() => new(FlyoutScenarioStepKind.WaitShown);

        public static FlyoutScenarioStep WaitPhase(TesseraFlyoutPhase phase) => new(FlyoutScenarioStepKind.WaitPhase, Phase: phase);

        public static FlyoutScenarioStep WaitHidden() => new(FlyoutScenarioStepKind.WaitHidden);

        public static FlyoutScenarioStep WaitTrace(string tracePrefix) => new(FlyoutScenarioStepKind.WaitTrace, TracePrefix: tracePrefix);
    }

    /// <summary>A named, scripted sequence of presenter calls: the fixed input of a flyout baseline.</summary>
    public sealed record FlyoutScenario(string Name, IReadOnlyList<FlyoutScenarioStep> Steps);

    /// <summary>
    /// The scenarios of the flyout baseline (audit task A5): skips, a volume scrub, caps presses, and the
    /// overlaps where one action lands in the middle of another's motion. Requests carry the same payload
    /// keys the Tessera request builder produces.
    /// </summary>
    public static class FlyoutScenarios
    {
        /// <summary>Long enough that no scenario is cut short by the flyout dismissing itself.</summary>
        private const int NoAutoDismissMs = 60_000;

        /// <summary>Trace lines the presenter writes when an exit motion starts and when it finishes.</summary>
        private const string ExitBegan = "motion plan entrance=False";

        private const string ExitEnded = "motion complete entrance=False";

        /// <summary>
        /// The flyout dismisses itself this soon after it is shown, which starts the exit motion while the entrance
        /// is still running. A later dismissal cannot be scripted headless: the dismiss timer is skipped while the
        /// pointer is over the window, and the headless pointer sits over it once it has settled. An early one does
        /// not depend on how long the entrance takes on this machine.
        /// </summary>
        private const int EarlyDismissMs = 30;

        private static readonly string[] ShortTitles = ["Humid", "Flamingo", "Midnight City"];

        private static readonly string[] LongTitles =
        [
            new string('A', 280) + " (Extended Mix)",
            new string('B', 300) + " (Live at the Opera House)",
            new string('C', 270) + " (Remastered)",
        ];

        public static IReadOnlyList<FlyoutScenario> All(string styleId)
        {
            return
            [
                new("skip-x3-short-titles", Skips(styleId, ShortTitles)),
                new("skip-x3-long-titles", Skips(styleId, LongTitles)),
                new("volume-scrub", VolumeScrub(styleId)),
                new("caps-x5", CapsPresses(styleId)),
                new("dismiss-then-skip",
                [
                    FlyoutScenarioStep.Show(Media(styleId, "Humid", EarlyDismissMs)),
                    FlyoutScenarioStep.WaitTrace(ExitEnded),
                    FlyoutScenarioStep.WaitHidden(),
                    FlyoutScenarioStep.Show(Media(styleId, "Flamingo")),
                    FlyoutScenarioStep.WaitPhase(TesseraFlyoutPhase.Shown),
                    FlyoutScenarioStep.Hide(),
                    FlyoutScenarioStep.WaitHidden(),
                ]),
                new("skip-during-exit",
                [
                    FlyoutScenarioStep.Show(Media(styleId, "Humid", EarlyDismissMs)),
                    FlyoutScenarioStep.WaitTrace(ExitBegan),
                    FlyoutScenarioStep.Show(Media(styleId, "Flamingo")),
                    FlyoutScenarioStep.WaitPhase(TesseraFlyoutPhase.Shown),
                    FlyoutScenarioStep.Hide(),
                    FlyoutScenarioStep.WaitHidden(),
                ]),
                new("dismiss-during-entrance",
                [
                    FlyoutScenarioStep.Show(Media(styleId, "Humid", EarlyDismissMs)),
                    FlyoutScenarioStep.WaitTrace(ExitEnded),
                    FlyoutScenarioStep.WaitHidden(),
                    FlyoutScenarioStep.Hide(),
                    FlyoutScenarioStep.WaitHidden(),
                ]),
            ];
        }

        private static List<FlyoutScenarioStep> Skips(string styleId, string[] titles)
        {
            // The first track change presents the flyout; later ones arrive while it is shown and patch it.
            List<FlyoutScenarioStep> steps =
            [
                FlyoutScenarioStep.Show(Media(styleId, titles[0])),
                FlyoutScenarioStep.WaitPhase(TesseraFlyoutPhase.Shown),
            ];
            foreach (string title in titles.Skip(1))
            {
                steps.Add(FlyoutScenarioStep.Update(Media(styleId, title)));
            }

            steps.Add(FlyoutScenarioStep.Hide());
            steps.Add(FlyoutScenarioStep.WaitHidden());
            return steps;
        }

        private static List<FlyoutScenarioStep> VolumeScrub(string styleId)
        {
            List<FlyoutScenarioStep> steps =
            [
                FlyoutScenarioStep.Show(Volume(styleId, 10)),
                FlyoutScenarioStep.WaitPhase(TesseraFlyoutPhase.Shown),
            ];

            // A scrub is a burst of updates with nothing between them.
            foreach (int level in new[] { 15, 20, 25, 30, 35, 40, 45, 50 })
            {
                steps.Add(FlyoutScenarioStep.Update(Volume(styleId, level)));
            }

            steps.Add(FlyoutScenarioStep.Hide());
            steps.Add(FlyoutScenarioStep.WaitHidden());
            return steps;
        }

        private static List<FlyoutScenarioStep> CapsPresses(string styleId)
        {
            List<FlyoutScenarioStep> steps =
            [
                FlyoutScenarioStep.Show(Caps(styleId, on: true)),
                FlyoutScenarioStep.WaitPhase(TesseraFlyoutPhase.Shown),
            ];
            foreach (bool on in new[] { false, true, false, true })
            {
                steps.Add(FlyoutScenarioStep.Update(Caps(styleId, on)));
            }

            steps.Add(FlyoutScenarioStep.Hide());
            steps.Add(FlyoutScenarioStep.WaitHidden());
            return steps;
        }

        private static FlyoutRequest Media(string styleId, string title, int autoDismissMs = NoAutoDismissMs)
        {
            return Request("media", styleId, new Dictionary<string, string>
            {
                ["mediaTitle"] = title,
                ["mediaArtist"] = "Test Artist",
                ["mediaPlaying"] = "1",
                ["showMediaStrip"] = "1",
            }, autoDismissMs);
        }

        private static FlyoutRequest Volume(string styleId, int percent)
        {
            return Request("vol", styleId, new Dictionary<string, string>
            {
                ["volume"] = (percent / 100.0).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                ["muted"] = "0",
            });
        }

        private static FlyoutRequest Caps(string styleId, bool on)
        {
            return Request("locks", styleId, new Dictionary<string, string>
            {
                ["lock"] = "CapsLock",
                ["on"] = on ? "1" : "0",
            });
        }

        private static FlyoutRequest Request(string kind, string styleId, Dictionary<string, string> payload, int autoDismissMs = NoAutoDismissMs)
        {
            return new FlyoutRequest(ModuleIds.Tessera, kind, styleId, Payload: payload, AutoDismissMs: autoDismissMs);
        }
    }
}
