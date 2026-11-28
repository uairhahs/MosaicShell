using Avalonia.Headless.XUnit;
using FluentAssertions;
using MosaicShell.Core.Modules;
using MosaicShell.Host.ViewModels;

namespace MosaicShell.Host.Tests
{
    /// <summary>
    /// Third-party modules from the Hub: Add from file installs a folder or .zip package and opens its page; its row is
    /// marked third-party; a manifest with rows the Hub left out says which on the page, for its developer.
    /// </summary>
    public sealed class AddFromPackageTests : HeadlessHostTest
    {
        private static string Package(string settingsJson)
        {
            string dir = Path.Combine(Path.GetTempPath(), "ms-pkg-" + Guid.NewGuid().ToString("N"));
            _ = Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "module.manifest.json"), $$"""
                { "Id": "HelloTile", "DisplayName": "Hello", "Kind": "Widget", "Styles": [ "Plain" ],
                  "Settings": {{settingsJson}} }
                """);
            return dir;
        }

        [AvaloniaFact]
        public async Task Adding_a_package_installs_it_and_opens_its_page()
        {
            MainViewModel vm = new();
            vm.NavigateCommand.Execute("Widgets");

            await vm.AddFromPackageAsync(Package("""[ { "Section": "S", "Rows": [ { "Key": "Sound", "Kind": "Toggle" } ] } ]"""));

            _ = ModuleCatalog.IsInstalled("HelloTile").Should().BeTrue();
            _ = vm.IsModuleConfig.Should().BeTrue();
            _ = vm.ConfigModuleId.Should().Be("HelloTile");
            _ = vm.ModulePage!.Row("Sound").Label.Should().Be("Sound");
            _ = vm.ModulePage.HasDeveloperNotes.Should().BeFalse();
            _ = vm.Widgets.Single(w => w.Id == "HelloTile").IsThirdParty.Should().BeTrue();
            _ = vm.Widgets.Single(w => w.Id == ModuleIds.Chrono).IsThirdParty.Should().BeFalse();
        }

        [AvaloniaFact]
        public async Task A_package_that_cannot_install_says_why()
        {
            MainViewModel vm = new();

            await vm.AddFromPackageAsync(Path.Combine(Path.GetTempPath(), "ms-missing-" + Guid.NewGuid().ToString("N")));

            _ = vm.StatusMessage.Should().StartWith("Could not install the package:");
            _ = vm.IsModuleConfig.Should().BeFalse();
        }

        [AvaloniaFact]
        public async Task Rows_the_Hub_left_out_are_listed_for_the_developer()
        {
            MainViewModel vm = new();

            await vm.AddFromPackageAsync(Package("""
                [ { "Section": "S", "Rows": [ { "Key": "Sound", "Kind": "Toggle" }, { "Key": "Dial", "Kind": "Knob" } ] } ]
                """));

            _ = vm.ModulePage!.DeveloperNotes.Should().ContainSingle().Which.Should().StartWith("Dial:");
        }
    }
}
