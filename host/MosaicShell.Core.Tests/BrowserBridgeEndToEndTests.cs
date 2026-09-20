using System.Diagnostics;
using System.IO.Pipes;
using System.Net;
using System.Text;
using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.BrowserBridge;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The whole Host side of the native path: a stand-in for the relay speaks the wire protocol over a real
    /// pipe, and the composite media service, the object the flyout reads, ends up with what it needs.
    /// </summary>
    public sealed class BrowserBridgeEndToEndTests : IDisposable
    {
        private const string PwaApp = "music.youtube.com-5929F88E_v";
        private const string Hello = """{"type":"hello","protocol":1,"extensionVersion":"0.1.0","browser":"edge"}""";

        private static readonly byte[] Png = [.. new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 }, .. new byte[52]];

        private readonly string _pipeName = $"MosaicShell.Test.{Guid.NewGuid():N}";
        private readonly List<IDisposable> _disposables = [];
        private readonly FakeSmtc _smtc = new();

        public void Dispose()
        {
            foreach (IDisposable d in _disposables)
            {
                d.Dispose();
            }
        }

        private CompositeMediaSessionService Stack()
        {
            BrowserSessionHub hub = new(TimeProvider.System);
            StubHttpHandler http = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Png) }));
            BrowserArtworkFetcher fetcher = new(http);
            BrowserPipeServer server = new(_pipeName, hub, BrowserSessionHub.DefaultMaxConnections + 1);
            server.Start();
            _disposables.Add(server);
            BrowserMediaSource source = new(hub, fetcher, () => _smtc.Current?.Title, fetcher);
            CompositeMediaSessionService composite = new(_smtc, source);
            _disposables.Add(composite);
            return composite;
        }

        private async Task<NamedPipeClientStream> RelayAsync()
        {
            NamedPipeClientStream relay = new(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            _disposables.Add(relay);
            await relay.ConnectAsync(5000);
            await WriteAsync(relay, Hello);
            _ = await ReadAsync(relay);
            return relay;
        }

        private static async Task WriteAsync(Stream stream, string json)
        {
            await stream.WriteAsync(NativeMessagingFraming.Encode(Encoding.UTF8.GetBytes(json)));
        }

        private static async Task<string?> ReadAsync(Stream stream)
        {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            byte[]? frame = await NativeMessagingFraming.ReadFrameAsync(stream, 1024 * 1024, cts.Token);
            return frame is null ? null : Encoding.UTF8.GetString(frame);
        }

        private static async Task<bool> WaitUntilAsync(Func<bool> condition)
        {
            Stopwatch clock = Stopwatch.StartNew();
            while (clock.ElapsedMilliseconds < 5000)
            {
                if (condition())
                {
                    return true;
                }

                await Task.Delay(10);
            }

            return condition();
        }

        private const string YouTubeMusicSession =
            """
            {"type":"session","tabId":7,"windowId":1,"origin":"https://music.youtube.com",
             "title":"Humid","artist":"Moody Good","album":"Album",
             "artwork":[{"src":"https://i.ytimg.com/vi/x/sddefault.jpg","sizes":"320x180"}],
             "playbackState":"playing","audible":true,"rating":"liked","capabilities":["rating","dislike"]}
            """;

        [Fact]
        public async Task The_flyout_gets_title_artist_cover_rating_and_capabilities_from_the_extension()
        {
            CompositeMediaSessionService composite = Stack();
            NamedPipeClientStream relay = await RelayAsync();
            _smtc.Set(new MediaSessionInfo("Humid | YouTube Music", null, PwaApp, true));

            await WriteAsync(relay, YouTubeMusicSession);
            bool complete = await WaitUntilAsync(() => composite.Current?.Artist == "Moody Good" && composite.Current.ThumbnailPng is not null);

            _ = complete.Should().BeTrue("the report and the cover download both have to arrive");
            MediaSessionInfo session = composite.Current!;
            _ = session.Title.Should().Be("Humid", "the site suffix is not part of the title");
            _ = session.Artist.Should().Be("Moody Good");
            _ = session.AppId.Should().Be(PwaApp);
            _ = session.ThumbnailPng.Should().Equal(Png);
            _ = session.LikeRating.Should().Be(MediaLikePolicy.Liked);
            _ = session.Capabilities.Should().Be(BrowserMediaCapabilities.Rating | BrowserMediaCapabilities.Dislike);
        }

        [Fact]
        public async Task Like_dislike_and_clear_from_the_flyout_reach_the_extension_as_wanted_states()
        {
            CompositeMediaSessionService composite = Stack();
            NamedPipeClientStream relay = await RelayAsync();
            _smtc.Set(new MediaSessionInfo("Humid | YouTube Music", null, PwaApp, true));
            await WriteAsync(relay, YouTubeMusicSession);
            _ = await WaitUntilAsync(() => composite.Current?.Artist == "Moody Good");

            await composite.ToggleLikeAsync(true);
            string? like = await ReadAsync(relay);
            await composite.ToggleDislikeAsync(true);
            string? dislike = await ReadAsync(relay);
            await composite.ToggleLikeAsync(false);
            string? clear = await ReadAsync(relay);

            _ = like.Should().Be("""{"type":"command","tabId":7,"action":"like"}""");
            _ = dislike.Should().Be("""{"type":"command","tabId":7,"action":"dislike"}""");
            _ = clear.Should().Be("""{"type":"command","tabId":7,"action":"clear"}""");
        }

        [Fact]
        public async Task A_browser_that_goes_away_leaves_the_flyout_with_what_Windows_knows()
        {
            CompositeMediaSessionService composite = Stack();
            NamedPipeClientStream relay = await RelayAsync();
            _smtc.Set(new MediaSessionInfo("Humid | YouTube Music", null, PwaApp, true));
            await WriteAsync(relay, YouTubeMusicSession);
            _ = await WaitUntilAsync(() => composite.Current?.Artist == "Moody Good");

            await relay.DisposeAsync();

            _ = (await WaitUntilAsync(() => composite.Current?.Artist is null)).Should().BeTrue();
            _ = composite.Current!.Title.Should().Be("Humid");
        }

        [Fact]
        public async Task A_track_change_in_the_tab_replaces_the_previous_track()
        {
            CompositeMediaSessionService composite = Stack();
            NamedPipeClientStream relay = await RelayAsync();
            _smtc.Set(new MediaSessionInfo("Humid | YouTube Music", null, PwaApp, true));
            await WriteAsync(relay, YouTubeMusicSession);
            _ = await WaitUntilAsync(() => composite.Current?.Artist == "Moody Good");

            _smtc.Set(new MediaSessionInfo("Walk Away | YouTube Music", null, PwaApp, true));
            await WriteAsync(relay, YouTubeMusicSession
                .Replace("Humid", "Walk Away", StringComparison.Ordinal)
                .Replace("Moody Good", "Sony Twain", StringComparison.Ordinal)
                .Replace("\"rating\":\"liked\"", "\"rating\":\"none\"", StringComparison.Ordinal));

            _ = (await WaitUntilAsync(() => composite.Current?.Artist == "Sony Twain")).Should().BeTrue();
            _ = composite.Current!.Title.Should().Be("Walk Away");
            _ = composite.Current.LikeRating.Should().Be(MediaLikePolicy.Unrated);
        }
    }
}
