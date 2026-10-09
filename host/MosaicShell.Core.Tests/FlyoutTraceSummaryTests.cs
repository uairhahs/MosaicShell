using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The summary is the "before" for the flyout pipeline rewrite: what today's code does for a scripted
    /// action, counted from its own trace, so a later change shows up as a difference. The lines below are
    /// real ones from flyout.log (prefix included) and the bare form the in-process sink receives.
    /// </summary>
    public sealed class FlyoutTraceSummaryTests
    {
        private const string P = "2026-10-04T17:14:29.096+01:00 run=e976f438 ";

        [Fact]
        public void An_empty_trace_summarises_to_zeros()
        {
            FlyoutTraceSummary summary = FlyoutTraceSummary.Parse([]);

            summary.ToGolden().Should().Be(FlyoutTraceSummary.Parse(["unrelated line"]).ToGolden());
            summary.Presents.Should().Be(0);
            summary.IngressDecisions.Should().BeEmpty();
            summary.MotionSequence.Should().BeEmpty();
        }

        [Fact]
        public void Counts_the_calls_that_enter_the_pipeline_by_kind_of_call()
        {
            FlyoutTraceSummary summary = FlyoutTraceSummary.Parse(
            [
                P + "INFO Show queued kind=media style=ModernFlyouts thread=2",
                P + "INFO Update queued kind=media style=ModernFlyouts thread=2",
                P + "INFO Update queued kind=media style=ModernFlyouts thread=2",
                P + "INFO SoftRefresh queued kind=media style=ModernFlyouts gen=1",
            ]);

            summary.ShowCalls.Should().Be(1);
            summary.UpdateCalls.Should().Be(2);
            summary.SoftRefreshCalls.Should().Be(1);
        }

        [Fact]
        public void Records_the_ingress_decisions_in_order()
        {
            FlyoutTraceSummary summary = FlyoutTraceSummary.Parse(
            [
                P + "INFO ingress decision=ColdBuild kind=vol style=Fluent",
                P + "INFO ingress decision=PatchActiveSession kind=media style=Windows11",
                P + "INFO ingress decision=RebuildInPlace kind=vol style=PlainText",
            ]);

            summary.IngressDecisions.Should().Equal("ColdBuild", "PatchActiveSession", "RebuildInPlace");
        }

        [Fact]
        public void Counts_presents_patches_rebuilds_and_revives_for_single_and_stacked_alike()
        {
            FlyoutTraceSummary summary = FlyoutTraceSummary.Parse(
            [
                P + "INFO presented kind=vol style=Fluent visible=True bounds=503x223 desired=503x223",
                P + "INFO stacked presented slots=2 focusDim=True",
                P + "INFO patch kind=media style=Windows11",
                P + "INFO stacked patch kind=media style=ModernFlyouts",
                P + "INFO stacked patch kind=media style=ModernFlyouts",
                P + "INFO rebuild path kind=vol reuseWasVisible=False w4 role=single phase=Hidden vis=0",
                P + "INFO revive kind=locks style=ModernFlyouts w3 role=single phase=Hidden vis=1",
                P + "INFO stacked revive kind=vol style=ModernFlyouts",
            ]);

            summary.Presents.Should().Be(2);
            summary.Patches.Should().Be(3);
            summary.Rebuilds.Should().Be(1);
            summary.Revives.Should().Be(2);
        }

        [Fact]
        public void Does_not_mistake_a_stacked_line_for_the_plain_one_or_count_it_twice()
        {
            FlyoutTraceSummary summary = FlyoutTraceSummary.Parse(
            [
                P + "INFO stacked patch kind=vol style=ModernFlyouts",
                P + "INFO stacked presented slots=2 focusDim=False",
                P + "INFO stacked revive kind=vol style=ModernFlyouts",
            ]);

            (summary.Patches, summary.Presents, summary.Revives).Should().Be((1, 1, 1));
        }

        [Fact]
        public void Reads_the_phase_sequence_from_the_motion_plans()
        {
            FlyoutTraceSummary summary = FlyoutTraceSummary.Parse(
            [
                P + "INFO motion plan entrance=True kind=vol style=Fluent ani=2 steps=24 showMedia=True stacked=False windows=1 roles=[single] plan=[Begin,Phase1,Wait,Phase2,Complete] claims=[Begin,Phase1:1,Wait,Phase2:1,Complete]",
                P + "INFO motion complete entrance=True elapsedMs=672 w1 role=single phase=Entering vis=1 op=1 motion=entering p2=0 gen=1 showing=1",
                P + "INFO motion plan entrance=False kind=vol style=ModernFlyouts ani=2 steps=24 showMedia=True stacked=True windows=2 roles=[Volume,Media] plan=[Begin,Phase2,Wait,Phase1,Complete] claims=[Begin,Phase2:2,Wait,Phase1:2,Complete]",
            ]);

            summary.MotionSequence.Should().Equal("enter", "exit");
        }

        [Fact]
        public void Counts_restarted_and_superseded_motion_and_marquee_restarts()
        {
            FlyoutTraceSummary summary = FlyoutTraceSummary.Parse(
            [
                P + "INFO MOTION RESTART begin(entrance=False) over in-flight w1 role=Volume phase=Exiting vis=1 op=1 motion=exiting p2=0 gen=10 showing=1 kind=media style=ModernFlyouts",
                P + "INFO motion superseded entrance=False elapsedMs=797 runGen=5 nowGen=6 w1 role=Volume phase=Exiting",
                P + "INFO motion superseded entrance=True elapsedMs=100 runGen=6 nowGen=7 w1 role=Volume phase=Entering",
                P + "DEBUG marquee restart n=3 reason=text len=24 granted=1 max=240 available=200 text=abc",
                P + "DEBUG marquee restart n=4 reason=attach len=24 granted=1 max=240 available=200 text=abc",
            ]);

            summary.MotionRestarts.Should().Be(1);
            summary.MotionSuperseded.Should().Be(2);
            summary.MarqueeRestarts.Should().Be(2);
        }

        [Fact]
        public void Keeps_the_largest_count_of_visible_process_windows_as_the_orphan_signal()
        {
            FlyoutTraceSummary summary = FlyoutTraceSummary.Parse(
            [
                P + "DEBUG visible windows 2 [1250AFE 400x65@2110,529 layered=0 \"MosaicShell - Tessera\"; 1020F68 400x238@2140,609 layered=0 \"MosaicShell - Tessera\"]",
                P + "DEBUG visible windows 4 [1250AFE 400x65@2110,529 layered=0 \"MosaicShell - Tessera\"; 370DAC 1920x1011@0,0 layered=1 \"MosaicShell - Focus dim\"]",
                P + "DEBUG visible windows 3 []",
            ]);

            summary.MaxVisibleProcessWindows.Should().Be(4);
        }

        [Fact]
        public void Reads_bare_sink_lines_without_the_timestamp_prefix()
        {
            FlyoutTraceSummary summary = FlyoutTraceSummary.Parse(
            [
                "ingress decision=ColdBuild kind=vol style=Fluent",
                "Show queued kind=vol style=Fluent thread=2",
            ]);

            summary.IngressDecisions.Should().Equal("ColdBuild");
            summary.ShowCalls.Should().Be(1);
        }

        [Fact]
        public void The_golden_text_is_stable_and_names_every_count()
        {
            FlyoutTraceSummary summary = FlyoutTraceSummary.Parse(
            [
                P + "INFO Show queued kind=vol style=Fluent thread=2",
                P + "INFO ingress decision=ColdBuild kind=vol style=Fluent",
                P + "INFO presented kind=vol style=Fluent visible=True",
                P + "INFO motion plan entrance=True kind=vol style=Fluent ani=2 steps=24 showMedia=True stacked=False windows=1 roles=[single] plan=[Begin] claims=[Begin]",
            ]);

            summary.ToGolden().Should().Be(
                "calls: show=1 update=0 softRefresh=0\n"
                + "decisions: ColdBuild\n"
                + "pipeline: presents=1 patches=0 rebuilds=0 revives=0\n"
                + "motion: enter\n"
                + "restarts: motion=0 superseded=0 marquee=0\n"
                + "maxVisibleProcessWindows: 0\n");
        }
    }
}
