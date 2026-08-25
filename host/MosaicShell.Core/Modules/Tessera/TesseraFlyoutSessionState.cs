using MosaicShell.Core.Capabilities;

namespace MosaicShell.Core.Modules.Tessera;

public enum TesseraFlyoutSessionMode
{
    None,
    Single,
    Stacked,
}

public readonly record struct TesseraFlyoutSessionSnapshot(
    bool EffectivelyShowing,
    int Generation,
    TesseraFlyoutSessionMode Mode,
    string Kind,
    string? StyleId);

/// <summary>
/// One Tessera HUD session. Generation increments on Present and Clear so
/// stale SoftRefresh / Patch work cannot land on a superseded kind or topology.
/// </summary>
public sealed class TesseraFlyoutSessionState
{
    public TesseraFlyoutSessionMode Mode { get; private set; }

    public int Generation { get; private set; }

    public string Kind { get; private set; } = "";

    public string? StyleId { get; private set; }

    public bool HasOpenSession => Mode != TesseraFlyoutSessionMode.None;

    public int Begin(TesseraFlyoutSessionMode mode, string kind, string? styleId)
    {
        if (mode == TesseraFlyoutSessionMode.None)
            throw new ArgumentOutOfRangeException(nameof(mode));

        Generation++;
        Mode = mode;
        Kind = kind ?? "";
        StyleId = styleId;
        return Generation;
    }

    public int Clear()
    {
        Generation++;
        Mode = TesseraFlyoutSessionMode.None;
        Kind = "";
        StyleId = null;
        return Generation;
    }

    public TesseraFlyoutSessionSnapshot Snapshot(bool effectivelyShowing) =>
        new(effectivelyShowing, Generation, Mode, Kind, StyleId);
}

public enum TesseraFlyoutIngressKind
{
    Present,
    Patch,
    SoftRefresh,
    Hide,
}

public readonly record struct TesseraFlyoutIngressWork(
    TesseraFlyoutIngressKind Kind,
    int Generation,
    FlyoutRequest Request,
    bool ResetDismiss);

public static class TesseraFlyoutIngressPolicy
{
    public const bool HostMustUseSingleIngressQueue = true;

    public const bool SoftRefreshMustHonorSessionGeneration = true;

    /// <summary>
    /// CapabilityFlyoutSession already chose Show / Update / SoftRefresh.
    /// Host must not call <see cref="TesseraFlyoutLiveSyncPolicy.ResolveAction"/> again.
    /// </summary>
    public const bool HostMustExecutePresenterCommandWithoutReResolve = true;

    public static bool IsStale(
        int workGeneration,
        int sessionGeneration,
        TesseraFlyoutIngressKind kind)
    {
        if (kind == TesseraFlyoutIngressKind.Present)
            return false;
        return SoftRefreshMustHonorSessionGeneration && workGeneration != sessionGeneration;
    }

    /// <summary>
    /// Last-value merge. SoftRefresh must not drop a pending Present or Patch.
    /// A Patch that lands while Present is queued updates that Present's payload.
    /// </summary>
    public static TesseraFlyoutIngressWork Merge(
        TesseraFlyoutIngressWork? pending,
        TesseraFlyoutIngressWork incoming)
    {
        if (pending is null)
            return incoming;

        var held = pending.Value;
        if (incoming.Kind == TesseraFlyoutIngressKind.Hide)
            return incoming;

        if (incoming.Kind == TesseraFlyoutIngressKind.SoftRefresh
            && held.Kind is TesseraFlyoutIngressKind.Present or TesseraFlyoutIngressKind.Patch)
            return held;

        if (held.Kind == TesseraFlyoutIngressKind.Present
            && incoming.Kind is TesseraFlyoutIngressKind.Present or TesseraFlyoutIngressKind.Patch)
        {
            return held with
            {
                Request = incoming.Request,
                ResetDismiss = held.ResetDismiss || incoming.ResetDismiss,
                Generation = incoming.Generation
            };
        }

        return incoming;
    }
}

/// <summary>Last-value ingress. Host stamps <see cref="TesseraFlyoutIngressWork.Generation"/> on enqueue.</summary>
public sealed class TesseraFlyoutIngressQueue
{
    private TesseraFlyoutIngressWork? _pending;

    public void Enqueue(TesseraFlyoutIngressWork work) =>
        _pending = TesseraFlyoutIngressPolicy.Merge(_pending, work);

    public TesseraFlyoutIngressWork? Peek() => _pending;

    public TesseraFlyoutIngressWork? Take(int sessionGeneration)
    {
        var work = _pending;
        _pending = null;
        if (work is null)
            return null;
        if (TesseraFlyoutIngressPolicy.IsStale(work.Value.Generation, sessionGeneration, work.Value.Kind))
            return null;
        return work;
    }

    public void CancelAll() => _pending = null;

    public bool HasPending => _pending is not null;
}
