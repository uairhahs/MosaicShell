using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The named motion looks offered in the Hub. Each one is only a set of the existing Tessera motion settings, so a
    /// preset can be applied, matched back from stored settings, and tuned afterwards without a schema change. They must be
    /// valid, findable, and, the reason they exist, look different from each other.
    /// </summary>
    public class TesseraMotionPresetsTests
    {
        private const double MinimumDistance = 0.9;

        [Fact]
        public void The_default_settings_are_one_of_the_presets_so_a_new_install_shows_a_selection()
        {
            TesseraMotionPreset? match = TesseraMotionPresets.Match(
                TesseraFlyoutAnimationPolicy.DefaultAni,
                TesseraFlyoutAnimationPolicy.DefaultAniSteps,
                TesseraFlyoutAnimationPolicy.DefaultEase,
                TesseraFlyoutAnimationPolicy.DefaultDisplacementPx);

            match.Should().NotBeNull();
            match!.Id.Should().Be("classic");
        }

        [Fact]
        public void Preset_ids_and_names_are_unique_and_never_the_reserved_custom_id()
        {
            TesseraMotionPresets.All.Select(p => p.Id).Should().OnlyHaveUniqueItems().And.NotContain("custom");
            TesseraMotionPresets.All.Select(p => p.Name).Should().OnlyHaveUniqueItems();
            TesseraMotionPresets.All.Should().OnlyContain(p => p.Name.Length > 0 && p.Description.Length > 0);
        }

        [Fact]
        public void Every_preset_holds_values_the_engine_would_not_change()
        {
            foreach (TesseraMotionPreset p in TesseraMotionPresets.All)
            {
                p.Ani.Should().BeInRange(0, 2, p.Id);
                TesseraFlyoutAnimationPolicy.NormalizeAniSteps(p.AniSteps).Should().Be(p.AniSteps, p.Id);
                TesseraFlyoutAnimationPolicy.NormalizeEase(p.AniEase).Should().Be(p.AniEase, p.Id);
                TesseraFlyoutAnimationPolicy.NormalizeDisplacementPx(p.DisplacementPx).Should().Be(p.DisplacementPx, p.Id);
            }
        }

        [Fact]
        public void Each_preset_is_found_again_from_the_settings_it_sets()
        {
            foreach (TesseraMotionPreset p in TesseraMotionPresets.All)
            {
                TesseraMotionPresets.Match(p.Ani, p.AniSteps, p.AniEase, p.DisplacementPx)?.Id.Should().Be(p.Id);
            }
        }

        [Fact]
        public void Settings_that_differ_from_every_preset_are_custom()
        {
            TesseraMotionPreset glide = TesseraMotionPresets.All.First(p => p.Ani == 1);

            TesseraMotionPresets.Match(glide.Ani, glide.AniSteps, glide.AniEase, glide.DisplacementPx + 5).Should().BeNull();
            TesseraMotionPresets.Match(glide.Ani, glide.AniSteps + 1, glide.AniEase, glide.DisplacementPx).Should().BeNull();
            TesseraMotionPresets.Match(glide.Ani, glide.AniSteps, "InOutBounce", glide.DisplacementPx).Should().BeNull();
        }

        [Fact]
        public void Matching_ignores_case_and_legacy_names_of_the_curve()
        {
            TesseraMotionPreset glide = TesseraMotionPresets.All.First(p => p.AniEase == "OutCubic");

            TesseraMotionPresets.Match(glide.Ani, glide.AniSteps, "outcubic", glide.DisplacementPx)?.Id.Should().Be(glide.Id);
        }

        [Fact]
        public void A_fade_only_preset_ignores_distance_and_duration_because_a_fade_uses_neither()
        {
            TesseraMotionPreset fade = TesseraMotionPresets.All.Single(p => p.Ani == 0);

            TesseraMotionPresets.Match(0, 40, fade.AniEase, 200)?.Id.Should().Be(fade.Id);
            TesseraMotionPresets.Match(0, 10, "InOutBack", 200).Should().BeNull();
        }

        [Fact]
        public void No_two_presets_look_alike()
        {
            // The complaint that led to presets was that every setting looked the same. A look is the path the flyout
            // takes over time: how far from rest it is (in 100 px units) and how visible it is, sampled every 40 ms.
            List<(string Id, double[] Look)> looks = [.. TesseraMotionPresets.All.Select(p => (p.Id, Look(p)))];
            List<string> alike = [];
            for (int i = 0; i < looks.Count; i++)
            {
                for (int j = i + 1; j < looks.Count; j++)
                {
                    double d = Math.Sqrt(looks[i].Look.Zip(looks[j].Look, (a, b) => (a - b) * (a - b)).Sum());
                    if (d <= MinimumDistance)
                    {
                        alike.Add($"{looks[i].Id} and {looks[j].Id} ({d:0.00})");
                    }
                }
            }

            alike.Should().BeEmpty($"every pair must be more than {MinimumDistance} apart");
        }

        [Fact]
        public void The_presets_cover_the_range_from_instant_to_slow_and_from_flat_to_overshooting()
        {
            IReadOnlyList<TesseraMotionPreset> all = TesseraMotionPresets.All;

            all.Min(p => p.Ani > 0 ? TesseraMotionPresets.DurationMsFromSteps(p.AniSteps) : int.MaxValue).Should().BeLessThanOrEqualTo(200);
            all.Max(p => p.Ani > 0 ? TesseraMotionPresets.DurationMsFromSteps(p.AniSteps) : 0).Should().BeGreaterThanOrEqualTo(560);
            all.Where(p => p.Ani > 0).Max(p => p.DisplacementPx).Should().BeGreaterThanOrEqualTo(120);
            all.Any(p => p.Ani == 0).Should().BeTrue("a plain fade is the quiet choice");
            all.Any(p => p.Ani == 2).Should().BeTrue("one look keeps the media strip reveal");
            all.Any(p => Peak(p) > 1.05).Should().BeTrue("at least one look visibly overshoots");
        }

        [Theory]
        [InlineData(10, 160)]
        [InlineData(20, 320)]
        [InlineData(40, 640)]
        public void Steps_and_milliseconds_convert_at_the_presentation_interval(int steps, int ms)
        {
            TesseraMotionPresets.DurationMsFromSteps(steps).Should().Be(ms);
            TesseraMotionPresets.StepsFromDurationMs(ms).Should().Be(steps);
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(159, 10)]
        [InlineData(330, 21)]
        [InlineData(5000, 40)]
        public void A_duration_outside_what_the_engine_plays_is_brought_inside_it(int ms, int steps)
        {
            TesseraMotionPresets.StepsFromDurationMs(ms).Should().Be(steps);
        }

        private static double Peak(TesseraMotionPreset p)
        {
            return Enumerable.Range(0, 201)
                .Max(i => TesseraFlyoutAnimationPolicy.SampleSteppedAt(0, 1, i / 200.0, p.AniSteps, p.AniEase, entrance: true));
        }

        private static double[] Look(TesseraMotionPreset p)
        {
            double durationMs = p.Ani > 0
                ? TesseraMotionPresets.DurationMsFromSteps(p.AniSteps)
                : TesseraFlyoutAnimationPolicy.FadeDurationMs;
            int fadeSteps = p.Ani > 0 ? p.AniSteps : TesseraFlyoutAnimationPolicy.ResolveFadeSampleCount();
            List<double> look = [];
            for (int t = 0; t <= 640; t += 40)
            {
                double progress = TesseraFlyoutAnimationPolicy.SampleSteppedAt(0, 1, t / durationMs, fadeSteps, p.AniEase, entrance: true);
                double offsetPx = p.Ani > 0 ? p.DisplacementPx * (1 - progress) : 0;
                look.Add(offsetPx / 100.0);
                look.Add(Math.Clamp(progress, 0, 1));

                // The media strip grows in after the slide when Ani is 2; nothing else reveals it.
                double reveal = 0;
                if (p.Ani == 2)
                {
                    int lead = TesseraFlyoutAnimationPolicy.ResolveShowPhase2LeadDelayMs(p.AniEase, p.AniSteps);
                    reveal = TesseraFlyoutAnimationPolicy.ResolvePhase2RevealProgressAt(
                        entrance: true, Math.Max(0, t - lead) / durationMs, p.AniSteps, p.AniEase);
                }

                look.Add(Math.Clamp(reveal, 0, 1));
            }

            return [.. look];
        }
    }
}
