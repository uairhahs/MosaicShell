using System.Diagnostics;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;
using MosaicShell.Host.Tiles.Tessera;

namespace MosaicShell.Host.Capabilities
{
    /// <summary>
    /// Phase 1 and phase 2 step runner. Sequencing lives in <see cref="FlyoutMotionSession"/>.
    /// </summary>
    internal static class FlyoutMotionController
    {
        internal sealed class MotionContext
        {
            public required FlyoutWindow Window { get; init; }
            public required double MonitorScale { get; init; }
            public required bool ShowMediaStrip { get; init; }
            public required bool RunPhase2 { get; init; }
            public Func<bool> IsCancelled { get; init; } = () => false;
            public CancellationToken MotionToken { get; init; }
        }

        internal static async Task RunPhase1OnlyAsync(MotionContext ctx, bool entrance)
        {
            if (ctx.IsCancelled())
            {
                return;
            }

            FlyoutRequest request = ctx.Window.FlyoutRequest;
            if (request.Ani <= 0)
            {
                await RunFadeAsync(ctx, entrance).ConfigureAwait(true);
                return;
            }

            await RunAvaloniaPhase1Async(ctx, entrance).ConfigureAwait(true);
        }

        internal static async Task RunPhase2SlotsAsync(IReadOnlyList<FlyoutWindow> windows, bool entrance)
        {
            if (windows.Count == 0)
            {
                return;
            }

            if (TesseraFlyoutMotionPlan.ShouldPumpSharedPhase2Progress(windows.Count))
            {
                await RunSharedPhase2Async(windows, entrance).ConfigureAwait(true);
                return;
            }

            await Task.WhenAll(windows.Select(w => w.RunMotionPhase2Async(entrance)))
                .ConfigureAwait(true);
        }

        internal static async Task RunPhase2OnlyAsync(MotionContext ctx, bool entrance)
        {
            if (ctx.IsCancelled())
            {
                return;
            }

            if (!ShouldRunPhase2(ctx))
            {
                if (entrance)
                {
                    EnsurePhase2Rest(ctx);
                }

                return;
            }

            if (TesseraFlyoutAnimationPolicy.Phase2MustSyncRevealRegionBeforeTweenBothWays)
            {
                ctx.Window.SyncRevealRegion();
            }

            await RunPhase2Async(ctx, entrance).ConfigureAwait(true);
        }

        private static bool ShouldRunPhase2(MotionContext ctx)
        {
            FlyoutRequest request = ctx.Window.FlyoutRequest;
            return ctx.RunPhase2
                   && TesseraFlyoutAnimationPolicy.Phase2RequiresAnimatedLayout(
                       request.Ani, request.StyleId, ctx.ShowMediaStrip);
        }

        private static async Task RunAvaloniaPhase1Async(MotionContext ctx, bool entrance)
        {
            FlyoutWindow window = ctx.Window;
            FlyoutRequest request = window.FlyoutRequest;
            Panel surface = window.MotionSurface;
            int ms = TesseraFlyoutAnimationPolicy.ResolvePhaseDurationMs(request.AniSteps);
            string ease = TesseraFlyoutAnimationPolicy.ResolvePhase1MotionEase(request.AniEase, entrance);
            string opacityEase = TesseraFlyoutAnimationPolicy.ResolvePhase1OpacityEase(request.AniEase, entrance);
            int steps = TesseraFlyoutAnimationPolicy.NormalizeAniSteps(request.AniSteps);
            bool slide = TesseraFlyoutAnimationPolicy.ShouldSlide(request.Ani);

            (double dxRest, double dyRest) = slide
                ? TesseraFlyoutAnimationPolicy.ResolveSlideOffsetDip(
                    ctx.MonitorScale, request.AniDir, request.AnimationDisplacement)
                : (0d, 0d);

            double opFrom = entrance ? 0d : 1d;
            double opTo = entrance ? 1d : 0d;
            double xFrom = entrance ? dxRest : 0d;
            double xTo = entrance ? 0d : dxRest;
            double yFrom = entrance ? dyRest : 0d;
            double yTo = entrance ? 0d : dyRest;

            bool moves = slide && (Math.Abs(dxRest) > 0.01 || Math.Abs(dyRest) > 0.01);
            TesseraSlideTarget target = TesseraFlyoutAnimationPolicy.ResolvePhase1SlideTarget(window.UsesOsAcrylicBackdrop());
            TranslateTransform? tt = null;
            if (moves && target == TesseraSlideTarget.RenderTransform)
            {
                tt = new TranslateTransform(xFrom, yFrom);
                window.RenderTransform = tt;
            }
            else
            {
                window.RenderTransform = null;
            }

            surface.Opacity = opFrom;

            List<Task> tasks =
            [
                AnimateSteppedAsync(
                    surface, Visual.OpacityProperty, opFrom, opTo, ms, opacityEase, steps, entrance,
                    cancellationToken: ctx.MotionToken),
            ];

            if (tt is not null)
            {
                // Avalonia 12 Animation.RunAsync casts the target to Visual. TranslateTransform
                // is Animatable but not Visual; that throws and aborts the whole show session
                // before phase 2. Run the X/Y setters on the window that owns RenderTransform.
                tasks.Add(AnimateSteppedAsync(window, TranslateTransform.XProperty, xFrom, xTo, ms, ease, steps, entrance,
                    cancellationToken: ctx.MotionToken));
                tasks.Add(AnimateSteppedAsync(window, TranslateTransform.YProperty, yFrom, yTo, ms, ease, steps, entrance,
                    cancellationToken: ctx.MotionToken));
            }
            else if (moves && target == TesseraSlideTarget.WindowPosition)
            {
                // Physical pixels: the same displacement the DIP offset was derived from.
                (int pxRest, int pyRest) = window.ResolveWindowSlideStartOffsetPx();
                tasks.Add(AnimateWindowSlideAsync(
                    window,
                    entrance ? pxRest : 0, entrance ? 0 : pxRest,
                    entrance ? pyRest : 0, entrance ? 0 : pyRest,
                    ms, ease, steps, entrance, ctx.MotionToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(true);
        }

        /// <summary>
        /// Moves the window (with its acrylic backdrop and region) from its rest position by an
        /// offset that runs the same stepped curve as the keyframe animations, sampled once per
        /// rendered frame against elapsed time, so a late frame catches up instead of stretching
        /// the run.
        /// </summary>
        private static Task AnimateWindowSlideAsync(
            FlyoutWindow window,
            double xFrom,
            double xTo,
            double yFrom,
            double yTo,
            int ms,
            string ease,
            int steps,
            bool entrance,
            CancellationToken cancellationToken)
        {
            TaskCompletionSource done = new(TaskCreationOptions.RunContinuationsAsynchronously);
            double duration = Math.Max(1, ms);
            long started = Stopwatch.GetTimestamp();

            void Frame(TimeSpan frameTime)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _ = done.TrySetResult();
                    return;
                }

                double t = Stopwatch.GetElapsedTime(started).TotalMilliseconds / duration;
                window.ApplySlideOffsetPx(
                    TesseraFlyoutAnimationPolicy.SampleSteppedAt(xFrom, xTo, t, steps, ease, entrance),
                    TesseraFlyoutAnimationPolicy.SampleSteppedAt(yFrom, yTo, t, steps, ease, entrance));
                if (t >= 1)
                {
                    _ = done.TrySetResult();
                    return;
                }

                window.RequestAnimationFrame(Frame);
            }

            window.ApplySlideOffsetPx(xFrom, yFrom);
            window.RequestAnimationFrame(Frame);
            return done.Task;
        }

        private static async Task RunPhase2Async(MotionContext ctx, bool entrance)
        {
            FlyoutRequest request = ctx.Window.FlyoutRequest;
            string ease = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(request.AniEase, entrance);
            int steps = TesseraFlyoutAnimationPolicy.NormalizeAniSteps(request.AniSteps);

            List<TesseraRevealHost> hosts = await CollectRevealHostsOrYieldAsync(ctx.Window).ConfigureAwait(true);
            if (hosts.Count == 0)
            {
                if (TesseraFlyoutAnimationPolicy.ShouldSnapMissingPhase2HostsToRest(entrance))
                {
                    EnsurePhase2Rest(ctx);
                }

                return;
            }

            ctx.Window.Phase2Animating = true;
            foreach (TesseraRevealHost host in hosts)
            {
                host.Phase2Engaged = true;
            }

            try
            {
                await PumpPhase2StepsAsync([(ctx, hosts)], ease, steps, entrance).ConfigureAwait(true);
            }
            finally
            {
                FinishPhase2(ctx.Window, hosts, entrance);
            }
        }

        private static async Task RunSharedPhase2Async(IReadOnlyList<FlyoutWindow> windows, bool entrance)
        {
            List<(MotionContext Ctx, List<TesseraRevealHost> Hosts)> armed = [];
            try
            {
                foreach (FlyoutWindow window in windows)
                {
                    if (!window.TryCreatePhase2Context(out MotionContext? ctx) || ctx is null)
                    {
                        continue;
                    }

                    if (ctx.IsCancelled())
                    {
                        continue;
                    }

                    if (!ShouldRunPhase2(ctx))
                    {
                        if (entrance)
                        {
                            EnsurePhase2Rest(ctx);
                        }

                        continue;
                    }

                    if (TesseraFlyoutAnimationPolicy.Phase2MustSyncRevealRegionBeforeTweenBothWays)
                    {
                        window.SyncRevealRegion();
                    }

                    List<TesseraRevealHost> hosts = await CollectRevealHostsOrYieldAsync(window).ConfigureAwait(true);
                    if (hosts.Count == 0)
                    {
                        if (TesseraFlyoutAnimationPolicy.ShouldSnapMissingPhase2HostsToRest(entrance))
                        {
                            EnsurePhase2Rest(ctx);
                        }

                        continue;
                    }

                    window.Phase2Animating = true;
                    foreach (TesseraRevealHost host in hosts)
                    {
                        host.Phase2Engaged = true;
                    }

                    armed.Add((ctx, hosts));
                }

                if (armed.Count == 0)
                {
                    return;
                }

                FlyoutRequest request = armed[0].Ctx.Window.FlyoutRequest;
                string ease = TesseraFlyoutAnimationPolicy.ResolvePhase2MotionEase(request.AniEase, entrance);
                int steps = TesseraFlyoutAnimationPolicy.NormalizeAniSteps(request.AniSteps);
                await PumpPhase2StepsAsync(armed, ease, steps, entrance).ConfigureAwait(true);
            }
            finally
            {
                foreach ((MotionContext? ctx, List<TesseraRevealHost>? hosts) in armed)
                {
                    FinishPhase2(ctx.Window, hosts, entrance);
                }
            }
        }

        /// <summary>
        /// Runs the reveal against elapsed time. It used to apply one lump per 16 ms <c>Task.Delay</c> and count steps, so a
        /// late wake-up stretched the run (714 ms against 384 planned, audit F05) and the reveal moved in visible jumps. Now
        /// every wake-up reads the clock, takes the interpolated progress for that moment and applies it, so a late tick
        /// catches up and the duration is the planned one. Each value waits for the layout and render work it queued, so
        /// updates run as fast as the UI thread can draw them and never pile up (asking every 8 ms made the cadence worse,
        /// about 14 updates an entrance against 20). Frame callbacks (<c>RequestAnimationFrame</c>) delivered only 16 a
        /// second while two acrylic windows repainted. The system timer is raised to 1 ms for the run so the short floor
        /// between updates is short. All slots share one clock and one progress value (a stacked cluster reveals in step).
        /// </summary>
        private static async Task PumpPhase2StepsAsync(
            IReadOnlyList<(MotionContext Ctx, List<TesseraRevealHost> Hosts)> slots,
            string ease,
            int steps,
            bool entrance)
        {
            if (slots.Count == 0)
            {
                return;
            }

            CancellationToken token = slots[0].Ctx.MotionToken;
            double durationMs = Math.Max(1, TesseraFlyoutAnimationPolicy.ResolvePhaseDurationMs(steps));
            bool timed = TesseraFlyoutDiagnostics.IsEnabled(DiagnosticLogLevel.Debug);
            List<double>? frameElapsedMs = timed ? [] : null;
            List<double>? applyCostMs = timed ? [] : null;
            using MotionTimerResolution timerScope = MotionTimerResolution.Begin();
            long started = Stopwatch.GetTimestamp();
            try
            {
                while (true)
                {
                    if (slots.Any(static s => s.Ctx.IsCancelled() || s.Ctx.MotionToken.IsCancellationRequested))
                    {
                        return;
                    }

                    double elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                    double t = Math.Min(1, elapsed / durationMs);
                    double progress = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgressAt(entrance, t, steps, ease);
                    long applySpan = TesseraFlyoutDiagnostics.BeginSpan();
                    long applyStart = Stopwatch.GetTimestamp();
                    foreach ((MotionContext? ctx, List<TesseraRevealHost>? hosts) in slots)
                    {
                        ApplyPhase2Progress(ctx, hosts, progress, entrance);
                    }

                    applyCostMs?.Add(Stopwatch.GetElapsedTime(applyStart).TotalMilliseconds);
                    TesseraFlyoutDiagnostics.EndSpan(applySpan, $"phase2Apply t={t:0.00}");
                    frameElapsedMs?.Add(elapsed);
                    if (t >= 1)
                    {
                        break;
                    }

                    // Wait for the layout and render work this value queued before asking for the next one, so updates
                    // never pile up behind the UI thread; then keep a short floor so an idle UI thread cannot spin us.
                    await Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Render);
                    double spent = Stopwatch.GetElapsedTime(started).TotalMilliseconds - elapsed;
                    if (spent < TesseraFlyoutAnimationPolicy.Phase2UpdateIntervalMs)
                    {
                        await Task.Delay(
                            Math.Max(1, (int)Math.Ceiling(TesseraFlyoutAnimationPolicy.Phase2UpdateIntervalMs - spent)), token)
                            .ConfigureAwait(true);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                /* superseded show/hide */
            }
            finally
            {
                if (frameElapsedMs is not null)
                {
                    TesseraFlyoutDiagnostics.Log(
                        DiagnosticLogLevel.Debug,
                        FlyoutPhaseTiming.Format(
                            slots[0].Ctx.Window.FlyoutRequest.StyleId,
                            entrance,
                            FlyoutPhaseTiming.SummarizeFrames(frameElapsedMs, durationMs))
                        + (applyCostMs is { Count: > 0 }
                            ? $" applyMeanMs={applyCostMs.Average():0.0} applyMaxMs={applyCostMs.Max():0.0}"
                            : string.Empty));
                }
            }
        }

        private static void ApplyPhase2Progress(
            MotionContext ctx,
            List<TesseraRevealHost> hosts,
            double progress,
            bool entrance)
        {
            bool wipeRegion = TesseraFlyoutHwndRegionSpec.ShouldWipeShowRegionOverRestLayout(
                entrance, ctx.Window.StackedRole, ctx.RunPhase2);
            foreach (TesseraRevealHost host in hosts)
            {
                host.Phase2Engaged = true;
                host.RevealProgress = wipeRegion
                    ? TesseraFlyoutHwndRegionSpec.ResolveShowLayoutRevealProgress(true)
                    : progress;
            }

            if (wipeRegion)
            {
                ctx.Window.SetMotionRegionProgress(progress);
            }

            ctx.Window.SyncRevealRegion();
            if (!wipeRegion)
            {
                ctx.Window.InvalidateVisual();
            }
        }

        private static void FinishPhase2(FlyoutWindow window, List<TesseraRevealHost> hosts, bool entrance)
        {
            window.Phase2Animating = false;
            foreach (TesseraRevealHost host in hosts)
            {
                host.Phase2Engaged = entrance;
            }
        }

        private static async Task<List<TesseraRevealHost>> CollectRevealHostsOrYieldAsync(FlyoutWindow window)
        {
            List<TesseraRevealHost> hosts = CollectRevealHosts(window);
            if (hosts.Count > 0)
            {
                return hosts;
            }

            await Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Render);
            return CollectRevealHosts(window);
        }

        private static List<TesseraRevealHost> CollectRevealHosts(FlyoutWindow window)
        {
            return [.. window.GetVisualDescendants().OfType<TesseraRevealHost>()];
        }

        private static async Task RunFadeAsync(MotionContext ctx, bool entrance)
        {
            FlyoutRequest request = ctx.Window.FlyoutRequest;
            int ms = TesseraFlyoutAnimationPolicy.ResolveDurationMs(0, request.AniSteps);
            Panel surface = ctx.Window.MotionSurface;
            double from = entrance ? 0d : surface.Opacity;
            double to = entrance ? 1d : 0d;
            ctx.Window.RenderTransform = null;
            await AnimateSteppedAsync(
                    surface,
                    Visual.OpacityProperty,
                    from,
                    to,
                    ms,
                    TesseraFlyoutAnimationPolicy.ResolvePhase1OpacityEase(request.AniEase, entrance),
                    TesseraFlyoutAnimationPolicy.ResolveFadeSampleCount(),
                    entrance,
                    cancellationToken: ctx.MotionToken)
                .ConfigureAwait(true);
        }

        internal static async Task AnimateSteppedAsync(
            Animatable target,
            AvaloniaProperty property,
            double from,
            double to,
            int ms,
            string ease,
            int steps,
            bool entrance,
            CancellationToken cancellationToken = default)
        {
            int duration = Math.Max(1, ms);
            int sampleCount = Math.Max(1, steps);
            Animation animation = new()
            {
                Duration = TimeSpan.FromMilliseconds(duration),
                FillMode = FillMode.Forward,
                Easing = new LinearEasing(),
            };

            for (int i = 0; i <= sampleCount; i++)
            {
                double value = TesseraFlyoutAnimationPolicy.InterpolateStepped(
                    from, to, i, sampleCount, ease, entrance);
                double linearT = i / (double)sampleCount;
                animation.Children.Add(new KeyFrame
                {
                    Cue = new Cue(linearT),
                    Setters = { new Setter(property, value) },
                });
            }

            try
            {
                await animation.RunAsync(target, cancellationToken).ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                /* superseded show/hide */
            }
        }

        private static void EnsurePhase2Rest(MotionContext ctx)
        {
            foreach (TesseraRevealHost host in ctx.Window.GetVisualDescendants().OfType<TesseraRevealHost>())
            {
                host.Phase2Engaged = true;
                host.RevealProgress = 1;
            }
        }
    }
}
