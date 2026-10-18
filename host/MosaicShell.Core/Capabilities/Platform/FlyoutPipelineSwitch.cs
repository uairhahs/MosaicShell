using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Capabilities.Platform
{
    /// <summary>Which resolver executes the presenter's ingress decision (C3a).</summary>
    public enum FlyoutPipelineMode
    {
        /// <summary>Today's <see cref="FlyoutIngressPolicy"/> executes; the unified resolver is not consulted.</summary>
        Legacy = 0,

        /// <summary>
        /// The legacy decision executes, and the unified <see cref="FlyoutResolver"/> is run on the same inputs so each
        /// disagreement can be traced. The default until the desktop pass (X2) passes.
        /// </summary>
        Shadow = 1,

        /// <summary>The unified resolver executes.</summary>
        Next = 2,
    }

    public static class FlyoutPipelineModes
    {
        /// <summary>Read once at startup, for example <c>next</c> to run the unified resolver.</summary>
        public const string EnvironmentVariable = "MOSAICSHELL_FLYOUT_PIPELINE";

        public const FlyoutPipelineMode Default = FlyoutPipelineMode.Shadow;

        /// <summary>
        /// The configured mode by name (case-insensitive), otherwise <see cref="Default"/>. Numbers and unknown names
        /// fall back to the default rather than guessing, as <c>DiagnosticLogLevels.Resolve</c> does.
        /// </summary>
        public static FlyoutPipelineMode Parse(string? configured)
        {
            return configured?.Trim().ToLowerInvariant() switch
            {
                "legacy" => FlyoutPipelineMode.Legacy,
                "shadow" => FlyoutPipelineMode.Shadow,
                "next" => FlyoutPipelineMode.Next,
                _ => Default,
            };
        }
    }

    /// <summary>
    /// One place where the legacy and the unified decisions differ. <see cref="LedgerFinding"/> names the row of the
    /// deliberate-changes ledger that explains it, or is null when nothing does (a regression until proven otherwise).
    /// </summary>
    public readonly record struct FlyoutShadowDisagreement(
        FlyoutIngressDecision Legacy,
        FlyoutIngressDecision Next,
        string? LedgerFinding)
    {
        public bool IsExplained => LedgerFinding is not null;

        public string ToTraceText()
        {
            return $"legacy={Legacy} next={Next} ledger={LedgerFinding ?? "none"}";
        }
    }

    /// <summary>What to execute, and the disagreement to trace when the mode is <see cref="FlyoutPipelineMode.Shadow"/>.</summary>
    public readonly record struct FlyoutPipelineChoice(
        FlyoutIngressDecision Executed,
        FlyoutShadowDisagreement? Disagreement);

    /// <summary>
    /// The pipeline switch. In <see cref="FlyoutPipelineMode.Shadow"/> the executed decision is always the legacy one;
    /// the only effect of the unified resolver is the disagreement it returns for the caller to trace.
    /// </summary>
    public static class FlyoutPipelineSwitch
    {
        public static FlyoutPipelineChoice Choose(
            FlyoutPipelineMode mode,
            FlyoutIngressState legacyState,
            FlyoutStructureKey? built,
            FlyoutStructureKey request)
        {
            ArgumentNullException.ThrowIfNull(legacyState);

            FlyoutIngressDecision legacy = FlyoutIngressPolicy.Resolve(legacyState);
            if (mode == FlyoutPipelineMode.Legacy)
            {
                return new FlyoutPipelineChoice(legacy, null);
            }

            FlyoutIngressDecision next = FlyoutResolver.Resolve(ToResolveState(legacyState, built, request));
            return Choose(mode, legacy, next);
        }

        public static FlyoutPipelineChoice Choose(
            FlyoutPipelineMode mode,
            FlyoutIngressDecision legacy,
            FlyoutIngressDecision next)
        {
            return mode switch
            {
                FlyoutPipelineMode.Next => new FlyoutPipelineChoice(next, null),
                FlyoutPipelineMode.Shadow when legacy != next =>
                    new FlyoutPipelineChoice(legacy, new FlyoutShadowDisagreement(legacy, next, LedgerFindingFor(legacy, next))),
                _ => new FlyoutPipelineChoice(legacy, null),
            };
        }

        /// <summary>The unified resolver's inputs from the legacy ones plus the structure keys.</summary>
        public static FlyoutResolveState ToResolveState(
            FlyoutIngressState legacyState,
            FlyoutStructureKey? built,
            FlyoutStructureKey request)
        {
            ArgumentNullException.ThrowIfNull(legacyState);
            return new FlyoutResolveState
            {
                HasExistingWindow = legacyState.HasExistingWindow,
                ReuseRegisteredHwnd = legacyState.ReuseRegisteredHwnd,
                BuiltStructure = built,
                RequestStructure = request,
                OpenKind = legacyState.OpenKind,
                RequestKind = legacyState.RequestKind,
                Phase = legacyState.Phase,
                EntranceMotionInFlight = legacyState.EntranceMotionInFlight,
                OpenWindowVisible = legacyState.OpenWindowVisible,
                AllowLivePatch = legacyState.AllowLivePatch,
            };
        }

        /// <summary>
        /// The two decisions the legacy resolver cannot produce are the two ledger rows that touch it: F02 (a changed
        /// structure rebuilds) and F04 (a request during an exit revives). They replace a patch, a hidden revive or an
        /// in-place rebuild; any other pairing is not on the ledger.
        /// </summary>
        private static string? LedgerFindingFor(FlyoutIngressDecision legacy, FlyoutIngressDecision next)
        {
            bool legacyReplaceable = legacy is FlyoutIngressDecision.PatchActiveSession
                or FlyoutIngressDecision.PatchVisibleCoalesced
                or FlyoutIngressDecision.ReviveHidden
                or FlyoutIngressDecision.RebuildInPlace;

            return legacyReplaceable
                ? next switch
                {
                    FlyoutIngressDecision.RebuildStructure => "F02",
                    FlyoutIngressDecision.ReviveDuringExit => "F04",
                    _ => null,
                }
                : null;
        }
    }
}
