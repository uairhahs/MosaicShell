using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests
{
    public class StyleCatalogTests
    {
        [Fact]
        public void Tessera_has_eleven_jaxcore_layouts()
        {
            _ = StyleCatalog.IdsFor("Tessera").Should().HaveCount(11);
            _ = StyleCatalog.IdsFor("Tessera").Should().Contain(["Fluent", "Windows11", "Compact", "MaterialYou"]);
        }

        [Fact]
        public void Tessera_layout_coverage_partitions_style_catalog()
        {
            _ = TesseraLayoutCoverage.CoversCatalog().Should().BeTrue();
            foreach (string id in StyleCatalog.IdsFor("Tessera"))
            {
                bool polished = TesseraLayoutCoverage.IsPolished(id);
                bool approx = TesseraLayoutCoverage.IsApproximate(id);
                _ = (polished ^ approx).Should().BeTrue($"style {id} must be polished or approximate");
            }
            _ = TesseraLayoutCoverage.IsPolished("Fluent").Should().BeTrue();
            _ = TesseraLayoutCoverage.IsApproximate("PlainText").Should().BeTrue();
        }

        [Fact]
        public void Tessera_layout_fidelity_partitions_style_catalog()
        {
            _ = TesseraLayoutCoverage.CoversLayoutFidelity().Should().BeTrue();
            foreach (string id in StyleCatalog.IdsFor("Tessera"))
            {
                bool signed = TesseraLayoutCoverage.IsLayoutFidelitySignedOff(id);
                bool deviated = TesseraLayoutCoverage.IsLayoutFidelityDeviated(id);
                _ = (signed ^ deviated).Should().BeTrue($"style {id} must be signed off or deviated");
            }
            _ = TesseraLayoutCoverage.AllLayoutFidelitySignedOff().Should().BeTrue();
            _ = TesseraLayoutCoverage.IsLayoutFidelitySignedOff("MaterialYou").Should().BeTrue();
            _ = TesseraLayoutCoverage.IsLayoutFidelitySignedOff("Radial").Should().BeTrue();
            _ = TesseraLayoutCoverage.IsLayoutFidelityDeviated("Radial").Should().BeFalse();
            _ = TesseraLayoutCoverage.IsLayoutFidelitySignedOff("Windows11").Should().BeTrue();
            _ = TesseraLayoutCoverage.IsLayoutFidelitySignedOff("CoreUI").Should().BeTrue();
            _ = TesseraLayoutCoverage.IsLayoutFidelitySignedOff("ModernFlyouts").Should().BeTrue();
        }

        [Fact]
        public void Tessera_layout_fidelity_github_screenshots_cover_every_style()
        {
            string dir = Path.Combine(FindRepoRoot(), TesseraLayoutCoverage.LayoutFidelityProofRelativeDirectory);
            _ = Directory.Exists(dir).Should().BeTrue(dir);
            foreach (string id in StyleCatalog.IdsFor("Tessera"))
            {
                string path = Path.Combine(dir, $"{id}.png");
                _ = File.Exists(path).Should().BeTrue($"missing layout proof {path}");
            }
        }

        private static string FindRepoRoot()
        {
            DirectoryInfo? dir = new(AppContext.BaseDirectory);
            while (dir is not null)
            {
                string proofs = Path.Combine(dir.FullName, TesseraLayoutCoverage.LayoutFidelityProofRelativeDirectory);
                if (Directory.Exists(proofs))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir is not null)
            {
                string proofs = Path.Combine(dir.FullName, TesseraLayoutCoverage.LayoutFidelityProofRelativeDirectory);
                if (Directory.Exists(proofs))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate repo .github/res/Tessera proofs.");
        }

        [Fact]
        public void Catalog_covers_widget_modules()
        {
            _ = StyleCatalog.IdsFor("Chrono").Should().NotBeEmpty();
            _ = StyleCatalog.IdsFor("Phono").Should().NotBeEmpty();
            _ = StyleCatalog.IdsFor("Mixdeck").Should().NotBeEmpty();
            _ = StyleCatalog.IsValid("Chrono", "Square").Should().BeTrue();
            _ = StyleCatalog.IsValid("Chrono", "Center").Should().BeTrue();
        }

        [Fact]
        public void Chrono_and_phono_ids_are_non_empty()
        {
            _ = StyleCatalog.IdsFor("Chrono").Should().NotBeEmpty();
            _ = StyleCatalog.IdsFor("Phono").Should().NotBeEmpty();
            _ = StyleCatalog.IsValid("Chrono", "Square").Should().BeTrue();
        }

        [Fact]
        public void StyleCatalog_For_uses_display_names()
        {
            _ = StyleCatalog.For("Tessera").Should().Contain(d =>
                d.StyleId == "MaterialYou" && d.DisplayName == "Material You");
            _ = StyleCatalog.For("Tessera").Should().Contain(d =>
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
            _ = Directory.CreateDirectory(Path.Combine(AppPaths.ModulesDirectory, "Tessera"));
        }

        public void Dispose()
        {
            AppPaths.ClearRootOverride();
            try { Directory.Delete(_root, true); } catch { /* ignore */ }
        }

        [Fact]
        public async Task Armed_tessera_shows_flyout_on_volume_change()
        {
            HostServices services = HostServicesFakes.Create();
            BridgeUi ui = new(new CaptureFlyouts(_shown));
            CapabilityRegistry registry = new();
            BuiltInCapabilityFactories.RegisterAll(registry);
            CapabilityDaemon daemon = new(registry, services, ui);

            _ = (await daemon.ArmAsync("Tessera")).Should().BeTrue();
            services.Audio.MasterVolume = 0.8;
            _ = _shown.Should().Contain(r => r.ModuleId == "Tessera" && r.Kind == "vol");
        }
    }
}
