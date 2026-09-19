using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Capabilities.Platform
{
    /// <summary>What the presenter must do with an incoming <c>FlyoutRequest</c>.</summary>
    public enum FlyoutIngressDecision
    {
        /// <summary>No reusable window: build content and a fresh HWND.</summary>
        ColdBuild = 0,

        /// <summary>A window exists but must not be reused: close it, then cold build.</summary>
        CloseThenColdBuild = 1,

        /// <summary>Entrance motion or a shown session: patch content in place, no motion restart.</summary>
        PatchActiveSession = 2,

        /// <summary>Visible and settled: patch through the update coalescer.</summary>
        PatchVisibleCoalesced = 3,

        /// <summary>Hidden window of the same kind and style: patch, show, replay the entrance.</summary>
        ReviveHidden = 4,

        /// <summary>Kind change that a reused HWND cannot survive: close and build a new one.</summary>
        RecreateHwnd = 5,

        /// <summary>Reuse the HWND but rebuild its content and present again.</summary>
        RebuildInPlace = 6,
    }

    /// <summary>Everything the ingress decision depends on, sampled once under the presenter lock.</summary>
    public sealed record FlyoutIngressState
    {
        public required bool HasExistingWindow { get; init; }

        /// <summary>Mirrors <see cref="TesseraFlyoutLiveSyncPolicy.MustReuseRegisteredFlyoutHwnd"/>.</summary>
        public required bool ReuseRegisteredHwnd { get; init; }

        public required string? OpenKind { get; init; }
        public required string? OpenStyleId { get; init; }
        public required string? RequestKind { get; init; }
        public required string? RequestStyleId { get; init; }

        public required TesseraFlyoutPhase Phase { get; init; }

        /// <summary>Entrance animation still running; the phase can lag behind it by a frame.</summary>
        public required bool EntranceMotionInFlight { get; init; }

        public required bool OpenWindowVisible { get; init; }

        /// <summary>Caller opts out of live patching (for example a forced re-present).</summary>
        public required bool AllowLivePatch { get; init; }
    }

    /// <summary>
    /// The single owner of "what does the presenter do with this request". The Host used to derive
    /// this from window state inline, in parallel with <see cref="CapabilityFlyoutSession"/> and
    /// <see cref="FlyoutRefreshPolicy"/>, with nothing checking the two agreed.
    /// </summary>
    public static class FlyoutIngressPolicy
    {
        public static FlyoutIngressDecision Resolve(FlyoutIngressState state)
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
            bool sessionActive = state.EntranceMotionInFlight || state.Phase.IsSessionActive();

            return sameSurface && sessionActive ? FlyoutIngressDecision.PatchActiveSession
                : sameSurface && state.OpenWindowVisible && state.AllowLivePatch ? FlyoutIngressDecision.PatchVisibleCoalesced
                : TesseraStatusFlyoutPolicy.MustRecreateHwndAfterMediaShellKind(state.OpenKind, state.RequestKind) ? FlyoutIngressDecision.RecreateHwnd
                : sameSurface && !state.OpenWindowVisible ? FlyoutIngressDecision.ReviveHidden
                : FlyoutIngressDecision.RebuildInPlace;
        }

        /// <summary>A patch may fail at the binding level; the presenter then falls back to this.</summary>
        public static FlyoutIngressDecision OnPatchMissed(FlyoutIngressDecision attempted)
        {
            return attempted is FlyoutIngressDecision.PatchActiveSession
                       or FlyoutIngressDecision.PatchVisibleCoalesced
                       or FlyoutIngressDecision.ReviveHidden
                ? FlyoutIngressDecision.RebuildInPlace
                : attempted;
        }

        private static bool SameKindAndStyle(FlyoutIngressState state)
        {
            return string.Equals(state.OpenKind ?? "", state.RequestKind ?? "", StringComparison.OrdinalIgnoreCase)
                && string.Equals(state.OpenStyleId ?? "", state.RequestStyleId ?? "", StringComparison.OrdinalIgnoreCase);
        }
    }
}
