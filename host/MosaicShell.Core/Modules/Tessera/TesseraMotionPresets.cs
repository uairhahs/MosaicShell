namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// One named motion look: a complete set of the Tessera motion settings (<c>Ani</c>, <c>AniSteps</c>, <c>AniEase</c> and
    /// <c>AnimationDisplacement</c>). A preset is not stored anywhere; choosing one writes these values, and the Hub finds
    /// the preset again by matching them, so tuning a value afterwards simply makes the look "Custom". The slide direction
    /// is not part of a look: it follows where the flyout sits on the screen, so it stays the user's choice.
    /// </summary>
    public sealed record TesseraMotionPreset(
        string Id,
        string Name,
        string Description,
        int Ani,
        int AniSteps,
        string AniEase,
        int DisplacementPx);

    public static class TesseraMotionPresets
    {
        /// <summary>
        /// The looks, quietest first. Each differs from every other in duration, distance, curve or whether the media strip
        /// is revealed, by enough to see side by side (<c>TesseraMotionPresetsTests</c> measures it).
        /// </summary>
        public static readonly IReadOnlyList<TesseraMotionPreset> All =
        [
            new("fade", "Fade", "No movement. The flyout fades in and out.",
                Ani: 0, AniSteps: 20, TesseraFlyoutAnimationPolicy.EaseOutCubic, DisplacementPx: 30),
            new("flick", "Flick", "A fast throw from far away. Arrives almost at once.",
                Ani: 1, AniSteps: 10, TesseraFlyoutAnimationPolicy.EaseOutExpo, DisplacementPx: 140),
            new("classic", "Classic", "The original: a quick slide, then the media strip grows in.",
                Ani: 2, AniSteps: 20, TesseraFlyoutAnimationPolicy.EaseOutQuart, DisplacementPx: 30),
            new("glide", "Glide", "A smooth, medium slide that settles softly.",
                Ani: 1, AniSteps: 22, TesseraFlyoutAnimationPolicy.EaseOutCubic, DisplacementPx: 90),
            new("cascade", "Cascade", "Slides in, pauses a beat, then the media strip unfolds.",
                Ani: 2, AniSteps: 28, TesseraFlyoutAnimationPolicy.EaseInOutCubic, DisplacementPx: 80),
            new("bounce", "Bounce", "Drops in and bounces to a stop.",
                Ani: 1, AniSteps: 32, TesseraFlyoutAnimationPolicy.EaseOutBounce, DisplacementPx: 120),
            new("spring", "Spring", "Wobbles into place like it is on a spring.",
                Ani: 1, AniSteps: 40, TesseraFlyoutAnimationPolicy.EaseOutElastic, DisplacementPx: 200),
            new("drift", "Drift", "Slow and long. Floats in from far away.",
                Ani: 1, AniSteps: 40, TesseraFlyoutAnimationPolicy.EaseInOutSine, DisplacementPx: 160),
        ];

        /// <summary>The preset whose values these are, or null for a custom mix.</summary>
        public static TesseraMotionPreset? Match(int ani, int aniSteps, string? aniEase, int displacementPx)
        {
            string ease = TesseraFlyoutAnimationPolicy.NormalizeEase(aniEase);
            foreach (TesseraMotionPreset preset in All)
            {
                if (preset.Ani != ani || !preset.AniEase.Equals(ease, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // A plain fade has a fixed length and does not move, so steps and distance cannot tell looks apart.
                if (ani == 0
                    || (preset.AniSteps == TesseraFlyoutAnimationPolicy.NormalizeAniSteps(aniSteps)
                        && preset.DisplacementPx == TesseraFlyoutAnimationPolicy.NormalizeDisplacementPx(displacementPx)))
                {
                    return preset;
                }
            }

            return null;
        }

        public static int DurationMsFromSteps(int aniSteps)
        {
            return TesseraFlyoutAnimationPolicy.ResolvePhaseDurationMs(aniSteps);
        }

        /// <summary>The nearest step count to a duration, kept inside what the engine plays.</summary>
        public static int StepsFromDurationMs(int durationMs)
        {
            return TesseraFlyoutAnimationPolicy.NormalizeAniSteps(
                (int)Math.Round(durationMs / (double)TesseraFlyoutAnimationPolicy.StepPresentationIntervalMs));
        }
    }
}
