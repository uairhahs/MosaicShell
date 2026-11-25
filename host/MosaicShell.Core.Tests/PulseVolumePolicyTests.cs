using FluentAssertions;
using MosaicShell.Core.Services.Pulse;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Volume and mute of the default output as <c>pactl -f json</c> reports them, mapped to the 0..1 master volume the
    /// flyouts use. Output captured from pactl 17 on PipeWire (2026-11-22).
    /// </summary>
    public sealed class PulseVolumePolicyTests
    {
        private const string Stereo56 =
            "{\"volume\":{\"front-left\":{\"value\":36700,\"value_percent\":\"56%\",\"db\":\"-15.11 dB\"},"
            + "\"front-right\":{\"value\":36700,\"value_percent\":\"56%\",\"db\":\"-15.11 dB\"}},\"balance\":0.00}";

        [Fact]
        public void Reads_each_channel_in_order()
        {
            _ = PulseVolumePolicy.TryParseVolume(Stereo56, out IReadOnlyList<PulseChannel> channels).Should().BeTrue();

            _ = channels.Should().Equal(new PulseChannel("front-left", 36700), new PulseChannel("front-right", 36700));
            _ = PulseVolumePolicy.Master(channels).Should().BeApproximately(36700 / 65536.0, 1e-9);
        }

        [Fact]
        public void The_master_is_the_loudest_channel_as_plasma_shows_it()
        {
            PulseChannel[] unbalanced = [new("front-left", 32768), new("front-right", 16384)];

            _ = PulseVolumePolicy.Master(unbalanced).Should().Be(0.5);
        }

        [Theory]
        [InlineData(98304u, 1.0)]
        [InlineData(0u, 0.0)]
        public void Over_amplified_volume_reads_as_full_and_silence_as_zero(uint value, double expected)
        {
            _ = PulseVolumePolicy.Master([new PulseChannel("mono", value)]).Should().Be(expected);
        }

        [Fact]
        public void Setting_the_master_keeps_the_balance()
        {
            PulseChannel[] unbalanced = [new("front-left", 32768), new("front-right", 16384)];

            _ = PulseVolumePolicy.ScaleTo(unbalanced, 0.25).Should().Equal(16384u, 8192u);
        }

        [Fact]
        public void Setting_from_silence_sets_every_channel_equal()
        {
            PulseChannel[] silent = [new("front-left", 0), new("front-right", 0)];

            _ = PulseVolumePolicy.ScaleTo(silent, 0.5).Should().Equal(32768u, 32768u);
        }

        [Theory]
        [InlineData(-0.2, 0u)]
        [InlineData(1.7, 65536u)]
        public void A_target_outside_0_to_1_is_clamped(double target, uint expected)
        {
            _ = PulseVolumePolicy.ScaleTo([new PulseChannel("mono", 30000)], target).Should().Equal(expected);
        }

        [Theory]
        [InlineData("{\"mute\":false}", false)]
        [InlineData("{\"mute\":true}", true)]
        public void Reads_mute(string json, bool expected)
        {
            _ = PulseVolumePolicy.TryParseMute(json, out bool muted).Should().BeTrue();
            _ = muted.Should().Be(expected);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Connection failure: Connection refused")]
        [InlineData("{\"volume\":{}}")]
        [InlineData("{\"other\":1}")]
        public void Anything_else_is_not_a_volume(string output)
        {
            _ = PulseVolumePolicy.TryParseVolume(output, out _).Should().BeFalse();
            _ = PulseVolumePolicy.TryParseMute(output, out _).Should().BeFalse();
        }

        // pactl 17.0 as NixOS ships it (2026-11-23): `-f json list sinks` is JSON, but `-f json get-sink-volume` and
        // `get-sink-mute` still print text, so the service read neither and showed 0% and muted.
        private const string ListSinks =
            "[{\"index\":131,\"state\":\"SUSPENDED\",\"name\":\"alsa_output.usb-headset\",\"mute\":true,"
            + "\"volume\":{\"mono\":{\"value\":65536,\"value_percent\":\"100%\",\"db\":\"0.00 dB\"}},\"balance\":0.00},"
            + "{\"index\":132,\"state\":\"RUNNING\",\"name\":\"alsa_output.pci-0000_00_1f.3.hdmi-stereo\","
            + "\"description\":\"Built-in Audio Digital Stereo (HDMI)\",\"driver\":\"PipeWire\",\"mute\":false,"
            + "\"volume\":{\"front-left\":{\"value\":20972,\"value_percent\":\"32%\",\"db\":\"-29.69 dB\"},"
            + "\"front-right\":{\"value\":20972,\"value_percent\":\"32%\",\"db\":\"-29.69 dB\"}},\"balance\":0.00,"
            + "\"base_volume\":{\"value\":65536,\"value_percent\":\"100%\",\"db\":\"0.00 dB\"}}]";

        [Fact]
        public void Reads_the_named_sink_from_list_sinks()
        {
            _ = PulseVolumePolicy.TryParseSink(
                    ListSinks, "alsa_output.pci-0000_00_1f.3.hdmi-stereo", out IReadOnlyList<PulseChannel> channels, out bool muted)
                .Should().BeTrue();

            _ = channels.Should().Equal(new PulseChannel("front-left", 20972), new PulseChannel("front-right", 20972));
            _ = muted.Should().BeFalse();
        }

        [Fact]
        public void Another_sink_in_the_list_is_not_read()
        {
            _ = PulseVolumePolicy.TryParseSink(ListSinks, "alsa_output.usb-headset", out IReadOnlyList<PulseChannel> channels, out bool muted)
                .Should().BeTrue();

            _ = channels.Should().Equal(new PulseChannel("mono", 65536));
            _ = muted.Should().BeTrue();
        }

        [Theory]
        [InlineData("alsa_output.missing")]
        [InlineData("")]
        public void A_sink_not_in_the_list_is_not_read(string sink)
        {
            _ = PulseVolumePolicy.TryParseSink(ListSinks, sink, out _, out _).Should().BeFalse();
        }

        [Theory]
        [InlineData("")]
        [InlineData("Volume: front-left: 20972 /  32% / -29.69 dB,   front-right: 20972 /  32% / -29.69 dB")]
        [InlineData("{\"volume\":{}}")]
        [InlineData("[{\"name\":\"s\",\"volume\":{}}]")]
        [InlineData("[{\"name\":\"s\",\"volume\":{\"mono\":{\"value\":1}}}]")]
        public void Anything_else_is_not_a_sink(string output)
        {
            _ = PulseVolumePolicy.TryParseSink(output, "s", out _, out _).Should().BeFalse();
        }

        [Fact]
        public void Unknown_fields_are_ignored()
        {
            string json = "{\"volume\":{\"mono\":{\"value\":100,\"extra\":[1,2]}},\"balance\":0,\"future\":{\"x\":1}}";

            _ = PulseVolumePolicy.TryParseVolume(json, out IReadOnlyList<PulseChannel> channels).Should().BeTrue();
            _ = channels.Should().Equal(new PulseChannel("mono", 100));
        }

        [Theory]
        [InlineData("Event 'change' on sink #78", true)]
        [InlineData("Event 'change' on server #-1", true)]
        [InlineData("Event 'new' on sink #90", true)]
        [InlineData("Event 'remove' on sink #90", true)]
        [InlineData("Event 'change' on client #2410", false)]
        [InlineData("Event 'change' on card #52", false)]
        [InlineData("Event 'change' on sink-input #12", false)]
        [InlineData("Event 'change' on source #3", false)]
        [InlineData("", false)]
        public void Only_output_device_and_default_device_events_need_a_refresh(string line, bool expected)
        {
            _ = PulseVolumePolicy.NeedsRefresh(line).Should().Be(expected);
        }
    }
}
