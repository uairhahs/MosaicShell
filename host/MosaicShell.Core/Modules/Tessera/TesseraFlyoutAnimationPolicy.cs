namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// Tessera flyout entrance/exit motion. Parity with JaxCore YourFlyouts
/// (<c>@Resources/Vars.inc</c>, <c>Func.lua</c> tweenAnimation / tweenAnimation2).
/// </summary>
public static class TesseraFlyoutAnimationPolicy
{
    public const string EaseLinear = "Linear";

    public const string EaseInSine = "InSine";
    public const string EaseOutSine = "OutSine";
    public const string EaseInOutSine = "InOutSine";

    public const string EaseInQuad = "InQuad";
    public const string EaseOutQuad = "OutQuad";
    public const string EaseInOutQuad = "InOutQuad";

    public const string EaseInCubic = "InCubic";
    public const string EaseOutCubic = "OutCubic";
    public const string EaseInOutCubic = "InOutCubic";

    public const string EaseInQuart = "InQuart";
    public const string EaseOutQuart = "OutQuart";
    public const string EaseInOutQuart = "InOutQuart";

    public const string EaseInQuint = "InQuint";
    public const string EaseOutQuint = "OutQuint";
    public const string EaseInOutQuint = "InOutQuint";

    public const string EaseInExpo = "InExpo";
    public const string EaseOutExpo = "OutExpo";
    public const string EaseInOutExpo = "InOutExpo";

    public const string EaseInCirc = "InCirc";
    public const string EaseOutCirc = "OutCirc";
    public const string EaseInOutCirc = "InOutCirc";

    public const string EaseInBack = "InBack";
    public const string EaseOutBack = "OutBack";
    public const string EaseInOutBack = "InOutBack";

    public const string EaseInElastic = "InElastic";
    public const string EaseOutElastic = "OutElastic";
    public const string EaseInOutElastic = "InOutElastic";

    public const string EaseInBounce = "InBounce";
    public const string EaseOutBounce = "OutBounce";
    public const string EaseInOutBounce = "InOutBounce";

    /// <summary>YourFlyouts default (<c>Easetype=OutQuart</c>).</summary>
    public const string DefaultEase = EaseOutQuart;

    /// <summary>YourFlyouts <c>AniSteps=20</c>.</summary>
    public const int DefaultAniSteps = 20;

    /// <summary>ActionTimer Repeat delay in Ani1/Ani2.inc (below a 60 Hz frame).</summary>
    public const int EncodedActionTimerIntervalMs = 2;

    /// <summary>
    /// Wall-clock per tween tick. One 60 Hz frame so 20 steps present 20 distinct
    /// eased samples. Encoding steps * EncodedActionTimerIntervalMs is 40 ms and
    /// every easing family looks like a pop.
    /// </summary>
    public const int StepPresentationIntervalMs = 16;

    /// <summary>Below this, In vs Out and Bounce vs Linear are not readable on a 60 Hz display.</summary>
    public const int MinPerceptiblePhaseDurationMs = 200;

    /// <summary>Ani2 Fancy pause between phase 1 and phase 2 (Ani2.inc Wait 100).</summary>
    public const int FancyPauseMs = 100;

    /// <summary>YourFlyouts <c>AnimationDisplacement=30</c> (physical pixels).</summary>
    public const int DefaultDisplacementPx = 30;

    public const int MinAniSteps = 10;
    public const int MaxAniSteps = 40;
    public const int MinDisplacementPx = 10;
    public const int MaxDisplacementPx = 200;

    /// <summary>Ani0 uses built-in fade instead of slide tween.</summary>
    public const bool AniNoneUsesBuiltInFade = true;

    /// <summary>
    /// YourFlyouts Ani0.inc Showfade ignores AniSteps. Rainmeter default FadeDuration is 250 ms.
    /// </summary>
    public const bool AniNoneIgnoresAniSteps = true;

    /// <summary>Rainmeter default <c>FadeDuration</c> (Main.ini does not override).</summary>
    public const int FadeDurationMs = 250;

    /// <summary>
    /// Ani0 fade keyframe count. Two samples would lerp linearly and hide In vs Out.
    /// </summary>
    public static int ResolveFadeSampleCount() =>
        Math.Max(MinAniSteps, FadeDurationMs / StepPresentationIntervalMs);

    /// <summary>
    /// YourFlyouts TweenNode1 is skin-wide (Ani2 In2). Stacked Host must not restrict
    /// phase 2 to the Media HWND.
    /// </summary>
    public const bool StackedPhase2MustNotBeMediaSlotOnly = true;

    /// <summary>
    /// Rainmeter YourFlyouts moves the skin HWND. Avalonia SoftFrost/OS acrylic repaints every
    /// <c>Position</c> step and reads clunky; Host slides via <see cref="MustAnimateRenderTransform"/>.
    /// </summary>
    public const bool Phase1MustUseWindowPosition = true;

    public const bool MustAnimateWindowPosition = false;

    /// <summary>Translate the whole flyout surface at a fixed anchor (GPU-friendly).</summary>
    public const bool MustAnimateRenderTransform = true;

    /// <summary>Host must not relayout mid-motion (prevents anchor snap).</summary>
    public const bool RelayoutMustDeferDuringMotion = true;

    /// <summary>
    /// Phase-2 HWND resize was tried so the Win11 card could grow. SoftFrost Transparent
    /// swapchain clears to black on each SetWindowPos. Host pre-sizes to rest bounds and clips.
    /// </summary>
    public const bool RelayoutAllowedDuringPhase2 = false;

    /// <summary>Phase 2 must clip in-place; do not SizeToContent / SetWindowPos per tween step.</summary>
    public const bool Phase2MustNotResizeHwnd = true;

    /// <summary>
    /// Fancy binders must not change DesiredSize (Width/Height/TrackThickness).
    /// Clip and RenderTransform follow TweenNode1; measure stays rest (YourFlyouts skin size).
    /// </summary>
    public const bool Phase2MustNotMutateLayoutMeasure = true;

    /// <summary>Aborted Fancy entrance on the owning generation must snap TweenNode1 to rest.</summary>
    public const bool CancelledEntranceMustSnapToRest = true;

    /// <summary>
    /// A visible session that is not running motion must keep TweenNode1 and the HWND
    /// region at rest. Cancelled/superseded Fancy must not leave a partial media clip
    /// (art column only, jumping scrubber).
    /// </summary>
    public const bool IdleShowingSessionMustSnapRevealToRest = true;

    /// <summary>ApplyRequest supersedes in-flight motion and must clear the Host motion flag.</summary>
    public const bool MotionAnimatingMustClearOnSupersede = true;

    /// <summary>
    /// Hide tweens must be cancelled when a show supersedes. Leaving them running
    /// writes TweenNode1 back to 0 after fade-in (empty media bay, jumping scrubber).
    /// </summary>
    public const bool SupersededMotionMustCancelInFlightTweens = true;

    public static bool ShouldDeferRelayoutDuringMotion(bool motionAnimating, bool phase2Animating)
    {
        _ = phase2Animating;
        return motionAnimating && RelayoutMustDeferDuringMotion;
    }

    public static readonly IReadOnlyList<string> AllEaseTypes =
    [
        EaseLinear,
        EaseInSine, EaseOutSine, EaseInOutSine,
        EaseInQuad, EaseOutQuad, EaseInOutQuad,
        EaseInCubic, EaseOutCubic, EaseInOutCubic,
        EaseInQuart, EaseOutQuart, EaseInOutQuart,
        EaseInQuint, EaseOutQuint, EaseInOutQuint,
        EaseInExpo, EaseOutExpo, EaseInOutExpo,
        EaseInCirc, EaseOutCirc, EaseInOutCirc,
        EaseInBack, EaseOutBack, EaseInOutBack,
        EaseInElastic, EaseOutElastic, EaseInOutElastic,
        EaseInBounce, EaseOutBounce, EaseInOutBounce,
    ];

    /// <summary>Easing families for Hub Motion UI (matches JaxCore Ease.inc grouping).</summary>
    public static readonly IReadOnlyList<string> EaseFamilies =
    [
        "Linear", "Sine", "Quad", "Cubic", "Quart", "Quint",
        "Expo", "Circ", "Back", "Elastic", "Bounce",
    ];

    public static readonly IReadOnlyList<string> EaseVariants = ["In", "Out", "InOut"];

    public const string DefaultEaseFamily = "Quart";
    public const string DefaultEaseVariant = "Out";

    public static bool ShouldSlide(int ani) => ani >= 1;

    public static bool ShouldAnimateOpacity(int ani) => ani >= 0;

    public static bool RequiresPhase2(int ani, bool showMediaStrip) =>
        ani >= 2 && showMediaStrip;

    public static int NormalizeAniSteps(int steps) =>
        Math.Clamp(steps, MinAniSteps, MaxAniSteps);

    public static int NormalizeDisplacementPx(int px) =>
        Math.Clamp(px, MinDisplacementPx, MaxDisplacementPx);

    public static int ResolveDurationMs(int ani, int aniSteps)
    {
        if (ani <= 0)
            return FadeDurationMs;

        return NormalizeAniSteps(aniSteps) * StepPresentationIntervalMs;
    }

    public static int ResolveSlideDistancePx(int displacementPx) =>
        NormalizeDisplacementPx(displacementPx);

    public static string NormalizeEase(string? ease)
    {
        if (string.IsNullOrWhiteSpace(ease))
            return DefaultEase;

        var id = ease.Trim();
        foreach (var known in AllEaseTypes)
        {
            if (id.Equals(known, StringComparison.OrdinalIgnoreCase))
                return known;
        }

        // Legacy aliases from earlier Tessera builds and JaxCore Cubic* naming.
        return id.ToLowerInvariant() switch
        {
            "cubicin" => EaseInCubic,
            "cubicout" => EaseOutCubic,
            "cubicinout" => EaseInOutCubic,
            "incubic" => EaseInCubic,
            "outcubic" => EaseOutCubic,
            "inoutcubic" => EaseInOutCubic,
            "inquart" => EaseInQuart,
            "outquart" => EaseOutQuart,
            "inoutquart" => EaseInOutQuart,
            _ => DefaultEase,
        };
    }

    /// <summary>Split normalized ease id into Hub family + variant pickers.</summary>
    public static (string Family, string Variant) SplitEase(string? ease)
    {
        var id = NormalizeEase(ease);
        if (id.Equals(EaseLinear, StringComparison.OrdinalIgnoreCase))
            return ("Linear", DefaultEaseVariant);

        foreach (var variant in EaseVariants.OrderByDescending(v => v.Length))
        {
            if (!id.StartsWith(variant, StringComparison.OrdinalIgnoreCase))
                continue;
            var family = id[variant.Length..];
            if (EaseFamilies.Contains(family, StringComparer.OrdinalIgnoreCase))
                return (EaseFamilies.First(f => f.Equals(family, StringComparison.OrdinalIgnoreCase)), variant);
        }

        return (DefaultEaseFamily, DefaultEaseVariant);
    }

    /// <summary>
    /// InOut of the selected family. Linear is unchanged. Entrance uses this so the
    /// first quarter stays near the hide pose and the last quarter settles into rest.
    /// Pure In* dumps the remaining travel into the final frames.
    /// </summary>
    public static string ResolveInOutEase(string? ease)
    {
        var id = NormalizeEase(ease);
        if (id.Equals(EaseLinear, StringComparison.OrdinalIgnoreCase))
            return EaseLinear;

        var (family, _) = SplitEase(id);
        return ComposeEase(family, "InOut");
    }

    /// <summary>
    /// Time-reverse of a selected ease. OutQuart hide lingers then leaves; show uses InQuart
    /// so it stays offscreen/dim then arrives. InOut and Linear are their own inverses.
    /// </summary>
    public static string InvertEaseVariant(string? ease)
    {
        var id = NormalizeEase(ease);
        if (id.Equals(EaseLinear, StringComparison.OrdinalIgnoreCase))
            return EaseLinear;

        var (family, variant) = SplitEase(id);
        var inverted = variant.Equals("In", StringComparison.OrdinalIgnoreCase)
            ? "Out"
            : variant.Equals("Out", StringComparison.OrdinalIgnoreCase)
                ? "In"
                : variant;
        return ComposeEase(family, inverted);
    }

    /// <summary>Compose Hub family + variant into canonical ease id.</summary>
    public static string ComposeEase(string? family, string? variant)
    {
        if (string.IsNullOrWhiteSpace(family)
            || family.Equals("Linear", StringComparison.OrdinalIgnoreCase))
            return EaseLinear;

        var v = string.IsNullOrWhiteSpace(variant) ? DefaultEaseVariant : variant.Trim();
        if (!EaseVariants.Contains(v, StringComparer.OrdinalIgnoreCase))
            v = DefaultEaseVariant;
        return NormalizeEase($"{v}{family.Trim()}");
    }

    /// <summary>Human-readable phase duration from step count.</summary>
    public static int ResolvePhaseDurationMs(int aniSteps) =>
        NormalizeAniSteps(aniSteps) * StepPresentationIntervalMs;

    /// <summary>Fancy entrance total (phase1 + pause + phase2) when media strip is shown.</summary>
    public static int ResolveFancyEntranceDurationMs(int aniSteps) =>
        ResolvePhaseDurationMs(aniSteps) * 2 + FancyPauseMs;

    /// <summary>Normalized 0..1 TweenNode progress for stepped motion keyframes.</summary>
    public static double ResolveNormalizedTweenProgress(int step, int aniSteps, string? ease, bool entrance) =>
        ResolveTweenNode(step, aniSteps, ease, entrance) / 100.0;

    /// <summary>
    /// Forward clock only. Fade in is fade out with endpoints swapped
    /// (<see cref="FadeInMustInverseFadeOut"/>).
    /// </summary>
    public static double InterpolateForward(double from, double to, int step, int aniSteps, string? ease)
    {
        var tn = ResolveNormalizedTweenProgress(step, aniSteps, ease, entrance: true);
        return from + (to - from) * tn;
    }

    /// <summary>Interpolate between <paramref name="from"/> and <paramref name="to"/> using stepped TweenNode progress.</summary>
    public static double InterpolateStepped(double from, double to, int step, int aniSteps, string? ease, bool entrance)
    {
        var tn = ResolveNormalizedTweenProgress(step, aniSteps, ease, entrance);
        var blend = entrance ? tn : 1 - tn;
        return from + (to - from) * blend;
    }

    /// <summary>Continuous easing sample (0..1) for Avalonia keyframe generation.</summary>
    public static double SampleEaseContinuous(double t, string? ease)
    {
        t = Math.Clamp(t, 0, 1);
        var fn = TesseraFlyoutTweenEngine.ResolveEasingFunction(NormalizeEase(ease));
        return Math.Clamp(fn(t, 0, 1, 1), 0, 1);
    }

    /// <summary>Penner-style easing sample. <paramref name="t"/> is 0..1 linear progress.</summary>
    public static double SampleEase(double t, string? ease)
    {
        t = Math.Clamp(t, 0, 1);
        var steps = DefaultAniSteps;
        var clock = (int)Math.Round(t * steps);
        return TesseraFlyoutTweenEngine.SampleNormalized(clock, steps, ease, entrance: true);
    }

    /// <summary>Resolve eased TweenNode after <paramref name="step"/> ticks (0..steps).</summary>
    public static double ResolveTweenNode(int step, int aniSteps, string? ease, bool entrance)
    {
        var steps = Math.Max(1, aniSteps);
        step = Math.Clamp(step, 0, steps);
        var tween = new TesseraFlyoutStepTween(steps, 0, 100, ease);
        if (!entrance)
        {
            tween.Advance(steps);
            for (var i = 0; i < step; i++)
                tween.Advance(-1);
        }
        else if (step > 0)
        {
            for (var i = 0; i < step; i++)
                tween.Advance(1);
        }

        return tween.Value;
    }

    /// <summary>Opacity from TweenNode (YourFlyouts SetTransparency TweenNode/100*255).</summary>
    public static double ResolveOpacityFromTweenNode(double tweenNode) =>
        Math.Clamp(tweenNode / 100.0, 0, 1);

    /// <summary>
    /// Slide factor from TweenNode for entrance (0 = full offset, 1 = rest).
    /// Matches Func.lua Left: (TN/100 - 1), Right: (1 - TN/100).
    /// </summary>
    public static double ResolveSlideFactor(string? aniDir, double tweenNode, bool entrance)
    {
        var tn = Math.Clamp(tweenNode, 0, 100) / 100.0;
        if (!entrance)
            tn = 1 - tn;

        return (aniDir ?? "Left").ToLowerInvariant() switch
        {
            "right" => 1 - tn,
            "bottom" => 1 - tn,
            _ => tn - 1,
        };
    }

    /// <summary>Slide offset in physical pixels at a TweenNode sample.</summary>
    public static (int Dx, int Dy) ResolveSlideOffsetPxAtTweenNode(
        string? aniDir,
        int displacementPx,
        double tweenNode,
        bool entrance)
    {
        var dist = ResolveSlideDistancePx(displacementPx);
        var factor = ResolveSlideFactor(aniDir, tweenNode, entrance);
        return (aniDir ?? "Left").ToLowerInvariant() switch
        {
            "right" => ((int)Math.Round(dist * factor), 0),
            "top" => (0, (int)Math.Round(dist * factor)),
            "bottom" => (0, (int)Math.Round(dist * factor)),
            _ => ((int)Math.Round(dist * factor), 0),
        };
    }

    /// <summary>Slide offset px at entrance start / exit end.</summary>
    public static (int Dx, int Dy) ResolveSlideOffsetPx(string? aniDir, int displacementPx)
    {
        var dist = ResolveSlideDistancePx(displacementPx);
        return (aniDir ?? "Left").ToLowerInvariant() switch
        {
            "right" => (dist, 0),
            "top" => (0, -dist),
            "bottom" => (0, dist),
            _ => (-dist, 0),
        };
    }

    /// <summary>TranslateTransform offset in DIP from physical pixel displacement.</summary>
    public static (double Dx, double Dy) ResolveSlideOffsetDip(
        double monitorScale,
        string? aniDir,
        int displacementPx,
        double tweenNode,
        bool entrance)
    {
        var scale = monitorScale > 0.1 ? monitorScale : 1.0;
        var (px, py) = ResolveSlideOffsetPxAtTweenNode(aniDir, displacementPx, tweenNode, entrance);
        return (px / scale, py / scale);
    }

    public static (double Dx, double Dy) ResolveSlideOffsetDip(
        double monitorScale,
        string? aniDir,
        int displacementPx)
    {
        var scale = monitorScale > 0.1 ? monitorScale : 1.0;
        var (px, py) = ResolveSlideOffsetPx(aniDir, displacementPx);
        return (px / scale, py / scale);
    }

    public enum FlyoutAnimationPhaseKind
    {
        Show,
        Phase1In,
        Phase1Out,
        Wait,
        Phase2In,
        Phase2Out,
        Hide,
    }

    public sealed record FlyoutAnimationPhase(FlyoutAnimationPhaseKind Kind, int StepCount = 0, int WaitMs = 0);

    /// <summary>ActionTimer sequence from Ani0/Ani1/Ani2.inc.</summary>
    public static IReadOnlyList<FlyoutAnimationPhase> ResolveEntranceSequence(int ani, bool showMediaStrip)
    {
        if (ani <= 0)
            return [new FlyoutAnimationPhase(FlyoutAnimationPhaseKind.Show)];

        var steps = DefaultAniSteps;
        if (ani == 1)
            return
            [
                new FlyoutAnimationPhase(FlyoutAnimationPhaseKind.Show),
                new FlyoutAnimationPhase(FlyoutAnimationPhaseKind.Phase1In, steps),
            ];

        var seq = new List<FlyoutAnimationPhase>
        {
            new(FlyoutAnimationPhaseKind.Show),
            new(FlyoutAnimationPhaseKind.Phase1In, steps),
        };
        if (RequiresPhase2(ani, showMediaStrip))
        {
            seq.Add(new FlyoutAnimationPhase(FlyoutAnimationPhaseKind.Wait, WaitMs: FancyPauseMs));
            seq.Add(new FlyoutAnimationPhase(FlyoutAnimationPhaseKind.Phase2In, steps));
        }
        return seq;
    }

    public static IReadOnlyList<FlyoutAnimationPhase> ResolveExitSequence(int ani, bool showMediaStrip)
    {
        if (ani <= 0)
            return [new FlyoutAnimationPhase(FlyoutAnimationPhaseKind.Hide)];

        var steps = DefaultAniSteps;
        if (ani == 1)
            return [new FlyoutAnimationPhase(FlyoutAnimationPhaseKind.Phase1Out, steps)];

        var seq = new List<FlyoutAnimationPhase>();
        if (RequiresPhase2(ani, showMediaStrip))
            seq.Add(new FlyoutAnimationPhase(FlyoutAnimationPhaseKind.Phase2Out, steps));
        if (RequiresPhase2(ani, showMediaStrip))
            seq.Add(new FlyoutAnimationPhase(FlyoutAnimationPhaseKind.Wait, WaitMs: FancyPauseMs));
        seq.Add(new FlyoutAnimationPhase(FlyoutAnimationPhaseKind.Phase1Out, steps));
        return seq;
    }

    /// <summary>Keyframe values are already eased; Avalonia must lerp linearly between them.</summary>
    public const bool SteppedKeyframesMustUseLinearInterpolation = true;

    /// <summary>
    /// Hide keeps the selected ease on the reverse clock. Show uses InOut of that family
    /// so arrival settles instead of slamming. Linear fade-in/out remains a numeric inverse.
    /// </summary>
    public const bool FadeInMustInverseFadeOut = true;

    /// <summary>Entrance uses InOut of the selected family, not a pure In* slam.</summary>
    public const bool Phase1EntranceMustUseInOutFamily = true;

    public const bool EntranceOpacityMustStayVisibleDuringInEase = true;

    public static string ResolvePhase1MotionEase(string? ease, bool entrance)
    {
        var id = NormalizeEase(ease);
        if (entrance && Phase1EntranceMustUseInOutFamily)
            return ResolveInOutEase(id);
        return id;
    }

    public static string ResolvePhase1OpacityEase(string? ease, bool entrance) =>
        ResolvePhase1MotionEase(ease, entrance);

    /// <summary>
    /// YourFlyouts TweenNode1 uses <c>Easetype</c> both ways. Hide steps the reverse
    /// clock (OutQuart lingers open, then wipes). Show steps the same ease forward
    /// so media wipes in instead of InQuart-slamming after volume phase 1.
    /// </summary>
    public const bool Phase2MustUseSelectedEaseBothWays = true;

    /// <summary>
    /// InQuart show dumps remaining travel into the last frames (volume sits, rest pops).
    /// Phase 2 must not invert Out* to In* to "complement" hide.
    /// </summary>
    public const bool Phase2ShowMustNotInvertToInEase = true;

    /// <summary>
    /// Stacked Host splits YourFlyouts' one skin into Volume/Media HWNDs. Show phase 1
    /// is the volume card only; media stays fully hidden until TweenNode1 (phase 2).
    /// Hide still fades every slot after the dissolve (YourFlyouts phase 1 out).
    /// </summary>
    public const bool StackedMediaMustSkipPhase1OnShow = true;

    /// <summary>
    /// Cluster show must not WhenAll each HWND through phase 1 then phase 2 independently:
    /// media phase 2 would start during the 100 ms pause while volume is still arriving,
    /// or worse, media would fade in as a finished card in phase 1.
    /// </summary>
    public const bool StackedShowMustFinishVolumePhase1BeforeMediaPhase2 = true;

    /// <summary>
    /// Cluster hide must not WhenAll each HWND through the full exit. A volume slot
    /// without a phase-2 host would start sliding while media is still dissolving.
    /// Reverse of show: all phase 2, then pause, then all phase 1.
    /// </summary>
    public const bool StackedHideMustFinishPhase2BeforeAnySlotPhase1 = true;

    /// <summary>
    /// Show and hide must SyncRevealRegion before TweenNode1 ticks. Skipping it on
    /// hide leaves a stale clip while phase 2 starts from rest progress.
    /// </summary>
    public const bool Phase2MustSyncRevealRegionBeforeTweenBothWays = true;

    /// <summary>
    /// Hide phase 2 must start at rest reveal (progress 1). Show starts at 0.
    /// A stuck partial clip would otherwise dissolve from the wrong width.
    /// </summary>
    public const bool Phase2HideMustStartFromRestReveal = true;

    /// <summary>
    /// A slot with no TesseraRevealHost (volume without a meter) must not snap
    /// TweenNode1 to rest on hide. That would pop media back open while it dissolves.
    /// </summary>
    public const bool Phase2HideMustNotSnapMissingHostsToRest = true;

    public static string ResolvePhase2MotionEase(string? ease, bool entrance)
    {
        _ = entrance;
        return NormalizeEase(ease);
    }

    public static bool Phase2RequiresAnimatedLayout(int ani, string? styleId, bool showMediaStrip)
    {
        if (ani < 2)
            return false;
        if (!TesseraFlyoutRevealSpec.StyleSupportsPhase2(styleId))
            return false;
        if (TesseraFlyoutRevealSpec.StylePhase2WithoutMediaStrip(styleId))
            return true;
        return showMediaStrip;
    }

    /// <summary>
    /// Fancy phase 2 for a Host slot. YourFlyouts runs In2 on the whole skin;
    /// stacked Volume/Device panels must still animate Animated meters.
    /// </summary>
    public static bool ShouldRunPhase2Reveal(
        int ani,
        string? styleId,
        bool showMediaStrip,
        TesseraStackedPanelRole? stackedRole)
    {
        _ = stackedRole;
        return Phase2RequiresAnimatedLayout(ani, styleId, showMediaStrip);
    }

    /// <summary>
    /// Phase 1 (slide + surface fade). Stacked media skips this on show so the rest
    /// is not faded in as a second finished acrylic card before TweenNode1.
    /// </summary>
    public static bool ShouldRunPhase1(
        int ani,
        string? styleId,
        bool showMediaStrip,
        TesseraStackedPanelRole? stackedRole,
        bool entrance)
    {
        if (!ShouldAnimateOpacity(ani))
            return false;
        if (!entrance
            || stackedRole != TesseraStackedPanelRole.Media
            || !StackedMediaMustSkipPhase1OnShow)
            return true;
        return !Phase2RequiresAnimatedLayout(ani, styleId, showMediaStrip);
    }

    public static bool ShouldHoldStackedMediaHiddenThroughShowPhase1(
        int ani,
        string? styleId,
        bool showMediaStrip,
        TesseraStackedPanelRole? stackedRole) =>
        !ShouldRunPhase1(ani, styleId, showMediaStrip, stackedRole, entrance: true);

    public static bool ExitMustMirrorEntrance => true;
}
