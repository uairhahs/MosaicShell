using FluentAssertions;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class HubTileStatusChromeSpecTests
{
    [Theory]
    [InlineData(false, false, false, false, HubTileStatusKind.NotInstalled)]
    [InlineData(true, false, false, false, HubTileStatusKind.Ready)]
    [InlineData(true, false, false, true, HubTileStatusKind.Running)]
    [InlineData(true, true, false, false, HubTileStatusKind.ReadyToArm)]
    [InlineData(true, true, true, false, HubTileStatusKind.Armed)]
    public void Resolve_maps_library_flags(
        bool installed, bool capability, bool armed, bool running, HubTileStatusKind expected)
    {
        HubTileStatusChromeSpec.Resolve(installed, capability, armed, running).Should().Be(expected);
    }

    [Theory]
    [InlineData(HubTileStatusKind.NotInstalled, "#6C7086")]
    [InlineData(HubTileStatusKind.Ready, "#A6E3A1")]
    [InlineData(HubTileStatusKind.ReadyToArm, "#F9E2AF")]
    [InlineData(HubTileStatusKind.Armed, "#89B4FA")]
    [InlineData(HubTileStatusKind.Running, "#94E2D5")]
    public void Hex_for_kind_is_mocha_and_parses(HubTileStatusKind kind, string hex)
    {
        HubTileStatusChromeSpec.HexFor(kind).Should().Be(hex);
        TesseraAccentColor.TryParse(hex, out _, out _, out _).Should().BeTrue();
    }

    [Fact]
    public void Capability_running_flag_does_not_override_armed_state()
    {
        // Widgets use Running; capabilities use Armed even if a running bit is set.
        HubTileStatusChromeSpec.Resolve(installed: true, isCapability: true, armed: true, running: true)
            .Should().Be(HubTileStatusKind.Armed);
    }
}
