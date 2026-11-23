using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class FlyoutIngressSessionTests
    {
        private static readonly FlyoutIngressDecision[] Patches =
            [FlyoutIngressDecision.PatchActiveSession, FlyoutIngressDecision.PatchVisibleCoalesced];

        [Fact]
        public void Every_ingress_but_a_patch_of_a_showing_session_presents_the_session()
        {
            foreach (FlyoutIngressDecision decision in Enum.GetValues<FlyoutIngressDecision>())
            {
                _ = FlyoutIngressPolicy.PresentsSession(decision).Should().Be(!Patches.Contains(decision), decision.ToString());
            }
        }

        [Fact]
        public void A_revived_hidden_flyout_presents_the_session()
        {
            // Transient dismiss stops the outside-click watcher and closes the dim with the hide. The revive path
            // skipped both, so a revived flyout had no dim and ignored outside clicks (found 2026-11-23).
            _ = FlyoutIngressPolicy.PresentsSession(FlyoutIngressDecision.ReviveHidden).Should().BeTrue();
        }

        [Fact]
        public void The_outside_click_watcher_is_armed_whenever_the_session_is_presented()
        {
            foreach (FlyoutIngressDecision decision in Enum.GetValues<FlyoutIngressDecision>())
            {
                _ = TesseraFlyoutOutsideClickPolicy.MustArmOnIngress(decision)
                    .Should().Be(FlyoutIngressPolicy.PresentsSession(decision), decision.ToString());
            }
        }

        [Fact]
        public void The_dim_is_synced_whenever_the_session_is_presented()
        {
            foreach (FlyoutIngressDecision decision in Enum.GetValues<FlyoutIngressDecision>())
            {
                _ = TesseraFocusDimPolicy.MustSyncOnIngress(decision)
                    .Should().Be(FlyoutIngressPolicy.PresentsSession(decision), decision.ToString());
            }
        }
    }
}
