using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class TesseraHubPreviewSpecTests
{
    [Fact]
    public void Max_height_fits_win11_rest_card()
    {
        var rest = TesseraStackedPlacementSpec.Win11VolumeHeightDip
                   + TesseraStackedPlacementSpec.Win11MediaHeightDip;
        TesseraHubPreviewSpec.Win11RestHeightDip.Should().Be(rest);
        TesseraHubPreviewSpec.MaxHeightDip.Should().BeGreaterThanOrEqualTo(rest);
        TesseraHubPreviewSpec.MaxHeightDip.Should().Be(rest);
    }
}
