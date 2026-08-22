using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Capabilities;
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
        StyleCatalog.IdsFor("Tessera").Should().Contain(["Fluent", "Win11", "Simple", "Pixel"]);
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
        TesseraLayoutCoverage.IsApproximate("Plainext").Should().BeTrue();
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
        TesseraLayoutCoverage.AllLayoutFidelitySignedOff().Should().BeFalse();
        TesseraLayoutCoverage.IsLayoutFidelitySignedOff("Pixel").Should().BeTrue();
        TesseraLayoutCoverage.IsLayoutFidelityDeviated("Smouti").Should().BeTrue();
        TesseraLayoutCoverage.IsLayoutFidelitySignedOff("Win11").Should().BeTrue();
        TesseraLayoutCoverage.IsLayoutFidelitySignedOff("CoreUI").Should().BeTrue();
        TesseraLayoutCoverage.IsLayoutFidelitySignedOff("Modern").Should().BeTrue();
    }

    [Fact]
    public void Catalog_covers_widget_modules()
    {
        StyleCatalog.IdsFor("Chrono").Should().NotBeEmpty();
        StyleCatalog.IdsFor("Phono").Should().NotBeEmpty();
        StyleCatalog.IdsFor("Mixdeck").Should().NotBeEmpty();
        StyleCatalog.IsValid("Chrono", "Center").Should().BeTrue();
    }

    [Fact]
    public void Chrono_and_phono_ids_are_non_empty()
    {
        StyleCatalog.IdsFor("Chrono").Should().NotBeEmpty();
        StyleCatalog.IdsFor("Phono").Should().NotBeEmpty();
        StyleCatalog.IsValid("Chrono", "Center").Should().BeTrue();
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
        var ui = new CaptureUi(_shown);
        var registry = new CapabilityRegistry();
        BuiltInCapabilityFactories.RegisterAll(registry);
        var daemon = new CapabilityDaemon(registry, services, ui);

        (await daemon.ArmAsync("Tessera")).Should().BeTrue();
        services.Audio.MasterVolume = 0.8;
        _shown.Should().Contain(r => r.ModuleId == "Tessera" && r.Kind == "vol");
    }

    private sealed class CaptureUi(List<FlyoutRequest> shown) : ICapabilityUiBridge
    {
        public IFlyoutPresenter Flyouts { get; } = new CaptureFlyouts(shown);
        public IHostUiBridge HostUi { get; } = NullHostUiBridge.Instance;
    }

    private sealed class CaptureFlyouts(List<FlyoutRequest> shown) : IFlyoutPresenter
    {
        public void Show(FlyoutRequest request) => shown.Add(request);
        public void Update(FlyoutRequest request) => shown.Add(request);
        public void SoftRefresh(FlyoutRequest request) { }
        public void Hide(string moduleId) { }
        public void HideAll() { }
        public bool IsVisible(string moduleId) => shown.Any(r => r.ModuleId == moduleId);
    }
}
