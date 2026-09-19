using System.Net;
using FluentAssertions;
using MosaicShell.Core.Services.BrowserBridge;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The Host fetches artwork on behalf of a web page it does not trust: bounded in size and time, only real
    /// images, never from a private address, never carrying credentials, and never in a loop after a failure.
    /// </summary>
    public class BrowserArtworkFetcherTests
    {
        private const string Url = "https://i.ytimg.com/vi/x/sddefault.jpg";

        private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0, 1, 2, 3, 4];

        private readonly ManualTimeProvider _clock = new();

        private static HttpResponseMessage Ok(byte[] body, string contentType = "image/png")
        {
            HttpResponseMessage response = new(HttpStatusCode.OK) { Content = new ByteArrayContent(body) };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            return response;
        }

        private BrowserArtworkFetcher Fetcher(FakeHandler handler, TimeSpan? timeout = null)
        {
            return new BrowserArtworkFetcher(handler, _clock, timeout);
        }

        [Fact]
        public async Task An_image_is_fetched_and_then_served_from_the_cache()
        {
            FakeHandler handler = new((_, _) => Task.FromResult(Ok(Png)));
            using BrowserArtworkFetcher fetcher = Fetcher(handler);

            await fetcher.RequestAsync(Url);
            await fetcher.RequestAsync(Url);

            _ = fetcher.TryGet(Url).Should().Equal(Png);
            _ = handler.Calls.Should().Be(1);
        }

        [Fact]
        public void Nothing_is_returned_for_a_url_that_was_never_requested()
        {
            using BrowserArtworkFetcher fetcher = Fetcher(new FakeHandler((_, _) => Task.FromResult(Ok(Png))));

            _ = fetcher.TryGet(Url).Should().BeNull();
        }

        [Fact]
        public async Task Fetched_is_raised_once_for_a_success()
        {
            using BrowserArtworkFetcher fetcher = Fetcher(new FakeHandler((_, _) => Task.FromResult(Ok(Png))));
            int raised = 0;
            fetcher.Fetched += (_, _) => raised++;

            await fetcher.RequestAsync(Url);
            await fetcher.RequestAsync(Url);

            _ = raised.Should().Be(1);
        }

        [Fact]
        public async Task Bytes_that_are_not_an_image_are_refused_whatever_the_content_type_says()
        {
            FakeHandler handler = new((_, _) => Task.FromResult(Ok("<html>sign in</html> padding"u8.ToArray(), "image/png")));
            using BrowserArtworkFetcher fetcher = Fetcher(handler);
            int raised = 0;
            fetcher.Fetched += (_, _) => raised++;

            await fetcher.RequestAsync(Url);

            _ = fetcher.TryGet(Url).Should().BeNull();
            _ = raised.Should().Be(0);
        }

        [Fact]
        public async Task A_declared_length_over_the_cap_is_refused_without_reading_the_body()
        {
            FakeHandler handler = new((_, _) =>
            {
                HttpResponseMessage response = Ok(Png);
                response.Content = new OversizedContent(BrowserArtworkFetcher.MaxBytes + 1, declared: true);
                return Task.FromResult(response);
            });
            using BrowserArtworkFetcher fetcher = Fetcher(handler);

            await fetcher.RequestAsync(Url);

            _ = fetcher.TryGet(Url).Should().BeNull();
        }

        [Fact]
        public async Task A_body_over_the_cap_is_refused_even_when_no_length_is_declared()
        {
            FakeHandler handler = new((_, _) =>
            {
                HttpResponseMessage response = Ok(Png);
                response.Content = new OversizedContent(BrowserArtworkFetcher.MaxBytes + 1, declared: false);
                return Task.FromResult(response);
            });
            using BrowserArtworkFetcher fetcher = Fetcher(handler);

            await fetcher.RequestAsync(Url);

            _ = fetcher.TryGet(Url).Should().BeNull();
        }

        [Fact]
        public async Task A_body_exactly_at_the_cap_is_accepted()
        {
            byte[] body = new byte[BrowserArtworkFetcher.MaxBytes];
            Png.CopyTo(body, 0);
            using BrowserArtworkFetcher fetcher = Fetcher(new FakeHandler((_, _) => Task.FromResult(Ok(body))));

            await fetcher.RequestAsync(Url);

            _ = fetcher.TryGet(Url).Should().HaveCount(BrowserArtworkFetcher.MaxBytes);
        }

        [Theory]
        [InlineData("http://i.ytimg.com/x.jpg")]
        [InlineData("https://127.0.0.1/x.jpg")]
        [InlineData("https://192.168.1.1/x.jpg")]
        [InlineData("https://localhost/x.jpg")]
        [InlineData("blob:https://a.example/uuid")]
        [InlineData("not a url")]
        public async Task An_unfetchable_url_is_refused_before_any_request_is_made(string url)
        {
            FakeHandler handler = new((_, _) => Task.FromResult(Ok(Png)));
            using BrowserArtworkFetcher fetcher = Fetcher(handler);

            await fetcher.RequestAsync(url);

            _ = handler.Calls.Should().Be(0);
            _ = fetcher.TryGet(url).Should().BeNull();
        }

        [Fact]
        public async Task A_server_error_yields_nothing_and_throws_nothing()
        {
            using BrowserArtworkFetcher fetcher = Fetcher(new FakeHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound))));

            await fetcher.RequestAsync(Url);

            _ = fetcher.TryGet(Url).Should().BeNull();
        }

        [Fact]
        public async Task A_network_failure_yields_nothing_and_throws_nothing()
        {
            using BrowserArtworkFetcher fetcher = Fetcher(new FakeHandler((_, _) => throw new HttpRequestException("connection refused")));

            await fetcher.RequestAsync(Url);

            _ = fetcher.TryGet(Url).Should().BeNull();
        }

        [Fact]
        public async Task A_server_that_never_answers_is_given_up_on()
        {
            FakeHandler handler = new(async (_, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return Ok(Png);
            });
            using BrowserArtworkFetcher fetcher = Fetcher(handler, timeout: TimeSpan.FromMilliseconds(100));

            Task work = fetcher.RequestAsync(Url);

            _ = (await Task.WhenAny(work, Task.Delay(5000))).Should().BeSameAs(work);
            _ = fetcher.TryGet(Url).Should().BeNull();
        }

        [Fact]
        public async Task A_failure_is_not_retried_until_the_backoff_has_passed()
        {
            int calls = 0;
            FakeHandler handler = new((_, _) =>
            {
                calls++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            });
            using BrowserArtworkFetcher fetcher = Fetcher(handler);

            await fetcher.RequestAsync(Url);
            await fetcher.RequestAsync(Url);
            _ = calls.Should().Be(1, "a failing image must not be requested on every flyout refresh");

            _clock.Advance(BrowserArtworkFetcher.FailureBackoff + TimeSpan.FromSeconds(1));
            await fetcher.RequestAsync(Url);

            _ = calls.Should().Be(2);
        }

        [Fact]
        public async Task Two_requests_for_the_same_url_in_flight_share_one_download()
        {
            TaskCompletionSource<HttpResponseMessage> gate = new();
            FakeHandler handler = new((_, _) => gate.Task);
            using BrowserArtworkFetcher fetcher = Fetcher(handler);

            Task first = fetcher.RequestAsync(Url);
            Task second = fetcher.RequestAsync(Url);
            gate.SetResult(Ok(Png));
            await Task.WhenAll(first, second);

            _ = handler.Calls.Should().Be(1);
        }

        [Fact]
        public async Task The_oldest_image_is_evicted_when_the_cache_is_full()
        {
            using BrowserArtworkFetcher fetcher = Fetcher(new FakeHandler((_, _) => Task.FromResult(Ok(Png))));
            for (int i = 0; i <= BrowserArtworkFetcher.MaxCachedEntries; i++)
            {
                await fetcher.RequestAsync($"https://a.example/{i}.png");
            }

            _ = fetcher.TryGet("https://a.example/0.png").Should().BeNull("it was the least recently used");
            _ = fetcher.TryGet($"https://a.example/{BrowserArtworkFetcher.MaxCachedEntries}.png").Should().NotBeNull();
        }

        [Fact]
        public async Task Reading_an_image_keeps_it_from_being_evicted()
        {
            using BrowserArtworkFetcher fetcher = Fetcher(new FakeHandler((_, _) => Task.FromResult(Ok(Png))));
            for (int i = 0; i < BrowserArtworkFetcher.MaxCachedEntries; i++)
            {
                await fetcher.RequestAsync($"https://a.example/{i}.png");
            }

            _ = fetcher.TryGet("https://a.example/0.png");
            await fetcher.RequestAsync("https://a.example/new.png");

            _ = fetcher.TryGet("https://a.example/0.png").Should().NotBeNull();
            _ = fetcher.TryGet("https://a.example/1.png").Should().BeNull();
        }

        [Fact]
        public async Task The_request_carries_no_credentials_referrer_or_cookies()
        {
            HttpRequestMessage? seen = null;
            FakeHandler handler = new((request, _) =>
            {
                seen = request;
                return Task.FromResult(Ok(Png));
            });
            using BrowserArtworkFetcher fetcher = Fetcher(handler);

            await fetcher.RequestAsync(Url);

            _ = seen.Should().NotBeNull();
            _ = seen!.Method.Should().Be(HttpMethod.Get);
            _ = seen.Headers.Authorization.Should().BeNull();
            _ = seen.Headers.Referrer.Should().BeNull();
            _ = seen.Headers.Contains("Cookie").Should().BeFalse();
        }

        [Theory]
        [InlineData("127.0.0.1")]
        [InlineData("localhost")]
        [InlineData("10.1.2.3")]
        public async Task The_connection_itself_refuses_a_non_public_address(string host)
        {
            Func<Task> connect = async () => await BrowserArtworkFetcher.ConnectPublicAsync(host, 443, CancellationToken.None);

            _ = await connect.Should().ThrowAsync<HttpRequestException>();
        }

        private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
        {
            private int _calls;

            public int Calls => _calls;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                _ = Interlocked.Increment(ref _calls);
                return respond(request, cancellationToken);
            }
        }

        /// <summary>A body of the given length that never allocates it, so the reader is what must stop.</summary>
        private sealed class OversizedContent(long length, bool declared) : HttpContent
        {
            protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            {
                return Task.CompletedTask;
            }

            protected override bool TryComputeLength(out long computed)
            {
                computed = declared ? length : 0;
                return declared;
            }

            protected override Task<Stream> CreateContentReadStreamAsync()
            {
                return Task.FromResult<Stream>(new ZeroStream(length, Png));
            }

            protected override Stream CreateContentReadStream(CancellationToken cancellationToken)
            {
                return new ZeroStream(length, Png);
            }
        }

        private sealed class ZeroStream(long length, byte[] prefix) : Stream
        {
            private long _position;

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => length;
            public override long Position { get => _position; set => throw new NotSupportedException(); }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return Read(buffer.AsSpan(offset, count));
            }

            public override int Read(Span<byte> buffer)
            {
                int n = (int)Math.Min(buffer.Length, length - _position);
                for (int i = 0; i < n; i++)
                {
                    buffer[i] = _position + i < prefix.Length ? prefix[_position + i] : (byte)0;
                }

                _position += n;
                return n;
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                return ValueTask.FromResult(Read(buffer.Span));
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }
    }
}
