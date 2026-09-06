using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class TesseraHubPreviewSpecTests
    {
        [Fact]
        public void Max_height_fits_tallest_rest_card()
        {
            double win11 = TesseraStackedPlacementSpec.Win11VolumeHeightDip
                        + TesseraStackedPlacementSpec.Win11MediaHeightDip;
            _ = TesseraHubPreviewSpec.Win11RestHeightDip.Should().Be(win11);
            _ = TesseraHubPreviewSpec.CoreUiRestHeightDip.Should().Be(TesseraCoreUiLayoutSpec.RestHeightDip);
            _ = TesseraHubPreviewSpec.FluentRestHeightDip.Should().Be(TesseraFluentLayoutSpec.HeightDip);
            _ = TesseraHubPreviewSpec.MaxHeightDip.Should().BeGreaterThanOrEqualTo(win11);
            _ = TesseraHubPreviewSpec.MaxHeightDip.Should().BeGreaterThanOrEqualTo(TesseraHubPreviewSpec.CoreUiRestHeightDip);
            _ = TesseraHubPreviewSpec.MaxHeightDip.Should().Be(
                Math.Max(win11, Math.Max(TesseraHubPreviewSpec.CoreUiRestHeightDip, TesseraHubPreviewSpec.FluentRestHeightDip)));
        }
    }
}
