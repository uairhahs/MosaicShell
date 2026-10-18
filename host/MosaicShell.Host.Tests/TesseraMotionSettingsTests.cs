using Avalonia.Headless.XUnit;
using FluentAssertions;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Settings;
using MosaicShell.Host.ViewModels;

namespace MosaicShell.Host.Tests
{
    /// <summary>
    /// The Hub's Motion section as a view model: choosing a look writes the engine's own settings, the plain controls map
    /// onto them, and whatever is stored is shown back as a look or as "Custom".
    /// </summary>
    public sealed class TesseraMotionSettingsTests : HeadlessHostTest
    {
        private static MainViewModel OpenTesseraSettings(Action<TesseraSettings>? configure = null)
        {
            TesseraSettings stored = new();
            configure?.Invoke(stored);
            ModuleSettingsStore.Save(ModuleIds.Tessera, stored);
            MainViewModel vm = new();
            vm.OpenModuleConfigById(ModuleIds.Tessera);
            return vm;
        }

        private static TesseraMotionPresetItem Preset(MainViewModel vm, string id)
        {
            return vm.TesseraMotionPresetItems.Single(p => p.Id == id);
        }

        [AvaloniaFact]
        public void Default_settings_open_with_the_classic_look_selected()
        {
            MainViewModel vm = OpenTesseraSettings();

            vm.TesseraMotionPresetName.Should().Be("Classic");
            vm.TesseraMotionIsCustom.Should().BeFalse();
            vm.TesseraMotionPresetItems.Where(p => p.IsSelected).Select(p => p.Id).Should().Equal("classic");
        }

        [AvaloniaFact]
        public void Stored_values_that_match_no_look_open_as_custom_and_show_what_they_are()
        {
            MainViewModel vm = OpenTesseraSettings(s =>
            {
                s.Ani = 2;
                s.AniEase = "InOutBack";
                s.AniSteps = 30;
                s.AnimationDisplacement = 50;
            });

            vm.TesseraMotionIsCustom.Should().BeTrue();
            vm.TesseraMotionPresetName.Should().Be("Custom");
            vm.TesseraMotionPresetItems.Should().OnlyContain(p => !p.IsSelected);
            vm.TesseraMotionDurationMs.Should().Be(480);
            vm.TesseraMotionDurationLabel.Should().Be("480 ms");
            vm.TesseraMotionDistanceLabel.Should().Be("50 px");
            vm.SelectedTesseraMotionCurve!.IsCustomPlaceholder.Should().BeTrue("InOutBack is not one of the named feels");
            vm.SelectedTesseraMotionCurve.Id.Should().Be("InOutBack");
        }

        [AvaloniaFact]
        public void Choosing_a_look_writes_its_values_and_selects_only_that_look()
        {
            MainViewModel vm = OpenTesseraSettings();
            TesseraMotionPresetItem bounce = Preset(vm, "bounce");

            vm.SelectTesseraMotionPresetCommand.Execute(bounce);

            vm.TesseraAni.Should().Be(bounce.Ani);
            vm.TesseraAniSteps.Should().Be(bounce.AniSteps);
            vm.TesseraAniEase.Should().Be(bounce.AniEase);
            vm.TesseraAnimationDisplacement.Should().Be(bounce.DisplacementPx);
            vm.TesseraMotionPresetItems.Where(p => p.IsSelected).Select(p => p.Id).Should().Equal("bounce");
            vm.TesseraMotionSummary.Should().Be(bounce.Description);
        }

        [AvaloniaFact]
        public void Choosing_a_look_leaves_the_direction_alone()
        {
            MainViewModel vm = OpenTesseraSettings(s => s.AniDir = "Top");

            vm.SelectTesseraMotionPresetCommand.Execute(Preset(vm, "drift"));

            vm.TesseraAniDir.Should().Be("Top");
            vm.TesseraMotionFromTop.Should().BeTrue();
            vm.TesseraMotionPresetItems.Should().OnlyContain(p => p.AniDir == "Top", "the previews slide from the chosen side");
        }

        [AvaloniaFact]
        public void Tuning_a_value_of_a_look_makes_it_custom_and_tuning_it_back_restores_the_look()
        {
            MainViewModel vm = OpenTesseraSettings();
            TesseraMotionPresetItem glide = Preset(vm, "glide");
            vm.SelectTesseraMotionPresetCommand.Execute(glide);

            vm.TesseraMotionDurationMs = TesseraMotionPresets.DurationMsFromSteps(glide.AniSteps) + 64;

            vm.TesseraMotionIsCustom.Should().BeTrue();
            vm.TesseraAniSteps.Should().Be(glide.AniSteps + 4);

            vm.TesseraMotionDurationMs = TesseraMotionPresets.DurationMsFromSteps(glide.AniSteps);

            vm.TesseraMotionPresetName.Should().Be("Glide");
        }

        [AvaloniaFact]
        public void The_speed_slider_is_snapped_to_whole_steps_inside_what_the_engine_plays()
        {
            MainViewModel vm = OpenTesseraSettings();

            vm.TesseraMotionDurationMs = 1000;
            vm.TesseraAniSteps.Should().Be(TesseraFlyoutAnimationPolicy.MaxAniSteps);

            vm.TesseraMotionDurationMs = 50;
            vm.TesseraAniSteps.Should().Be(TesseraFlyoutAnimationPolicy.MinAniSteps);
        }

        [AvaloniaFact]
        public void The_two_switches_map_to_the_three_animation_modes()
        {
            MainViewModel vm = OpenTesseraSettings();

            vm.TesseraMotionRevealsMedia = false;
            vm.TesseraAni.Should().Be(1, "slide without the media reveal");

            vm.TesseraMotionRevealsMedia = true;
            vm.TesseraAni.Should().Be(2);

            vm.TesseraMotionSlides = false;
            vm.TesseraAni.Should().Be(0, "fade only");
            vm.TesseraMotionRevealEnabled.Should().BeFalse("a fade has no media reveal to switch on");

            vm.TesseraMotionSlides = true;
            vm.TesseraAni.Should().Be(2, "the reveal choice is remembered");
        }

        [AvaloniaFact]
        public void The_direction_buttons_set_the_direction_and_only_one_is_on()
        {
            MainViewModel vm = OpenTesseraSettings();

            vm.TesseraMotionFromBottom = true;

            vm.TesseraAniDir.Should().Be("Bottom");
            (vm.TesseraMotionFromLeft, vm.TesseraMotionFromRight, vm.TesseraMotionFromTop, vm.TesseraMotionFromBottom)
                .Should().Be((false, false, false, true));
        }

        [AvaloniaFact]
        public void Choosing_a_named_feel_sets_the_curve_and_removes_the_custom_entry()
        {
            MainViewModel vm = OpenTesseraSettings(s => s.AniEase = "InOutBack");
            vm.TesseraMotionCurves.Should().ContainSingle(c => c.IsCustomPlaceholder);

            vm.SelectedTesseraMotionCurve = vm.TesseraMotionCurves.First(c => c.Id == TesseraFlyoutAnimationPolicy.EaseOutBack);

            vm.TesseraAniEase.Should().Be(TesseraFlyoutAnimationPolicy.EaseOutBack);
            vm.TesseraMotionCurves.Should().NotContain(c => c.IsCustomPlaceholder);
            vm.SelectedTesseraMotionCurve!.Name.Should().Be("Overshoot");
        }

        [AvaloniaFact]
        public void The_preview_card_follows_the_settings_as_they_stand()
        {
            MainViewModel vm = OpenTesseraSettings();

            vm.TesseraAnimationDisplacement = 120;
            vm.TesseraMotionFromRight = true;

            vm.TesseraMotionCurrent.DisplacementPx.Should().Be(120);
            vm.TesseraMotionCurrent.AniDir.Should().Be("Right");
            vm.TesseraMotionCurrent.Ani.Should().Be(vm.TesseraAni);
        }

        [AvaloniaFact]
        public void Every_look_is_offered_in_the_grid()
        {
            MainViewModel vm = OpenTesseraSettings();

            vm.TesseraMotionPresetItems.Select(p => p.Id).Should().Equal(TesseraMotionPresets.All.Select(p => p.Id));
        }
    }
}
