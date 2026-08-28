using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class TesseraFlyoutPresentHandoffPolicyTests
    {
        [Fact]
        public void Present_handoff_contracts_are_armed()
        {
            _ = TesseraFlyoutPresentHandoffPolicy.PresentMustInvalidatePendingQueuesOnSessionChange.Should().BeTrue();
            _ = TesseraFlyoutPresentHandoffPolicy.PresentMustStopOutsideClickBeforeRearm.Should().BeTrue();
            _ = TesseraFlyoutPresentHandoffPolicy.SoftRefreshMustScheduleDeferredLastValue.Should().BeTrue();
            _ = TesseraFlyoutOutsideClickPolicy.PresentMustStopPriorWatcherBeforeRearm.Should().BeTrue();
            _ = TesseraFlyoutLiveSyncPolicy.SoftRefreshMustScheduleDeferredLastValue.Should().BeTrue();
            _ = TesseraFlyoutPresentHandoffPolicy.SoftRefreshMustHonorSessionGeneration.Should().BeTrue();
            _ = TesseraFlyoutPresentHandoffPolicy.HideAllMustRunHideSynchronouslyOnUiThread.Should().BeTrue();
            _ = TesseraFlyoutPresentHandoffPolicy.ShouldPostHideToUiThread(alreadyOnUiThread: true)
                .Should().BeFalse();
            _ = TesseraFlyoutPresentHandoffPolicy.ShouldPostHideToUiThread(alreadyOnUiThread: false)
                .Should().BeTrue();
            _ = TesseraFlyoutOutsideClickPolicy.PatchMustRefreshBoundsWithoutRearm.Should().BeTrue();
            _ = TesseraFlyoutLiveSyncPolicy.PatchImpliesOutsideClickRearm.Should().BeFalse();
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
            _ = TesseraFlyoutPresentHandoffPolicy.MustInvalidatePendingWork(
                    hasOpen, openStacked, nextStacked, openKind, nextKind, openStyle, nextStyle)
                .Should().Be(expected);
        }

        [Fact]
        public void Status_kind_does_not_inherit_stacked_acrylic_eligibility()
        {
            Dictionary<string, string> stripOn = new() { ["showMediaStrip"] = "1", ["acrylic"] = "1" };
            _ = TesseraFlyoutMaterialFactory
                .OsAcrylicEligibleFromPayload(stripOn, "ModernFlyouts", kind: "locks")
                .Should().Be(TesseraOsAcrylicTrialPolicy.IsEligibleFromPayload(stripOn, "ModernFlyouts"));
        }
    }
}
