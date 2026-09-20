using Avalonia.Threading;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host.Capabilities
{
    /// <summary>
    /// Executes <see cref="TesseraFlyoutMotionPlan"/> for one HWND or a stacked cluster.
    /// Phase work stays in <see cref="FlyoutMotionController"/>; this type is the only sequencer.
    /// </summary>
    internal static class FlyoutMotionSession
    {
        public static Task RunShowAsync(IReadOnlyList<FlyoutWindow> windows)
        {
            return RunAsync(windows, entrance: true);
        }

        public static Task RunHideAsync(IReadOnlyList<FlyoutWindow> windows)
        {
            return RunAsync(windows, entrance: false);
        }

        private static async Task RunAsync(IReadOnlyList<FlyoutWindow> windows, bool entrance)
        {
            if (windows.Count == 0)
            {
                return;
            }

            _ = TesseraFlyoutMotionPlan.HostMustExecuteMotionPlanBothTopologies;

            FlyoutWindow sample = windows[0];
            FlyoutRequest request = sample.FlyoutRequest;
            bool showMedia = TesseraFlyoutRequestBuilder.ShowMediaStripFromPayload(request.Payload);
            bool stacked = windows.Any(static w => w.StackedRole is not null);
            TesseraFlyoutMotionPlan plan = entrance
                ? TesseraFlyoutMotionPlan.ResolveShow(request.Ani, request.StyleId, showMedia, stacked)
                : TesseraFlyoutMotionPlan.ResolveHide(request.Ani, request.StyleId, showMedia, stacked);

            // Volume and media entrances measured ~1.8x apart on the same style; these are every
            // input the plan is built from, plus how many slots each phase actually claims, so
            // the divergence can be read off directly instead of inferred from total duration.
            TesseraFlyoutDiagnostics.Log(
                $"motion plan entrance={entrance} kind={request.Kind} style={request.StyleId} "
                + $"ani={request.Ani} steps={request.AniSteps} showMedia={showMedia} stacked={stacked} "
                + $"windows={windows.Count} roles=[{string.Join(",", windows.Select(static w => w.StackedRole?.ToString() ?? "single"))}] "
                + $"plan=[{string.Join(",", plan.Steps.Select(static s => s.Kind.ToString()))}] "
                + $"claims=[{string.Join(",", plan.Steps.Select(s => s.Kind is TesseraFlyoutMotionStepKind.Phase1 or TesseraFlyoutMotionStepKind.Phase2 ? $"{s.Kind}:{Filter(windows, s).Count}" : s.Kind.ToString()))}]");

            try
            {
                foreach (FlyoutWindow window in windows)
                {
                    window.BeginMotion(entrance);
                }

                if (entrance
                    && TesseraFlyoutAnimationPolicy.ShowPhase2MustOverlapUntilMidBand
                    && plan.Steps.Any(static s => s.Kind == TesseraFlyoutMotionStepKind.Phase2))
                {
                    await RunOverlappedShowAsync(windows, plan).ConfigureAwait(true);
                }
                else
                {
                    foreach (TesseraFlyoutMotionStep step in plan.Steps)
                    {
                        switch (step.Kind)
                        {
                            case TesseraFlyoutMotionStepKind.Begin:
                            case TesseraFlyoutMotionStepKind.Complete:
                                break;
                            case TesseraFlyoutMotionStepKind.Wait:
                                if (step.WaitMs > 0)
                                {
                                    await Task.Delay(step.WaitMs).ConfigureAwait(true);
                                }

                                if (entrance)
                                {
                                    foreach (FlyoutWindow window in windows)
                                    {
                                        window.PreparePhase2Show();
                                    }

                                    if (TesseraFlyoutAnimationPolicy.PreparePhase2ShowMustYieldForRender
                                                                        || TesseraFlyoutAnimationPolicy.PreparePhase2ShowMustPaintRestClientBeforeWipe)
                                    {
                                        await Dispatcher.UIThread.InvokeAsync(
                                            static () => { },
                                            DispatcherPriority.Render);
                                    }

                                    foreach (FlyoutWindow window in windows)
                                    {
                                        window.PreparePhase2ShowArmWipe();
                                    }
                                }

                                break;
                            case TesseraFlyoutMotionStepKind.Phase1:
                                List<FlyoutWindow> phase1 = Filter(windows, step);
                                if (phase1.Count > 0)
                                {
                                    await Task.WhenAll(phase1.Select(w => w.RunMotionPhase1Async(step.Entrance)))
                                                                    .ConfigureAwait(true);
                                }
                                break;
                            case TesseraFlyoutMotionStepKind.Phase2:
                                List<FlyoutWindow> phase2 = Filter(windows, step);
                                if (phase2.Count > 0)
                                {
                                    await FlyoutMotionController.RunPhase2SlotsAsync(phase2, step.Entrance)
                                                                    .ConfigureAwait(true);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TesseraFlyoutDiagnostics.Log(
                    $"flyout motion failed: entrance={entrance} style={request.StyleId} stacked={stacked} {ex}");
            }
            finally
            {
                foreach (FlyoutWindow window in windows)
                {
                    try
                    {
                        window.CompleteMotion(entrance);
                    }
                    catch (Exception ex)
                    {
                        // The window can be disposed by a competing operation while this
                        // sequence was still in flight; cleanup must not throw a second,
                        // unhandled exception on top of whatever RunAsync already caught.
                        TesseraFlyoutDiagnostics.Log($"flyout motion cleanup failed: {ex}");
                    }
                }
            }
        }

        private static List<FlyoutWindow> Filter(
            IReadOnlyList<FlyoutWindow> windows,
            TesseraFlyoutMotionStep step)
        {
            List<FlyoutWindow> matched = [];
            foreach (FlyoutWindow window in windows)
            {
                FlyoutRequest request = window.FlyoutRequest;
                bool showMedia = TesseraFlyoutRequestBuilder.ShowMediaStripFromPayload(request.Payload);
                if (TesseraFlyoutMotionPlan.SlotParticipates(
                        step, request.Ani, request.StyleId, showMedia, window.StackedRole))
                {
                    matched.Add(window);
                }
            }

            return matched;
        }

        private static async Task RunOverlappedShowAsync(
            IReadOnlyList<FlyoutWindow> windows,
            TesseraFlyoutMotionPlan plan)
        {
            FlyoutRequest request = windows[0].FlyoutRequest;
            int steps = TesseraFlyoutAnimationPolicy.NormalizeAniSteps(request.AniSteps);
            int lead = TesseraFlyoutAnimationPolicy.ResolveShowPhase2LeadDelayMs(request.AniEase, steps);
            TesseraFlyoutMotionStep phase1Step = plan.Steps.First(static s => s.Kind == TesseraFlyoutMotionStepKind.Phase1);
            TesseraFlyoutMotionStep phase2Step = plan.Steps.First(static s => s.Kind == TesseraFlyoutMotionStepKind.Phase2);
            List<FlyoutWindow> phase1 = Filter(windows, phase1Step);
            List<FlyoutWindow> phase2 = Filter(windows, phase2Step);

            Task p1Task = phase1.Count > 0
                ? Task.WhenAll(phase1.Select(w => w.RunMotionPhase1Async(true)))
                : Task.CompletedTask;

            if (lead > 0)
            {
                await Task.Delay(lead).ConfigureAwait(true);
            }

            foreach (FlyoutWindow window in windows)
            {
                window.PreparePhase2Show();
            }

            if (TesseraFlyoutAnimationPolicy.PreparePhase2ShowMustYieldForRender
                || TesseraFlyoutAnimationPolicy.PreparePhase2ShowMustPaintRestClientBeforeWipe)
            {
                await Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Render);
            }

            foreach (FlyoutWindow window in windows)
            {
                window.PreparePhase2ShowArmWipe();
            }

            Task p2Task = phase2.Count > 0
                ? FlyoutMotionController.RunPhase2SlotsAsync(phase2, entrance: true)
                : Task.CompletedTask;
            await Task.WhenAll(p1Task, p2Task).ConfigureAwait(true);
        }
    }
}
