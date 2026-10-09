using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The scenarios are the fixed inputs of the flyout baseline: the same scripted actions replayed
    /// against today's presenter and, later, against the rewritten one. Each step waits on a state, never
    /// on a delay, so a replay does not depend on how fast the machine is.
    /// </summary>
    public sealed class FlyoutScenarioTests
    {
        private static readonly string[] Expected =
        [
            "skip-x3-short-titles",
            "skip-x3-long-titles",
            "volume-scrub",
            "caps-x5",
            "dismiss-then-skip",
            "skip-during-exit",
            "dismiss-during-entrance",
        ];

        [Fact]
        public void The_catalog_holds_the_scenarios_the_plan_names()
        {
            FlyoutScenarios.All("Fluent").Select(s => s.Name).Should().Equal(Expected);
        }

        [Fact]
        public void Every_scenario_is_for_the_Tessera_module_in_the_style_asked_for()
        {
            foreach (FlyoutScenario scenario in FlyoutScenarios.All("Windows11"))
            {
                foreach (FlyoutScenarioStep step in scenario.Steps.Where(s => s.Request is not null))
                {
                    step.Request!.ModuleId.Should().Be(ModuleIds.Tessera, scenario.Name);
                    step.Request.StyleId.Should().Be("Windows11", scenario.Name);
                }
            }
        }

        [Fact]
        public void Every_scenario_leaves_the_flyout_hidden_so_the_next_one_starts_clean()
        {
            foreach (FlyoutScenario scenario in FlyoutScenarios.All("Fluent"))
            {
                scenario.Steps[^1].Should().Be(FlyoutScenarioStep.WaitHidden(), scenario.Name);
                scenario.Steps.Should().Contain(s => s.Kind == FlyoutScenarioStepKind.Hide, scenario.Name);
            }
        }

        [Fact]
        public void Only_the_exit_scenarios_let_a_flyout_dismiss_itself_and_only_on_the_first_request()
        {
            string[] selfDismissing = ["dismiss-then-skip", "skip-during-exit", "dismiss-during-entrance"];
            foreach (FlyoutScenario scenario in FlyoutScenarios.All("Fluent"))
            {
                FlyoutRequest[] requests = [.. scenario.Steps.Where(s => s.Request is not null).Select(s => s.Request!)];
                bool dismisses = selfDismissing.Contains(scenario.Name);
                requests.Skip(dismisses ? 1 : 0).Should().OnlyContain(r => r.AutoDismissMs >= 60_000, scenario.Name);
                if (dismisses)
                {
                    requests[0].AutoDismissMs.Should().BeLessThan(100, scenario.Name);
                }
            }
        }

        [Fact]
        public void Overlapping_actions_are_ordered_by_a_trace_event_not_a_delay()
        {
            FlyoutScenario exit = FlyoutScenarios.All("Fluent").Single(s => s.Name == "skip-during-exit");
            FlyoutScenario entrance = FlyoutScenarios.All("Fluent").Single(s => s.Name == "dismiss-during-entrance");

            // The skip is sent once the exit motion is seen to have begun.
            exit.Steps.Select(s => s.Kind).Take(3).Should().Equal(
                FlyoutScenarioStepKind.Show, FlyoutScenarioStepKind.WaitTrace, FlyoutScenarioStepKind.Show);
            exit.Steps[1].TracePrefix.Should().Be("motion plan entrance=False");

            // The flyout is asked to dismiss almost at once, so the exit begins while it is still entering.
            entrance.Steps[0].Request!.AutoDismissMs.Should().BeLessThan(100);
            entrance.Steps[1].Kind.Should().Be(FlyoutScenarioStepKind.WaitTrace);
        }

        [Fact]
        public void The_long_title_scenario_uses_titles_longer_than_any_style_cap()
        {
            FlyoutScenario scenario = FlyoutScenarios.All("Fluent").Single(s => s.Name == "skip-x3-long-titles");

            scenario.Steps.Where(s => s.Request is not null)
                .Select(s => s.Request!.Payload!["mediaTitle"].Length)
                .Should().OnlyContain(length => length > 260);
        }

        [Fact]
        public void The_volume_scrub_sends_distinct_levels_in_a_row_without_waiting()
        {
            FlyoutScenario scenario = FlyoutScenarios.All("Fluent").Single(s => s.Name == "volume-scrub");

            string[] levels = [.. scenario.Steps.Where(s => s.Request is not null).Select(s => s.Request!.Payload!["volume"])];
            levels.Distinct().Count().Should().BeGreaterThanOrEqualTo(5);
            scenario.Steps.Where(s => s.Kind == FlyoutScenarioStepKind.Update).Should().HaveCountGreaterThanOrEqualTo(5);
        }
    }
}
