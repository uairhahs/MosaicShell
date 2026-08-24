using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class TesseraFlyoutPresentHandoffPolicyTests
{
    [Fact]
    public void Present_handoff_contracts_are_armed()
    {
        TesseraFlyoutPresentHandoffPolicy.PresentMustInvalidatePendingQueuesOnSessionChange.Should().BeTrue();
        TesseraFlyoutPresentHandoffPolicy.PresentMustStopOutsideClickBeforeRearm.Should().BeTrue();
        TesseraFlyoutPresentHandoffPolicy.SoftRefreshMustScheduleDeferredLastValue.Should().BeTrue();
        TesseraFlyoutOutsideClickPolicy.PresentMustStopPriorWatcherBeforeRearm.Should().BeTrue();
        TesseraFlyoutLiveSyncPolicy.SoftRefreshMustScheduleDeferredLastValue.Should().BeTrue();
        TesseraFlyoutPresentHandoffPolicy.HideAllMustRunHideSynchronouslyOnUiThread.Should().BeTrue();
        TesseraFlyoutPresentHandoffPolicy.ShouldPostHideToUiThread(alreadyOnUiThread: true)
            .Should().BeFalse();
        TesseraFlyoutPresentHandoffPolicy.ShouldPostHideToUiThread(alreadyOnUiThread: false)
            .Should().BeTrue();
        TesseraFlyoutOutsideClickPolicy.PatchMustRefreshBoundsWithoutRearm.Should().BeTrue();
        TesseraFlyoutLiveSyncPolicy.PatchImpliesOutsideClickRearm.Should().BeFalse();
    }

    [Theory]
    [InlineData(false, false, false, "vol", "locks", "Meter", "Meter", false)]
    [InlineData(true, false, true, "locks", "vol", "Meter", "Meter", true)]
    [InlineData(true, true, false, "vol", "locks", "Meter", "Meter", true)]
    [InlineData(true, false, false, "locks", "vol", "Meter", "Meter", true)]
    [InlineData(true, false, false, "vol", "vol", "Meter", "Meter", false)]
    [InlineData(true, false, false, "vol", "vol", "Meter", "Fluent", true)]
    [InlineData(true, true, true, "vol", "vol", "Meter", "Meter", false)]
    public void Must_invalidate_pending_work_on_session_change(
        bool hasOpen,
        bool openStacked,
        bool nextStacked,
        string openKind,
        string nextKind,
        string openStyle,
        string nextStyle,
        bool expected)
    {
        TesseraFlyoutPresentHandoffPolicy.MustInvalidatePendingWork(
                hasOpen, openStacked, nextStacked, openKind, nextKind, openStyle, nextStyle)
            .Should().Be(expected);
    }

    [Fact]
    public void Status_kind_does_not_inherit_stacked_acrylic_eligibility()
    {
        var stripOn = new Dictionary<string, string> { ["showMediaStrip"] = "1", ["acrylic"] = "1" };
        TesseraFlyoutMaterialFactory
            .OsAcrylicEligibleFromPayload(stripOn, "ModernFlyouts", kind: "locks")
            .Should().Be(TesseraOsAcrylicTrialPolicy.IsEligibleFromPayload(stripOn, "ModernFlyouts"));
    }
}
