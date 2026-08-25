using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host.Capabilities;

/// <summary>
/// Executes <see cref="TesseraFlyoutMotionPlan"/> for one HWND or a stacked cluster.
/// Phase work stays in <see cref="FlyoutMotionController"/>; this type is the only sequencer.
/// </summary>
internal static class FlyoutMotionSession
{
    public static Task RunShowAsync(IReadOnlyList<FlyoutWindow> windows) =>
        RunAsync(windows, entrance: true);

    public static Task RunHideAsync(IReadOnlyList<FlyoutWindow> windows) =>
        RunAsync(windows, entrance: false);

    private static async Task RunAsync(IReadOnlyList<FlyoutWindow> windows, bool entrance)
    {
        if (windows.Count == 0)
            return;

        _ = TesseraFlyoutMotionPlan.HostMustExecuteMotionPlanBothTopologies;

        var sample = windows[0];
        var request = sample.FlyoutRequest;
        var showMedia = TesseraFlyoutRequestBuilder.ShowMediaStripFromPayload(request.Payload);
        var stacked = windows.Any(static w => w.StackedRole is not null);
        var plan = entrance
            ? TesseraFlyoutMotionPlan.ResolveShow(request.Ani, request.StyleId, showMedia, stacked)
            : TesseraFlyoutMotionPlan.ResolveHide(request.Ani, request.StyleId, showMedia, stacked);

        try
        {
            foreach (var window in windows)
                window.BeginMotion(entrance);

            foreach (var step in plan.Steps)
            {
                switch (step.Kind)
                {
                    case TesseraFlyoutMotionStepKind.Begin:
                    case TesseraFlyoutMotionStepKind.Complete:
                        break;
                    case TesseraFlyoutMotionStepKind.Wait:
                        if (step.WaitMs > 0)
                            await Task.Delay(step.WaitMs).ConfigureAwait(true);
                        if (entrance)
                        {
                            foreach (var window in windows)
                                window.PreparePhase2Show();
                        }

                        break;
                    case TesseraFlyoutMotionStepKind.Phase1:
                        var phase1 = Filter(windows, step);
                        if (phase1.Count > 0)
                            await Task.WhenAll(phase1.Select(w => w.RunMotionPhase1Async(step.Entrance)))
                                .ConfigureAwait(true);
                        break;
                    case TesseraFlyoutMotionStepKind.Phase2:
                        var phase2 = Filter(windows, step);
                        if (phase2.Count > 0)
                            await Task.WhenAll(phase2.Select(w => w.RunMotionPhase2Async(step.Entrance)))
                                .ConfigureAwait(true);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            TesseraFlyoutDiagnostics.Log($"flyout motion failed: {ex.Message}");
        }
        finally
        {
            foreach (var window in windows)
                window.CompleteMotion(entrance);
        }
    }

    private static List<FlyoutWindow> Filter(
        IReadOnlyList<FlyoutWindow> windows,
        TesseraFlyoutMotionStep step)
    {
        var matched = new List<FlyoutWindow>();
        foreach (var window in windows)
        {
            var request = window.FlyoutRequest;
            var showMedia = TesseraFlyoutRequestBuilder.ShowMediaStripFromPayload(request.Payload);
            if (TesseraFlyoutMotionPlan.SlotParticipates(
                    step, request.Ani, request.StyleId, showMedia, window.StackedRole))
                matched.Add(window);
        }

        return matched;
    }
}
