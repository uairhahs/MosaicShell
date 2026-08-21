using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Modules;

namespace MosaicShell.Core.Tests;

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
        var expected = new[]
        {
            "Tessera", "Mixdeck", "Inlay", "Slate", "Chord", "Substrate",
            "Chrono", "Phono", "Pulse", "Canvas"
        };

        ModuleCatalog.All.Select(m => m.Id).Should().Equal(expected);
    }

    [Fact]
    public void Modules_and_Widgets_partition_matches_hub_library_columns()
    {
        ModuleCatalog.Modules.Select(m => m.Id).Should().Equal(
            "Tessera", "Mixdeck", "Inlay", "Slate", "Chord", "Substrate");
        ModuleCatalog.Widgets.Select(m => m.Id).Should().Equal(
            "Chrono", "Phono", "Pulse", "Canvas");
    }

    [Fact]
    public void IsInstalled_is_false_until_module_directory_exists()
    {
        ModuleCatalog.IsInstalled("Tessera").Should().BeFalse();
        Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Tessera"));
        ModuleCatalog.IsInstalled("Tessera").Should().BeTrue();
    }

    [Fact]
    public void TryGet_returns_module_metadata()
    {
        ModuleCatalog.TryGet("Mixdeck", out var info).Should().BeTrue();
        info!.DisplayName.Should().Be("Mixdeck");
        info.Kind.Should().Be(ModuleKind.Module);
        info.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void TryGet_unknown_id_returns_false()
    {
        ModuleCatalog.TryGet("NotAModule", out _).Should().BeFalse();
    }
}
