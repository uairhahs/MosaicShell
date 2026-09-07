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
