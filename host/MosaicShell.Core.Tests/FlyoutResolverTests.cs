using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// One resolver for "what does the presenter do with this request", for both topologies (audit F06). It must agree
    /// with the legacy single-window <see cref="FlyoutIngressPolicy"/> wherever that was right, and differ from it on
    /// purpose in two places: a changed structure is a rebuild (F02) and a request during an exit revives (F04).
    /// </summary>
    public sealed class FlyoutResolverTests
    {
        private static FlyoutRequest Request(string kind = "vol", string style = "Fluent", bool showMedia = false)
        {
            return new FlyoutRequest(
                ModuleIds.Tessera,
                kind,
                style,
                Payload: new Dictionary<string, string> { ["showMediaStrip"] = showMedia ? "1" : "0" });
        }

        private static FlyoutResolveState State(
            FlyoutRequest built,
            FlyoutRequest request,
            TesseraFlyoutPhase phase = TesseraFlyoutPhase.Shown,
            TesseraFlyoutSessionMode topology = TesseraFlyoutSessionMode.Single,
            bool hasWindow = true,
            bool visible = true,
            bool entranceInFlight = false,
            bool allowLivePatch = true,
            bool reuseHwnd = true,
            TesseraFlyoutSessionMode? builtTopology = null)
        {
            return new FlyoutResolveState
            {
                HasExistingWindow = hasWindow,
                ReuseRegisteredHwnd = reuseHwnd,
                BuiltStructure = FlyoutStructureKey.From(built, builtTopology ?? topology),
                RequestStructure = FlyoutStructureKey.From(request, topology),
                OpenKind = built.Kind,
                RequestKind = request.Kind,
                Phase = phase,
                EntranceMotionInFlight = entranceInFlight,
                OpenWindowVisible = visible,
                AllowLivePatch = allowLivePatch,
            };
        }

        [Fact]
        public void No_window_is_a_cold_build()
        {
            FlyoutResolver.Resolve(State(Request(), Request(), hasWindow: false)).Should().Be(FlyoutIngressDecision.ColdBuild);
        }

        [Fact]
        public void A_window_that_must_not_be_reused_is_closed_then_built_cold()
        {
            FlyoutResolver.Resolve(State(Request(), Request(), reuseHwnd: false))
                .Should().Be(FlyoutIngressDecision.CloseThenColdBuild);
        }

        [Theory]
        [InlineData(TesseraFlyoutSessionMode.Single)]
        [InlineData(TesseraFlyoutSessionMode.Stacked)]
        public void The_same_structure_on_a_shown_flyout_patches_the_active_session_in_either_topology(TesseraFlyoutSessionMode topology)
        {
            FlyoutResolver.Resolve(State(Request(), Request(), TesseraFlyoutPhase.Shown, topology))
                .Should().Be(FlyoutIngressDecision.PatchActiveSession);
        }

        [Theory]
        [InlineData(TesseraFlyoutSessionMode.Single)]
        [InlineData(TesseraFlyoutSessionMode.Stacked)]
        public void The_same_structure_on_a_visible_window_with_no_active_session_is_a_coalesced_patch(TesseraFlyoutSessionMode topology)
        {
            // SoftFrost keeps the HWND visible at opacity zero: no session phase, but a window that can take a patch.
            FlyoutResolver.Resolve(State(Request(), Request(), TesseraFlyoutPhase.Hidden, topology, visible: true))
                .Should().Be(FlyoutIngressDecision.PatchVisibleCoalesced);
        }

        [Theory]
        [InlineData(TesseraFlyoutSessionMode.Single)]
        [InlineData(TesseraFlyoutSessionMode.Stacked)]
        public void The_same_structure_during_an_entrance_patches_the_active_session(TesseraFlyoutSessionMode topology)
        {
            FlyoutResolver.Resolve(State(Request(), Request(), TesseraFlyoutPhase.Entering, topology, entranceInFlight: true))
                .Should().Be(FlyoutIngressDecision.PatchActiveSession);
        }

        [Fact]
        public void A_changed_media_strip_is_a_rebuild_whatever_the_phase_never_a_patch()
        {
            FlyoutRequest built = Request(showMedia: false);
            FlyoutRequest request = Request(showMedia: true);

            foreach (TesseraFlyoutPhase phase in new[] { TesseraFlyoutPhase.Entering, TesseraFlyoutPhase.Shown, TesseraFlyoutPhase.Exiting })
            {
                FlyoutResolver.Resolve(State(built, request, phase, entranceInFlight: phase == TesseraFlyoutPhase.Entering))
                    .Should().Be(FlyoutIngressDecision.RebuildStructure, phase.ToString());
            }
        }

        [Fact]
        public void A_changed_topology_is_a_rebuild()
        {
            FlyoutResolver.Resolve(State(Request(), Request(), TesseraFlyoutPhase.Shown, TesseraFlyoutSessionMode.Stacked,
                    builtTopology: TesseraFlyoutSessionMode.Single))
                .Should().Be(FlyoutIngressDecision.RebuildStructure);
        }

        [Fact]
        public void A_request_during_an_exit_revives_the_flyout_in_either_topology()
        {
            foreach (TesseraFlyoutSessionMode topology in new[] { TesseraFlyoutSessionMode.Single, TesseraFlyoutSessionMode.Stacked })
            {
                FlyoutResolver.Resolve(State(Request(), Request(), TesseraFlyoutPhase.Exiting, topology))
                    .Should().Be(FlyoutIngressDecision.ReviveDuringExit, topology.ToString());
            }
        }

        [Fact]
        public void A_hidden_window_of_the_same_surface_is_revived_hidden()
        {
            FlyoutResolver.Resolve(State(Request(), Request(), TesseraFlyoutPhase.Hidden, visible: false))
                .Should().Be(FlyoutIngressDecision.ReviveHidden);
        }

        [Fact]
        public void A_different_kind_on_a_visible_flyout_rebuilds_in_place()
        {
            FlyoutResolver.Resolve(State(Request("vol"), Request("media"))).Should().Be(FlyoutIngressDecision.RebuildInPlace);
        }

        [Fact]
        public void A_status_flyout_after_the_media_shell_kind_recreates_the_hwnd()
        {
            // The legacy rule: media to a status kind (locks) cannot reuse the shell window.
            FlyoutIngressState legacy = Legacy(Request("media"), Request("locks"));

            FlyoutResolver.Resolve(State(Request("media"), Request("locks")))
                .Should().Be(FlyoutIngressPolicy.Resolve(legacy));
            FlyoutResolver.Resolve(State(Request("media"), Request("locks"))).Should().Be(FlyoutIngressDecision.RecreateHwnd);
        }

        [Fact]
        public void A_patch_that_will_not_apply_is_not_allowed_to_patch_a_visible_flyout()
        {
            FlyoutResolver.Resolve(State(Request(), Request(), allowLivePatch: false))
                .Should().Be(FlyoutIngressPolicy.Resolve(Legacy(Request(), Request(), allowLivePatch: false)));
        }

        // ---- parity with the legacy resolver where it was right ----

        private static FlyoutIngressState Legacy(
            FlyoutRequest built,
            FlyoutRequest request,
            TesseraFlyoutPhase phase = TesseraFlyoutPhase.Shown,
            bool visible = true,
            bool entranceInFlight = false,
            bool allowLivePatch = true)
        {
            return new FlyoutIngressState
            {
                HasExistingWindow = true,
                ReuseRegisteredHwnd = true,
                OpenKind = built.Kind,
                OpenStyleId = built.StyleId,
                RequestKind = request.Kind,
                RequestStyleId = request.StyleId,
                Phase = phase,
                EntranceMotionInFlight = entranceInFlight,
                OpenWindowVisible = visible,
                AllowLivePatch = allowLivePatch,
            };
        }

        [Fact]
        public void It_agrees_with_the_legacy_resolver_on_every_state_where_neither_a_structure_change_nor_an_exit_is_involved()
        {
            string[] kinds = ["vol", "media", "locks"];
            string[] styles = ["Fluent", "Windows11"];
            TesseraFlyoutPhase[] phases = [TesseraFlyoutPhase.Hidden, TesseraFlyoutPhase.Entering, TesseraFlyoutPhase.Shown];
            int compared = 0;

            foreach (string openKind in kinds)
            foreach (string requestKind in kinds)
            foreach (string openStyle in styles)
            foreach (string requestStyle in styles)
            foreach (TesseraFlyoutPhase phase in phases)
            foreach (bool visible in new[] { true, false })
            foreach (bool inFlight in new[] { true, false })
            foreach (bool allow in new[] { true, false })
            {
                FlyoutRequest built = Request(openKind, openStyle);
                FlyoutRequest request = Request(requestKind, requestStyle);

                FlyoutIngressDecision expected = FlyoutIngressPolicy.Resolve(Legacy(built, request, phase, visible, inFlight, allow));
                FlyoutIngressDecision actual = FlyoutResolver.Resolve(State(
                    built, request, phase, visible: visible, entranceInFlight: inFlight, allowLivePatch: allow));

                actual.Should().Be(
                    expected,
                    $"open {openKind}/{openStyle} request {requestKind}/{requestStyle} phase {phase} visible {visible} inFlight {inFlight} allow {allow}");
                compared++;
            }

            compared.Should().Be(3 * 3 * 2 * 2 * 3 * 2 * 2 * 2);
        }

        [Fact]
        public void The_legacy_resolver_applies_then_loses_a_patch_during_an_exit_and_the_new_one_revives()
        {
            FlyoutIngressState legacy = Legacy(Request(), Request(), TesseraFlyoutPhase.Exiting);

            FlyoutIngressPolicy.Resolve(legacy).Should().Be(FlyoutIngressDecision.PatchVisibleCoalesced);
            FlyoutResolver.Resolve(State(Request(), Request(), TesseraFlyoutPhase.Exiting))
                .Should().Be(FlyoutIngressDecision.ReviveDuringExit);
        }
    }
}
