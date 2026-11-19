namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>Who runs a flyout's motion: its own window, or the presenter driving a cluster of windows.</summary>
    public enum FlyoutMotionDriver
    {
        Window,
        Presenter,
    }

    public enum FlyoutLifecycleActionKind
    {
        /// <summary>Play the entrance from <see cref="FlyoutLifecycleAction.FromOpacity"/>.</summary>
        StartEntrance,

        /// <summary>Play the exit from <see cref="FlyoutLifecycleAction.FromOpacity"/>.</summary>
        StartExit,

        /// <summary>
        /// Cancel the exit in flight and fade back in from the current opacity, over the base duration scaled by
        /// <see cref="FlyoutLifecycleAction.DurationScale"/>, the distance still to cover.
        /// </summary>
        ReviveFromExit,

        /// <summary>Build and swap in new content.</summary>
        ApplyContent,

        /// <summary>Update the existing content in place.</summary>
        PatchContent,

        /// <summary>Snap the reveal to rest, so a skipped entrance does not leave the start pose.</summary>
        SnapToRest,

        ResetDismissTimer,

        /// <summary>The exit has finished: hide the window (SoftFrost hides, it does not close).</summary>
        Hide,
    }

    /// <summary>
    /// One instruction for the window executor. <see cref="Generation"/> identifies the motion an instruction starts,
    /// so the completion that comes back can be told from a stale one.
    /// </summary>
    public readonly record struct FlyoutLifecycleAction(
        FlyoutLifecycleActionKind Kind,
        int Generation = 0,
        double FromOpacity = 0,
        double DurationScale = 1,
        FlyoutMotionDriver Driver = FlyoutMotionDriver.Window);

    /// <summary>
    /// The phase of one flyout and what to do on each event that changes it (audit F04). The Host window is an
    /// executor of the actions returned here; it no longer sets phase and motion flags in several places.
    /// <para>
    /// Opacity is only ever an input: it says where a motion starts from. No phase is inferred from a visibility
    /// threshold. Every motion carries a generation, and a completion with an old generation is ignored, so a
    /// superseded animation cannot hide or settle a flyout that has moved on.
    /// </para>
    /// </summary>
    public sealed class FlyoutWindowLifecycle
    {
        private static readonly IReadOnlyList<FlyoutLifecycleAction> None = [];

        private int _generation;

        public TesseraFlyoutPhase Phase { get; private set; } = TesseraFlyoutPhase.Hidden;

        /// <summary>True from a dismiss until that exit completes or is cancelled by a revive.</summary>
        public bool ExitInFlight { get; private set; }

        /// <summary>
        /// Who runs the motion. It is set when an entrance begins and kept across rebuilds while the flyout is
        /// up, so a rebuild cannot make the window run an exit the presenter already ran (F04 gap 5).
        /// </summary>
        public FlyoutMotionDriver Driver { get; private set; } = FlyoutMotionDriver.Window;

        /// <summary>
        /// Starts an entrance. The phase is Entering at once, even when the executor defers the motion itself
        /// (stacked show defers it by dispatcher hops), so a request in that gap is a patch (F04 gap 4).
        /// </summary>
        public IReadOnlyList<FlyoutLifecycleAction> BeginEntrance(double currentOpacity, FlyoutMotionDriver driver)
        {
            Driver = driver;
            ExitInFlight = false;
            Phase = TesseraFlyoutPhase.Entering;
            return [new(FlyoutLifecycleActionKind.StartEntrance, ++_generation, currentOpacity, Driver: Driver)];
        }

        public IReadOnlyList<FlyoutLifecycleAction> CompleteEntrance(int generation)
        {
            if (generation != _generation || Phase != TesseraFlyoutPhase.Entering)
            {
                return None;
            }

            Phase = TesseraFlyoutPhase.Shown;
            return None;
        }

        public IReadOnlyList<FlyoutLifecycleAction> CompleteExit(int generation)
        {
            if (generation != _generation || Phase != TesseraFlyoutPhase.Exiting)
            {
                return None;
            }

            Phase = TesseraFlyoutPhase.Hidden;
            ExitInFlight = false;
            return [new(FlyoutLifecycleActionKind.Hide, generation)];
        }

        /// <summary>The running motion is replaced or cancelled by the executor: its completion is now stale.</summary>
        public void Supersede()
        {
            _generation++;
        }

        public IReadOnlyList<FlyoutLifecycleAction> Dismiss(double currentOpacity)
        {
            if (Phase is TesseraFlyoutPhase.Hidden or TesseraFlyoutPhase.Exiting)
            {
                return None;
            }

            Phase = TesseraFlyoutPhase.Exiting;
            ExitInFlight = true;
            return [new(FlyoutLifecycleActionKind.StartExit, ++_generation, currentOpacity, Driver: Driver)];
        }

        /// <summary>A request that rebuilds the content (cold build, rebuild in place, revive of a hidden window).</summary>
        public IReadOnlyList<FlyoutLifecycleAction> ApplyRequest(double currentOpacity, FlyoutMotionDriver? driver = null)
        {
            switch (Phase)
            {
                case TesseraFlyoutPhase.Hidden:
                    {
                        FlyoutLifecycleAction entrance = BeginEntrance(currentOpacity, driver ?? Driver)[0];
                        return [new(FlyoutLifecycleActionKind.ApplyContent), entrance];
                    }

                case TesseraFlyoutPhase.Entering:
                    return [new(FlyoutLifecycleActionKind.ApplyContent), new(FlyoutLifecycleActionKind.ResetDismissTimer)];

                case TesseraFlyoutPhase.Shown:
                    return
                    [
                        new(FlyoutLifecycleActionKind.ApplyContent),
                        new(FlyoutLifecycleActionKind.SnapToRest),
                        new(FlyoutLifecycleActionKind.ResetDismissTimer),
                    ];

                default:
                    return
                    [
                        new(FlyoutLifecycleActionKind.ApplyContent),
                        Revive(currentOpacity),
                        new(FlyoutLifecycleActionKind.ResetDismissTimer),
                    ];
            }
        }

        /// <summary>A request that only updates the content in place.</summary>
        public IReadOnlyList<FlyoutLifecycleAction> Patch(double currentOpacity)
        {
            return Phase switch
            {
                TesseraFlyoutPhase.Hidden => None,
                TesseraFlyoutPhase.Exiting =>
                [
                    new(FlyoutLifecycleActionKind.PatchContent),
                    Revive(currentOpacity),
                    new(FlyoutLifecycleActionKind.ResetDismissTimer),
                ],
                _ => [new(FlyoutLifecycleActionKind.PatchContent), new(FlyoutLifecycleActionKind.ResetDismissTimer)],
            };
        }

        /// <summary>
        /// A new request during an exit is the user acting again: the exit is cancelled and the flyout fades back in
        /// from where it is, over the share of the entrance that is left to cover.
        /// </summary>
        private FlyoutLifecycleAction Revive(double currentOpacity)
        {
            Phase = TesseraFlyoutPhase.Entering;
            ExitInFlight = false;
            double clamped = Math.Clamp(currentOpacity, 0, 1);
            return new(FlyoutLifecycleActionKind.ReviveFromExit, ++_generation, clamped, 1 - clamped, Driver);
        }
    }
}
