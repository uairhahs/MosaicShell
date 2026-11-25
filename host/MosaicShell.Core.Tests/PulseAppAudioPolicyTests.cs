using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Pulse;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Per-app audio from <c>pactl -f json list sink-inputs</c>. The Edge record is trimmed from pactl 17 on PipeWire
    /// (2026-11-24); the others follow its shape.
    /// </summary>
    public sealed class PulseAppAudioPolicyTests
    {
        private static string Stream(uint index, string? name, string? binary, uint left, uint right, bool mute, string media = "Playback")
        {
            string props = string.Join(",", new[]
            {
                name is null ? null : $"\"application.name\": \"{name}\"",
                binary is null ? null : $"\"application.process.binary\": \"{binary}\"",
                $"\"media.name\": \"{media}\"",
            }.Where(p => p is not null));
            return $"{{\"index\": {index}, \"sink\": 51, \"mute\": {(mute ? "true" : "false")}, \"corked\": false, "
                + $"\"volume\": {{\"front-left\": {{\"value\": {left}, \"value_percent\": \"x\", \"db\": \"x\"}}, "
                + $"\"front-right\": {{\"value\": {right}, \"value_percent\": \"x\", \"db\": \"x\"}}}}, \"properties\": {{{props}}}}}";
        }

        private static string List(params string[] streams)
        {
            return "[" + string.Join(",", streams) + "]";
        }

        [Fact]
        public void A_playing_app_is_one_session_with_its_name_volume_and_mute()
        {
            IReadOnlyList<PulseApp> apps = PulseAppAudioPolicy.ParseApps(List(Stream(367, "Microsoft Edge", "msedge", 65536, 65536, mute: false)));

            _ = apps.Select(a => a.ToSession()).Should().Equal(new AppAudioSession("app:msedge", "Microsoft Edge", 1.0, false));
        }

        [Fact]
        public void Two_streams_of_one_process_are_one_session()
        {
            // Two playing tabs: Windows shows the browser once, so this does too, at its loudest stream.
            IReadOnlyList<PulseApp> apps = PulseAppAudioPolicy.ParseApps(List(
                Stream(367, "Microsoft Edge", "msedge", 32768, 32768, mute: false),
                Stream(370, "Spotify", "spotify", 65536, 65536, mute: true),
                Stream(371, "Microsoft Edge", "msedge", 49152, 49152, mute: false)));

            _ = apps.Select(a => a.ToSession()).Should().Equal(
                new AppAudioSession("app:msedge", "Microsoft Edge", 0.75, false),
                new AppAudioSession("app:spotify", "Spotify", 1.0, true));
            _ = apps[0].Streams.Select(s => s.Index).Should().Equal(367u, 371u);
        }

        [Fact]
        public void An_app_is_muted_only_when_every_stream_is()
        {
            PulseApp app = PulseAppAudioPolicy.ParseApps(List(
                Stream(1, "mpv", "mpv", 65536, 65536, mute: true),
                Stream(2, "mpv", "mpv", 65536, 65536, mute: false)))[0];

            _ = app.ToSession().IsMuted.Should().BeFalse();
        }

        [Fact]
        public void Without_a_process_the_name_groups_and_without_either_the_stream_stands_alone()
        {
            IReadOnlyList<PulseApp> apps = PulseAppAudioPolicy.ParseApps(List(
                Stream(5, "Game", null, 65536, 65536, mute: false),
                Stream(6, null, null, 65536, 65536, mute: false, media: "Notification")));

            _ = apps.Select(a => (a.Id, a.DisplayName)).Should().Equal(("name:Game", "Game"), ("stream:6", "Notification"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Failure: Connection refused")]
        [InlineData("[{\"index\": 1}]")]
        public void Unusable_output_lists_nothing(string? json)
        {
            _ = PulseAppAudioPolicy.ParseApps(json).Should().BeEmpty();
        }

        [Fact]
        public void A_volume_change_scales_every_stream_of_the_app_keeping_its_balance()
        {
            PulseApp app = PulseAppAudioPolicy.ParseApps(List(
                Stream(367, "Microsoft Edge", "msedge", 65536, 32768, mute: false),
                Stream(371, "Microsoft Edge", "msedge", 65536, 65536, mute: false)))[0];

            string[][] volume =
            [
                ["set-sink-input-volume", "367", "32768", "16384"],
                ["set-sink-input-volume", "371", "32768", "32768"],
            ];
            string[][] mute = [["set-sink-input-mute", "367", "1"], ["set-sink-input-mute", "371", "1"]];

            _ = PulseAppAudioPolicy.VolumeCommands(app, 0.5).Should().BeEquivalentTo(volume, o => o.WithStrictOrdering());
            _ = PulseAppAudioPolicy.MuteCommands(app, true).Should().BeEquivalentTo(
                mute,
                o => o.WithStrictOrdering());
        }

        [Theory]
        [InlineData("Event 'new' on sink-input #372", true)]
        [InlineData("Event 'change' on sink-input #367", true)]
        [InlineData("Event 'remove' on sink-input #367", true)]
        [InlineData("Event 'change' on sink #51", false)]
        [InlineData("Event 'new' on client #90", false)]
        [InlineData(null, false)]
        public void Only_stream_events_need_a_re_read(string? line, bool expected)
        {
            _ = PulseAppAudioPolicy.NeedsRefresh(line).Should().Be(expected);
        }
    }
}
