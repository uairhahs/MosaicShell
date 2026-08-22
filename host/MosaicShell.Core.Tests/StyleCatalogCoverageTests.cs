using MosaicShell.Core.Modules.Tessera;
using FluentAssertions;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests;

public class StyleCatalogCoverageTests
{
    [Fact]
    public void Tessera_style_catalog_is_fully_classified()
    {
        TesseraLayoutCoverage.CoversCatalog().Should().BeTrue();
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
        var method = coverageType.GetMethod("CoversCatalog");
        method.Should().NotBeNull();
        var covers = (bool)method!.Invoke(null, null)!;
        covers.Should().BeTrue($"{coverageType.Name} should classify every StyleCatalog id");
    }

    [Fact]
    public void Flagship_styles_are_documented_without_flipping_fidelity_flags()
    {
        ChronoLayoutCoverage.IsFlagship("Center").Should().BeTrue();
        PhonoLayoutCoverage.IsFlagship("Simple").Should().BeTrue();
        PulseLayoutCoverage.IsFlagship("Regular").Should().BeTrue();
        CanvasLayoutCoverage.IsFlagship("DEFAULT").Should().BeTrue();
        MixdeckLayoutCoverage.IsFlagship("Fluent").Should().BeTrue();
        InlayLayoutCoverage.IsFlagship("Win11").Should().BeTrue();
        TesseraLayoutCoverage.IsPolished("Pixel").Should().BeTrue();
        TesseraLayoutCoverage.UsesStackedMediaStrip("Pixel").Should().BeFalse();
        TesseraLayoutCoverage.UsesStackedMediaStrip("Modern").Should().BeTrue();
    }
}
