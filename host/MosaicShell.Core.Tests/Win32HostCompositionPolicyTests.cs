using FluentAssertions;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class Win32HostCompositionPolicyTests
{
    [Fact]
    public void Prefer_winui_composition_tracks_soft_frost_window_mapping()
    {
        Win32HostCompositionPolicy.PreferWinUiComposition.Should().Be(
            TesseraFlyoutWindowPolicy.PreferWinUiCompositionForSoftFrost,
            "Program must read Win32HostCompositionPolicy only; SoftFrost owns the mapping");
    }
}
