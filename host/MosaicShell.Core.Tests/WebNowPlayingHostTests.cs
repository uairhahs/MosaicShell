using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using FluentAssertions;
using MosaicShell.Core.Services.WebNowPlaying;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Drives <see cref="WebNowPlayingReduxHost"/> like the browser extension (WNPLIB rev 3),
    /// without Chrome - isolates album-art protocol handling.
    /// </summary>
    public class WebNowPlayingHostTests
    {
        // 1×1 PNG (shared with TesseraAlbumArtRegressionTests)
        internal static readonly byte[] TinyPng =
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
            0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, 0x54, 0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0x00,
            0x00, 0x00, 0x03, 0x00, 0x01, 0x00, 0x05, 0xFE, 0x02, 0xFE, 0xDC, 0xCC, 0x59, 0xE7, 0x00, 0x00,
            0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
        ];

        [Fact]
        public async Task Host_sends_revision3_hello_on_connect()
        {
            await using WnpTestSession session = await WnpTestSession.StartAsync();
            string hello = await session.ReceiveTextAsync();
            _ = hello.Should().StartWith("ADAPTER_VERSION ");
            _ = hello.Should().Contain("WNPLIB_REVISION 3");
        }

        [Fact]
        public async Task Host_applies_binary_cover_after_player_added()
        {
            await using WnpTestSession session = await WnpTestSession.StartAsync();
            _ = await session.ReceiveTextAsync(); // hello

            Task changed = session.WaitForChangeAsync();
            await session.SendTextAsync(BuildPlayerAdded(id: 7, title: "Track A", coverSrc: ""));
            await changed;

            _ = session.Host.Active.Should().NotBeNull();
            _ = session.Host.Active.Title.Should().Be("Track A");
            _ = session.Host.Active.CoverPng.Should().BeNull("no cover yet");

            changed = session.WaitForChangeAsync();
            await session.SendBinaryCoverAsync(playerId: 7, TinyPng);
            await changed;

            _ = session.Host.Active.CoverPng.Should().NotBeNull();
            _ = session.Host.Active.CoverPng.Length.Should().Be(TinyPng.Length);
            _ = session.Host.Active.CoverPng.Should().Equal(TinyPng);
        }

        [Fact]
        public async Task Host_keeps_pending_binary_cover_if_it_arrives_before_player()
        {
            await using WnpTestSession session = await WnpTestSession.StartAsync();
            _ = await session.ReceiveTextAsync();

            await session.SendBinaryCoverAsync(playerId: 3, TinyPng);
            await Task.Delay(50);
            _ = session.Host.Active.Should().BeNull("cover alone does not create a player");

            Task changed = session.WaitForChangeAsync(h => h.Active?.CoverPng is { Length: > 32 });
            await session.SendTextAsync(BuildPlayerAdded(id: 3, title: "Late Player", coverSrc: ""));
            await changed;

            _ = session.Host.Active.Title.Should().Be("Late Player");
            _ = session.Host.Active.CoverPng.Should().Equal(TinyPng);
        }

        [Fact]
        public async Task Host_fetches_cover_src_http_when_binary_missing()
        {
            using StaticPngServer staticFiles = new(TinyPng);
            await using WnpTestSession session = await WnpTestSession.StartAsync();
            _ = await session.ReceiveTextAsync();

            Task changed = session.WaitForChangeAsync(predicate: h =>
                h.Active?.CoverPng is { Length: > 32 });
            await session.SendTextAsync(BuildPlayerAdded(
                id: 1,
                title: "From URL",
                coverSrc: staticFiles.Url));
            await changed;

            _ = session.Host.Active!.Title.Should().Be("From URL");
            _ = session.Host.Active.CoverPng.Should().Equal(TinyPng);
        }

        [Fact]
        public async Task Host_updates_position_without_dropping_cover()
        {
            await using WnpTestSession session = await WnpTestSession.StartAsync();
            _ = await session.ReceiveTextAsync();

            Task changed = session.WaitForChangeAsync();
            await session.SendTextAsync(BuildPlayerAdded(id: 2, title: "Scrub", coverSrc: ""));
            await changed;
            changed = session.WaitForChangeAsync();
            await session.SendBinaryCoverAsync(2, TinyPng);
            await changed;

            // Partial update: empty name/title/artist/album/cover (5), then state|pos|dur
            changed = session.WaitForChangeAsync();
            await session.SendTextAsync("1 2 2||||||0|42|180|");
            await changed;

            _ = session.Host.Active!.CoverPng.Should().Equal(TinyPng);
            _ = session.Host.Active.PositionSeconds.Should().Be(42);
            _ = session.Host.Active.DurationSeconds.Should().Be(180);
        }

        [Fact]
        public void MakePlayerData_field_layout_matches_extension()
        {
            // Mirrors extension makePlayerData: id|name|title|artist|album|cover|state|pos|dur|vol|..
            string blob = "9|YouTube Music|Hello|World|Alb|https://cdn.example/cover.png|0|10|200|80|0|1|0|0|1|1|1|1|1|1|1|1|1|1|100|200|300|";
            List<string> f = WebNowPlayingReduxHost.SplitFields(blob);
            _ = f[0].Should().Be("9");
            _ = f[1].Should().Be("YouTube Music");
            _ = f[2].Should().Be("Hello");
            _ = f[3].Should().Be("World");
            _ = f[4].Should().Be("Alb");
            _ = f[5].Should().Be("https://cdn.example/cover.png");
            _ = f[6].Should().Be("0");
            _ = f[7].Should().Be("10");
            _ = f[8].Should().Be("200");
            _ = f[9].Should().Be("80");
        }

        [Fact]
        public void JsUint32_truncates_like_dataview_setUint32()
        {
            // Real extension id from live probe
            const long id = 1588068110496L;
            uint trunc = WebNowPlayingReduxHost.ToJsUint32(id);
            _ = trunc.Should().Be(unchecked((uint)id));

            // Binary frame uses only 4 LE bytes
            Span<byte> buf = stackalloc byte[4];
            _ = BitConverter.TryWriteBytes(buf, trunc);
            _ = BitConverter.ToUInt32(buf).Should().Be(trunc);
        }

        [Fact]
        public async Task Host_attaches_cover_when_player_id_exceeds_int32()
        {
            // Reproduces live YTM: id=1588068110496, binary uses setUint32 truncation
            const long bigId = 1588068110496L;
            await using WnpTestSession session = await WnpTestSession.StartAsync();
            _ = await session.ReceiveTextAsync();

            Task changed = session.WaitForChangeAsync(h => h.Active?.CoverPng is { Length: > 32 });
            // Binary first (extension order), truncated uint32 id
            await session.SendBinaryCoverAsync(WebNowPlayingReduxHost.ToJsUint32(bigId), TinyPng);
            await session.SendTextAsync(BuildPlayerAdded(bigId, "ALL THAT I WANT", coverSrc: ""));
            await changed;

            _ = session.Host.Active!.Title.Should().Be("ALL THAT I WANT");
            _ = session.Host.Active.CoverPng.Should().Equal(TinyPng);
        }

        [Fact]
        public async Task Host_resets_stale_rating_when_title_changes_without_rating_field()
        {
            await using WnpTestSession session = await WnpTestSession.StartAsync();
            _ = await session.ReceiveTextAsync();

            await session.SendTextAsync(BuildPlayerAdded(id: 5, title: "Liked Song", coverSrc: "", rating: 5));
            await session.WaitForChangeAsync();
            _ = session.Host.Active!.Rating.Should().Be(5);

            await session.SendTextAsync("1 5 ||New Song||||0|42|180|");
            await session.WaitForChangeAsync();
            _ = session.Host.Active!.Title.Should().Be("New Song");
            _ = session.Host.Active!.Rating.Should().Be(0);
        }

        [Fact]
        public async Task Host_set_like_sends_rating_5_for_unrated_track()
        {
            await using WnpTestSession session = await WnpTestSession.StartAsync();
            _ = await session.ReceiveTextAsync();

            await session.SendTextAsync(BuildPlayerAdded(id: 8, title: "Fresh", coverSrc: "", rating: 0));
            await session.WaitForChangeAsync();

            Task<string> evt = session.WaitForOutboundEventAsync();
            await session.Host.TrySetLikeAsync(wantLiked: true);
            string msg = await evt;
            _ = msg.Should().MatchRegex(@"^8 \d+ 5 1$");
        }

        [Fact]
        public async Task Host_set_like_sends_rating_5_when_stale_host_rating_but_ui_wants_like()
        {
            await using WnpTestSession session = await WnpTestSession.StartAsync();
            _ = await session.ReceiveTextAsync();

            await session.SendTextAsync(BuildPlayerAdded(id: 10, title: "Stale", coverSrc: "", rating: 5));
            await session.WaitForChangeAsync();

            Task<string> evt = session.WaitForOutboundEventAsync();
            await session.Host.TrySetLikeAsync(wantLiked: true);
            string msg = await evt;
            _ = msg.Should().MatchRegex(@"^10 \d+ 5 1$");
        }

        [Fact]
        public async Task Host_set_unlike_sends_ytm_toggle_rating_when_host_knows_liked()
        {
            await using WnpTestSession session = await WnpTestSession.StartAsync();
            _ = await session.ReceiveTextAsync();

            await session.SendTextAsync(BuildPlayerAdded(id: 9, title: "Liked", coverSrc: "", rating: 5));
            await session.WaitForChangeAsync();

            Task<string> evt = session.WaitForOutboundEventAsync();
            await session.Host.TrySetLikeAsync(wantLiked: false);
            string msg = await evt;
            _ = msg.Should().MatchRegex(@"^9 \d+ 5 1$");
        }

        [Fact]
        public async Task Host_set_unlike_is_noop_when_host_rating_unrated()
        {
            await using WnpTestSession session = await WnpTestSession.StartAsync();
            _ = await session.ReceiveTextAsync();

            await session.SendTextAsync(BuildPlayerAdded(id: 11, title: "Fresh", coverSrc: "", rating: 0));
            await session.WaitForChangeAsync();

            await session.Host.TrySetLikeAsync(wantLiked: false);
            // No outbound event, must not send SET_RATING 0 (YTM thumbs-down).
        }

        private static string BuildPlayerAdded(long id, string title, string coverSrc, int rating = 0)
        {
            static string Esc(string s)
            {
                return string.IsNullOrEmpty(s) ? "\u0001" : s.Replace("|", "\\|");
            }

            string data =
                $"{id}|YouTube Music|{Esc(title)}|Artist|\u0001|{Esc(coverSrc)}|0|5|100|100|{rating}|1|0|0|1|1|1|1|1|1|1|1|1|1|1|2|3|";
            return $"0 {id} {data}";
        }

        private sealed class WnpTestSession : IAsyncDisposable
        {
            public WebNowPlayingReduxHost Host { get; }
            private readonly ClientWebSocket _ws = new();
            private readonly CancellationTokenSource _cts = new();

            private WnpTestSession(WebNowPlayingReduxHost host)
            {
                Host = host;
            }

            public static async Task<WnpTestSession> StartAsync()
            {
                int port = GetFreePort();
                WebNowPlayingReduxHost host = new(port);
                host.Start();
                await host.WaitUntilListeningAsync();

                WnpTestSession session = new(host);
                await session._ws.ConnectAsync(new Uri($"ws://127.0.0.1:{port}/"), session._cts.Token);
                return session;
            }

            public Task<string> ReceiveTextAsync()
            {
                return ReceiveTextCoreAsync();
            }

            public async Task SendTextAsync(string text)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, _cts.Token);
            }

            public async Task SendBinaryCoverAsync(uint playerIdTrunc, byte[] png)
            {
                byte[] payload = new byte[4 + png.Length];
                _ = BitConverter.TryWriteBytes(payload.AsSpan(0, 4), playerIdTrunc);
                png.CopyTo(payload, 4);
                await _ws.SendAsync(payload, WebSocketMessageType.Binary, true, _cts.Token);
            }

            public Task SendBinaryCoverAsync(int playerId, byte[] png)
            {
                return SendBinaryCoverAsync(unchecked((uint)playerId), png);
            }

            public Task WaitForChangeAsync(Func<WebNowPlayingReduxHost, bool>? predicate = null)
            {
                TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
                void Handler(object? _, EventArgs __)
                {
                    if (predicate is null || predicate(Host))
                    {
                        Host.Changed -= Handler;
                        _ = tcs.TrySetResult();
                    }
                }

                Host.Changed += Handler;
                if (predicate is not null && predicate(Host))
                {
                    Host.Changed -= Handler;
                    _ = tcs.TrySetResult();
                }

                return tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            }

            public Task<string> WaitForOutboundEventAsync()
            {
                return ReceiveTextCoreAsync();
            }

            private async Task<string> ReceiveTextCoreAsync()
            {
                byte[] buffer = new byte[4096];
                using MemoryStream ms = new();
                WebSocketReceiveResult result;
                do
                {
                    result = await _ws.ReceiveAsync(buffer, _cts.Token).WaitAsync(TimeSpan.FromSeconds(5));
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                _ = result.MessageType.Should().Be(WebSocketMessageType.Text);
                return Encoding.UTF8.GetString(ms.ToArray());
            }

            public async ValueTask DisposeAsync()
            {
                try
                {
                    if (_ws.State == WebSocketState.Open)
                    {
                        await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
                    }
                }
                catch { /* ignore */ }
                _ws.Dispose();
                _cts.Cancel();
                _cts.Dispose();
                Host.Dispose();
            }

            private static int GetFreePort()
            {
                TcpListener listener = new(IPAddress.Loopback, 0);
                listener.Start();
                int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                listener.Stop();
                return port;
            }
        }

        private sealed class StaticPngServer : IDisposable
        {
            private readonly HttpListener _listener = new();
            private readonly byte[] _png;
            private readonly CancellationTokenSource _cts = new();

            public string Url { get; }

            public StaticPngServer(byte[] png)
            {
                _png = png;
                int port = GetFreePort();
                Url = $"http://127.0.0.1:{port}/cover.png";
                _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                _listener.Start();
                _ = Task.Run(LoopAsync);
            }

            private async Task LoopAsync()
            {
                while (!_cts.IsCancellationRequested && _listener.IsListening)
                {
                    try
                    {
                        HttpListenerContext ctx = await _listener.GetContextAsync().WaitAsync(_cts.Token);
                        ctx.Response.ContentType = "image/png";
                        ctx.Response.ContentLength64 = _png.Length;
                        await ctx.Response.OutputStream.WriteAsync(_png);
                        ctx.Response.Close();
                    }
                    catch { break; }
                }
            }

            public void Dispose()
            {
                _cts.Cancel();
                try { _listener.Stop(); } catch { /* ignore */ }
                _listener.Close();
                _cts.Dispose();
            }

            private static int GetFreePort()
            {
                TcpListener listener = new(IPAddress.Loopback, 0);
                listener.Start();
                int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                listener.Stop();
                return port;
            }
        }
    }
}
