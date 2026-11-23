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

            vm.TesseraMaterialSelectable.Should().BeFalse();
            vm.TesseraMaterialFixedNote.Should().Contain("Plain Text").And.Contain("solid panel");
        }

        [AvaloniaFact]
        public void Switching_to_a_style_the_material_changes_enables_it_and_keeps_the_saved_choice()
        {
            MainViewModel vm = OpenTesseraSettings("MaterialYou");
            List<string?> changed = [];
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.ModuleStyle = "Fluent";

            vm.TesseraMaterialSelectable.Should().BeTrue();
            vm.TesseraMaterialFixedNote.Should().BeEmpty();
            changed.Should().Contain(nameof(MainViewModel.TesseraMaterialSelectable));
            vm.SelectedTesseraMaterial!.Choice.Should().Be(TesseraMaterialChoice.Frosted);
        }
    }
}
