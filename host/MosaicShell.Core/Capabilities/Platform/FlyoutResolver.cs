using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Capabilities.Platform
{
    /// <summary>
    /// Everything the unified resolver depends on, for either topology, sampled once under the presenter lock.
    /// </summary>
    public sealed record FlyoutResolveState
    {
        public required bool HasExistingWindow { get; init; }

        /// <summary>Mirrors <see cref="TesseraFlyoutLiveSyncPolicy.MustReuseRegisteredFlyoutHwnd"/>.</summary>
        public required bool ReuseRegisteredHwnd { get; init; }

        /// <summary>
        /// What the open content was built from, or null when that is not known. Null is treated as compatible, which
        /// is what the legacy resolver assumed.
        /// </summary>
        public required FlyoutStructureKey? BuiltStructure { get; init; }

        /// <summary>What the incoming request would be built from, on the topology it would run on.</summary>
        public required FlyoutStructureKey RequestStructure { get; init; }

        /// <summary>The raw kinds, for the status-flyout rule, which reads them as the Host has always spelled them.</summary>
        public required string? OpenKind { get; init; }

        public required string? RequestKind { get; init; }

        public required TesseraFlyoutPhase Phase { get; init; }

        /// <summary>Entrance animation still running; the phase can lag behind it by a frame.</summary>
        public required bool EntranceMotionInFlight { get; init; }

        public required bool OpenWindowVisible { get; init; }

        /// <summary>Caller opts out of live patching (for example a forced re-present).</summary>
        public required bool AllowLivePatch { get; init; }
    }

    /// <summary>
    /// The single owner of "what does the presenter do with this request" for single and stacked topologies (audit F06).
    /// It replaces the parallel deciders in the Host. Differences from <see cref="FlyoutIngressPolicy"/>, on purpose:
    /// a changed structure is <see cref="FlyoutIngressDecision.RebuildStructure"/>, never a patch (F02), and a
    /// request during an exit is <see cref="FlyoutIngressDecision.ReviveDuringExit"/> instead of a patch that lands on
    /// a flyout that is disappearing (F04).
    /// </summary>
    public static class FlyoutResolver
    {
        public static FlyoutIngressDecision Resolve(FlyoutResolveState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            if (!state.HasExistingWindow)
            {
                return FlyoutIngressDecision.ColdBuild;
            }

            if (!state.ReuseRegisteredHwnd)
            {
                return FlyoutIngressDecision.CloseThenColdBuild;
            }

            bool sameSurface = SameKindAndStyle(state);
            bool structureHolds = state.BuiltStructure is not { } built || state.RequestStructure.CanPatch(built);

            bool exiting = state.Phase == TesseraFlyoutPhase.Exiting;
            bool sessionActive = state.EntranceMotionInFlight || state.Phase.IsSessionActive();
            bool livePatch = state.OpenWindowVisible && state.AllowLivePatch;
            FlyoutIngressDecision orRebuild = FlyoutIngressDecision.RebuildStructure;

            // The order is the legacy resolver's; a structure that no longer holds turns each patch into a rebuild.
            return sameSurface && exiting
                ? structureHolds ? FlyoutIngressDecision.ReviveDuringExit : orRebuild
                : sameSurface && sessionActive
                ? structureHolds ? FlyoutIngressDecision.PatchActiveSession : orRebuild
                : sameSurface && livePatch
                ? structureHolds ? FlyoutIngressDecision.PatchVisibleCoalesced : orRebuild
                : TesseraStatusFlyoutPolicy.MustRecreateHwndAfterMediaShellKind(state.OpenKind, state.RequestKind)
                ? FlyoutIngressDecision.RecreateHwnd
                : sameSurface && !state.OpenWindowVisible
                ? FlyoutIngressDecision.ReviveHidden
                : FlyoutIngressDecision.RebuildInPlace;
        }

        private static bool SameKindAndStyle(FlyoutResolveState state)
        {
            FlyoutStructureKey request = state.RequestStructure;
            return state.BuiltStructure is { } built
                ? built.Kind == request.Kind && built.Style == request.Style
                : string.Equals(state.OpenKind ?? "", state.RequestKind ?? "", StringComparison.OrdinalIgnoreCase);
        }
    }
}
