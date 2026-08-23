using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.Services;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests;

public class StyleCatalogTests
{
    [Fact]
    public void Tessera_has_eleven_jaxcore_layouts()
    {
        StyleCatalog.IdsFor("Tessera").Should().HaveCount(11);
        StyleCatalog.IdsFor("Tessera").Should().Contain(["Fluent", "Windows11", "Compact", "MaterialYou"]);
    }

    [Fact]
    public void Tessera_layout_coverage_partitions_style_catalog()
    {
        TesseraLayoutCoverage.CoversCatalog().Should().BeTrue();
        foreach (var id in StyleCatalog.IdsFor("Tessera"))
        {
            var polished = TesseraLayoutCoverage.IsPolished(id);
            var approx = TesseraLayoutCoverage.IsApproximate(id);
            (polished ^ approx).Should().BeTrue($"style {id} must be polished or approximate");
        }
        TesseraLayoutCoverage.IsPolished("Fluent").Should().BeTrue();
        TesseraLayoutCoverage.IsApproximate("PlainText").Should().BeTrue();
    }

    [Fact]
    public void Tessera_layout_fidelity_partitions_style_catalog()
    {
        TesseraLayoutCoverage.CoversLayoutFidelity().Should().BeTrue();
        foreach (var id in StyleCatalog.IdsFor("Tessera"))
        {
            var signed = TesseraLayoutCoverage.IsLayoutFidelitySignedOff(id);
            var deviated = TesseraLayoutCoverage.IsLayoutFidelityDeviated(id);
            (signed ^ deviated).Should().BeTrue($"style {id} must be signed off or deviated");
        }
        TesseraLayoutCoverage.AllLayoutFidelitySignedOff().Should().BeTrue();
        TesseraLayoutCoverage.IsLayoutFidelitySignedOff("MaterialYou").Should().BeTrue();
        TesseraLayoutCoverage.IsLayoutFidelitySignedOff("Radial").Should().BeTrue();
        TesseraLayoutCoverage.IsLayoutFidelityDeviated("Radial").Should().BeFalse();
        TesseraLayoutCoverage.IsLayoutFidelitySignedOff("Windows11").Should().BeTrue();
        TesseraLayoutCoverage.IsLayoutFidelitySignedOff("CoreUI").Should().BeTrue();
        TesseraLayoutCoverage.IsLayoutFidelitySignedOff("ModernFlyouts").Should().BeTrue();
    }

    [Fact]
    public void Tessera_layout_fidelity_github_screenshots_cover_every_style()
    {
        var dir = Path.Combine(FindRepoRoot(), TesseraLayoutCoverage.LayoutFidelityProofRelativeDirectory);
        Directory.Exists(dir).Should().BeTrue(dir);
        foreach (var id in StyleCatalog.IdsFor("Tessera"))
        {
            var path = Path.Combine(dir, $"{id}.png");
            File.Exists(path).Should().BeTrue($"missing layout proof {path}");
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var proofs = Path.Combine(dir.FullName, TesseraLayoutCoverage.LayoutFidelityProofRelativeDirectory);
            if (Directory.Exists(proofs))
                return dir.FullName;
            dir = dir.Parent;
        }

        dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            var proofs = Path.Combine(dir.FullName, TesseraLayoutCoverage.LayoutFidelityProofRelativeDirectory);
            if (Directory.Exists(proofs))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repo .github/res/Tessera proofs.");
    }

    [Fact]
    public void Catalog_covers_widget_modules()
    {
        StyleCatalog.IdsFor("Chrono").Should().NotBeEmpty();
        StyleCatalog.IdsFor("Phono").Should().NotBeEmpty();
        StyleCatalog.IdsFor("Mixdeck").Should().NotBeEmpty();
        StyleCatalog.IsValid("Chrono", "Square").Should().BeTrue();
        StyleCatalog.IsValid("Chrono", "Center").Should().BeTrue();
    }

    [Fact]
    public void Chrono_and_phono_ids_are_non_empty()
    {
        StyleCatalog.IdsFor("Chrono").Should().NotBeEmpty();
        StyleCatalog.IdsFor("Phono").Should().NotBeEmpty();
        StyleCatalog.IsValid("Chrono", "Square").Should().BeTrue();
    }

    [Fact]
    public void StyleCatalog_For_uses_display_names()
    {
        StyleCatalog.For("Tessera").Should().Contain(d =>
            d.StyleId == "MaterialYou" && d.DisplayName == "Material You");
        StyleCatalog.For("Tessera").Should().Contain(d =>
            d.StyleId == "Windows11" && d.DisplayName == "Windows 11");
    }
}

public class TesseraCapabilityTests : IDisposable
{
    private readonly string _root;
    private readonly List<FlyoutRequest> _shown = [];

    public TesseraCapabilityTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "MosaicTessera_" + Guid.NewGuid().ToString("N"));
        AppPaths.SetRootOverride(_root);
        AppPaths.EnsureLayout();
        Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Tessera"));
    }

    public void Dispose()
    {
        AppPaths.ClearRootOverride();
        try { Directory.Delete(_root, true); } catch { /* ignore */ }
    }

    [Fact]
    public async Task Armed_tessera_shows_flyout_on_volume_change()
    {
        var services = HostServicesFakes.Create();
        var ui = new BridgeUi(new CaptureFlyouts(_shown));
        var registry = new CapabilityRegistry();
        BuiltInCapabilityFactories.RegisterAll(registry);
        var daemon = new CapabilityDaemon(registry, services, ui);

        (await daemon.ArmAsync("Tessera")).Should().BeTrue();
        services.Audio.MasterVolume = 0.8;
        _shown.Should().Contain(r => r.ModuleId == "Tessera" && r.Kind == "vol");
    }
}
