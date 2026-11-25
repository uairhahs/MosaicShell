using FluentAssertions;
using MosaicShell.Core.Services.Linux;
using MosaicShell.Core.Services.Mpris;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary>
    /// Players disagree on D-Bus types for the same MPRIS field. Each case here is a shape seen in the wild; the decoder
    /// must read all of them and must never throw on a field it does not understand.
    /// </summary>
    public class MprisVariantDecoderTests
    {
        private const string Bus = "org.mpris.MediaPlayer2.test";
        private static readonly Dictionary<string, VariantValue> None = [];

        private static Dictionary<string, VariantValue> Player(Dictionary<string, VariantValue> metadata, string status = "Playing")
        {
            return new()
            {
                ["PlaybackStatus"] = VariantValue.String(status),
                ["Metadata"] = new Dict<string, VariantValue>(metadata).AsVariantValue(),
                ["Position"] = VariantValue.Int64(13_562_341),
                ["CanSeek"] = VariantValue.Bool(true),
            };
        }

        [Fact]
        public void Chromium_shape_decodes()
        {
            Dictionary<string, VariantValue> root = new() { ["Identity"] = VariantValue.String("Microsoft Edge") };
            Dictionary<string, VariantValue> player = Player(new()
            {
                ["mpris:trackid"] = VariantValue.ObjectPath(new ObjectPath("/org/chromium/MediaPlayer2/TrackList/Track419B")),
                ["mpris:length"] = VariantValue.Int64(165_201_927),
                ["xesam:title"] = VariantValue.String("Todo Lo Que Soy | YouTube Music"),
                ["xesam:artist"] = VariantValue.Array(new[] { "" }),
                ["xesam:album"] = VariantValue.String(""),
            });

            MprisPlayerState state = MprisVariantDecoder.Decode(Bus, root, player, ownerPid: 66176);

            _ = state.Identity.Should().Be("Microsoft Edge");
            _ = state.DesktopEntry.Should().BeNull();
            _ = state.PlaybackStatus.Should().Be(MprisPlaybackStatus.Playing);
            _ = state.TrackId.Should().Be("/org/chromium/MediaPlayer2/TrackList/Track419B");
            _ = state.LengthMicros.Should().Be(165_201_927);
            _ = state.PositionMicros.Should().Be(13_562_341);
            _ = state.Title.Should().Be("Todo Lo Que Soy | YouTube Music");
            _ = state.Artists.Should().Equal("");
            _ = state.CanSeek.Should().BeTrue();
            _ = state.LoopStatus.Should().BeNull();
            _ = state.Shuffle.Should().BeNull();
            _ = state.OwnerPid.Should().Be(66176);
            _ = state.MirroredPid.Should().BeNull();
        }

        [Fact]
        public void Plasma_mirror_shape_decodes_with_its_kde_pid()
        {
            Dictionary<string, VariantValue> root = new()
            {
                ["Identity"] = VariantValue.String("Microsoft Edge"),
                ["DesktopEntry"] = VariantValue.String("microsoft-edge"),
            };
            Dictionary<string, VariantValue> player = Player(new()
            {
                ["kde:pid"] = VariantValue.UInt32(66176),
                ["mpris:artUrl"] = VariantValue.String("file:///tmp/plasma-browser-integration_artwork_JwuFJz.jpg"),
                ["mpris:length"] = VariantValue.Int64(165_000_000),
                ["mpris:trackid"] = VariantValue.ObjectPath(new ObjectPath("/org/kde/plasma/browser_integration/1337")),
                ["xesam:artist"] = VariantValue.Array(new[] { "Ramon Bedoya & Yerathel" }),
                ["xesam:title"] = VariantValue.String("Todo Lo Que Soy"),
            });
            player["LoopStatus"] = VariantValue.String("None");

            MprisPlayerState state = MprisVariantDecoder.Decode(Bus, root, player, ownerPid: 70001);

            _ = state.DesktopEntry.Should().Be("microsoft-edge");
            _ = state.ArtUrl.Should().Be("file:///tmp/plasma-browser-integration_artwork_JwuFJz.jpg");
            _ = state.Artists.Should().Equal("Ramon Bedoya & Yerathel");
            _ = state.LoopStatus.Should().Be("None");
            _ = state.MirroredPid.Should().Be(66176);
        }

        [Theory]
        [InlineData("int32")]
        [InlineData("uint64")]
        [InlineData("double")]
        [InlineData("uint32")]
        public void Length_reads_whatever_integer_or_float_type_the_player_used(string type)
        {
            VariantValue length = type switch
            {
                "int32" => VariantValue.Int32(200_000_000),
                "uint64" => VariantValue.UInt64(200_000_000),
                "uint32" => VariantValue.UInt32(200_000_000),
                _ => VariantValue.Double(200_000_000.0),
            };

            MprisPlayerState state = MprisVariantDecoder.Decode(Bus, None, Player(new() { ["mpris:length"] = length }), null);

            _ = state.LengthMicros.Should().Be(200_000_000);
        }

        [Fact]
        public void A_string_trackid_and_a_bare_string_artist_are_accepted()
        {
            MprisPlayerState state = MprisVariantDecoder.Decode(Bus, None, Player(new()
            {
                ["mpris:trackid"] = VariantValue.String("/some/track"),
                ["xesam:artist"] = VariantValue.String("Solo"),
            }), null);

            _ = state.TrackId.Should().Be("/some/track");
            _ = state.Artists.Should().Equal("Solo");
        }

        [Fact]
        public void A_field_of_an_unexpected_type_is_ignored_not_thrown()
        {
            MprisPlayerState state = MprisVariantDecoder.Decode(Bus, None, Player(new()
            {
                ["xesam:title"] = VariantValue.Int32(5),
                ["mpris:length"] = VariantValue.String("long"),
                ["mpris:artUrl"] = VariantValue.Bool(true),
            }), null);

            _ = state.Title.Should().BeNull();
            _ = state.LengthMicros.Should().Be(0);
            _ = state.ArtUrl.Should().BeNull();
        }

        [Theory]
        [InlineData("Playing", MprisPlaybackStatus.Playing)]
        [InlineData("Paused", MprisPlaybackStatus.Paused)]
        [InlineData("Stopped", MprisPlaybackStatus.Stopped)]
        [InlineData("Buffering", MprisPlaybackStatus.Stopped)]
        public void Playback_status_maps_and_unknown_reads_as_stopped(string raw, MprisPlaybackStatus expected)
        {
            MprisPlayerState state = MprisVariantDecoder.Decode(Bus, None, Player([], raw), null);

            _ = state.PlaybackStatus.Should().Be(expected);
        }

        [Fact]
        public void Missing_everything_still_yields_a_player()
        {
            MprisPlayerState state = MprisVariantDecoder.Decode(Bus, None, None, null);

            _ = state.BusName.Should().Be(Bus);
            _ = state.PlaybackStatus.Should().Be(MprisPlaybackStatus.Stopped);
            _ = state.Artists.Should().BeEmpty();
        }

        [Fact]
        public void Shuffle_reads_when_published()
        {
            Dictionary<string, VariantValue> player = Player([]);
            player["Shuffle"] = VariantValue.Bool(true);

            _ = MprisVariantDecoder.Decode(Bus, None, player, null).Shuffle.Should().BeTrue();
        }
    }
}
