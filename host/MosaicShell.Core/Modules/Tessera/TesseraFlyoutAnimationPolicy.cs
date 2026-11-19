namespace MosaicShell.Core.Modules.Tessera
{
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
        public const int DefaultAni = 2;

        public const int DefaultAniSteps = 20;

        /// <summary>
        /// Wall-clock per tween tick. One 60 Hz frame so 20 steps present 20 distinct
        /// eased samples. Encoding steps * EncodedActionTimerIntervalMs is 40 ms and
        /// every easing family looks like a pop.
        /// </summary>
        public const int StepPresentationIntervalMs = 16;

        /// <summary>
        /// The shortest time between two phase 2 updates, in milliseconds. The value applied comes from elapsed time, so this
        /// is only a floor that keeps an idle UI thread from being spun: a late update catches up, it does not stretch the run.
        /// </summary>
        public const int Phase2UpdateIntervalMs = 4;

        /// <summary>Ani2 Fancy pause between phase 1 and phase 2 (Ani2.inc Wait 100).</summary>
        public const int FancyPauseMs = 100;

        /// <summary>YourFlyouts <c>AnimationDisplacement=30</c> (physical pixels).</summary>
        public const int DefaultDisplacementPx = 30;

        public const int MinAniSteps = 10;
        public const int MaxAniSteps = 40;
        public const int MinDisplacementPx = 10;
        public const int MaxDisplacementPx = 200;

        /// <summary>Rainmeter default <c>FadeDuration</c> (Main.ini does not override).</summary>
        public const int FadeDurationMs = 250;

        /// <summary>
        /// Ani0 fade keyframe count. Two samples would lerp linearly and hide In vs Out.
        /// </summary>
        public static int ResolveFadeSampleCount()
        {
            return Math.Max(MinAniSteps, FadeDurationMs / StepPresentationIntervalMs);
        }

        /// <summary>
        /// What the phase 1 slide moves. YourFlyouts moves the skin window. An OS acrylic window's
        /// blur belongs to the HWND, so sliding only its content (RenderTransform) moved the card's
        /// border across a still blurred pane; that window moves as a whole. A window that paints
        /// its own background (SoftFrost) slides its content, which carries the background with it.
        /// </summary>
        public static TesseraSlideTarget ResolvePhase1SlideTarget(bool osAcrylicBackdrop)
        {
            return osAcrylicBackdrop ? TesseraSlideTarget.WindowPosition : TesseraSlideTarget.RenderTransform;
        }

        /// <summary>
        /// Where a window that slides as a whole starts its entrance, relative to its rest position,
        /// in physical pixels; (0, 0) when the window does not slide. The window must already be
        /// there when it is shown: shown at rest, it jumped to this offset when phase 1 began and
        /// slid back (the Modern Flyouts jiggle, 2026-10-04).
        /// </summary>
        public static (int Dx, int Dy) ResolveWindowEntranceStartOffsetPx(
            int ani, string? aniDir, int displacementPx, bool osAcrylicBackdrop)
        {
            return ShouldSlide(ani) && ResolvePhase1SlideTarget(osAcrylicBackdrop) == TesseraSlideTarget.WindowPosition
                ? ResolveSlideOffsetPx(aniDir, displacementPx)
                : (0, 0);
        }

        /// <summary>
        /// Value at linear time <paramref name="linearT"/> (0..1) of a stepped run: the eased value at
        /// each step (<see cref="InterpolateStepped"/>), straight-line between steps, as the keyframe
        /// animation does. For drivers that are not Avalonia animations (window position).
        /// </summary>
        public static double SampleSteppedAt(
            double from, double to, double linearT, int aniSteps, string? ease, bool entrance)
        {
            int steps = Math.Max(1, aniSteps);
            double t = Math.Clamp(linearT, 0, 1) * steps;
            int k = Math.Min((int)Math.Floor(t), steps - 1);
            double a = InterpolateStepped(from, to, k, steps, ease, entrance);
            double b = InterpolateStepped(from, to, k + 1, steps, ease, entrance);
            return a + ((b - a) * (t - k));
        }

        /// <summary>Host must not relayout mid-motion (prevents anchor snap).</summary>
        public const bool RelayoutMustDeferDuringMotion = true;

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

        public static bool ShouldSlide(int ani)
        {
            return ani >= 1;
        }

        public static bool ShouldAnimateOpacity(int ani)
        {
            return ani >= 0;
        }

        public static int NormalizeAniSteps(int steps)
        {
            return Math.Clamp(steps, MinAniSteps, MaxAniSteps);
        }

        public static int NormalizeDisplacementPx(int px)
        {
            return Math.Clamp(px, MinDisplacementPx, MaxDisplacementPx);
        }

        public static int ResolveDurationMs(int ani, int aniSteps)
        {
            return ani <= 0 ? FadeDurationMs : NormalizeAniSteps(aniSteps) * StepPresentationIntervalMs;
        }

        public static int ResolveSlideDistancePx(int displacementPx)
        {
            return NormalizeDisplacementPx(displacementPx);
        }

        public static string NormalizeEase(string? ease)
        {
            if (string.IsNullOrWhiteSpace(ease))
            {
                return DefaultEase;
            }

            string id = ease.Trim();
            foreach (string known in AllEaseTypes)
            {
                if (id.Equals(known, StringComparison.OrdinalIgnoreCase))
                {
                    return known;
                }
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
            string id = NormalizeEase(ease);
            if (id.Equals(EaseLinear, StringComparison.OrdinalIgnoreCase))
            {
                return ("Linear", DefaultEaseVariant);
            }

            foreach (string? variant in EaseVariants.OrderByDescending(v => v.Length))
            {
                if (!id.StartsWith(variant, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string family = id[variant.Length..];
                if (EaseFamilies.Contains(family, StringComparer.OrdinalIgnoreCase))
                {
                    return (EaseFamilies.First(f => f.Equals(family, StringComparison.OrdinalIgnoreCase)), variant);
                }
            }

            return (DefaultEaseFamily, DefaultEaseVariant);
        }

        /// <summary>Compose Hub family + variant into canonical ease id.</summary>
        public static string ComposeEase(string? family, string? variant)
        {
            if (string.IsNullOrWhiteSpace(family)
                || family.Equals("Linear", StringComparison.OrdinalIgnoreCase))
            {
                return EaseLinear;
            }

            string v = string.IsNullOrWhiteSpace(variant) ? DefaultEaseVariant : variant.Trim();
            if (!EaseVariants.Contains(v, StringComparer.OrdinalIgnoreCase))
            {
                v = DefaultEaseVariant;
            }

            return NormalizeEase($"{v}{family.Trim()}");
        }

        /// <summary>Human-readable phase duration from step count.</summary>
        public static int ResolvePhaseDurationMs(int aniSteps)
        {
            return NormalizeAniSteps(aniSteps) * StepPresentationIntervalMs;
        }

        /// <summary>Normalized 0..1 TweenNode progress for stepped motion keyframes.</summary>
        public static double ResolveNormalizedTweenProgress(int step, int aniSteps, string? ease, bool entrance)
        {
            return ResolveTweenNode(step, aniSteps, ease, entrance) / 100.0;
        }

        /// <summary>Interpolate between <paramref name="from"/> and <paramref name="to"/> using stepped TweenNode progress.</summary>
        public static double InterpolateStepped(double from, double to, int step, int aniSteps, string? ease, bool entrance)
        {
            double tn = ResolveNormalizedTweenProgress(step, aniSteps, ease, entrance);
            double blend = entrance ? tn : 1 - tn;
            return from + ((to - from) * blend);
        }

        /// <summary>Resolve eased TweenNode after <paramref name="step"/> ticks (0..steps).</summary>
        public static double ResolveTweenNode(int step, int aniSteps, string? ease, bool entrance)
        {
            int steps = Math.Max(1, aniSteps);
            step = Math.Clamp(step, 0, steps);
            TesseraFlyoutStepTween tween = new(steps, 0, 100, ease);
            if (!entrance)
            {
                _ = tween.Advance(steps);
                for (int i = 0; i < step; i++)
                {
                    _ = tween.Advance(-1);
                }
            }
            else if (step > 0)
            {
                for (int i = 0; i < step; i++)
                {
                    _ = tween.Advance(1);
                }
            }

            return tween.Value;
        }

        /// <summary>
        /// Slide factor from TweenNode for entrance (0 = full offset, 1 = rest).
        /// Matches Func.lua Left: (TN/100 - 1), Right: (1 - TN/100).
        /// </summary>
        public static double ResolveSlideFactor(string? aniDir, double tweenNode, bool entrance)
        {
            double tn = Math.Clamp(tweenNode, 0, 100) / 100.0;
            if (!entrance)
            {
                tn = 1 - tn;
            }

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
            int dist = ResolveSlideDistancePx(displacementPx);
            double factor = ResolveSlideFactor(aniDir, tweenNode, entrance);
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
            int dist = ResolveSlideDistancePx(displacementPx);
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
            double scale = monitorScale > 0.1 ? monitorScale : 1.0;
            (int px, int py) = ResolveSlideOffsetPxAtTweenNode(aniDir, displacementPx, tweenNode, entrance);
            return (px / scale, py / scale);
        }

        public static (double Dx, double Dy) ResolveSlideOffsetDip(
            double monitorScale,
            string? aniDir,
            int displacementPx)
        {
            double scale = monitorScale > 0.1 ? monitorScale : 1.0;
            (int px, int py) = ResolveSlideOffsetPx(aniDir, displacementPx);
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

        /// <summary>
        /// The curve phase 1 plays: exactly the one chosen, entering and leaving. The Hub preview draws the same curve, so
        /// what you pick is what you see. (Entrances used to be remapped to the InOut form of the chosen family, which made
        /// every Out* curve play as the same symmetric S and the settings look identical.)
        /// </summary>
        public static string ResolvePhase1MotionEase(string? ease, bool entrance)
        {
            _ = entrance;
            return NormalizeEase(ease);
        }

        public static string ResolvePhase1OpacityEase(string? ease, bool entrance)
        {
            return ResolvePhase1MotionEase(ease, entrance);
        }

        /// <summary>Show progress band that must be occupied for a fraction of phase 2.</summary>
        public const double Phase2ShowMidBandMin = 0.15;

        /// <summary>
        /// Show InOut sits near 0 for a long time. Stacking FancyPause on top of that is the
        /// empty gap after volume rests. Hide keeps the pause after the dissolve.
        /// </summary>
        public const bool ShowMustSkipFancyPauseBeforePhase2 = true;

        /// <summary>
        /// Start show phase 2 early enough that clip reaches <see cref="Phase2ShowMidBandMin"/>
        /// as phase 1 finishes. Otherwise InOutQuint is still ~0.005 after 80 ms of phase 2.
        /// </summary>
        public const bool ShowPhase2MustOverlapUntilMidBand = true;

        public static int ResolveInterPhaseWaitMs(bool entrance)
        {
            return entrance && ShowMustSkipFancyPauseBeforePhase2 ? 0 : FancyPauseMs;
        }

        public static int ResolvePhase2ShowOverlapMs(string? ease, int aniSteps)
        {
            int steps = NormalizeAniSteps(aniSteps);
            string showEase = ResolvePhase2MotionEase(ease, entrance: true);
            for (int s = 0; s <= steps; s++)
            {
                double p = ResolvePhase2RevealProgress(true, s, steps, showEase);
                if (p >= Phase2ShowMidBandMin)
                {
                    return s * StepPresentationIntervalMs;
                }
            }

            return 0;
        }

        public static int ResolveShowPhase2LeadDelayMs(string? ease, int aniSteps)
        {
            int phaseMs = ResolvePhaseDurationMs(aniSteps);
            int overlap = ResolvePhase2ShowOverlapMs(ease, aniSteps);
            return Math.Max(0, phaseMs - overlap);
        }

        /// <summary>
        /// Stacked volume divider and media clip/region must sample the same vsync p.
        /// Independent per-HWND pumps plus HRGN-only media wipe are two clocks.
        /// </summary>
        public const bool Phase2MustShareOneVsyncProgressAcrossStackedSlots = true;

        /// <summary>
        /// Stacked Host splits YourFlyouts' one skin into Volume/Media HWNDs. Show phase 1
        /// is the volume card only; media stays fully hidden until TweenNode1 (phase 2).
        /// Hide still fades every slot after the dissolve (YourFlyouts phase 1 out).
        /// </summary>
        public const bool StackedMediaMustSkipPhase1OnShow = true;

        /// <summary>
        /// Skip-phase-1 leaves stacked media at Opacity 0. Host must unhide that HWND
        /// in the collapsed pose (clip 0 / 1px region) before phase 2. If it stays
        /// invisible, TweenNode1 never paints and CompleteMotion is a rest-frame pop.
        /// </summary>
        public const bool PreparePhase2ShowMustUnhideStackedMedia = true;

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

        /// <summary>
        /// Unhiding collapsed media must reach the compositor before phase 2 ticks.
        /// Applying Opacity 1 and TweenNode1 0 in the same turn as the first rest
        /// frame still pops.
        /// </summary>
        public const bool PreparePhase2ShowMustYieldForRender = true;

        /// <summary>
        /// Stacked media must compose at rest (opacity 0 is OK) before the HRGN wipe
        /// arms. Clipping first leaves WM_PAINT with nothing to reveal.
        /// </summary>
        public const bool PreparePhase2ShowMustPaintRestClientBeforeWipe = true;

        public static bool ShouldSnapMissingPhase2HostsToRest(bool entrance)
        {
            return entrance && !Phase2HideMustNotSnapMissingHostsToRest;
        }

        public static double ResolvePhase2RevealProgress(
            bool entrance,
            int step,
            int aniSteps,
            string? ease)
        {
            double from = entrance ? 0d : 1d;
            double to = entrance ? 1d : 0d;
            return InterpolateStepped(from, to, step, aniSteps, ease, entrance);
        }

        /// <summary>
        /// The reveal progress at linear time <paramref name="linearT"/> (0..1) of the run, for a driver that samples once
        /// per rendered frame: the stepped value at each step, straight-line between steps, so a high refresh rate sees a
        /// smooth run and a late frame catches up instead of stretching it. Equals
        /// <see cref="ResolvePhase2RevealProgress"/> at every whole step.
        /// </summary>
        public static double ResolvePhase2RevealProgressAt(bool entrance, double linearT, int aniSteps, string? ease)
        {
            double from = entrance ? 0d : 1d;
            double to = entrance ? 1d : 0d;
            return SampleSteppedAt(from, to, linearT, aniSteps, ease, entrance);
        }

        /// <summary>The curve phase 2 plays: the chosen one, as in phase 1.</summary>
        public static string ResolvePhase2MotionEase(string? ease, bool entrance)
        {
            _ = entrance;
            return NormalizeEase(ease);
        }

        public static bool Phase2RequiresAnimatedLayout(int ani, string? styleId, bool showMediaStrip)
        {
            return ani >= 2 && TesseraFlyoutRevealSpec.StyleSupportsPhase2(styleId) && (TesseraFlyoutRevealSpec.StylePhase2WithoutMediaStrip(styleId) || showMediaStrip);
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
            if (!Phase2RequiresAnimatedLayout(ani, styleId, showMediaStrip))
            {
                return false;
            }

            // MaterialYou uses in-layout media (single HWND), so skip the separate media slot
            if (TesseraFlyoutTweenTargetCatalog.StylePhase2UsesInLayoutMedia(styleId)
                && stackedRole == TesseraStackedPanelRole.Media)
            {
                return false;
            }

            // For stacked styles, all slots with animated targets run phase 2
            return true;
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
            return ShouldAnimateOpacity(ani) && (!entrance
                || stackedRole != TesseraStackedPanelRole.Media
                || !StackedMediaMustSkipPhase1OnShow || !Phase2RequiresAnimatedLayout(ani, styleId, showMediaStrip));
        }

        public static bool ShouldHoldStackedMediaHiddenThroughShowPhase1(
            int ani,
            string? styleId,
            bool showMediaStrip,
            TesseraStackedPanelRole? stackedRole)
        {
            return !ShouldRunPhase1(ani, styleId, showMediaStrip, stackedRole, entrance: true);
        }

        public static bool ExitMustMirrorEntrance => true;
    }

    /// <summary>What the phase 1 slide moves (<see cref="TesseraFlyoutAnimationPolicy.ResolvePhase1SlideTarget"/>).</summary>
    public enum TesseraSlideTarget
    {
        /// <summary>The window content, by a TranslateTransform; the HWND stays put.</summary>
        RenderTransform,

        /// <summary>The HWND itself, with its acrylic backdrop and region.</summary>
        WindowPosition,
    }
}
