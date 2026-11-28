using FluentAssertions;
using MosaicShell.Core.HubShell;
using MosaicShell.Core.Modules;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The tray menu (U24): one switch per installed module, so a module turns on and off without the Hub, then Open and
    /// Quit. The same list fills GNOME's top bar menu over D-Bus.
    /// </summary>
    public sealed class TrayMenuTests
    {
        private static readonly IReadOnlyList<ModuleInfo> Catalog =
        [
            new("Tessera", "Tessera", "", ModuleKind.Capability),
            new("Mixdeck", "Mixdeck", "", ModuleKind.Capability),
            new("Chrono", "Chrono", "", ModuleKind.Widget),
            new("Slate", "Slate", "", ModuleKind.Capability),
            new("Acme", "Acme Clock", "", ModuleKind.Hybrid),
        ];

        [Fact]
        public void Only_installed_modules_are_listed_background_ones_first_in_catalog_order()
        {
            HashSet<string> installed = ["Chrono", "Slate", "Tessera", "Acme"];
            HashSet<string> on = ["Tessera", "Chrono"];

            IReadOnlyList<TrayModule> items = TrayMenu.Modules(Catalog, installed.Contains, on.Contains);

            _ = items.Should().Equal(
                new TrayModule("Tessera", "Tessera", true),
                new TrayModule("Slate", "Slate", false),
                new TrayModule("Acme", "Acme Clock", false),
                new TrayModule("Chrono", "Chrono", true));
        }

        [Fact]
        public void With_nothing_installed_the_menu_has_only_Open_and_Quit()
        {
            _ = TrayMenu.Modules(Catalog, _ => false, _ => false).Should().BeEmpty();
            _ = TrayMenu.OpenLabel.Should().Be("Open MosaicShell");
            _ = TrayMenu.QuitLabel.Should().Be("Quit MosaicShell", "the same words as Settings' close choice");
        }
    }
}
