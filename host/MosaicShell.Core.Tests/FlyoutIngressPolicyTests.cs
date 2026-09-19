using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class FlyoutIngressPolicyTests
    {
        private static FlyoutIngressState State(
            bool hasExistingWindow = true,
            bool reuseRegisteredHwnd = true,
            string openKind = "vol",
            string? openStyleId = "Fluent",
            string requestKind = "vol",
            string? requestStyleId = "Fluent",
            TesseraFlyoutPhase phase = TesseraFlyoutPhase.Shown,
            bool entranceMotionInFlight = false,
            bool openWindowVisible = true,
            bool allowLivePatch = true)
        {
            return new FlyoutIngressState
            {
                HasExistingWindow = hasExistingWindow,
                ReuseRegisteredHwnd = reuseRegisteredHwnd,
                OpenKind = openKind,
                OpenStyleId = openStyleId,
                RequestKind = requestKind,
                RequestStyleId = requestStyleId,
                Phase = phase,
                EntranceMotionInFlight = entranceMotionInFlight,
                OpenWindowVisible = openWindowVisible,
                AllowLivePatch = allowLivePatch,
            };
        }

        [Fact]
        public void No_window_yet_is_a_cold_build()
        {
            _ = FlyoutIngressPolicy.Resolve(State(hasExistingWindow: false))
                .Should().Be(FlyoutIngressDecision.ColdBuild);
        }

        [Fact]
        public void Existing_window_that_may_not_be_reused_is_closed_first()
        {
            _ = FlyoutIngressPolicy.Resolve(State(reuseRegisteredHwnd: false))
                .Should().Be(FlyoutIngressDecision.CloseThenColdBuild);
        }

        [Theory]
        [InlineData(TesseraFlyoutPhase.Entering)]
        [InlineData(TesseraFlyoutPhase.Shown)]
        public void Same_kind_and_style_during_an_active_session_patches_in_place(TesseraFlyoutPhase phase)
        {
            _ = FlyoutIngressPolicy.Resolve(State(phase: phase))
                .Should().Be(FlyoutIngressDecision.PatchActiveSession);
        }

        [Fact]
        public void Entrance_motion_counts_as_active_even_when_the_phase_lags()
        {
            _ = FlyoutIngressPolicy.Resolve(State(
                    phase: TesseraFlyoutPhase.Hidden,
                    entranceMotionInFlight: true))
                .Should().Be(FlyoutIngressDecision.PatchActiveSession);
        }

        [Fact]
        public void A_different_kind_during_an_active_session_does_not_patch()
        {
            _ = FlyoutIngressPolicy.Resolve(State(requestKind: "media"))
                .Should().NotBe(FlyoutIngressDecision.PatchActiveSession);
        }

        [Fact]
        public void A_different_style_during_an_active_session_does_not_patch()
        {
            _ = FlyoutIngressPolicy.Resolve(State(requestStyleId: "Windows11"))
                .Should().NotBe(FlyoutIngressDecision.PatchActiveSession);
        }

        [Fact]
        public void Kind_and_style_comparison_ignores_case()
        {
            _ = FlyoutIngressPolicy.Resolve(State(openKind: "VOL", openStyleId: "fluent"))
                .Should().Be(FlyoutIngressDecision.PatchActiveSession);
        }

        [Fact]
        public void Null_and_empty_style_ids_are_the_same_style()
        {
            _ = FlyoutIngressPolicy.Resolve(State(openStyleId: null, requestStyleId: ""))
                .Should().Be(FlyoutIngressDecision.PatchActiveSession);
        }

        [Fact]
        public void Visible_but_settled_window_takes_the_coalesced_patch_path()
        {
            _ = FlyoutIngressPolicy.Resolve(State(phase: TesseraFlyoutPhase.Exiting))
                .Should().Be(FlyoutIngressDecision.PatchVisibleCoalesced);
        }

        [Fact]
        public void Live_patch_can_be_refused_by_the_caller()
        {
            _ = FlyoutIngressPolicy.Resolve(State(
                    phase: TesseraFlyoutPhase.Exiting,
                    allowLivePatch: false))
                .Should().Be(FlyoutIngressDecision.RebuildInPlace);
        }

        [Fact]
        public void Hidden_window_of_the_same_kind_and_style_is_revived()
        {
            _ = FlyoutIngressPolicy.Resolve(State(
                    phase: TesseraFlyoutPhase.Hidden,
                    openWindowVisible: false))
                .Should().Be(FlyoutIngressDecision.ReviveHidden);
        }

        [Fact]
        public void Status_after_a_media_shell_recreates_the_hwnd()
        {
            _ = FlyoutIngressPolicy.Resolve(State(
                    openKind: "media",
                    requestKind: "locks",
                    phase: TesseraFlyoutPhase.Hidden,
                    openWindowVisible: false))
                .Should().Be(FlyoutIngressDecision.RecreateHwnd);
        }

        [Fact]
        public void Recreate_wins_over_revive_when_both_could_apply()
        {
            // Revive requires same kind, so a kind change that needs a fresh HWND must not be revived.
            FlyoutIngressDecision decision = FlyoutIngressPolicy.Resolve(State(
                openKind: "media",
                requestKind: "locks",
                phase: TesseraFlyoutPhase.Hidden,
                openWindowVisible: false));

            _ = decision.Should().NotBe(FlyoutIngressDecision.ReviveHidden);
        }

        [Fact]
        public void Different_kind_on_a_visible_window_rebuilds_in_place()
        {
            _ = FlyoutIngressPolicy.Resolve(State(
                    openKind: "vol",
                    requestKind: "bright",
                    phase: TesseraFlyoutPhase.Shown))
                .Should().Be(FlyoutIngressDecision.RebuildInPlace);
        }

        [Fact]
        public void Style_change_on_a_hidden_window_rebuilds_rather_than_revives()
        {
            _ = FlyoutIngressPolicy.Resolve(State(
                    requestStyleId: "Gnome",
                    phase: TesseraFlyoutPhase.Hidden,
                    openWindowVisible: false))
                .Should().Be(FlyoutIngressDecision.RebuildInPlace);
        }

        [Fact]
        public void Decisions_that_need_an_existing_window_never_appear_without_one()
        {
            FlyoutIngressDecision[] needWindow =
            [
                FlyoutIngressDecision.PatchActiveSession,
                FlyoutIngressDecision.PatchVisibleCoalesced,
                FlyoutIngressDecision.ReviveHidden,
                FlyoutIngressDecision.RebuildInPlace,
                FlyoutIngressDecision.RecreateHwnd,
            ];

            foreach (bool visible in new[] { true, false })
            {
                foreach (TesseraFlyoutPhase phase in Enum.GetValues<TesseraFlyoutPhase>())
                {
                    FlyoutIngressDecision decision = FlyoutIngressPolicy.Resolve(State(
                        hasExistingWindow: false,
                        phase: phase,
                        openWindowVisible: visible));

                    _ = needWindow.Should().NotContain(decision);
                }
            }
        }

        [Fact]
        public void Resolve_is_total_over_the_state_space()
        {
            bool[] bools = [true, false];
            string[] kinds = ["vol", "media", "locks"];
            string?[] styles = [null, "Fluent", "Gnome"];

            foreach (bool has in bools)
            {
                foreach (bool reuse in bools)
                {
                    foreach (string openKind in kinds)
                    {
                        foreach (string reqKind in kinds)
                        {
                            foreach (string? openStyle in styles)
                            {
                                foreach (string? reqStyle in styles)
                                {
                                    foreach (TesseraFlyoutPhase phase in Enum.GetValues<TesseraFlyoutPhase>())
                                    {
                                        foreach (bool motion in bools)
                                        {
                                            foreach (bool visible in bools)
                                            {
                                                foreach (bool allow in bools)
                                                {
                                                    FlyoutIngressDecision d = FlyoutIngressPolicy.Resolve(State(
                                                        has, reuse, openKind, openStyle, reqKind, reqStyle,
                                                        phase, motion, visible, allow));

                                                    _ = Enum.IsDefined(d).Should().BeTrue();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
