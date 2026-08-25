using Avalonia.Threading;
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
            var t0 = Environment.TickCount64;
            foreach (var window in windows)
                window.BeginMotion(entrance);

            if (entrance
                && TesseraFlyoutAnimationPolicy.ShowPhase2MustOverlapUntilMidBand
                && plan.Steps.Any(static s => s.Kind == TesseraFlyoutMotionStepKind.Phase2))
            {
                await RunOverlappedShowAsync(windows, plan, t0).ConfigureAwait(true);
            }
            else
            {
            foreach (var step in plan.Steps)
            {
                switch (step.Kind)
                {
                    case TesseraFlyoutMotionStepKind.Begin:
                    case TesseraFlyoutMotionStepKind.Complete:
                        break;
                    case TesseraFlyoutMotionStepKind.Wait:
                        #region agent log
                        TesseraFlyoutDiagnostics.AgentLog(
                            "A",
                            "FlyoutMotionSession.Wait",
                            "wait start",
                            new
                            {
                                entrance,
                                waitMs = step.WaitMs,
                                elapsedMs = Environment.TickCount64 - t0,
                                stacked,
                                style = request.StyleId,
                            });
                        #endregion
                        if (step.WaitMs > 0)
                            await Task.Delay(step.WaitMs).ConfigureAwait(true);
                        if (entrance)
                        {
                            foreach (var window in windows)
                                window.PreparePhase2Show();
                            if (TesseraFlyoutAnimationPolicy.PreparePhase2ShowMustYieldForRender
                                || TesseraFlyoutAnimationPolicy.PreparePhase2ShowMustPaintRestClientBeforeWipe)
                            {
                                await Dispatcher.UIThread.InvokeAsync(
                                    static () => { },
                                    DispatcherPriority.Render);
                            }

                            foreach (var window in windows)
                                window.PreparePhase2ShowArmWipe();
                            #region agent log
                            TesseraFlyoutDiagnostics.AgentLog(
                                "A",
                                "FlyoutMotionSession.Wait",
                                "show prepare after yield",
                                new
                                {
                                    elapsedMs = Environment.TickCount64 - t0,
                                    mediaOpacity = windows
                                        .Where(w => w.StackedRole == TesseraStackedPanelRole.Media)
                                        .Select(w => w.Opacity)
                                        .DefaultIfEmpty(-1)
                                        .First(),
                                });
                            #endregion
                        }

                        #region agent log
                        TesseraFlyoutDiagnostics.AgentLog(
                            "A",
                            "FlyoutMotionSession.Wait",
                            "wait end",
                            new { entrance, elapsedMs = Environment.TickCount64 - t0 });
                        #endregion
                        break;
                    case TesseraFlyoutMotionStepKind.Phase1:
                        var phase1 = Filter(windows, step);
                        #region agent log
                        TesseraFlyoutDiagnostics.AgentLog(
                            "E",
                            "FlyoutMotionSession.Phase1",
                            "phase1 start",
                            new
                            {
                                entrance,
                                count = phase1.Count,
                                elapsedMs = Environment.TickCount64 - t0,
                            });
                        #endregion
                        if (phase1.Count > 0)
                            await Task.WhenAll(phase1.Select(w => w.RunMotionPhase1Async(step.Entrance)))
                                .ConfigureAwait(true);
                        #region agent log
                        TesseraFlyoutDiagnostics.AgentLog(
                            "E",
                            "FlyoutMotionSession.Phase1",
                            "phase1 end",
                            new { entrance, elapsedMs = Environment.TickCount64 - t0 });
                        #endregion
                        break;
                    case TesseraFlyoutMotionStepKind.Phase2:
                        var phase2 = Filter(windows, step);
                        #region agent log
                        TesseraFlyoutDiagnostics.AgentLog(
                            "C",
                            "FlyoutMotionSession.Phase2",
                            "phase2 start",
                            new
                            {
                                entrance,
                                count = phase2.Count,
                                elapsedMs = Environment.TickCount64 - t0,
                                ease = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(
                                    request.AniEase, step.Entrance),
                            });
                        #endregion
                        if (phase2.Count > 0)
                            await FlyoutMotionController.RunPhase2SlotsAsync(phase2, step.Entrance)
                                .ConfigureAwait(true);
                        #region agent log
                        TesseraFlyoutDiagnostics.AgentLog(
                            "C",
                            "FlyoutMotionSession.Phase2",
                            "phase2 end",
                            new { entrance, elapsedMs = Environment.TickCount64 - t0 });
                        #endregion
                        break;
                }
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

    private static async Task RunOverlappedShowAsync(
        IReadOnlyList<FlyoutWindow> windows,
        TesseraFlyoutMotionPlan plan,
        long t0)
    {
        var request = windows[0].FlyoutRequest;
        var steps = TesseraFlyoutAnimationPolicy.NormalizeAniSteps(request.AniSteps);
        var overlap = TesseraFlyoutAnimationPolicy.ResolvePhase2ShowOverlapMs(request.AniEase, steps);
        var lead = TesseraFlyoutAnimationPolicy.ResolveShowPhase2LeadDelayMs(request.AniEase, steps);
        var phase1Step = plan.Steps.First(static s => s.Kind == TesseraFlyoutMotionStepKind.Phase1);
        var phase2Step = plan.Steps.First(static s => s.Kind == TesseraFlyoutMotionStepKind.Phase2);
        var phase1 = Filter(windows, phase1Step);
        var phase2 = Filter(windows, phase2Step);

        #region agent log
        TesseraFlyoutDiagnostics.AgentLog(
            "B",
            "FlyoutMotionSession.RunOverlappedShowAsync",
            "overlap schedule",
            new
            {
                ease = request.AniEase,
                steps,
                overlapMs = overlap,
                leadMs = lead,
                phaseMs = TesseraFlyoutAnimationPolicy.ResolvePhaseDurationMs(steps),
                elapsedMs = Environment.TickCount64 - t0,
            });
        #endregion

        var p1Task = phase1.Count > 0
            ? Task.WhenAll(phase1.Select(w => w.RunMotionPhase1Async(true)))
            : Task.CompletedTask;

        if (lead > 0)
            await Task.Delay(lead).ConfigureAwait(true);

        foreach (var window in windows)
            window.PreparePhase2Show();
        if (TesseraFlyoutAnimationPolicy.PreparePhase2ShowMustYieldForRender
            || TesseraFlyoutAnimationPolicy.PreparePhase2ShowMustPaintRestClientBeforeWipe)
        {
            await Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Render);
        }

        foreach (var window in windows)
            window.PreparePhase2ShowArmWipe();

        #region agent log
        TesseraFlyoutDiagnostics.AgentLog(
            "B",
            "FlyoutMotionSession.RunOverlappedShowAsync",
            "phase2 start overlapped",
            new
            {
                elapsedMs = Environment.TickCount64 - t0,
                count = phase2.Count,
                ease = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(request.AniEase, true),
            });
        #endregion

        var p2Task = phase2.Count > 0
            ? FlyoutMotionController.RunPhase2SlotsAsync(phase2, entrance: true)
            : Task.CompletedTask;
        await Task.WhenAll(p1Task, p2Task).ConfigureAwait(true);

        #region agent log
        TesseraFlyoutDiagnostics.AgentLog(
            "B",
            "FlyoutMotionSession.RunOverlappedShowAsync",
            "overlap complete",
            new { elapsedMs = Environment.TickCount64 - t0 });
        #endregion
    }
}
