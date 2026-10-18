using System.Runtime.CompilerServices;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;

namespace MosaicShell.Host.Tests
{
    /// <summary>
    /// The recorded "before" of the flyout pipeline rewrite (audit task A5, headless half). Each scenario is
    /// replayed three times against the unmodified presenter: the runs must agree with each other, which
    /// proves the replay is repeatable, and with the golden summary under <c>Baselines/</c>, which pins what
    /// the pipeline does today. Only the single-window path runs headless; the stacked path needs Windows
    /// composition and is captured on the desktop (A6).
    /// <para>
    /// A change to the pipeline that moves a count is not necessarily wrong, but it must be a decision:
    /// re-record with <c>MOSAICSHELL_RECORD_BASELINES=1</c>, read the diff, and explain it.
    /// </para>
    /// </summary>
    public sealed class FlyoutBaselineTests : HeadlessHostTest
    {
        private const string RecordVariable = "MOSAICSHELL_RECORD_BASELINES";
        private const int Repeats = 3;

        public FlyoutBaselineTests()
            : base(HostLaunchOptions.TesseraForceSoftwareRenderFlag)
        {
        }

        public static TheoryData<string, string> Cases()
        {
            TheoryData<string, string> cases = [];
            foreach (string style in new[] { "Fluent", "Windows11" })
            {
                foreach (FlyoutScenario scenario in FlyoutScenarios.All(style))
                {
                    cases.Add(style, scenario.Name);
                }
            }

            return cases;
        }

        [AvaloniaTheory]
        [MemberData(nameof(Cases))]
        public void The_replay_is_repeatable_and_matches_the_recorded_baseline(string style, string scenarioName)
        {
            FlyoutScenario scenario = FlyoutScenarios.All(style).Single(s => s.Name == scenarioName);

            string[] runs = [.. Enumerable.Range(0, Repeats).Select(_ => FlyoutScenarioDriver.Run(scenario, FlyoutPipelineMode.Legacy).ToGolden())];

            runs.Distinct().Should().ContainSingle(
                $"'{scenarioName}' in {style} must replay the same way every time, but the runs were:\n{string.Join("\n---\n", runs)}");

            string path = GoldenPath(style, scenarioName);
            if (Environment.GetEnvironmentVariable(RecordVariable) == "1")
            {
                _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, runs[0]);
                return;
            }

            File.Exists(path).Should().BeTrue($"no baseline at {path}; record it with {RecordVariable}=1");
            runs[0].Should().Be(File.ReadAllText(path).ReplaceLineEndings("\n"), $"the recorded baseline for '{scenarioName}' in {style}");
        }

        [AvaloniaTheory]
        [MemberData(nameof(Cases))]
        public void Shadow_mode_executes_the_legacy_path_and_reports_no_unexplained_disagreement(string style, string scenarioName)
        {
            FlyoutScenario scenario = FlyoutScenarios.All(style).Single(s => s.Name == scenarioName);

            FlyoutScenarioDriver.ScenarioReplay replay = FlyoutScenarioDriver.Replay(scenario, FlyoutPipelineMode.Shadow);

            replay.Summary.ToGolden().Should().Be(
                File.ReadAllText(GoldenPath(style, scenarioName)).ReplaceLineEndings("\n"),
                "shadow mode must not change what the presenter does");
            replay.ShadowDisagreements.Where(d => d.EndsWith("ledger=none", StringComparison.Ordinal)).Should().BeEmpty(
                "every disagreement must map to a row of the deliberate-changes ledger");
        }

        [AvaloniaTheory]
        [MemberData(nameof(Cases))]
        public void Next_mode_replays_without_a_diff_unless_shadow_names_a_ledger_row(string style, string scenarioName)
        {
            FlyoutScenario scenario = FlyoutScenarios.All(style).Single(s => s.Name == scenarioName);
            int disagreements = FlyoutScenarioDriver.Replay(scenario, FlyoutPipelineMode.Shadow).ShadowDisagreements.Count;

            string next = FlyoutScenarioDriver.Run(scenario, FlyoutPipelineMode.Next).ToGolden();

            if (disagreements == 0)
            {
                next.Should().Be(
                    File.ReadAllText(GoldenPath(style, scenarioName)).ReplaceLineEndings("\n"),
                    "the unified resolver agrees with the legacy one on every request of this scenario");
            }
        }

        private static string GoldenPath(string style, string scenarioName, [CallerFilePath] string thisFile = "")
        {
            return Path.Combine(Path.GetDirectoryName(thisFile)!, "Baselines", style, scenarioName + ".txt");
        }
    }
}
