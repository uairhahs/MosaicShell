using System.Reflection;
using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests
{
    public class StyleCatalogCoverageTests
    {
        [Fact]
        public void Tessera_style_catalog_is_fully_classified()
        {
            _ = TesseraLayoutCoverage.CoversCatalog().Should().BeTrue();
        }

        [Theory]
        [InlineData(typeof(ChronoLayoutCoverage))]
        [InlineData(typeof(PhonoLayoutCoverage))]
        [InlineData(typeof(PulseLayoutCoverage))]
        [InlineData(typeof(CanvasLayoutCoverage))]
        [InlineData(typeof(MixdeckLayoutCoverage))]
        [InlineData(typeof(InlayLayoutCoverage))]
        [InlineData(typeof(ChordLayoutCoverage))]
        [InlineData(typeof(SubstrateLayoutCoverage))]
        [InlineData(typeof(SlateLayoutCoverage))]
        public void Widget_and_capability_styles_have_coverage_entries(Type coverageType)
        {
            MethodInfo? method = coverageType.GetMethod("CoversCatalog");
            _ = method.Should().NotBeNull();
            bool covers = (bool)method!.Invoke(null, null)!;
            _ = covers.Should().BeTrue($"{coverageType.Name} should classify every StyleCatalog id");
        }

        [Fact]
        public void Flagship_styles_are_documented_without_flipping_fidelity_flags()
        {
            _ = ChronoLayoutCoverage.IsFlagship("Square").Should().BeTrue();
            _ = ChronoLayoutCoverage.IsFlagship("Center").Should().BeTrue();
            _ = PhonoLayoutCoverage.IsFlagship("Compact").Should().BeTrue();
            _ = PhonoLayoutCoverage.IsFlagship("Simple").Should().BeTrue();
            _ = PulseLayoutCoverage.IsFlagship("Regular").Should().BeTrue();
            _ = CanvasLayoutCoverage.IsFlagship("DEFAULT").Should().BeTrue();
            _ = MixdeckLayoutCoverage.IsFlagship("Fluent").Should().BeTrue();
            _ = InlayLayoutCoverage.IsFlagship("Windows11").Should().BeTrue();
            _ = InlayLayoutCoverage.IsFlagship("Win11").Should().BeTrue();
            _ = TesseraLayoutCoverage.IsPolished("MaterialYou").Should().BeTrue();
            _ = TesseraLayoutCoverage.IsPolished("Pixel").Should().BeTrue();
            _ = TesseraLayoutCoverage.UsesStackedMediaStrip("MaterialYou").Should().BeFalse();
            _ = TesseraLayoutCoverage.UsesStackedMediaStrip("ModernFlyouts").Should().BeTrue();
            _ = TesseraLayoutCoverage.RequiresLiveVolumePercentLabel("Meter").Should().BeTrue();
            _ = TesseraLayoutCoverage.RequiresLiveVolumePercentLabel("Amber").Should().BeTrue();
            _ = TesseraLayoutCoverage.RequiresLiveVolumePercentLabel("Fluent").Should().BeFalse();
        }
    }
}
