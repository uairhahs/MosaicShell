using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The five lifecycle holes of audit F04 as event sequences, plus the ordinary paths and stale-event handling.
    /// Opacity is only ever an input (where a motion starts from); no assertion infers a phase from a threshold.
    /// </summary>
    public sealed class FlyoutWindowLifecycleTests
    {
        private static FlyoutWindowLifecycle Shown(FlyoutMotionDriver driver = FlyoutMotionDriver.Window)
        {
            FlyoutWindowLifecycle lifecycle = new();
            FlyoutLifecycleAction entrance = lifecycle.BeginEntrance(0, driver).Single();
            lifecycle.CompleteEntrance(entrance.Generation);
            return lifecycle;
        }

        private static IEnumerable<FlyoutLifecycleActionKind> Kinds(IReadOnlyList<FlyoutLifecycleAction> actions)
        {
            return actions.Select(a => a.Kind);
        }

        // ---- ordinary paths ----

        [Fact]
        public void A_new_lifecycle_is_hidden()
        {
            FlyoutWindowLifecycle lifecycle = new();

            lifecycle.Phase.Should().Be(TesseraFlyoutPhase.Hidden);
            lifecycle.ExitInFlight.Should().BeFalse();
        }

        [Fact]
        public void An_entrance_runs_from_hidden_through_entering_to_shown()
        {
            FlyoutWindowLifecycle lifecycle = new();

            IReadOnlyList<FlyoutLifecycleAction> actions = lifecycle.BeginEntrance(0, FlyoutMotionDriver.Window);

            lifecycle.Phase.Should().Be(TesseraFlyoutPhase.Entering);
            actions.Should().ContainSingle().Which.Kind.Should().Be(FlyoutLifecycleActionKind.StartEntrance);
            lifecycle.CompleteEntrance(actions[0].Generation);
            lifecycle.Phase.Should().Be(TesseraFlyoutPhase.Shown);
        }

        [Fact]
        public void A_dismiss_of_a_shown_flyout_exits_then_hides()
        {
            FlyoutWindowLifecycle lifecycle = Shown();

            IReadOnlyList<FlyoutLifecycleAction> dismiss = lifecycle.Dismiss(1);

            lifecycle.Phase.Should().Be(TesseraFlyoutPhase.Exiting);
            lifecycle.ExitInFlight.Should().BeTrue();
            dismiss.Should().ContainSingle().Which.Kind.Should().Be(FlyoutLifecycleActionKind.StartExit);

            IReadOnlyList<FlyoutLifecycleAction> end = lifecycle.CompleteExit(dismiss[0].Generation);

            lifecycle.Phase.Should().Be(TesseraFlyoutPhase.Hidden);
            lifecycle.ExitInFlight.Should().BeFalse();
            Kinds(end).Should().Equal(FlyoutLifecycleActionKind.Hide);
        }

        [Fact]
        public void A_patch_on_a_shown_flyout_patches_and_keeps_it_alive_without_motion()
        {
            FlyoutWindowLifecycle lifecycle = Shown();

            IReadOnlyList<FlyoutLifecycleAction> actions = lifecycle.Patch(1);

            lifecycle.Phase.Should().Be(TesseraFlyoutPhase.Shown);
            Kinds(actions).Should().Equal(FlyoutLifecycleActionKind.PatchContent, FlyoutLifecycleActionKind.ResetDismissTimer);
        }

        [Fact]
        public void A_rebuild_on_a_shown_flyout_swaps_content_at_rest_without_replaying_the_entrance()
        {
            FlyoutWindowLifecycle lifecycle = Shown();

            IReadOnlyList<FlyoutLifecycleAction> actions = lifecycle.ApplyRequest(1);

            lifecycle.Phase.Should().Be(TesseraFlyoutPhase.Shown);
            Kinds(actions).Should().Equal(
                FlyoutLifecycleActionKind.ApplyContent,
                FlyoutLifecycleActionKind.SnapToRest,
                FlyoutLifecycleActionKind.ResetDismissTimer);
        }

        [Fact]
        public void A_request_on_a_hidden_window_builds_and_runs_a_fresh_entrance()
        {
            FlyoutWindowLifecycle lifecycle = new();

            IReadOnlyList<FlyoutLifecycleAction> actions = lifecycle.ApplyRequest(0);

            lifecycle.Phase.Should().Be(TesseraFlyoutPhase.Entering);
            Kinds(actions).Should().Equal(FlyoutLifecycleActionKind.ApplyContent, FlyoutLifecycleActionKind.StartEntrance);
        }

        [Fact]
        public void A_patch_on_a_hidden_window_does_nothing_because_there_is_nothing_to_patch()
        {
            FlyoutWindowLifecycle lifecycle = new();

            lifecycle.Patch(0).Should().BeEmpty();
            lifecycle.Phase.Should().Be(TesseraFlyoutPhase.Hidden);
        }

        [Fact]
        public void A_second_dismiss_while_exiting_does_not_start_a_second_exit()
        {
            FlyoutWindowLifecycle lifecycle = Shown();
            _ = lifecycle.Dismiss(1);

            lifecycle.Dismiss(0.6).Should().BeEmpty();
            lifecycle.Phase.Should().Be(TesseraFlyoutPhase.Exiting);
        }

        // ---- F04 gap 1: rebuild during exit ----

        [Fact]
        public void F04_1_a_rebuild_during_exit_revives_the_flyout_and_no_exit_is_left_in_flight()
        {
            FlyoutWindowLifecycle lifecycle = Shown();
            _ = lifecycle.Dismiss(1);

            IReadOnlyList<FlyoutLifecycleAction> actions = lifecycle.ApplyRequest(0.6);

            lifecycle.Phase.Should().NotBe(TesseraFlyoutPhase.Exiting);
            lifecycle.ExitInFlight.Should().BeFalse();
            Kinds(actions).Should().Contain(FlyoutLifecycleActionKind.ApplyContent).And.Contain(FlyoutLifecycleActionKind.ReviveFromExit);

            // The next dismiss fades; it does not take a shortcut for an exit that is no longer running.
            FlyoutLifecycleAction revive = actions.Single(a => a.Kind == FlyoutLifecycleActionKind.ReviveFromExit);
            lifecycle.CompleteEntrance(revive.Generation);
            lifecycle.Phase.Should().Be(TesseraFlyoutPhase.Shown);
            Kinds(lifecycle.Dismiss(1)).Should().Equal(FlyoutLifecycleActionKind.StartExit);
        }

        // ---- F04 gap 2: patch during exit ----

        [Fact]
        public void F04_2_a_patch_during_exit_is_not_lost_it_revives_the_flyout_and_resets_the_dismiss_timer()
        {
            FlyoutWindowLifecycle lifecycle = Shown();
            _ = lifecycle.Dismiss(1);

            IReadOnlyList<FlyoutLifecycleAction> actions = lifecycle.Patch(0.5);

            Kinds(actions).Should().Equal(
                FlyoutLifecycleActionKind.PatchContent,
                FlyoutLifecycleActionKind.ReviveFromExit,
                FlyoutLifecycleActionKind.ResetDismissTimer);
            lifecycle.Phase.Should().NotBe(TesseraFlyoutPhase.Exiting);
            lifecycle.ExitInFlight.Should().BeFalse();
        }

        // ---- F04 gap 3: motion starts from the current opacity ----

        [Fact]
        public void F04_3_a_dismiss_during_entrance_exits_from_the_current_opacity_not_from_full()
        {
            FlyoutWindowLifecycle lifecycle = new();
            _ = lifecycle.BeginEntrance(0, FlyoutMotionDriver.Window);

            FlyoutLifecycleAction exit = lifecycle.Dismiss(0.4).Single();

            exit.Kind.Should().Be(FlyoutLifecycleActionKind.StartExit);
            exit.FromOpacity.Should().Be(0.4);
            lifecycle.Phase.Should().Be(TesseraFlyoutPhase.Exiting);
        }

        [Fact]
        public void F04_3_a_revive_during_exit_fades_in_from_the_current_opacity_for_the_remaining_distance_only()
        {
            FlyoutWindowLifecycle lifecycle = Shown();
            _ = lifecycle.Dismiss(1);

            FlyoutLifecycleAction revive = lifecycle.Patch(0.25).Single(a => a.Kind == FlyoutLifecycleActionKind.ReviveFromExit);

            revive.FromOpacity.Should().Be(0.25);
            revive.DurationScale.Should().BeApproximately(0.75, 1e-9);
        }

        // ---- F04 gap 4: stacked entrance gap ----

        [Fact]
        public void F04_4_the_stacked_entrance_is_entering_at_once_so_a_request_in_the_gap_is_a_patch_not_a_second_entrance()
        {
            FlyoutWindowLifecycle lifecycle = new();

            // The presenter defers the motion by two dispatcher hops; the phase must already say Entering.
            _ = lifecycle.BeginEntrance(0, FlyoutMotionDriver.Presenter);
            lifecycle.Phase.Should().Be(TesseraFlyoutPhase.Entering);

            IReadOnlyList<FlyoutLifecycleAction> actions = lifecycle.Patch(0);

            Kinds(actions).Should().NotContain(FlyoutLifecycleActionKind.StartEntrance);
            Kinds(actions).Should().Equal(FlyoutLifecycleActionKind.PatchContent, FlyoutLifecycleActionKind.ResetDismissTimer);
        }

        // ---- F04 gap 5: double exit after a stacked rebuild while visible ----

        [Fact]
        public void F04_5_a_rebuild_while_visible_keeps_the_presenter_as_the_driver_so_one_exit_runs()
        {
            FlyoutWindowLifecycle lifecycle = Shown(FlyoutMotionDriver.Presenter);
            _ = lifecycle.ApplyRequest(1);

            lifecycle.Driver.Should().Be(FlyoutMotionDriver.Presenter);

            IReadOnlyList<FlyoutLifecycleAction> dismiss = lifecycle.Dismiss(1);

            dismiss.Should().ContainSingle();
            dismiss[0].Kind.Should().Be(FlyoutLifecycleActionKind.StartExit);
            dismiss[0].Driver.Should().Be(FlyoutMotionDriver.Presenter);
        }

        // ---- stale events ----

        [Fact]
        public void A_completion_of_a_motion_that_was_superseded_is_ignored()
        {
            FlyoutWindowLifecycle lifecycle = new();
            FlyoutLifecycleAction first = lifecycle.BeginEntrance(0, FlyoutMotionDriver.Window).Single();
            FlyoutLifecycleAction exit = lifecycle.Dismiss(0.5).Single();

            // The first entrance finishes late, after the exit began.
            lifecycle.CompleteEntrance(first.Generation).Should().BeEmpty();
            lifecycle.Phase.Should().Be(TesseraFlyoutPhase.Exiting);

            lifecycle.CompleteExit(exit.Generation);
            lifecycle.Phase.Should().Be(TesseraFlyoutPhase.Hidden);
        }

        [Fact]
        public void A_stale_exit_completion_cannot_hide_a_flyout_that_was_revived()
        {
            FlyoutWindowLifecycle lifecycle = Shown();
            FlyoutLifecycleAction exit = lifecycle.Dismiss(1).Single();
            _ = lifecycle.Patch(0.8);

            lifecycle.CompleteExit(exit.Generation).Should().BeEmpty();
            lifecycle.Phase.Should().NotBe(TesseraFlyoutPhase.Hidden);
        }

        [Fact]
        public void Supersede_makes_the_running_motions_completion_stale_without_changing_the_phase()
        {
            FlyoutWindowLifecycle lifecycle = new();
            FlyoutLifecycleAction entrance = lifecycle.BeginEntrance(0, FlyoutMotionDriver.Window).Single();

            lifecycle.Supersede();

            lifecycle.CompleteEntrance(entrance.Generation).Should().BeEmpty();
            lifecycle.Phase.Should().Be(TesseraFlyoutPhase.Entering);
        }
    }
}
