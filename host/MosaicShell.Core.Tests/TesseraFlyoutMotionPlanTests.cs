using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class TesseraFlyoutMotionPlanTests
{
    [Fact]
    public void Host_must_execute_one_plan_for_single_and_stacked()
    {
        TesseraFlyoutMotionPlan.HostMustExecuteMotionPlanBothTopologies.Should().BeTrue();
        TesseraFlyoutMotionPlan.MotionPlanMustClusterStackedSlots.Should().BeTrue();
        TesseraFlyoutMotionPlan.HostMustPumpSharedPhase2Progress.Should().BeTrue();
        TesseraFlyoutAnimationPolicy.Phase2MustShareOneVsyncProgressAcrossStackedSlots.Should().BeTrue();
        TesseraFlyoutMotionPlan.ShouldPumpSharedPhase2Progress(slotCount: 2).Should().BeTrue();
        TesseraFlyoutMotionPlan.ShouldPumpSharedPhase2Progress(slotCount: 1).Should().BeFalse();
    }

    [Fact]
    public void Fluent_stacked_fancy_show_is_phase1_wait_phase2()
    {
        var plan = TesseraFlyoutMotionPlan.ResolveShow(
            ani: 2, "Fluent", showMediaStrip: true, stacked: true);

        plan.Entrance.Should().BeTrue();
        plan.Clustered.Should().BeTrue();
        plan.Steps.Select(s => s.Kind).Should().Equal([
            TesseraFlyoutMotionStepKind.Begin,
            TesseraFlyoutMotionStepKind.Phase1,
            TesseraFlyoutMotionStepKind.Wait,
            TesseraFlyoutMotionStepKind.Phase2,
            TesseraFlyoutMotionStepKind.Complete,
        ]);
        plan.Steps[1].SlotFilter.Should().Be(TesseraFlyoutMotionSlotFilter.Phase1Slots);
        plan.Steps[1].Entrance.Should().BeTrue();
        plan.Steps[2].WaitMs.Should().Be(0);
        TesseraFlyoutMotionPlan.ResolveHide(2, "Fluent", true, stacked: true)
            .Steps.Single(s => s.Kind == TesseraFlyoutMotionStepKind.Wait)
            .WaitMs.Should().Be(TesseraFlyoutAnimationPolicy.FancyPauseMs);
        plan.Steps[3].SlotFilter.Should().Be(TesseraFlyoutMotionSlotFilter.Phase2Slots);
        plan.Steps[3].Entrance.Should().BeTrue();

        TesseraFlyoutMotionPlan.SlotParticipates(
                plan.Steps[1], 2, "Fluent", true, TesseraStackedPanelRole.Volume)
            .Should().BeTrue();
        TesseraFlyoutMotionPlan.SlotParticipates(
                plan.Steps[1], 2, "Fluent", true, TesseraStackedPanelRole.Media)
            .Should().BeFalse();
        TesseraFlyoutMotionPlan.SlotParticipates(
                plan.Steps[3], 2, "Fluent", true, TesseraStackedPanelRole.Media)
            .Should().BeTrue();
        TesseraFlyoutMotionPlan.SlotParticipates(
                plan.Steps[3], 2, "Fluent", true, TesseraStackedPanelRole.Volume)
            .Should().BeTrue();
    }

    [Fact]
    public void Fluent_stacked_fancy_hide_reverses_clustered_phases()
    {
        var hide = TesseraFlyoutMotionPlan.ResolveHide(
            ani: 2, "Fluent", showMediaStrip: true, stacked: true);

        hide.Entrance.Should().BeFalse();
        hide.Clustered.Should().BeTrue();
        hide.Steps.Select(s => s.Kind).Should().Equal([
            TesseraFlyoutMotionStepKind.Begin,
            TesseraFlyoutMotionStepKind.Phase2,
            TesseraFlyoutMotionStepKind.Wait,
            TesseraFlyoutMotionStepKind.Phase1,
            TesseraFlyoutMotionStepKind.Complete,
        ]);
        hide.Steps[1].Entrance.Should().BeFalse();
        hide.Steps[1].SlotFilter.Should().Be(TesseraFlyoutMotionSlotFilter.Phase2Slots);
        hide.Steps[3].SlotFilter.Should().Be(TesseraFlyoutMotionSlotFilter.Phase1Slots);
        hide.Steps[3].Entrance.Should().BeFalse();

        TesseraFlyoutMotionPlan.SlotParticipates(
                hide.Steps[3], 2, "Fluent", true, TesseraStackedPanelRole.Media)
            .Should().BeTrue();
    }

    [Fact]
    public void Hide_motion_kinds_are_the_reverse_of_show()
    {
        foreach (var stacked in new[] { false, true })
        {
            var show = TesseraFlyoutMotionPlan.ResolveShow(2, "Fluent", true, stacked);
            var hide = TesseraFlyoutMotionPlan.ResolveHide(2, "Fluent", true, stacked);
            TesseraFlyoutMotionPlan.MotionKinds(hide)
                .Should()
                .Equal(TesseraFlyoutMotionPlan.MotionKinds(show).Reverse());
        }
    }

    [Fact]
    public void Single_hwnd_fancy_uses_all_slots_same_phase_order()
    {
        var show = TesseraFlyoutMotionPlan.ResolveShow(
            ani: 2, "Fluent", showMediaStrip: true, stacked: false);

        show.Clustered.Should().BeFalse();
        show.Steps.Select(s => s.Kind).Should().Equal([
            TesseraFlyoutMotionStepKind.Begin,
            TesseraFlyoutMotionStepKind.Phase1,
            TesseraFlyoutMotionStepKind.Wait,
            TesseraFlyoutMotionStepKind.Phase2,
            TesseraFlyoutMotionStepKind.Complete,
        ]);
        show.Steps[1].SlotFilter.Should().Be(TesseraFlyoutMotionSlotFilter.All);
        show.Steps[3].SlotFilter.Should().Be(TesseraFlyoutMotionSlotFilter.All);
        TesseraFlyoutMotionPlan.SlotParticipates(
                show.Steps[1], 2, "Fluent", true, stackedRole: null)
            .Should().BeTrue();
    }

    [Fact]
    public void Media_skip_phase1_on_show_is_in_the_plan_not_a_host_fork()
    {
        var show = TesseraFlyoutMotionPlan.ResolveShow(2, "Fluent", true, stacked: true);
        var hide = TesseraFlyoutMotionPlan.ResolveHide(2, "Fluent", true, stacked: true);
        var showP1 = show.Steps.Single(s => s.Kind == TesseraFlyoutMotionStepKind.Phase1);
        var hideP1 = hide.Steps.Single(s => s.Kind == TesseraFlyoutMotionStepKind.Phase1);

        TesseraFlyoutMotionPlan.SlotParticipates(
                showP1, 2, "Fluent", true, TesseraStackedPanelRole.Media)
            .Should().BeFalse();
        TesseraFlyoutMotionPlan.SlotParticipates(
                hideP1, 2, "Fluent", true, TesseraStackedPanelRole.Media)
            .Should().BeTrue();
    }

    [Fact]
    public void Square_without_media_still_includes_phase2()
    {
        var show = TesseraFlyoutMotionPlan.ResolveShow(
            ani: 2, "Square", showMediaStrip: false, stacked: false);
        show.Steps.Select(s => s.Kind).Should().Contain(TesseraFlyoutMotionStepKind.Phase2);
        TesseraFlyoutMotionPlan.ResolveShow(2, "Fluent", showMediaStrip: false, stacked: false)
            .Steps.Select(s => s.Kind)
            .Should()
            .Equal([
                TesseraFlyoutMotionStepKind.Begin,
                TesseraFlyoutMotionStepKind.Phase1,
                TesseraFlyoutMotionStepKind.Complete,
            ]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Ani0_and_ani1_are_phase1_only_both_ways(int ani)
    {
        var show = TesseraFlyoutMotionPlan.ResolveShow(ani, "Fluent", true, stacked: true);
        var hide = TesseraFlyoutMotionPlan.ResolveHide(ani, "Fluent", true, stacked: true);
        show.Steps.Select(s => s.Kind).Should().Equal([
            TesseraFlyoutMotionStepKind.Begin,
            TesseraFlyoutMotionStepKind.Phase1,
            TesseraFlyoutMotionStepKind.Complete,
        ]);
        TesseraFlyoutMotionPlan.MotionKinds(hide)
            .Should()
            .Equal(TesseraFlyoutMotionPlan.MotionKinds(show).Reverse());
    }
}
