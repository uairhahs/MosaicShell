using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class TesseraFlyoutRefreshPolicyTests
    {
        [Theory]
        [InlineData(false, "media", "media", TesseraFlyoutSyncAction.Present)]
        [InlineData(true, "media", "media", TesseraFlyoutSyncAction.Patch)]
        [InlineData(true, "vol", "vol", TesseraFlyoutSyncAction.Patch)]
        [InlineData(true, "locks", "locks", TesseraFlyoutSyncAction.Patch)]
        [InlineData(true, "vol", "media", TesseraFlyoutSyncAction.Present)]
        public void Warm_same_kind_patches_cold_presents(
            bool showing,
            string openKind,
            string nextKind,
            TesseraFlyoutSyncAction expected)
        {
            _ = TesseraFlyoutRefreshPolicy.ResolvePresentation(
                showing,
                openKind,
                nextKind,
                "Fluent",
                "Fluent").Should().Be(expected);
        }

        [Fact]
        public void Visible_media_shell_hook_patches_not_presents()
        {
            _ = TesseraFlyoutRefreshPolicy.ResolvePresentation(
                isEffectivelyShowing: true,
                openKind: "media",
                nextKind: "media",
                openStyle: "Fluent",
                nextStyle: "Fluent").Should().Be(TesseraFlyoutSyncAction.Patch);
        }

        [Fact]
        public void Status_toggle_while_showing_same_kind_patches()
        {
            _ = TesseraFlyoutRefreshPolicy.ResolvePresentation(
                isEffectivelyShowing: true,
                openKind: "locks",
                nextKind: "locks",
                openStyle: "Fluent",
                nextStyle: "Fluent").Should().Be(TesseraFlyoutSyncAction.Patch);
        }
    }
}
