using MosaicShell.Core.Capabilities;

namespace MosaicShell.Core.Modules.Tessera
{
    public enum TesseraFlyoutSessionMode
    {
        None,
        Single,
        Stacked,
    }

    /// <summary>
    /// Explicit session lifecycle phase.
    /// Replaces inference from opacity threshold (which collapsed entering, exiting, and pre-motion gap into hidden).
    /// </summary>
    public enum TesseraFlyoutPhase
    {
        Hidden,
        Entering,
        Shown,
        Exiting,
    }

    public static class TesseraFlyoutPhaseExtensions
    {
        /// <summary>
        /// True when a session exists and should accept in-place patches rather than cold-presenting.
        /// Both Entering (animation in flight) and Shown (visible at resting opacity) are active sessions.
        /// </summary>
        public static bool IsSessionActive(this TesseraFlyoutPhase phase)
        {
            return phase is TesseraFlyoutPhase.Entering or TesseraFlyoutPhase.Shown;
        }
    }

    public readonly record struct TesseraFlyoutSessionSnapshot(
        bool EffectivelyShowing,
        int Generation,
        TesseraFlyoutSessionMode Mode,
        string Kind,
        string? StyleId,
        TesseraFlyoutPhase Phase = TesseraFlyoutPhase.Hidden)
    {
        public bool IsSessionActive =>
            Phase.IsSessionActive() || (Phase == TesseraFlyoutPhase.Hidden && EffectivelyShowing);
    }

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

        public TesseraFlyoutPhase Phase { get; private set; } = TesseraFlyoutPhase.Hidden;

        public bool HasOpenSession => Mode != TesseraFlyoutSessionMode.None && Phase != TesseraFlyoutPhase.Hidden;

        public int Begin(TesseraFlyoutSessionMode mode, string kind, string? styleId, TesseraFlyoutPhase phase = TesseraFlyoutPhase.Entering)
        {
            if (mode == TesseraFlyoutSessionMode.None)
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            Generation++;
            Mode = mode;
            Kind = kind ?? "";
            StyleId = styleId;
            Phase = phase;
            return Generation;
        }

        public void SetPhase(TesseraFlyoutPhase phase)
        {
            Phase = phase;
        }

        public int Clear()
        {
            Generation++;
            Mode = TesseraFlyoutSessionMode.None;
            Kind = "";
            StyleId = null;
            Phase = TesseraFlyoutPhase.Hidden;
            return Generation;
        }

        public TesseraFlyoutSessionSnapshot Snapshot(bool effectivelyShowing, TesseraFlyoutPhase? phase = null)
        {
            return new(effectivelyShowing, Generation, Mode, Kind, StyleId, phase ?? Phase);
        }
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
            return kind != TesseraFlyoutIngressKind.Present && SoftRefreshMustHonorSessionGeneration && workGeneration != sessionGeneration;
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
            {
                return incoming;
            }

            TesseraFlyoutIngressWork held = pending.Value;
            return incoming.Kind == TesseraFlyoutIngressKind.Hide
                ? incoming
                : incoming.Kind == TesseraFlyoutIngressKind.SoftRefresh
                && held.Kind is TesseraFlyoutIngressKind.Present or TesseraFlyoutIngressKind.Patch
                ? held
                : held.Kind == TesseraFlyoutIngressKind.Present
                && incoming.Kind is TesseraFlyoutIngressKind.Present or TesseraFlyoutIngressKind.Patch
                ? (held with
                {
                    Request = incoming.Request,
                    ResetDismiss = held.ResetDismiss || incoming.ResetDismiss,
                    Generation = incoming.Generation
                })
                : incoming;
        }
    }

    /// <summary>Last-value ingress. Host stamps <see cref="TesseraFlyoutIngressWork.Generation"/> on enqueue.</summary>
    public sealed class TesseraFlyoutIngressQueue
    {
        private TesseraFlyoutIngressWork? _pending;

        public void Enqueue(TesseraFlyoutIngressWork work)
        {
            _pending = TesseraFlyoutIngressPolicy.Merge(_pending, work);
        }

        public TesseraFlyoutIngressWork? Peek()
        {
            return _pending;
        }

        public TesseraFlyoutIngressWork? Take(int sessionGeneration)
        {
            TesseraFlyoutIngressWork? work = _pending;
            _pending = null;
            return work is null
                ? null
                : TesseraFlyoutIngressPolicy.IsStale(work.Value.Generation, sessionGeneration, work.Value.Kind) ? null : work;
        }

        public void CancelAll()
        {
            _pending = null;
        }

        public bool HasPending => _pending is not null;
    }
}
