using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class TesseraFlyoutMotionPlanTests
    {
        [Fact]
        public void Host_must_execute_one_plan_for_single_and_stacked()
        {
            _ = TesseraFlyoutMotionPlan.HostMustExecuteMotionPlanBothTopologies.Should().BeTrue();
            _ = TesseraFlyoutMotionPlan.MotionPlanMustClusterStackedSlots.Should().BeTrue();
            _ = TesseraFlyoutMotionPlan.HostMustPumpSharedPhase2Progress.Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.Phase2MustShareOneVsyncProgressAcrossStackedSlots.Should().BeTrue();
            _ = TesseraFlyoutMotionPlan.ShouldPumpSharedPhase2Progress(slotCount: 2).Should().BeTrue();
            _ = TesseraFlyoutMotionPlan.ShouldPumpSharedPhase2Progress(slotCount: 1).Should().BeFalse();
        }

        [Fact]
        public void Fluent_stacked_fancy_show_is_phase1_wait_phase2()
        {
            TesseraFlyoutMotionPlan plan = TesseraFlyoutMotionPlan.ResolveShow(
                ani: 2, "Fluent", showMediaStrip: true, stacked: true);

            _ = plan.Entrance.Should().BeTrue();
            _ = plan.Clustered.Should().BeTrue();
            _ = plan.Steps.Select(s => s.Kind).Should().Equal([
                TesseraFlyoutMotionStepKind.Begin,
                TesseraFlyoutMotionStepKind.Phase1,
                TesseraFlyoutMotionStepKind.Wait,
                TesseraFlyoutMotionStepKind.Phase2,
                TesseraFlyoutMotionStepKind.Complete,
            ]);
            _ = plan.Steps[1].SlotFilter.Should().Be(TesseraFlyoutMotionSlotFilter.Phase1Slots);
            _ = plan.Steps[1].Entrance.Should().BeTrue();
            _ = plan.Steps[2].WaitMs.Should().Be(0);
            _ = TesseraFlyoutMotionPlan.ResolveHide(2, "Fluent", true, stacked: true)
                .Steps.Single(s => s.Kind == TesseraFlyoutMotionStepKind.Wait)
                .WaitMs.Should().Be(TesseraFlyoutAnimationPolicy.FancyPauseMs);
            _ = plan.Steps[3].SlotFilter.Should().Be(TesseraFlyoutMotionSlotFilter.Phase2Slots);
            _ = plan.Steps[3].Entrance.Should().BeTrue();

            _ = TesseraFlyoutMotionPlan.SlotParticipates(
                    plan.Steps[1], 2, "Fluent", true, TesseraStackedPanelRole.Volume)
                .Should().BeTrue();
            _ = TesseraFlyoutMotionPlan.SlotParticipates(
                    plan.Steps[1], 2, "Fluent", true, TesseraStackedPanelRole.Media)
                .Should().BeFalse();
            _ = TesseraFlyoutMotionPlan.SlotParticipates(
                    plan.Steps[3], 2, "Fluent", true, TesseraStackedPanelRole.Media)
                .Should().BeTrue();
            _ = TesseraFlyoutMotionPlan.SlotParticipates(
                    plan.Steps[3], 2, "Fluent", true, TesseraStackedPanelRole.Volume)
                .Should().BeTrue();
        }

        [Fact]
        public void Fluent_stacked_fancy_hide_reverses_clustered_phases()
        {
            TesseraFlyoutMotionPlan hide = TesseraFlyoutMotionPlan.ResolveHide(
                ani: 2, "Fluent", showMediaStrip: true, stacked: true);

            _ = hide.Entrance.Should().BeFalse();
            _ = hide.Clustered.Should().BeTrue();
            _ = hide.Steps.Select(s => s.Kind).Should().Equal([
                TesseraFlyoutMotionStepKind.Begin,
                TesseraFlyoutMotionStepKind.Phase2,
                TesseraFlyoutMotionStepKind.Wait,
                TesseraFlyoutMotionStepKind.Phase1,
                TesseraFlyoutMotionStepKind.Complete,
            ]);
            _ = hide.Steps[1].Entrance.Should().BeFalse();
            _ = hide.Steps[1].SlotFilter.Should().Be(TesseraFlyoutMotionSlotFilter.Phase2Slots);
            _ = hide.Steps[3].SlotFilter.Should().Be(TesseraFlyoutMotionSlotFilter.Phase1Slots);
            _ = hide.Steps[3].Entrance.Should().BeFalse();

            _ = TesseraFlyoutMotionPlan.SlotParticipates(
                    hide.Steps[3], 2, "Fluent", true, TesseraStackedPanelRole.Media)
                .Should().BeTrue();
        }

        [Fact]
        public void Hide_motion_kinds_are_the_reverse_of_show()
        {
            foreach (bool stacked in new[] { false, true })
            {
                TesseraFlyoutMotionPlan show = TesseraFlyoutMotionPlan.ResolveShow(2, "Fluent", true, stacked);
                TesseraFlyoutMotionPlan hide = TesseraFlyoutMotionPlan.ResolveHide(2, "Fluent", true, stacked);
                _ = TesseraFlyoutMotionPlan.MotionKinds(hide)
                    .Should()
                    .Equal(TesseraFlyoutMotionPlan.MotionKinds(show).Reverse());
            }
        }

        [Fact]
        public void Single_hwnd_fancy_uses_all_slots_same_phase_order()
        {
            TesseraFlyoutMotionPlan show = TesseraFlyoutMotionPlan.ResolveShow(
                ani: 2, "Fluent", showMediaStrip: true, stacked: false);

            _ = show.Clustered.Should().BeFalse();
            _ = show.Steps.Select(s => s.Kind).Should().Equal([
                TesseraFlyoutMotionStepKind.Begin,
                TesseraFlyoutMotionStepKind.Phase1,
                TesseraFlyoutMotionStepKind.Wait,
                TesseraFlyoutMotionStepKind.Phase2,
                TesseraFlyoutMotionStepKind.Complete,
            ]);
            _ = show.Steps[1].SlotFilter.Should().Be(TesseraFlyoutMotionSlotFilter.All);
            _ = show.Steps[3].SlotFilter.Should().Be(TesseraFlyoutMotionSlotFilter.All);
            _ = TesseraFlyoutMotionPlan.SlotParticipates(
                    show.Steps[1], 2, "Fluent", true, stackedRole: null)
                .Should().BeTrue();
        }

        [Fact]
        public void Media_skip_phase1_on_show_is_in_the_plan_not_a_host_fork()
        {
            TesseraFlyoutMotionPlan show = TesseraFlyoutMotionPlan.ResolveShow(2, "Fluent", true, stacked: true);
            TesseraFlyoutMotionPlan hide = TesseraFlyoutMotionPlan.ResolveHide(2, "Fluent", true, stacked: true);
            TesseraFlyoutMotionStep showP1 = show.Steps.Single(s => s.Kind == TesseraFlyoutMotionStepKind.Phase1);
            TesseraFlyoutMotionStep hideP1 = hide.Steps.Single(s => s.Kind == TesseraFlyoutMotionStepKind.Phase1);

            _ = TesseraFlyoutMotionPlan.SlotParticipates(
                    showP1, 2, "Fluent", true, TesseraStackedPanelRole.Media)
                .Should().BeFalse();
            _ = TesseraFlyoutMotionPlan.SlotParticipates(
                    hideP1, 2, "Fluent", true, TesseraStackedPanelRole.Media)
                .Should().BeTrue();
        }

        [Fact]
        public void Square_without_media_still_includes_phase2()
        {
            TesseraFlyoutMotionPlan show = TesseraFlyoutMotionPlan.ResolveShow(
                ani: 2, "Square", showMediaStrip: false, stacked: false);
            _ = show.Steps.Select(s => s.Kind).Should().Contain(TesseraFlyoutMotionStepKind.Phase2);
            _ = TesseraFlyoutMotionPlan.ResolveShow(2, "Fluent", showMediaStrip: false, stacked: false)
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
            TesseraFlyoutMotionPlan show = TesseraFlyoutMotionPlan.ResolveShow(ani, "Fluent", true, stacked: true);
            TesseraFlyoutMotionPlan hide = TesseraFlyoutMotionPlan.ResolveHide(ani, "Fluent", true, stacked: true);
            _ = show.Steps.Select(s => s.Kind).Should().Equal([
                TesseraFlyoutMotionStepKind.Begin,
                TesseraFlyoutMotionStepKind.Phase1,
                TesseraFlyoutMotionStepKind.Complete,
            ]);
            _ = TesseraFlyoutMotionPlan.MotionKinds(hide)
                .Should()
                .Equal(TesseraFlyoutMotionPlan.MotionKinds(show).Reverse());
        }
    }
}
