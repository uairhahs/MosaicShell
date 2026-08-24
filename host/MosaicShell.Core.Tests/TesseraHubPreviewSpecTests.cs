using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class TesseraHubPreviewSpecTests
{
    [Fact]
    public void Max_height_fits_tallest_rest_card()
    {
        var win11 = TesseraStackedPlacementSpec.Win11VolumeHeightDip
                    + TesseraStackedPlacementSpec.Win11MediaHeightDip;
        TesseraHubPreviewSpec.Win11RestHeightDip.Should().Be(win11);
        TesseraHubPreviewSpec.CoreUiRestHeightDip.Should().Be(TesseraCoreUiLayoutSpec.RestHeightDip);
        TesseraHubPreviewSpec.FluentRestHeightDip.Should().Be(TesseraFluentLayoutSpec.HeightDip);
        TesseraHubPreviewSpec.MaxHeightDip.Should().BeGreaterThanOrEqualTo(win11);
        TesseraHubPreviewSpec.MaxHeightDip.Should().BeGreaterThanOrEqualTo(TesseraHubPreviewSpec.CoreUiRestHeightDip);
        TesseraHubPreviewSpec.MaxHeightDip.Should().Be(
            Math.Max(win11, Math.Max(TesseraHubPreviewSpec.CoreUiRestHeightDip, TesseraHubPreviewSpec.FluentRestHeightDip)));
    }
}
