using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    public class TesseraFlyoutDismissPolicyTests
    {
        [Theory]
        [InlineData(TesseraFlyoutPhase.Exiting, 4, true, true)]
        [InlineData(TesseraFlyoutPhase.Hidden, 4, true, true)]
        [InlineData(TesseraFlyoutPhase.Entering, 4, true, false)]
        [InlineData(TesseraFlyoutPhase.Shown, 4, true, false)]
        [InlineData(TesseraFlyoutPhase.Hidden, 5, true, false)]
        [InlineData(TesseraFlyoutPhase.Exiting, 5, true, false)]
        [InlineData(TesseraFlyoutPhase.Hidden, 4, false, false)]
        public void Dismiss_completion_hides_native_window_unless_superseded_or_already_hidden(
            TesseraFlyoutPhase phase,
            int currentGeneration,
            bool windowVisible,
            bool expected)
        {
            _ = TesseraFlyoutDismissPolicy.ShouldFinishTransientDismiss(
                4, currentGeneration, phase, windowVisible).Should().Be(expected);
        }

        [Fact]
        public void A_stacked_exit_finishes_when_nothing_superseded_it()
        {
            _ = TesseraFlyoutDismissPolicy.ShouldFinishStackedTransientDismiss(
                4, 4, [7, 9], [7, 9], TesseraFlyoutPhase.Exiting, anyWindowVisible: true).Should().BeTrue();
        }

        [Fact]
        public void A_present_during_a_stacked_exit_cancels_the_dismissal()
        {
            // A same-kind request re-presented the slots mid-exit without a new session (generation 4 both times);
            // only their motion generations moved. The stale exit then hid them anyway and the flyout vanished
            // despite the request (KDE, 2026-11-23).
            _ = TesseraFlyoutDismissPolicy.ShouldFinishStackedTransientDismiss(
                4, 4, [7, 9], [8, 10], TesseraFlyoutPhase.Exiting, anyWindowVisible: true).Should().BeFalse();
        }

        [Fact]
        public void One_re_presented_slot_is_enough_to_cancel_the_dismissal()
        {
            _ = TesseraFlyoutDismissPolicy.ShouldFinishStackedTransientDismiss(
                4, 4, [7, 9], [7, 10], TesseraFlyoutPhase.Exiting, anyWindowVisible: true).Should().BeFalse();
        }

        [Fact]
        public void A_changed_slot_set_cancels_the_dismissal()
        {
            _ = TesseraFlyoutDismissPolicy.ShouldFinishStackedTransientDismiss(
                4, 4, [7, 9], [7], TesseraFlyoutPhase.Exiting, anyWindowVisible: true).Should().BeFalse();
        }

        [Theory]
        [InlineData(5, TesseraFlyoutPhase.Exiting, true)]
        [InlineData(4, TesseraFlyoutPhase.Shown, true)]
        [InlineData(4, TesseraFlyoutPhase.Exiting, false)]
        public void A_stacked_exit_keeps_the_single_window_rules(int currentSession, TesseraFlyoutPhase phase, bool anyVisible)
        {
            _ = TesseraFlyoutDismissPolicy.ShouldFinishStackedTransientDismiss(
                4, currentSession, [7, 9], [7, 9], phase, anyVisible).Should().BeFalse();
        }

        [Fact]
        public void A_stacked_dismiss_begins_when_no_exit_is_running()
        {
            _ = TesseraFlyoutDismissPolicy.ShouldBeginStackedDismiss(null, [7, 9]).Should().BeTrue();
        }

        [Fact]
        public void A_dismiss_during_the_running_stacked_exit_leaves_that_exit_alone()
        {
            // The session auto-dismiss timer outlived an outside-click dismiss and fired 0.87 s into the exit; the second
            // dismiss restarted the motion at 22% opacity and replayed the media card's collapse (Windows, 2026-11-23).
            _ = TesseraFlyoutDismissPolicy.ShouldBeginStackedDismiss([7, 9], [7, 9]).Should().BeFalse();
        }

        [Theory]
        [InlineData(new[] { 8, 10 })]
        [InlineData(new[] { 7, 10 })]
        [InlineData(new[] { 7 })]
        public void A_dismiss_after_a_present_superseded_the_exit_begins_a_new_one(int[] current)
        {
            // A same-kind request re-presented the slots mid-exit; the windows can still report Exiting afterwards, so
            // only the motion generations show that the old exit no longer runs and this dismiss must hide them.
            _ = TesseraFlyoutDismissPolicy.ShouldBeginStackedDismiss([7, 9], current).Should().BeTrue();
        }

        [Fact]
        public void Completed_fade_still_requires_native_hide()
        {
            const bool windowVisible = true;
            _ = TesseraFlyoutLiveSyncPolicy.IsEffectivelyShowing(windowVisible, 0).Should().BeFalse();
            _ = TesseraFlyoutDismissPolicy.ShouldFinishTransientDismiss(
                4, 4, TesseraFlyoutPhase.Hidden, windowVisible).Should().BeTrue();
        }

        [Theory]
        [InlineData(TesseraFlyoutRefreshTrigger.VolumeTick, true)]
        [InlineData(TesseraFlyoutRefreshTrigger.BrightnessTick, true)]
        [InlineData(TesseraFlyoutRefreshTrigger.StatusToggle, true)]
        [InlineData(TesseraFlyoutRefreshTrigger.ShellMediaHook, true)]
        public void User_intent_triggers_reset_auto_dismiss(
            TesseraFlyoutRefreshTrigger trigger,
            bool expected)
        {
            MediaSessionInfo prev = new("A", "Artist", "app", true, null, 10, 180);
            MediaSessionInfo next = new("A", "Artist", "app", true, null, 0.5, 180);
            _ = TesseraFlyoutDismissPolicy.ShouldResetAutoDismiss(trigger, prev, next).Should().Be(expected);
        }

        [Fact]
        public void Media_title_change_resets_auto_dismiss()
        {
            MediaSessionInfo prev = new("A", "Artist", "app", true, null, 10, 180);
            MediaSessionInfo next = new("B", "Artist", "app", true, null, 0.5, 180);
            _ = TesseraFlyoutDismissPolicy.ShouldResetAutoDismiss(
                TesseraFlyoutRefreshTrigger.MediaSessionChanged, prev, next).Should().BeTrue();
        }

        [Fact]
        public void Media_position_restart_with_stale_title_resets_auto_dismiss()
        {
            MediaSessionInfo prev = new("A", "Artist", "app", true, null, 40, 180);
            MediaSessionInfo next = new("A", "Artist", "app", true, null, 0.5, 180);
            _ = TesseraFlyoutDismissPolicy.ShouldResetAutoDismiss(
                TesseraFlyoutRefreshTrigger.MediaSessionChanged, prev, next).Should().BeTrue();
        }
    }

    public class TesseraMediaTrackBoundaryTests
    {
        [Fact]
        public void Title_change_is_track_boundary()
        {
            MediaSessionInfo prev = new("A", "Artist", "app", true, null, 10, 180);
            MediaSessionInfo next = new("B", "Artist", "app", true, null, 0.5, 180);
            _ = TesseraMediaFlyoutPolicy.IsTrackBoundary(prev, next).Should().BeTrue();
        }

        [Fact]
        public void Late_album_art_is_not_track_boundary()
        {
            MediaSessionInfo prev = new("A", "Artist", "app", true, null, 10, 180);
            MediaSessionInfo next = new("A", "Artist", "app", true, [1, 2, 3], 10, 180);
            _ = TesseraMediaFlyoutPolicy.IsTrackBoundary(prev, next).Should().BeFalse();
        }

        [Fact]
        public void Play_state_toggle_is_not_track_boundary()
        {
            MediaSessionInfo prev = new("A", "Artist", "app", true, null, 10, 180);
            MediaSessionInfo next = new("A", "Artist", "app", false, null, 10, 180);
            _ = TesseraMediaFlyoutPolicy.IsTrackBoundary(prev, next).Should().BeFalse();
        }

        [Fact]
        public void Position_restart_with_stale_title_is_boundary_and_resets_dismiss()
        {
            MediaSessionInfo prev = new("A", "Artist", "app", true, null, 40, 180);
            MediaSessionInfo next = new("A", "Artist", "app", true, null, 0.5, 180);
            _ = TesseraMediaFlyoutPolicy.IsTrackBoundary(prev, next).Should().BeTrue();
            _ = TesseraMediaFlyoutPolicy.ShouldResetDismissForMediaChange(prev, next).Should().BeTrue();
        }
    }
}
