using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The pipeline switch (C3a): <c>Legacy</c> runs today's resolver, <c>Shadow</c> runs it too and records where the
    /// unified resolver would have chosen differently, <c>Next</c> runs the unified resolver. Shadow must never change
    /// what executes, and every disagreement it reports must map to a row of the deliberate-changes ledger.
    /// </summary>
    public sealed class FlyoutPipelineSwitchTests
    {
        [Theory]
        [InlineData(null, FlyoutPipelineMode.Shadow)]
        [InlineData("", FlyoutPipelineMode.Shadow)]
        [InlineData("legacy", FlyoutPipelineMode.Legacy)]
        [InlineData("LEGACY", FlyoutPipelineMode.Legacy)]
        [InlineData(" shadow ", FlyoutPipelineMode.Shadow)]
        [InlineData("next", FlyoutPipelineMode.Next)]
        [InlineData("Next", FlyoutPipelineMode.Next)]
        [InlineData("2", FlyoutPipelineMode.Shadow)]
        [InlineData("fast", FlyoutPipelineMode.Shadow)]
        public void The_mode_is_read_by_name_and_defaults_to_shadow(string? configured, FlyoutPipelineMode expected)
        {
            FlyoutPipelineModes.Parse(configured).Should().Be(expected);
        }

        [Fact]
        public void Legacy_executes_the_legacy_decision_and_records_nothing()
        {
            FlyoutPipelineChoice choice = FlyoutPipelineSwitch.Choose(
                FlyoutPipelineMode.Legacy,
                FlyoutIngressDecision.PatchActiveSession,
                FlyoutIngressDecision.RebuildStructure);

            choice.Executed.Should().Be(FlyoutIngressDecision.PatchActiveSession);
            choice.Disagreement.Should().BeNull();
        }

        [Fact]
        public void Next_executes_the_unified_decision_and_records_nothing()
        {
            FlyoutPipelineChoice choice = FlyoutPipelineSwitch.Choose(
                FlyoutPipelineMode.Next,
                FlyoutIngressDecision.PatchActiveSession,
                FlyoutIngressDecision.RebuildStructure);

            choice.Executed.Should().Be(FlyoutIngressDecision.RebuildStructure);
            choice.Disagreement.Should().BeNull();
        }

        [Fact]
        public void Shadow_executes_the_legacy_decision_when_the_two_agree_and_records_nothing()
        {
            FlyoutPipelineChoice choice = FlyoutPipelineSwitch.Choose(
                FlyoutPipelineMode.Shadow,
                FlyoutIngressDecision.PatchVisibleCoalesced,
                FlyoutIngressDecision.PatchVisibleCoalesced);

            choice.Executed.Should().Be(FlyoutIngressDecision.PatchVisibleCoalesced);
            choice.Disagreement.Should().BeNull();
        }

        [Theory]
        [InlineData(FlyoutIngressDecision.PatchActiveSession, FlyoutIngressDecision.RebuildStructure, "F02")]
        [InlineData(FlyoutIngressDecision.PatchVisibleCoalesced, FlyoutIngressDecision.RebuildStructure, "F02")]
        [InlineData(FlyoutIngressDecision.ReviveHidden, FlyoutIngressDecision.RebuildStructure, "F02")]
        [InlineData(FlyoutIngressDecision.RebuildInPlace, FlyoutIngressDecision.RebuildStructure, "F02")]
        [InlineData(FlyoutIngressDecision.PatchActiveSession, FlyoutIngressDecision.ReviveDuringExit, "F04")]
        [InlineData(FlyoutIngressDecision.PatchVisibleCoalesced, FlyoutIngressDecision.ReviveDuringExit, "F04")]
        [InlineData(FlyoutIngressDecision.ReviveHidden, FlyoutIngressDecision.ReviveDuringExit, "F04")]
        [InlineData(FlyoutIngressDecision.RebuildInPlace, FlyoutIngressDecision.ReviveDuringExit, "F04")]
        public void Shadow_executes_the_legacy_decision_and_maps_a_known_difference_to_its_ledger_finding(
            FlyoutIngressDecision legacy, FlyoutIngressDecision next, string finding)
        {
            FlyoutPipelineChoice choice = FlyoutPipelineSwitch.Choose(FlyoutPipelineMode.Shadow, legacy, next);

            choice.Executed.Should().Be(legacy);
            choice.Disagreement.Should().NotBeNull();
            choice.Disagreement!.Value.Legacy.Should().Be(legacy);
            choice.Disagreement.Value.Next.Should().Be(next);
            choice.Disagreement.Value.LedgerFinding.Should().Be(finding);
            choice.Disagreement.Value.IsExplained.Should().BeTrue();
        }

        [Theory]
        [InlineData(FlyoutIngressDecision.ColdBuild, FlyoutIngressDecision.RebuildInPlace)]
        [InlineData(FlyoutIngressDecision.PatchActiveSession, FlyoutIngressDecision.PatchVisibleCoalesced)]
        [InlineData(FlyoutIngressDecision.RecreateHwnd, FlyoutIngressDecision.RebuildStructure)]
        [InlineData(FlyoutIngressDecision.RebuildInPlace, FlyoutIngressDecision.ReviveHidden)]
        public void Shadow_reports_a_difference_that_is_not_on_the_ledger_as_unexplained(
            FlyoutIngressDecision legacy, FlyoutIngressDecision next)
        {
            FlyoutPipelineChoice choice = FlyoutPipelineSwitch.Choose(FlyoutPipelineMode.Shadow, legacy, next);

            choice.Executed.Should().Be(legacy);
            choice.Disagreement.Should().NotBeNull();
            choice.Disagreement!.Value.LedgerFinding.Should().BeNull();
            choice.Disagreement.Value.IsExplained.Should().BeFalse();
        }

        [Fact]
        public void A_disagreement_renders_as_one_trace_line_with_both_values()
        {
            var disagreement = new FlyoutShadowDisagreement(
                FlyoutIngressDecision.PatchActiveSession, FlyoutIngressDecision.RebuildStructure, "F02");

            disagreement.ToTraceText().Should().Be("legacy=PatchActiveSession next=RebuildStructure ledger=F02");
            new FlyoutShadowDisagreement(FlyoutIngressDecision.ColdBuild, FlyoutIngressDecision.RebuildInPlace, null)
                .ToTraceText().Should().Be("legacy=ColdBuild next=RebuildInPlace ledger=none");
        }

        private static FlyoutRequest Request(string kind = "vol", string style = "Fluent", bool showMedia = false)
        {
            return new FlyoutRequest(
                ModuleIds.Tessera,
                kind,
                style,
                Payload: new Dictionary<string, string> { ["showMediaStrip"] = showMedia ? "1" : "0" });
        }

        private static FlyoutIngressState Ingress(
            FlyoutRequest open, FlyoutRequest request, TesseraFlyoutPhase phase, bool visible, bool entrance = false)
        {
            return new FlyoutIngressState
            {
                HasExistingWindow = true,
                ReuseRegisteredHwnd = true,
                OpenKind = open.Kind,
                OpenStyleId = open.StyleId,
                RequestKind = request.Kind,
                RequestStyleId = request.StyleId,
                Phase = phase,
                EntranceMotionInFlight = entrance,
                OpenWindowVisible = visible,
                AllowLivePatch = true,
            };
        }

        [Fact]
        public void The_adapter_carries_the_legacy_inputs_into_the_unified_state()
        {
            FlyoutRequest built = Request(showMedia: false);
            FlyoutRequest request = Request(showMedia: true);
            FlyoutIngressState legacy = Ingress(built, request, TesseraFlyoutPhase.Shown, visible: true, entrance: true);

            FlyoutResolveState state = FlyoutPipelineSwitch.ToResolveState(
                legacy,
                FlyoutStructureKey.From(built, TesseraFlyoutSessionMode.Single),
                FlyoutStructureKey.From(request, TesseraFlyoutSessionMode.Single));

            state.HasExistingWindow.Should().BeTrue();
            state.ReuseRegisteredHwnd.Should().BeTrue();
            state.OpenKind.Should().Be(legacy.OpenKind);
            state.RequestKind.Should().Be(legacy.RequestKind);
            state.Phase.Should().Be(TesseraFlyoutPhase.Shown);
            state.EntranceMotionInFlight.Should().BeTrue();
            state.OpenWindowVisible.Should().BeTrue();
            state.AllowLivePatch.Should().BeTrue();
            state.BuiltStructure.Should().NotBeNull();
        }

        [Fact]
        public void A_showMedia_flip_on_a_shown_flyout_is_a_shadow_disagreement_on_the_f02_row()
        {
            FlyoutRequest built = Request(showMedia: false);
            FlyoutRequest request = Request(showMedia: true);
            FlyoutIngressState legacy = Ingress(built, request, TesseraFlyoutPhase.Shown, visible: true);

            FlyoutPipelineChoice choice = FlyoutPipelineSwitch.Choose(
                FlyoutPipelineMode.Shadow,
                legacy,
                FlyoutStructureKey.From(built, TesseraFlyoutSessionMode.Single),
                FlyoutStructureKey.From(request, TesseraFlyoutSessionMode.Single));

            choice.Executed.Should().Be(FlyoutIngressDecision.PatchActiveSession);
            choice.Disagreement!.Value.LedgerFinding.Should().Be("F02");
        }

        [Fact]
        public void An_unchanged_structure_on_a_shown_flyout_has_no_shadow_disagreement()
        {
            FlyoutRequest built = Request();
            FlyoutIngressState legacy = Ingress(built, Request(), TesseraFlyoutPhase.Shown, visible: true);
            FlyoutStructureKey key = FlyoutStructureKey.From(built, TesseraFlyoutSessionMode.Single);

            FlyoutPipelineChoice choice = FlyoutPipelineSwitch.Choose(FlyoutPipelineMode.Shadow, legacy, key, key);

            choice.Executed.Should().Be(FlyoutIngressDecision.PatchActiveSession);
            choice.Disagreement.Should().BeNull();
        }

        [Fact]
        public void A_request_during_an_exit_is_a_shadow_disagreement_on_the_f04_row()
        {
            FlyoutRequest built = Request();
            FlyoutIngressState legacy = Ingress(built, Request(), TesseraFlyoutPhase.Exiting, visible: true);
            FlyoutStructureKey key = FlyoutStructureKey.From(built, TesseraFlyoutSessionMode.Single);

            FlyoutPipelineChoice choice = FlyoutPipelineSwitch.Choose(FlyoutPipelineMode.Shadow, legacy, key, key);

            choice.Executed.Should().Be(FlyoutIngressPolicy.Resolve(legacy));
            choice.Disagreement!.Value.LedgerFinding.Should().Be("F04");
            choice.Disagreement.Value.Next.Should().Be(FlyoutIngressDecision.ReviveDuringExit);
        }

        [Fact]
        public void Shadow_never_changes_what_executes_across_the_state_table()
        {
            string[] kinds = ["vol", "media", "status"];
            string[] styles = ["Fluent", "Windows11"];
            TesseraFlyoutPhase[] phases = Enum.GetValues<TesseraFlyoutPhase>();
            int checkedStates = 0;

            foreach (string openKind in kinds)
            foreach (string requestKind in kinds)
            foreach (string openStyle in styles)
            foreach (string requestStyle in styles)
            foreach (bool showMedia in new[] { false, true })
            foreach (TesseraFlyoutPhase phase in phases)
            foreach (bool visible in new[] { false, true })
            foreach (bool entrance in new[] { false, true })
            {
                FlyoutRequest built = Request(openKind, openStyle, showMedia: false);
                FlyoutRequest request = Request(requestKind, requestStyle, showMedia);
                FlyoutIngressState legacy = Ingress(built, request, phase, visible, entrance);

                FlyoutPipelineChoice choice = FlyoutPipelineSwitch.Choose(
                    FlyoutPipelineMode.Shadow,
                    legacy,
                    FlyoutStructureKey.From(built, TesseraFlyoutSessionMode.Single),
                    FlyoutStructureKey.From(request, TesseraFlyoutSessionMode.Single));

                choice.Executed.Should().Be(FlyoutIngressPolicy.Resolve(legacy));
                if (choice.Disagreement is { } disagreement)
                {
                    disagreement.IsExplained.Should().BeTrue(
                        $"{openKind}/{openStyle} -> {requestKind}/{requestStyle} media={showMedia} phase={phase} "
                        + $"visible={visible} entrance={entrance}: {disagreement.ToTraceText()}");
                }

                checkedStates++;
            }

            checkedStates.Should().BeGreaterThan(500);
        }
    }
}
