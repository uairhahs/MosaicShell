using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests;

public class TesseraFlyoutDismissPolicyTests
{
    [Theory]
    [InlineData(TesseraFlyoutRefreshTrigger.VolumeTick, true)]
    [InlineData(TesseraFlyoutRefreshTrigger.BrightnessTick, true)]
    [InlineData(TesseraFlyoutRefreshTrigger.StatusToggle, true)]
    [InlineData(TesseraFlyoutRefreshTrigger.ShellMediaHook, true)]
    public void User_intent_triggers_reset_auto_dismiss(
        TesseraFlyoutRefreshTrigger trigger,
        bool expected)
    {
        var prev = new MediaSessionInfo("A", "Artist", "app", true, null, 10, 180);
        var next = new MediaSessionInfo("A", "Artist", "app", true, null, 0.5, 180);
        TesseraFlyoutDismissPolicy.ShouldResetAutoDismiss(trigger, prev, next).Should().Be(expected);
    }

    [Fact]
    public void Media_title_change_resets_auto_dismiss()
    {
        var prev = new MediaSessionInfo("A", "Artist", "app", true, null, 10, 180);
        var next = new MediaSessionInfo("B", "Artist", "app", true, null, 0.5, 180);
        TesseraFlyoutDismissPolicy.ShouldResetAutoDismiss(
            TesseraFlyoutRefreshTrigger.MediaSessionChanged, prev, next).Should().BeTrue();
    }

    [Fact]
    public void Media_position_restart_with_stale_title_does_not_reset_auto_dismiss()
    {
        var prev = new MediaSessionInfo("A", "Artist", "app", true, null, 40, 180);
        var next = new MediaSessionInfo("A", "Artist", "app", true, null, 0.5, 180);
        TesseraFlyoutDismissPolicy.ShouldResetAutoDismiss(
            TesseraFlyoutRefreshTrigger.MediaSessionChanged, prev, next).Should().BeFalse();
    }
}

public class TesseraMediaTrackBoundaryTests
{
    [Fact]
    public void Title_change_is_track_boundary()
    {
        var prev = new MediaSessionInfo("A", "Artist", "app", true, null, 10, 180);
        var next = new MediaSessionInfo("B", "Artist", "app", true, null, 0.5, 180);
        TesseraMediaFlyoutPolicy.IsTrackBoundary(prev, next).Should().BeTrue();
    }

    [Fact]
    public void Late_album_art_is_not_track_boundary()
    {
        var prev = new MediaSessionInfo("A", "Artist", "app", true, null, 10, 180);
        var next = new MediaSessionInfo("A", "Artist", "app", true, [1, 2, 3], 10, 180);
        TesseraMediaFlyoutPolicy.IsTrackBoundary(prev, next).Should().BeFalse();
    }

    [Fact]
    public void Play_state_toggle_is_not_track_boundary()
    {
        var prev = new MediaSessionInfo("A", "Artist", "app", true, null, 10, 180);
        var next = new MediaSessionInfo("A", "Artist", "app", false, null, 10, 180);
        TesseraMediaFlyoutPolicy.IsTrackBoundary(prev, next).Should().BeFalse();
    }

    [Fact]
    public void Position_restart_with_stale_title_is_boundary_but_not_dismiss_reset()
    {
        var prev = new MediaSessionInfo("A", "Artist", "app", true, null, 40, 180);
        var next = new MediaSessionInfo("A", "Artist", "app", true, null, 0.5, 180);
        TesseraMediaFlyoutPolicy.IsTrackBoundary(prev, next).Should().BeTrue();
        TesseraMediaFlyoutPolicy.ShouldResetDismissForMediaChange(prev, next).Should().BeFalse();
    }
}
