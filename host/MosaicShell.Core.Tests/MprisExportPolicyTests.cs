using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Mpris;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The MPRIS player the Host publishes on Linux from Grout's report, so Plasma's media controls, the lock screen and
    /// anything else that reads MPRIS get the page's own title, artist and cover for the whole track.
    /// </summary>
    public class MprisExportPolicyTests
    {
        private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 1, 2, 3];
        private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, 0x4A, 0x46, 0x49, 0x46, 0, 1, 9];

        private static BrowserPlayerSnapshot Snapshot(
            string title = "Vision", string artist = "Pìjus", BrowserPlaybackState state = BrowserPlaybackState.Playing,
            double position = 12.5, double duration = 201.4)
        {
            return new BrowserPlayerSnapshot
            {
                Name = "YouTube Music",
                Title = title,
                Artist = artist,
                Album = "Vision - Single",
                State = state,
                PositionSeconds = position,
                DurationSeconds = duration,
                CoverPng = Png,
            };
        }

        [Theory]
        [InlineData("org.mpris.MediaPlayer2.mosaicshell", true)]
        [InlineData("org.mpris.MediaPlayer2.mosaicshell.instance42", true)]
        [InlineData("org.mpris.MediaPlayer2.mosaicshellish", false)]
        [InlineData("org.mpris.MediaPlayer2.plasma-browser-integration", false)]
        public void The_host_recognises_its_own_player_so_it_never_reads_back_what_it_publishes(string busName, bool expected)
        {
            _ = MprisExportPolicy.IsOwnPlayer(busName).Should().Be(expected);
        }

        [Theory]
        [InlineData(BrowserPlaybackState.Playing, "Playing")]
        [InlineData(BrowserPlaybackState.Paused, "Paused")]
        [InlineData(BrowserPlaybackState.Stopped, "Stopped")]
        public void Playback_status_uses_the_MPRIS_names(BrowserPlaybackState state, string expected)
        {
            _ = MprisExportPolicy.PlaybackStatus(state).Should().Be(expected);
        }

        [Fact]
        public void A_track_id_is_a_valid_object_path_that_is_stable_per_track()
        {
            string id = MprisExportPolicy.TrackId("Vision", "Pìjus");

            _ = id.Should().MatchRegex("^/org/mosaicshell/track/t[0-9a-f]{16}$");
            _ = MprisExportPolicy.TrackId("Vision", "Pìjus").Should().Be(id);
            _ = MprisExportPolicy.TrackId("Rush", "Pìjus").Should().NotBe(id);
            _ = MprisExportPolicy.TrackId("Vision", "Someone else").Should().NotBe(id);
        }

        [Theory]
        [InlineData(201.4, 201_400_000L)]
        [InlineData(0.0, null)]
        [InlineData(-3.0, null)]
        public void Length_is_in_microseconds_and_left_out_when_unknown(double seconds, long? expected)
        {
            _ = MprisExportPolicy.LengthMicros(seconds).Should().Be(expected);
        }

        [Fact]
        public void The_cover_file_is_named_by_its_content_and_kind()
        {
            string png = MprisExportPolicy.ArtFileName(Png);

            _ = png.Should().MatchRegex("^cover-[0-9a-f]{16}\\.png$");
            _ = MprisExportPolicy.ArtFileName(Png).Should().Be(png, "the same cover keeps the same file, so clients cache it");
            _ = MprisExportPolicy.ArtFileName(Jpeg).Should().EndWith(".jpg");
        }

        [Fact]
        public void A_playing_report_becomes_the_published_state()
        {
            MprisExportedState? state = MprisExportPolicy.From(Snapshot(), "file:///cache/cover-x.png", "microsoft-edge");

            _ = state.Should().NotBeNull();
            _ = state!.PlaybackStatus.Should().Be("Playing");
            _ = state.Identity.Should().Be("YouTube Music");
            _ = state.DesktopEntry.Should().Be("microsoft-edge");
            _ = state.PositionMicros.Should().Be(12_500_000L);
            _ = state.Track.Title.Should().Be("Vision");
            _ = state.Track.Artists.Should().Equal("Pìjus");
            _ = state.Track.Album.Should().Be("Vision - Single");
            _ = state.Track.LengthMicros.Should().Be(201_400_000L);
            _ = state.Track.ArtUrl.Should().Be("file:///cache/cover-x.png");
            _ = state.Track.TrackId.Should().Be(MprisExportPolicy.TrackId("Vision", "Pìjus"));
        }

        [Fact]
        public void Nothing_is_published_without_a_report_a_title_or_while_stopped()
        {
            _ = MprisExportPolicy.From(null, null, null).Should().BeNull();
            _ = MprisExportPolicy.From(Snapshot(title: " "), null, null).Should().BeNull();
            _ = MprisExportPolicy.From(Snapshot(state: BrowserPlaybackState.Stopped), null, null).Should().BeNull();
        }

        [Fact]
        public void A_paused_report_is_still_published()
        {
            _ = MprisExportPolicy.From(Snapshot(state: BrowserPlaybackState.Paused), null, null)!.PlaybackStatus.Should().Be("Paused");
        }

        [Fact]
        public void The_artist_is_published_as_the_MPRIS_artist_list()
        {
            _ = MprisExportPolicy.From(Snapshot(artist: "1tbsp & Cherry Chola"), null, null)!.Track.Artists
                .Should().Equal("1tbsp & Cherry Chola");
            _ = MprisExportPolicy.From(Snapshot(artist: ""), null, null)!.Track.Artists.Should().BeEmpty();
        }

        [Fact]
        public void Only_the_properties_that_changed_are_announced_and_never_position()
        {
            MprisExportedState first = MprisExportPolicy.From(Snapshot(), "file:///a.png", null)!;
            MprisExportedState paused = MprisExportPolicy.From(Snapshot(state: BrowserPlaybackState.Paused, position: 40), "file:///a.png", null)!;
            MprisExportedState next = MprisExportPolicy.From(Snapshot(title: "Rush"), "file:///b.png", null)!;
            MprisExportedState later = MprisExportPolicy.From(Snapshot(position: 60), "file:///a.png", null)!;

            _ = MprisExportPolicy.ChangedPlayerProperties(null, first).Should().Contain(["PlaybackStatus", "Metadata"]);
            _ = MprisExportPolicy.ChangedPlayerProperties(first, paused).Should().Equal("PlaybackStatus");
            _ = MprisExportPolicy.ChangedPlayerProperties(first, next).Should().Equal("Metadata");
            _ = MprisExportPolicy.ChangedPlayerProperties(first, later).Should().BeEmpty("position is read, not announced; a jump is a Seeked signal");
        }

        [Theory]
        [InlineData(10_000_000L, 1.0, true, 11_000_000L, false)]
        [InlineData(10_000_000L, 1.0, true, 11_900_000L, false)]
        [InlineData(10_000_000L, 1.0, true, 60_000_000L, true)]
        [InlineData(10_000_000L, 1.0, true, 2_000_000L, true)]
        [InlineData(10_000_000L, 5.0, false, 10_000_000L, false)]
        [InlineData(10_000_000L, 5.0, false, 30_000_000L, true)]
        public void A_position_that_departs_from_where_playback_would_be_is_a_seek(
            long before, double elapsedSeconds, bool wasPlaying, long now, bool expected)
        {
            _ = MprisExportPolicy.IsSeek(before, elapsedSeconds, wasPlaying, now).Should().Be(expected);
        }

        [Theory]
        [InlineData(true, 2.0, 14_500_000L)]
        [InlineData(false, 2.0, 12_500_000L)]
        [InlineData(true, 500.0, 201_400_000L)]
        public void The_position_read_between_reports_runs_on_while_playing_and_stops_at_the_end(
            bool playing, double sinceReport, long expected)
        {
            MprisExportedState state = MprisExportPolicy.From(
                Snapshot(state: playing ? BrowserPlaybackState.Playing : BrowserPlaybackState.Paused), null, null)!;

            _ = MprisExportPolicy.PositionAt(state, sinceReport).Should().Be(expected);
        }

        [Fact]
        public void Timing_comes_from_the_platform_session_when_it_is_the_same_track()
        {
            // Grout reports no timing; the browser's own MPRIS player has it, as SMTC does on Windows.
            BrowserPlayerSnapshot report = Snapshot(position: 0, duration: 0);
            MediaSessionInfo session = new("vision ", "Pìjus", "microsoft-edge", true, PositionSeconds: 75.6, DurationSeconds: 157);

            MprisExportedState state = MprisExportPolicy.From(report, null, null, session)!;

            _ = state.PositionMicros.Should().Be(75_600_000L);
            _ = state.Track.LengthMicros.Should().Be(157_000_000L);
        }

        [Fact]
        public void A_session_for_another_track_lends_no_timing()
        {
            BrowserPlayerSnapshot report = Snapshot(position: 0, duration: 0);
            MediaSessionInfo session = new("Something else", "Band", "spotify", true, PositionSeconds: 75.6, DurationSeconds: 157);

            MprisExportedState state = MprisExportPolicy.From(report, null, null, session)!;

            _ = state.PositionMicros.Should().Be(0);
            _ = state.Track.LengthMicros.Should().BeNull();
        }

        [Fact]
        public void A_browser_session_claims_the_browser_process_so_its_own_player_is_hidden()
        {
            // Plasma hides a player whose process another player names in kde:pid, as it does for its own mirror.
            _ = MprisExportPolicy.From(Snapshot(), null, "edge", null, browserProcessId: 13951)!.Track.KdePid.Should().Be(13951);
            _ = MprisExportPolicy.From(Snapshot(), null, desktopEntry: null, null, browserProcessId: 13951)!.Track.KdePid
                .Should().BeNull("a process is only claimed while the session is the browser");
        }

        [Fact]
        public void A_site_suffixed_session_title_still_lends_its_timing()
        {
            MediaSessionInfo session = new("Vision | YouTube Music", null, "edge", true, PositionSeconds: 10, DurationSeconds: 157);

            _ = MprisExportPolicy.From(Snapshot(position: 0, duration: 0), null, null, session)!.Track.LengthMicros.Should().Be(157_000_000L);
        }

        [Fact]
        public void A_new_browser_process_is_announced_as_new_metadata()
        {
            MprisExportedState before = MprisExportPolicy.From(Snapshot(), null, "edge", null, browserProcessId: 1)!;
            MprisExportedState after = MprisExportPolicy.From(Snapshot(), null, "edge", null, browserProcessId: 2)!;

            _ = MprisExportPolicy.ChangedPlayerProperties(before, after).Should().Equal("Metadata");
        }

        [Theory]
        [InlineData("edge", "/opt/microsoft/msedge/msedge", null, "microsoft-edge")]
        [InlineData("edge", "/opt/microsoft/msedge-dev/msedge", null, "microsoft-edge-dev")]
        [InlineData("edge", "/app/extra/msedge", "com.microsoft.Edge", "com.microsoft.Edge")]
        [InlineData("microsoft-edge", "/opt/microsoft/msedge/msedge", null, "microsoft-edge")]
        [InlineData("chromium", "/usr/lib/chromium/chromium", null, "chromium-browser")]
        [InlineData("brave", null, null, "brave-browser")]
        [InlineData("vivaldi", null, null, "vivaldi-stable")]
        [InlineData("unknownbrowser", null, null, "unknownbrowser")]
        public void The_desktop_entry_is_the_browsers_installed_one_so_clients_show_its_icon(
            string appId, string? exePath, string? flatpakAppId, string expected)
        {
            // Edge's own MPRIS player names itself "edge", for which there is no .desktop file and so no icon.
            HashSet<string> installed =
            [
                "microsoft-edge", "microsoft-edge-dev", "com.microsoft.Edge", "chromium-browser", "brave-browser", "vivaldi-stable",
            ];

            _ = MprisExportPolicy.DesktopEntryFor(appId, exePath, flatpakAppId, installed.Contains).Should().Be(expected);
        }

        [Fact]
        public void A_channel_that_is_not_installed_falls_back_to_one_that_is()
        {
            HashSet<string> installed = ["microsoft-edge"];

            _ = MprisExportPolicy.DesktopEntryFor("edge", "/opt/microsoft/msedge-beta/msedge", null, installed.Contains)
                .Should().Be("microsoft-edge");
        }

        [Fact]
        public void Each_tab_has_its_own_bus_name_under_the_hosts()
        {
            string name = MprisExportPolicy.BusNameFor("c1_t42");

            _ = name.Should().Be("org.mpris.MediaPlayer2.mosaicshell.c1_t42");
            _ = MprisExportPolicy.IsOwnPlayer(name).Should().BeTrue();
        }

        [Theory]
        [InlineData("Vision", "edge", "Vision", true)]
        [InlineData("Vision | YouTube Music", "edge", "Vision", true)]
        [InlineData("A video", "edge", "Vision", false)]
        [InlineData("Vision", "spotify", "Vision", false)]
        [InlineData(null, null, "Vision", false)]
        public void Only_the_tab_the_browser_is_playing_through_its_own_player_takes_transport(
            string? sessionTitle, string? sessionApp, string tabTitle, bool expected)
        {
            // Grout cannot play, pause or seek; the browser's own MPRIS player can, but only for its active tab.
            MediaSessionInfo? session = sessionTitle is null ? null : new MediaSessionInfo(sessionTitle, null, sessionApp, true);

            _ = MprisExportPolicy.IsControllableTab(session, Snapshot(title: tabTitle)).Should().Be(expected);
        }

        [Fact]
        public void A_tab_that_gains_or_loses_control_announces_its_capabilities()
        {
            MprisExportedState controllable = MprisExportPolicy.From(Snapshot(), null, null, controllable: true)!;
            MprisExportedState not = MprisExportPolicy.From(Snapshot(), null, null, controllable: false)!;

            _ = MprisExportPolicy.ChangedPlayerProperties(controllable, not)
                .Should().BeEquivalentTo(["CanPlay", "CanPause", "CanGoNext", "CanGoPrevious", "CanSeek"]);
        }
    }
}
