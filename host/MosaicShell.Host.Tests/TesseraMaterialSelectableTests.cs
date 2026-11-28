using Avalonia.Headless.XUnit;
using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Services.Xdg;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;
using MosaicShell.Host.ViewModels;

namespace MosaicShell.Host.Tests
{
    /// <summary>
    /// The Hub's Material choice is disabled for a style it does not change (MaterialYou, PlainText always draw a
    /// solid panel), with a note saying so, and the saved choice is kept for the other styles.
    /// </summary>
    public sealed class TesseraMaterialSelectableTests : HeadlessHostTest
    {
        private static MainViewModel OpenTesseraSettings(string style)
        {
            TesseraSettings stored = new() { Style = style };
            TesseraMaterialChoicePolicy.Apply(stored, TesseraMaterialChoice.Frosted);
            ModuleSettingsStore.Save(ModuleIds.Tessera, stored);
            MainViewModel vm = new();
            vm.OpenModuleConfigById(ModuleIds.Tessera);
            return vm;
        }

        [AvaloniaFact]
        public void A_style_the_material_does_not_change_disables_the_choice_with_a_note()
        {
            MainViewModel vm = OpenTesseraSettings("PlainText");

            _ = vm.TesseraMaterialSelectable.Should().BeFalse();
            _ = vm.TesseraMaterialFixedNote.Should().Contain("Plain Text").And.Contain("solid panel");
        }

        [AvaloniaFact]
        public void Switching_to_a_style_the_material_changes_enables_it_and_keeps_the_saved_choice()
        {
            MainViewModel vm = OpenTesseraSettings("MaterialYou");
            List<string?> changed = [];
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.ModuleStyle = "Fluent";

            _ = vm.TesseraMaterialSelectable.Should().BeTrue();
            _ = vm.TesseraMaterialFixedNote.Should().BeEmpty();
            _ = changed.Should().Contain(nameof(MainViewModel.TesseraMaterialSelectable));
            _ = vm.SelectedTesseraMaterial!.Choice.Should().Be(TesseraMaterialChoice.Frosted);
        }

        [AvaloniaFact]
        public void Without_a_backend_Native_Translucency_is_listed_greyed_out_with_the_reason()
        {
            NativeTranslucencyBackend original = NativeTranslucencyPlatform.Backend;
            try
            {
                NativeTranslucencyPlatform.Set(NativeTranslucencyBackend.None);
                MainViewModel vm = OpenTesseraSettings("Fluent");

                TesseraMaterialOption native = vm.TesseraMaterialOptions.Single(o => o.Choice == TesseraMaterialChoice.NativeTranslucency);
                _ = native.IsAvailable.Should().BeFalse();
                _ = vm.TesseraMaterialOptions.Where(o => o.Choice != TesseraMaterialChoice.NativeTranslucency)
                    .Should().OnlyContain(o => o.IsAvailable);
                _ = vm.SelectedTesseraMaterial!.Choice.Should().NotBe(TesseraMaterialChoice.NativeTranslucency);
                _ = vm.TesseraNativeUnavailableNote.Should().Be(
                    TesseraMaterialChoicePolicy.NativeUnavailableText(OperatingSystem.IsWindows(), XdgCurrentDesktop.Current));
            }
            finally
            {
                NativeTranslucencyPlatform.Set(original);
            }
        }

        [AvaloniaFact]
        public void With_a_backend_Native_Translucency_is_a_normal_choice_with_no_note()
        {
            NativeTranslucencyBackend original = NativeTranslucencyPlatform.Backend;
            try
            {
                NativeTranslucencyPlatform.Set(OperatingSystem.IsWindows() ? NativeTranslucencyBackend.WindowsAcrylic : NativeTranslucencyBackend.KWinBlur);
                MainViewModel vm = OpenTesseraSettings("Fluent");

                _ = vm.TesseraMaterialOptions.Should().OnlyContain(o => o.IsAvailable);
                _ = vm.TesseraNativeUnavailableNote.Should().BeEmpty();
            }
            finally
            {
                NativeTranslucencyPlatform.Set(original);
            }
        }
    }
}
