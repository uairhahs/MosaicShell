using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Mpris;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// How MPRIS players become one <see cref="MediaSessionInfo"/>. The player shapes here are taken from a live KDE
    /// session (2026-11-18): Edge's own player and the plasma-browser-integration mirror of the same tab.
    /// </summary>
    public class MprisPolicyTests
    {
        private static readonly DateTimeOffset T0 = new(2026, 11, 18, 20, 0, 0, TimeSpan.Zero);

        /// <summary>Edge's own MPRIS player: tab title, empty artist, no art, no desktop entry.</summary>
        private static MprisPlayerState EdgeNative()
        {
            return new MprisPlayerState("org.mpris.MediaPlayer2.edge.instance66176")
            {
                Identity = "Microsoft Edge",
                PlaybackStatus = MprisPlaybackStatus.Playing,
                TrackId = "/org/chromium/MediaPlayer2/TrackList/Track419B",
                Title = "Todo Lo Que Soy | YouTube Music",
                Artists = [""],
                LengthMicros = 165_201_927,
                PositionMicros = 13_562_341,
                OwnerPid = 66176,
            };
        }

        /// <summary>The plasma-browser-integration mirror of that tab: clean metadata, art, and kde:pid naming Edge.</summary>
        private static MprisPlayerState PlasmaMirror()
        {
            return new MprisPlayerState("org.mpris.MediaPlayer2.plasma-browser-integration")
            {
                Identity = "Microsoft Edge",
                DesktopEntry = "microsoft-edge",
                PlaybackStatus = MprisPlaybackStatus.Playing,
                TrackId = "/org/kde/plasma/browser_integration/1337",
                Title = "Todo Lo Que Soy",
                Artists = ["Ramon Bedoya & Yerathel"],
                ArtUrl = "file:///tmp/plasma-browser-integration_artwork_JwuFJz.jpg",
                LengthMicros = 165_000_000,
                PositionMicros = 13_722_032,
                LoopStatus = "None",
                OwnerPid = 70001,
                MirroredPid = 66176,
            };
        }

        [Theory]
        [InlineData("org.mpris.MediaPlayer2.spotify", true)]
        [InlineData("org.mpris.MediaPlayer2.edge.instance66176", true)]
        [InlineData("org.mpris.MediaPlayer2", false)]
        [InlineData("org.mpris.MediaPlayer2.", false)]
        [InlineData("org.freedesktop.Notifications", false)]
        [InlineData(":1.42", false)]
        [InlineData(null, false)]
        public void Only_names_under_the_mpris_prefix_are_players(string? name, bool expected)
        {
            _ = MprisPolicy.IsPlayerBusName(name).Should().Be(expected);
        }

        [Theory]
        [InlineData("org.mpris.MediaPlayer2.spotify", null, "spotify")]
        [InlineData("org.mpris.MediaPlayer2.edge.instance66176", null, "edge")]
        [InlineData("org.mpris.MediaPlayer2.firefox.instance_1_23", null, "firefox")]
        [InlineData("org.mpris.MediaPlayer2.chromium.instance4021", null, "chromium")]
        [InlineData("org.mpris.MediaPlayer2.plasma-browser-integration", "microsoft-edge", "microsoft-edge")]
        [InlineData("org.mpris.MediaPlayer2.vlc", "  ", "vlc")]
        public void The_app_id_is_the_desktop_entry_or_the_bus_name_without_its_instance(
            string busName, string? desktopEntry, string expected)
        {
            MprisPlayerState player = new(busName) { DesktopEntry = desktopEntry };

            _ = MprisPolicy.AppIdOf(player).Should().Be(expected);
        }

        [Fact]
        public void Artists_join_and_blank_ones_count_as_none()
        {
            _ = MprisPolicy.JoinArtists(["A", "", " ", "B"]).Should().Be("A, B");
            _ = MprisPolicy.JoinArtists([""]).Should().BeNull();
            _ = MprisPolicy.JoinArtists([]).Should().BeNull();
        }

        [Fact]
        public void A_player_maps_to_a_session_in_seconds()
        {
            byte[] art = [1, 2, 3];

            MediaSessionInfo info = MprisPolicy.ToSessionInfo(PlasmaMirror(), art);

            _ = info.Title.Should().Be("Todo Lo Que Soy");
            _ = info.Artist.Should().Be("Ramon Bedoya & Yerathel");
            _ = info.AppId.Should().Be("microsoft-edge");
            _ = info.IsPlaying.Should().BeTrue();
            _ = info.ThumbnailPng.Should().BeSameAs(art);
            _ = info.PositionSeconds.Should().BeApproximately(13.722032, 1e-9);
            _ = info.DurationSeconds.Should().Be(165);
            _ = info.LikeRating.Should().BeNull();
            _ = info.Capabilities.Should().Be(BrowserMediaCapabilities.None);
        }

        [Fact]
        public void Negative_length_or_position_reads_as_zero()
        {
            MprisPlayerState odd = PlasmaMirror() with { LengthMicros = -1, PositionMicros = -5 };

            MediaSessionInfo info = MprisPolicy.ToSessionInfo(odd, null);

            _ = info.DurationSeconds.Should().Be(0);
            _ = info.PositionSeconds.Should().Be(0);
        }

        [Fact]
        public void A_mirror_replaces_the_player_it_mirrors()
        {
            IReadOnlyList<MprisPlayerState> shown = MprisPolicy.WithoutMirrored([EdgeNative(), PlasmaMirror()]);

            _ = shown.Select(p => p.BusName).Should().Equal("org.mpris.MediaPlayer2.plasma-browser-integration");
        }

        [Fact]
        public void A_mirror_of_a_process_that_has_no_player_hides_nothing()
        {
            MprisPlayerState unrelated = EdgeNative() with { OwnerPid = 12345 };

            IReadOnlyList<MprisPlayerState> shown = MprisPolicy.WithoutMirrored([unrelated, PlasmaMirror()]);

            _ = shown.Should().HaveCount(2);
        }

        [Fact]
        public void A_player_with_an_unknown_pid_is_never_treated_as_mirrored()
        {
            MprisPlayerState unknown = EdgeNative() with { OwnerPid = null };
            MprisPlayerState mirror = PlasmaMirror() with { MirroredPid = null };

            _ = MprisPolicy.WithoutMirrored([unknown, mirror]).Should().HaveCount(2);
        }

        [Fact]
        public void The_most_recently_started_player_wins_while_several_play()
        {
            MprisPlayerState a = new("org.mpris.MediaPlayer2.a") { PlaybackStatus = MprisPlaybackStatus.Playing };
            MprisPlayerState b = new("org.mpris.MediaPlayer2.b") { PlaybackStatus = MprisPlaybackStatus.Playing };
            Dictionary<string, DateTimeOffset> since = new()
            {
                [a.BusName] = T0,
                [b.BusName] = T0.AddSeconds(5),
            };

            _ = MprisPolicy.PickActive([a, b], previous: a.BusName, since)!.BusName.Should().Be(b.BusName);
        }

        [Fact]
        public void A_playing_player_beats_the_paused_one_that_was_shown()
        {
            MprisPlayerState paused = new("org.mpris.MediaPlayer2.a") { PlaybackStatus = MprisPlaybackStatus.Paused };
            MprisPlayerState playing = new("org.mpris.MediaPlayer2.b") { PlaybackStatus = MprisPlaybackStatus.Playing };

            _ = MprisPolicy.PickActive([paused, playing], previous: paused.BusName, new Dictionary<string, DateTimeOffset>())!
                .BusName.Should().Be(playing.BusName);
        }

        [Fact]
        public void When_nothing_plays_the_player_already_shown_stays()
        {
            MprisPlayerState a = new("org.mpris.MediaPlayer2.a") { PlaybackStatus = MprisPlaybackStatus.Paused };
            MprisPlayerState b = new("org.mpris.MediaPlayer2.b") { PlaybackStatus = MprisPlaybackStatus.Paused };
            Dictionary<string, DateTimeOffset> since = new() { [a.BusName] = T0.AddSeconds(9) };

            _ = MprisPolicy.PickActive([a, b], previous: b.BusName, since)!.BusName.Should().Be(b.BusName);
        }

        [Fact]
        public void When_nothing_plays_and_nothing_was_shown_the_last_to_play_is_picked()
        {
            MprisPlayerState a = new("org.mpris.MediaPlayer2.a") { PlaybackStatus = MprisPlaybackStatus.Paused };
            MprisPlayerState b = new("org.mpris.MediaPlayer2.b") { PlaybackStatus = MprisPlaybackStatus.Stopped, Title = "Last track" };
            Dictionary<string, DateTimeOffset> since = new()
            {
                [a.BusName] = T0,
                [b.BusName] = T0.AddSeconds(3),
            };

            _ = MprisPolicy.PickActive([a, b], previous: null, since)!.BusName.Should().Be(b.BusName);
        }

        [Fact]
        public void A_stopped_player_with_no_track_is_not_a_session()
        {
            MprisPlayerState idle = new("org.mpris.MediaPlayer2.a") { PlaybackStatus = MprisPlaybackStatus.Stopped };

            _ = MprisPolicy.PickActive([idle], previous: idle.BusName, new Dictionary<string, DateTimeOffset>()).Should().BeNull();
        }

        [Fact]
        public void No_players_means_no_session()
        {
            _ = MprisPolicy.PickActive([], previous: null, new Dictionary<string, DateTimeOffset>()).Should().BeNull();
        }

        [Theory]
        [InlineData("None", "Playlist")]
        [InlineData("Playlist", "Track")]
        [InlineData("Track", "None")]
        [InlineData(null, "Playlist")]
        [InlineData("garbage", "None")]
        public void Repeat_cycles_like_the_windows_order(string? current, string expected)
        {
            _ = MprisPolicy.NextLoopStatus(current).Should().Be(expected);
        }

        [Theory]
        [InlineData("/org/kde/plasma/browser_integration/1337", true)]
        [InlineData("/org/mpris/MediaPlayer2/TrackList/NoTrack", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void Absolute_seek_needs_a_real_track_id(string? trackId, bool expected)
        {
            _ = MprisPolicy.CanSeekAbsolute(trackId).Should().Be(expected);
        }
    }
}
