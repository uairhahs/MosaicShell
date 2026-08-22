using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests;

public class TesseraFlyoutRequestBuilderTests
{
    [Fact]
    public void BuildPayload_matches_capability_shape_for_volume()
    {
        var services = HostServicesFakes.Create();
        services.Audio.MasterVolume = 0.62;
        var settings = new TesseraSettings { Style = "Fluent", ShowMediaStripOnVolume = true };
        var builder = new TesseraFlyoutRequestBuilder();

        var request = builder.Build(services, settings, "vol");
        request.ModuleId.Should().Be("Tessera");
        request.Kind.Should().Be("vol");
        request.StyleId.Should().Be("Fluent");
        request.Payload!["volume"].Should().Be("0.62");
        request.Payload["showMediaStrip"].Should().Be(
            TesseraLayoutCoverage.UsesStackedMediaStrip("Fluent") ? "1" : "0");
    }

    [Fact]
    public void BuildLivePayload_honors_show_media_strip_override()
    {
        var services = HostServicesFakes.Create();
        var settings = new TesseraSettings { Style = "Pixel", ShowMediaStripOnVolume = true };
        var builder = new TesseraFlyoutRequestBuilder();

        builder.BuildLivePayload(services, settings, showMediaStripOverride: false)["showMediaStrip"]
            .Should().Be("0");
    }
}
