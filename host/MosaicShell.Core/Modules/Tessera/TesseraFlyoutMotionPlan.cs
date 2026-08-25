namespace MosaicShell.Core.Modules.Tessera;

public enum TesseraFlyoutMotionStepKind
{
    Begin,
    Phase1,
    Wait,
    Phase2,
    Complete,
}

/// <summary>
/// Which stacked HWNDs run a phase. Single-HWND plans use <see cref="All"/>.
/// </summary>
public enum TesseraFlyoutMotionSlotFilter
{
    All,
    Phase1Slots,
    Phase2Slots,
}

public readonly record struct TesseraFlyoutMotionStep(
    TesseraFlyoutMotionStepKind Kind,
    bool Entrance,
    TesseraFlyoutMotionSlotFilter SlotFilter,
    int WaitMs = 0);

/// <summary>
/// One sequencer for Fancy show/hide. Host stacked cluster and single HWND
/// execute this plan; they must not maintain a parallel phase graph.
/// </summary>
public sealed class TesseraFlyoutMotionPlan
{
    /// <summary>Host FlyoutMotionSession must run this plan for both topologies.</summary>
    public const bool HostMustExecuteMotionPlanBothTopologies = true;

    /// <summary>
    /// Stacked slots share one plan (all phase 2, then all phase 1 on hide).
    /// Do not WhenAll each HWND through the full entrance/exit independently.
    /// </summary>
    public const bool MotionPlanMustClusterStackedSlots = true;

    /// <summary>
    /// Clustered phase 2 must apply one progress per dispatcher tick to every slot.
    /// WhenAll of independent 16 ms pumps lets divider and media drift.
    /// </summary>
    public const bool HostMustPumpSharedPhase2Progress = true;

    public static bool ShouldPumpSharedPhase2Progress(int slotCount) =>
        HostMustPumpSharedPhase2Progress
        && TesseraFlyoutAnimationPolicy.Phase2MustShareOneVsyncProgressAcrossStackedSlots
        && slotCount > 1;

    public required bool Entrance { get; init; }

    public required bool Clustered { get; init; }

    public required IReadOnlyList<TesseraFlyoutMotionStep> Steps { get; init; }

    public static TesseraFlyoutMotionPlan ResolveShow(
        int ani,
        string? styleId,
        bool showMediaStrip,
        bool stacked) =>
        Build(
            entrance: true,
            ani,
            styleId,
            showMediaStrip,
            stacked,
            TesseraFlyoutAnimationPolicy.StackedShowMustFinishVolumePhase1BeforeMediaPhase2);

    public static TesseraFlyoutMotionPlan ResolveHide(
        int ani,
        string? styleId,
        bool showMediaStrip,
        bool stacked) =>
        Build(
            entrance: false,
            ani,
            styleId,
            showMediaStrip,
            stacked,
            TesseraFlyoutAnimationPolicy.StackedHideMustFinishPhase2BeforeAnySlotPhase1);

    public static IReadOnlyList<TesseraFlyoutMotionStepKind> MotionKinds(TesseraFlyoutMotionPlan plan)
    {
        var kinds = new List<TesseraFlyoutMotionStepKind>();
        foreach (var step in plan.Steps)
        {
            if (step.Kind is TesseraFlyoutMotionStepKind.Begin or TesseraFlyoutMotionStepKind.Complete)
                continue;
            kinds.Add(step.Kind);
        }

        return kinds;
    }

    public static bool SlotParticipates(
        TesseraFlyoutMotionStep step,
        int ani,
        string? styleId,
        bool showMediaStrip,
        TesseraStackedPanelRole? stackedRole)
    {
        return step.SlotFilter switch
        {
            TesseraFlyoutMotionSlotFilter.Phase1Slots =>
                TesseraFlyoutAnimationPolicy.ShouldRunPhase1(
                    ani, styleId, showMediaStrip, stackedRole, step.Entrance),
            TesseraFlyoutMotionSlotFilter.Phase2Slots =>
                TesseraFlyoutAnimationPolicy.ShouldRunPhase2Reveal(
                    ani, styleId, showMediaStrip, stackedRole),
            _ => true,
        };
    }

    private static TesseraFlyoutMotionPlan Build(
        bool entrance,
        int ani,
        string? styleId,
        bool showMediaStrip,
        bool stacked,
        bool clusterFlag)
    {
        var clustered = stacked && MotionPlanMustClusterStackedSlots && clusterFlag;
        var phase1Filter = clustered
            ? TesseraFlyoutMotionSlotFilter.Phase1Slots
            : TesseraFlyoutMotionSlotFilter.All;
        var phase2Filter = clustered
            ? TesseraFlyoutMotionSlotFilter.Phase2Slots
            : TesseraFlyoutMotionSlotFilter.All;
        var runPhase2 = TesseraFlyoutAnimationPolicy.Phase2RequiresAnimatedLayout(
            ani, styleId, showMediaStrip);

        var steps = new List<TesseraFlyoutMotionStep>
        {
            new(TesseraFlyoutMotionStepKind.Begin, entrance, TesseraFlyoutMotionSlotFilter.All),
        };

        if (entrance)
        {
            steps.Add(new(TesseraFlyoutMotionStepKind.Phase1, true, phase1Filter));
            if (runPhase2)
            {
                steps.Add(new(
                    TesseraFlyoutMotionStepKind.Wait,
                    true,
                    TesseraFlyoutMotionSlotFilter.All,
                    TesseraFlyoutAnimationPolicy.ResolveInterPhaseWaitMs(entrance: true)));
                steps.Add(new(TesseraFlyoutMotionStepKind.Phase2, true, phase2Filter));
            }
        }
        else
        {
            if (runPhase2)
            {
                steps.Add(new(TesseraFlyoutMotionStepKind.Phase2, false, phase2Filter));
                steps.Add(new(
                    TesseraFlyoutMotionStepKind.Wait,
                    false,
                    TesseraFlyoutMotionSlotFilter.All,
                    TesseraFlyoutAnimationPolicy.ResolveInterPhaseWaitMs(entrance: false)));
            }

            steps.Add(new(TesseraFlyoutMotionStepKind.Phase1, false, phase1Filter));
        }

        steps.Add(new(TesseraFlyoutMotionStepKind.Complete, entrance, TesseraFlyoutMotionSlotFilter.All));

        return new TesseraFlyoutMotionPlan
        {
            Entrance = entrance,
            Clustered = clustered,
            Steps = steps,
        };
    }
}
