using System.Net;
using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.BrowserBridge;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The native browser source presents what the extension reports as the player the flyout shows: names, cover
    /// (fetched without blocking), rating, capabilities and the commands it may be asked to run.
    /// </summary>
    public sealed class BrowserMediaSourceTests : IDisposable
    {
        private const string Hello = """{"type":"hello","protocol":1,"extensionVersion":"0.1.0","browser":"edge"}""";
        private const string ArtUrl = "https://i.ytimg.com/vi/x/sddefault.jpg";

        private static readonly byte[] Png = [.. new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 }, .. new byte[52]];

        private readonly ManualTimeProvider _clock = new();
        private readonly List<IDisposable> _disposables = [];

        public void Dispose()
        {
            foreach (IDisposable d in _disposables)
            {
                d.Dispose();
            }
        }

        private static string Session(string title, string extra = "", int tab = 7, string origin = "https://music.youtube.com", string state = "playing")
        {
            return $$"""{"type":"session","tabId":{{tab}},"windowId":1,"origin":"{{origin}}","title":"{{title}}","playbackState":"{{state}}","audible":true{{extra}}}""";
        }

        private (BrowserMediaSource Source, BrowserSessionHub Hub, FakeBrowserConnection Connection, int Id, StubHttpHandler Http) Build(string? smtcTitle = null)
        {
            BrowserSessionHub hub = new(_clock);
            StubHttpHandler http = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Png) }));
            BrowserArtworkFetcher fetcher = new(http, _clock);
            BrowserMediaSource source = new(hub, fetcher, () => smtcTitle);
            _disposables.Add(source);
            _disposables.Add(fetcher);
            FakeBrowserConnection connection = new();
            int id = hub.Open(connection);
            hub.Receive(id, System.Text.Encoding.UTF8.GetBytes(Hello));
            return (source, hub, connection, id, http);
        }

        private static void Send(BrowserSessionHub hub, int id, string json)
        {
            hub.Receive(id, System.Text.Encoding.UTF8.GetBytes(json));
        }

        [Fact]
        public void Nothing_is_playing_until_a_tab_reports()
        {
            (BrowserMediaSource source, _, _, _, _) = Build();

            _ = source.Active.Should().BeNull();
        }

        [Fact]
        public void A_reported_tab_is_presented_as_a_player()
        {
            (BrowserMediaSource source, BrowserSessionHub hub, _, int id, _) = Build();
            Send(hub, id, Session("Humid", ",\"artist\":\"Moody Good\",\"album\":\"Album\""));

            BrowserPlayerSnapshot player = source.Active!;

            _ = player.Name.Should().Be("YouTube Music");
            _ = player.Title.Should().Be("Humid");
            _ = player.Artist.Should().Be("Moody Good");
            _ = player.Album.Should().Be("Album");
            _ = player.State.Should().Be(BrowserPlaybackState.Playing);
            _ = player.PositionSeconds.Should().Be(0, "Windows already supplies the timeline");
            _ = MediaLikePolicy.IsYouTubeMusic(player.Name).Should().BeTrue();
        }

        [Theory]
        [InlineData("playing", BrowserPlaybackState.Playing)]
        [InlineData("paused", BrowserPlaybackState.Paused)]
        [InlineData("none", BrowserPlaybackState.Stopped)]
        public void The_playback_state_is_carried_over(string wire, BrowserPlaybackState expected)
        {
            (BrowserMediaSource source, BrowserSessionHub hub, _, int id, _) = Build();
            Send(hub, id, Session("Song", state: wire));

            _ = source.Active!.State.Should().Be(expected);
        }

        [Fact]
        public void Rating_and_capabilities_are_carried_over()
        {
            (BrowserMediaSource source, BrowserSessionHub hub, _, int id, _) = Build();
            Send(hub, id, Session("Song", ",\"rating\":\"disliked\",\"capabilities\":[\"rating\",\"dislike\"]"));

            BrowserPlayerSnapshot player = source.Active!;

            _ = player.Rating.Should().Be(BrowserRating.Disliked);
            _ = player.Capabilities.Should().Be(BrowserMediaCapabilities.Rating | BrowserMediaCapabilities.Dislike);
        }

        [Fact]
        public void The_tab_Windows_agrees_with_is_the_one_presented()
        {
            (BrowserMediaSource source, BrowserSessionHub hub, _, int id, _) = Build(smtcTitle: "First song | YouTube Music");
            Send(hub, id, Session("First song", tab: 1));
            _clock.Advance(TimeSpan.FromSeconds(5));
            Send(hub, id, Session("Second song", tab: 2));

            _ = source.Active!.Title.Should().Be("First song");
        }

        [Fact]
        public async Task The_cover_is_fetched_in_the_background_and_the_flyout_is_told_when_it_arrives()
        {
            (BrowserMediaSource source, BrowserSessionHub hub, _, int id, _) = Build();
            Send(hub, id, Session("Song", $",\"artwork\":[{{\"src\":\"{ArtUrl}\",\"sizes\":\"320x180\"}}]"));
            TaskCompletionSource arrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
            source.Changed += (_, _) => arrived.TrySetResult();

            BrowserPlayerSnapshot before = source.Active!;
            _ = await Task.WhenAny(arrived.Task, Task.Delay(5000));

            _ = before.CoverPng.Should().BeNull("reading the player must not wait on the network");
            _ = arrived.Task.IsCompletedSuccessfully.Should().BeTrue();
            _ = source.Active!.CoverPng.Should().Equal(Png);
        }

        [Fact]
        public async Task Artwork_the_Host_may_not_fetch_is_never_requested()
        {
            (BrowserMediaSource source, BrowserSessionHub hub, _, int id, StubHttpHandler http) = Build();
            Send(hub, id, Session("Song", ",\"artwork\":[{\"src\":\"https://192.168.1.1/cover.png\"}]"));

            _ = source.Active!.CoverPng.Should().BeNull();
            await Task.Delay(100);

            _ = http.Calls.Should().Be(0);
        }

        [Theory]
        [InlineData(true, "like")]
        [InlineData(false, "clear")]
        public async Task Liking_and_unliking_ask_the_extension_for_that_state(bool liked, string action)
        {
            (BrowserMediaSource source, BrowserSessionHub hub, FakeBrowserConnection connection, int id, _) = Build();
            Send(hub, id, Session("Song"));
            connection.Sent.Clear();

            await source.SetLikedAsync(liked);

            _ = connection.Sent.Should().Equal($$"""{"type":"command","tabId":7,"action":"{{action}}"}""");
        }

        [Theory]
        [InlineData(true, "dislike")]
        [InlineData(false, "clear")]
        public async Task Disliking_and_undisliking_ask_the_extension_for_that_state(bool disliked, string action)
        {
            (BrowserMediaSource source, BrowserSessionHub hub, FakeBrowserConnection connection, int id, _) = Build();
            Send(hub, id, Session("Song"));
            connection.Sent.Clear();

            await source.SetDislikedAsync(disliked);

            _ = connection.Sent.Should().Equal($$"""{"type":"command","tabId":7,"action":"{{action}}"}""");
        }

        [Fact]
        public async Task A_command_with_no_player_does_nothing()
        {
            (BrowserMediaSource source, _, FakeBrowserConnection connection, _, _) = Build();
            connection.Sent.Clear();

            await source.SetLikedAsync(true);
            await source.SetDislikedAsync(true);

            _ = connection.Sent.Should().BeEmpty();
        }

        [Fact]
        public async Task Shuffle_and_repeat_are_not_offered_and_do_nothing()
        {
            (BrowserMediaSource source, BrowserSessionHub hub, FakeBrowserConnection connection, int id, _) = Build();
            Send(hub, id, Session("Song"));
            connection.Sent.Clear();

            await source.ToggleShuffleAsync();
            await source.ToggleRepeatAsync();

            _ = connection.Sent.Should().BeEmpty();
            _ = source.Active!.Capabilities.Should().NotHaveFlag(BrowserMediaCapabilities.Shuffle);
        }

        [Fact]
        public void A_change_in_the_hub_is_a_change_in_the_source()
        {
            (BrowserMediaSource source, BrowserSessionHub hub, _, int id, _) = Build();
            int changes = 0;
            source.Changed += (_, _) => changes++;

            Send(hub, id, Session("Song"));

            _ = changes.Should().Be(1);
        }

        [Fact]
        public void A_disposed_source_stops_reporting_changes()
        {
            (BrowserMediaSource source, BrowserSessionHub hub, _, int id, _) = Build();
            int changes = 0;
            source.Changed += (_, _) => changes++;
            source.Dispose();

            Send(hub, id, Session("Song"));

            _ = changes.Should().Be(0);
        }

        [Theory]
        [InlineData("https://music.youtube.com", "YouTube Music")]
        [InlineData("https://www.youtube.com", "YouTube")]
        [InlineData("https://youtube.com", "YouTube")]
        [InlineData("https://m.youtube.com", "YouTube")]
        [InlineData("https://open.spotify.com", "Spotify")]
        [InlineData("https://soundcloud.com", "SoundCloud")]
        [InlineData("https://www.soundcloud.com", "SoundCloud")]
        [InlineData("https://someone.bandcamp.com", "Bandcamp")]
        [InlineData("https://www.example.org", "example.org")]
        [InlineData("https://player.example.org:8443", "player.example.org")]
        public void A_site_is_named_from_its_origin(string origin, string expected)
        {
            _ = BrowserSiteNames.FromOrigin(origin).Should().Be(expected);
        }
    }
}
