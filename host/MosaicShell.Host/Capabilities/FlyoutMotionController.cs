using Avalonia;
using Avalonia.Animation;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Host.Tiles.Tessera;

namespace MosaicShell.Host.Capabilities;

/// <summary>
/// YourFlyouts motion orchestration. Phase 1 uses Avalonia <see cref="Animation"/> on
/// <see cref="FlyoutWindow.MotionSurface"/>; phase 2 animates per-component reveal hosts.
/// Keyframes sample Core stepped TweenNode progress (YourFlyouts ActionTimer ticks).
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
    }

    public static async Task RunEntranceAsync(MotionContext ctx)
    {
        if (ctx.IsCancelled())
            return;

        var request = ctx.Window.FlyoutRequest;
        if (request.Ani <= 0)
        {
            await RunFadeAsync(ctx, entrance: true).ConfigureAwait(true);
            EnsurePhase2Rest(ctx);
            return;
        }

        await RunAvaloniaPhase1Async(ctx, entrance: true).ConfigureAwait(true);
        if (ctx.IsCancelled())
            return;

        if (ShouldRunPhase2(ctx))
        {
            await Task.Delay(TesseraFlyoutAnimationPolicy.FancyPauseMs).ConfigureAwait(true);
            if (ctx.IsCancelled())
                return;
            await RunPhase2Async(ctx, entrance: true).ConfigureAwait(true);
        }
        else
        {
            EnsurePhase2Rest(ctx);
        }
    }

    public static async Task RunExitAsync(MotionContext ctx)
    {
        if (ctx.IsCancelled())
            return;

        var request = ctx.Window.FlyoutRequest;
        if (request.Ani <= 0)
        {
            await RunFadeAsync(ctx, entrance: false).ConfigureAwait(true);
            return;
        }

        if (ShouldRunPhase2(ctx))
        {
            await RunPhase2Async(ctx, entrance: false).ConfigureAwait(true);
            if (ctx.IsCancelled())
                return;
            await Task.Delay(TesseraFlyoutAnimationPolicy.FancyPauseMs).ConfigureAwait(true);
            if (ctx.IsCancelled())
                return;
        }

        await RunAvaloniaPhase1Async(ctx, entrance: false).ConfigureAwait(true);
    }

    private static bool ShouldRunPhase2(MotionContext ctx)
    {
        var request = ctx.Window.FlyoutRequest;
        return ctx.RunPhase2
               && TesseraFlyoutAnimationPolicy.Phase2RequiresAnimatedLayout(
                   request.Ani, request.StyleId, ctx.ShowMediaStrip);
    }

    private static async Task RunAvaloniaPhase1Async(MotionContext ctx, bool entrance)
    {
        var window = ctx.Window;
        var request = window.FlyoutRequest;
        var surface = window.MotionSurface;
        var ms = TesseraFlyoutAnimationPolicy.ResolvePhaseDurationMs(request.AniSteps);
        var ease = TesseraFlyoutAnimationPolicy.NormalizeEase(request.AniEase);
        var steps = TesseraFlyoutAnimationPolicy.NormalizeAniSteps(request.AniSteps);
        var slide = TesseraFlyoutAnimationPolicy.ShouldSlide(request.Ani);

        var (dxRest, dyRest) = slide
            ? TesseraFlyoutAnimationPolicy.ResolveSlideOffsetDip(
                ctx.MonitorScale, request.AniDir, request.AnimationDisplacement)
            : (0d, 0d);

        var opFrom = entrance ? 0d : 1d;
        var opTo = entrance ? 1d : 0d;
        var xFrom = entrance ? dxRest : 0d;
        var xTo = entrance ? 0d : dxRest;
        var yFrom = entrance ? dyRest : 0d;
        var yTo = entrance ? 0d : dyRest;

        TranslateTransform? tt = null;
        if (slide && TesseraFlyoutAnimationPolicy.MustAnimateRenderTransform
            && (Math.Abs(dxRest) > 0.01 || Math.Abs(dyRest) > 0.01))
        {
            tt = new TranslateTransform(xFrom, yFrom);
            window.RenderTransform = tt;
        }
        else
        {
            window.RenderTransform = null;
        }

        surface.Opacity = opFrom;

        var tasks = new List<Task>
        {
            AnimateSteppedAsync(surface, Visual.OpacityProperty, opFrom, opTo, ms, ease, steps, entrance),
        };

        if (tt is not null)
        {
            tasks.Add(AnimateSteppedAsync(tt, TranslateTransform.XProperty, xFrom, xTo, ms, ease, steps, entrance));
            tasks.Add(AnimateSteppedAsync(tt, TranslateTransform.YProperty, yFrom, yTo, ms, ease, steps, entrance));
        }

        await Task.WhenAll(tasks).ConfigureAwait(true);
    }

    private static async Task RunPhase2Async(MotionContext ctx, bool entrance)
    {
        var request = ctx.Window.FlyoutRequest;
        var ms = TesseraFlyoutAnimationPolicy.ResolvePhaseDurationMs(request.AniSteps);
        var ease = TesseraFlyoutAnimationPolicy.NormalizeEase(request.AniEase);
        var from = entrance ? 0d : 1d;
        var to = entrance ? 1d : 0d;
        var steps = TesseraFlyoutAnimationPolicy.NormalizeAniSteps(request.AniSteps);

        var hosts = ctx.Window.GetVisualDescendants().OfType<TesseraRevealHost>().ToList();
        if (hosts.Count == 0)
            return;

        ctx.Window.Phase2Animating = true;
        foreach (var host in hosts)
            host.Phase2Engaged = true;
        try
        {
            var tasks = hosts.Select(h =>
                AnimateSteppedAsync(h, TesseraRevealHost.RevealProgressProperty, from, to, ms, ease, steps, entrance));
            await Task.WhenAll(tasks).ConfigureAwait(true);
        }
        finally
        {
            ctx.Window.Phase2Animating = false;
            if (entrance)
            {
                foreach (var host in hosts)
                    host.Phase2Engaged = true;
            }
            else
            {
                foreach (var host in hosts)
                    host.Phase2Engaged = false;
            }
        }
    }

    private static async Task RunFadeAsync(MotionContext ctx, bool entrance)
    {
        var request = ctx.Window.FlyoutRequest;
        var ms = TesseraFlyoutAnimationPolicy.ResolveDurationMs(0, request.AniSteps);
        var surface = ctx.Window.MotionSurface;
        var from = entrance ? 0d : surface.Opacity;
        var to = entrance ? 1d : 0d;
        ctx.Window.RenderTransform = null;
        await AnimateSteppedAsync(
                surface,
                Visual.OpacityProperty,
                from,
                to,
                ms,
                TesseraFlyoutAnimationPolicy.EaseLinear,
                steps: 1,
                entrance)
            .ConfigureAwait(true);
    }

    private static Task AnimateSteppedAsync(
        Animatable target,
        AvaloniaProperty property,
        double from,
        double to,
        int ms,
        string ease,
        int steps,
        bool entrance)
    {
        var duration = Math.Max(1, ms);
        var sampleCount = Math.Max(1, steps);
        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(duration),
            FillMode = FillMode.Forward,
        };

        for (var i = 0; i <= sampleCount; i++)
        {
            var linearT = i / (double)sampleCount;
            var value = TesseraFlyoutAnimationPolicy.InterpolateStepped(from, to, i, sampleCount, ease, entrance);
            animation.Children.Add(new KeyFrame
            {
                Cue = new Cue(linearT),
                Setters = { new Setter(property, value) },
            });
        }

        return animation.RunAsync(target);
    }

    private static void EnsurePhase2Rest(MotionContext ctx)
    {
        foreach (var host in ctx.Window.GetVisualDescendants().OfType<TesseraRevealHost>())
        {
            host.Phase2Engaged = true;
            host.RevealProgress = 1;
        }
    }
}
