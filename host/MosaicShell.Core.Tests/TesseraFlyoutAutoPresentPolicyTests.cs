using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class TesseraFlyoutAutoPresentPolicyTests
{
    [Theory]
    [InlineData(TesseraFlyoutRefreshTrigger.VolumeTick)]
    [InlineData(TesseraFlyoutRefreshTrigger.BrightnessTick)]
    [InlineData(TesseraFlyoutRefreshTrigger.StatusToggle)]
    [InlineData(TesseraFlyoutRefreshTrigger.ShellMediaHook)]
    public void User_intent_clears_suppress_gate(TesseraFlyoutRefreshTrigger trigger) =>
        TesseraFlyoutAutoPresentPolicy.IsUserIntentTrigger(trigger).Should().BeTrue();

    [Fact]
    public void Media_session_change_is_not_user_intent() =>
        TesseraFlyoutAutoPresentPolicy.IsUserIntentTrigger(
            TesseraFlyoutRefreshTrigger.MediaSessionChanged).Should().BeFalse();

    [Fact]
    public void Cold_present_allowed_when_not_suppressed() =>
        TesseraFlyoutAutoPresentPolicy.ShouldColdPresent(
            suppressAfterUserDismiss: false,
            TesseraFlyoutRefreshTrigger.MediaSessionChanged).Should().BeTrue();

    [Fact]
    public void Cold_present_blocked_for_media_while_suppressed() =>
        TesseraFlyoutAutoPresentPolicy.ShouldColdPresent(
            suppressAfterUserDismiss: true,
            TesseraFlyoutRefreshTrigger.MediaSessionChanged).Should().BeFalse();

    [Theory]
    [InlineData(TesseraFlyoutRefreshTrigger.VolumeTick)]
    [InlineData(TesseraFlyoutRefreshTrigger.BrightnessTick)]
    [InlineData(TesseraFlyoutRefreshTrigger.StatusToggle)]
    [InlineData(TesseraFlyoutRefreshTrigger.ShellMediaHook)]
    public void Cold_present_allowed_for_user_intent_while_suppressed(TesseraFlyoutRefreshTrigger trigger) =>
        TesseraFlyoutAutoPresentPolicy.ShouldColdPresent(
            suppressAfterUserDismiss: true,
            trigger).Should().BeTrue();
}
