using FluentAssertions;
using MosaicShell.Core.Modules;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Catalog parity with JaxCore / CoreWebResources SkinList.
    /// </summary>
    public class ModuleCatalogTests : IDisposable
    {
        private readonly string _home;

        public ModuleCatalogTests()
        {
            _home = Path.Combine(Path.GetTempPath(), "ms-catalog-" + Guid.NewGuid().ToString("N"));
            AppPaths.SetRootOverride(_home);
            AppPaths.EnsureLayout();
        }

        public void Dispose()
        {
            AppPaths.ClearRootOverride();
            try { Directory.Delete(_home, recursive: true); } catch { /* ignore */ }
        }

        [Fact]
        public void Catalog_matches_CoreWebResources_SkinList_order()
        {
            string[] expected =
            [
                "Tessera", "Mixdeck", "Inlay", "Slate", "Chord", "Substrate",
                "Chrono", "Phono", "Pulse", "Canvas"
            ];

            _ = ModuleCatalog.All.Select(m => m.Id).Should().Equal(expected);
        }

        [Fact]
        public void Modules_and_Widgets_partition_matches_hub_library_columns()
        {
            _ = ModuleCatalog.Modules.Select(m => m.Id).Should().Equal(
                "Tessera", "Mixdeck", "Inlay", "Slate", "Chord", "Substrate");
            _ = ModuleCatalog.Widgets.Select(m => m.Id).Should().Equal(
                "Chrono", "Phono", "Pulse", "Canvas");
        }

        [Fact]
        public void IsInstalled_is_false_until_module_directory_exists()
        {
            _ = ModuleCatalog.IsInstalled("Tessera").Should().BeFalse();
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Tessera"));
            _ = ModuleCatalog.IsInstalled("Tessera").Should().BeTrue();
        }

        [Fact]
        public void TryGet_returns_module_metadata()
        {
            _ = ModuleCatalog.TryGet("Mixdeck", out ModuleInfo? info).Should().BeTrue();
            _ = info!.DisplayName.Should().Be("Mixdeck");
            _ = info.Kind.Should().Be(ModuleKind.Capability);
            _ = info.Description.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void TryGet_unknown_id_returns_false_when_not_installed()
        {
            _ = ModuleCatalog.TryGet("NotAModule", out _).Should().BeFalse();
        }

        [Fact]
        public void Discover_installed_manifest_appears_in_All()
        {
            string id = "ExtSample";
            string dir = Path.Combine(AppPaths.ModulesDirectory, id);
            _ = Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "module.manifest.json"), /*lang=json,strict*/ """
            {
              "Id": "ExtSample",
              "DisplayName": "External Sample",
              "Description": "Discovered from disk.",
              "Kind": "Widget",
              "Styles": [ "DEFAULT" ]
            }
            """);

            _ = ModuleCatalog.TryGet(id, out ModuleInfo? info).Should().BeTrue();
            _ = info!.DisplayName.Should().Be("External Sample");
            _ = info.Kind.Should().Be(ModuleKind.Widget);
            _ = ModuleCatalog.All.Select(m => m.Id).Should().Contain(id);
            _ = ModuleCatalog.Widgets.Select(m => m.Id).Should().Contain(id);
        }
    }
}
