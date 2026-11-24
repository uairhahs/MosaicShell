using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Pulse;

namespace MosaicShell.Core.Tests
{
    /// <summary>Output devices from <c>pactl -f json list sinks</c>; the HDMI sink is as PipeWire reported it (2026-11-24).</summary>
    public sealed class PulseOutputDevicePolicyTests
    {
        private const string Sinks =
            "[{\"index\": 51, \"name\": \"alsa_output.pci-0000_00_1f.3.hdmi-stereo\", \"description\": \"Built-in Audio Digital Stereo (HDMI)\"},"
            + "{\"index\": 60, \"name\": \"bluez_output.AA_BB.1\", \"description\": \"\"}]";

        [Fact]
        public void Each_sink_is_an_output_named_as_Plasma_shows_it_and_the_default_is_marked()
        {
            // pactl prints the default with a trailing newline.
            _ = PulseOutputDevicePolicy.ParseOutputs(Sinks, "alsa_output.pci-0000_00_1f.3.hdmi-stereo\n").Should().Equal(
                new AudioOutputDevice("alsa_output.pci-0000_00_1f.3.hdmi-stereo", "Built-in Audio Digital Stereo (HDMI)", true),
                new AudioOutputDevice("bluez_output.AA_BB.1", "bluez_output.AA_BB.1", false));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Failure: Connection refused")]
        [InlineData("[{\"index\": 1}]")]
        public void Unusable_output_lists_nothing(string? json)
        {
            _ = PulseOutputDevicePolicy.ParseOutputs(json, "x").Should().BeEmpty();
        }
    }
}
