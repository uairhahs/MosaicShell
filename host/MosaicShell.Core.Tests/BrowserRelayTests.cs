using System.IO.Pipes;
using System.Text;
using FluentAssertions;
using MosaicShell.Core.Services.BrowserBridge;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The relay is the native messaging host the browser starts. It carries frames between the browser (standard
    /// input and output) and the Host (a named pipe), survives the Host not running or restarting, replays the
    /// extension's hello to every new Host connection, and exits when the browser goes away.
    /// </summary>
    public sealed class BrowserRelayTests : IDisposable
    {
        private const string Hello = """{"type":"hello","protocol":1,"extensionVersion":"0.1.0","browser":"edge"}""";
        private const string Session = """{"type":"session","tabId":7,"windowId":1,"origin":"https://music.youtube.com","playbackState":"playing"}""";

        private readonly List<IDisposable> _disposables = [];
        private readonly CancellationTokenSource _stop = new();

        public void Dispose()
        {
            _stop.Cancel();
            foreach (IDisposable d in _disposables)
            {
                d.Dispose();
            }

            _stop.Dispose();
        }

        /// <summary>Two connected ends of one duplex stream.</summary>
        private async Task<(Stream Inner, Stream Outer)> DuplexAsync()
        {
            string name = $"MosaicShell.Test.{Guid.NewGuid():N}";
            // Buffered, as the pipes between a browser and its native host are: with no buffer a write waits for the reader.
            NamedPipeServerStream server = new(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 64 * 1024, 64 * 1024);
            NamedPipeClientStream client = new(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
            _disposables.Add(server);
            _disposables.Add(client);
            await Task.WhenAll(server.WaitForConnectionAsync(), client.ConnectAsync(5000));
            return (server, client);
        }

        private static async Task WriteAsync(Stream stream, string json)
        {
            using CancellationTokenSource cts = new(5000);
            await stream.WriteAsync(NativeMessagingFraming.Encode(Encoding.UTF8.GetBytes(json)), cts.Token);
        }

        private static async Task<string?> ReadAsync(Stream stream, int timeoutMs = 5000)
        {
            using CancellationTokenSource cts = new(timeoutMs);
            byte[]? frame = await NativeMessagingFraming.ReadFrameAsync(stream, 1024 * 1024, cts.Token);
            return frame is null ? null : Encoding.UTF8.GetString(frame);
        }

        /// <summary>True when no further frame arrives within a short wait.</summary>
        private static async Task<bool> NothingMoreAsync(Stream stream)
        {
            try
            {
                _ = await ReadAsync(stream, 300);
                return false;
            }
            catch (OperationCanceledException)
            {
                return true;
            }
        }

        private static TimeSpan NoWait(int attempt)
        {
            return TimeSpan.FromMilliseconds(5);
        }

        /// <summary>A Host the relay can connect to, one connection at a time, as many times as the test hands out.</summary>
        private sealed class FakeHost(BrowserRelayTests owner)
        {
            private readonly Queue<Stream?> _next = new();

            public int Attempts { get; private set; }

            public async Task<Stream> AcceptNextAsync()
            {
                (Stream inner, Stream outer) = await owner.DuplexAsync();
                _next.Enqueue(inner);
                return outer;
            }

            public void FailNextAttempt()
            {
                _next.Enqueue(null);
            }

            public Task<Stream?> ConnectAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Attempts++;
                return Task.FromResult(_next.Count > 0 ? _next.Dequeue() : null);
            }
        }

        private async Task<(Task Run, Stream Browser, FakeHost Host)> StartAsync()
        {
            (Stream inner, Stream browser) = await DuplexAsync();
            FakeHost host = new(this);
            BrowserRelay relay = new(inner, inner, host.ConnectAsync, NoWait);
            Task run = relay.RunAsync(_stop.Token);
            return (run, browser, host);
        }

        [Fact]
        public async Task The_hello_and_later_frames_reach_the_Host()
        {
            (_, Stream browser, FakeHost host) = await StartAsync();
            Stream hostEnd = await host.AcceptNextAsync();

            // As the extension does: hello first, then state once the Host has answered. A frame sent before the relay
            // has reached the Host is dropped by design, and the Host's resync makes the extension send it again.
            await WriteAsync(browser, Hello);
            _ = (await ReadAsync(hostEnd)).Should().Be(Hello);
            await WriteAsync(browser, Session);

            _ = (await ReadAsync(hostEnd)).Should().Be(Session);
        }

        [Fact]
        public async Task A_frame_from_the_Host_reaches_the_browser()
        {
            (_, Stream browser, FakeHost host) = await StartAsync();
            Stream hostEnd = await host.AcceptNextAsync();
            await WriteAsync(browser, Hello);
            _ = await ReadAsync(hostEnd);

            await WriteAsync(hostEnd, """{"type":"resync"}""");

            _ = (await ReadAsync(browser)).Should().Be("""{"type":"resync"}""");
        }

        [Fact]
        public async Task Frames_sent_while_no_Host_is_running_are_dropped_and_only_the_hello_is_replayed()
        {
            (_, Stream browser, FakeHost host) = await StartAsync();
            await WriteAsync(browser, Hello);
            await WriteAsync(browser, Session);
            await Task.Delay(100);

            Stream hostEnd = await host.AcceptNextAsync();

            _ = (await ReadAsync(hostEnd)).Should().Be(Hello);
            _ = (await NothingMoreAsync(hostEnd)).Should().BeTrue("the stale session is not replayed; the Host asks for a resync instead");
        }

        [Fact]
        public async Task The_relay_keeps_trying_until_the_Host_appears()
        {
            (_, Stream browser, FakeHost host) = await StartAsync();
            await WriteAsync(browser, Hello);
            host.FailNextAttempt();
            host.FailNextAttempt();

            Stream hostEnd = await host.AcceptNextAsync();

            _ = (await ReadAsync(hostEnd)).Should().Be(Hello);
            _ = host.Attempts.Should().BeGreaterThanOrEqualTo(3);
        }

        [Fact]
        public async Task A_restarted_Host_gets_the_hello_again_and_the_browser_stays_connected()
        {
            (_, Stream browser, FakeHost host) = await StartAsync();
            Stream first = await host.AcceptNextAsync();
            await WriteAsync(browser, Hello);
            _ = await ReadAsync(first);

            await first.DisposeAsync();
            Stream second = await host.AcceptNextAsync();

            _ = (await ReadAsync(second)).Should().Be(Hello);
            await WriteAsync(browser, Session);
            _ = (await ReadAsync(second)).Should().Be(Session);
        }

        [Fact]
        public async Task A_frame_before_the_hello_is_not_forwarded_because_the_Host_requires_the_hello_first()
        {
            (_, Stream browser, FakeHost host) = await StartAsync();
            Stream hostEnd = await host.AcceptNextAsync();
            await Task.Delay(100);

            await WriteAsync(browser, Session);
            await WriteAsync(browser, Hello);

            _ = (await ReadAsync(hostEnd)).Should().Be(Hello);
            _ = (await NothingMoreAsync(hostEnd)).Should().BeTrue();
        }

        [Fact]
        public async Task The_relay_exits_when_the_browser_closes_its_end()
        {
            (Task run, Stream browser, FakeHost host) = await StartAsync();
            Stream hostEnd = await host.AcceptNextAsync();
            await WriteAsync(browser, Hello);
            _ = await ReadAsync(hostEnd);

            await browser.DisposeAsync();

            _ = (await Task.WhenAny(run, Task.Delay(5000))).Should().BeSameAs(run);
            _ = (await ReadAsync(hostEnd)).Should().BeNull("the relay closed its connection to the Host");
        }

        [Fact]
        public async Task A_frame_over_the_size_limit_ends_the_relay_because_the_stream_can_no_longer_be_trusted()
        {
            (Task run, Stream browser, FakeHost host) = await StartAsync();
            Stream hostEnd = await host.AcceptNextAsync();
            await WriteAsync(browser, Hello);
            _ = await ReadAsync(hostEnd);

            await browser.WriteAsync(BitConverter.GetBytes((uint)(BrowserProtocol.MaxMessageBytes + 1)));

            _ = (await Task.WhenAny(run, Task.Delay(5000))).Should().BeSameAs(run);
        }

        [Fact]
        public async Task Cancelling_stops_the_relay()
        {
            (Task run, _, _) = await StartAsync();

            await _stop.CancelAsync();

            _ = (await Task.WhenAny(run, Task.Delay(5000))).Should().BeSameAs(run);
        }

        [Theory]
        [InlineData(0, 250)]
        [InlineData(1, 500)]
        [InlineData(2, 1000)]
        [InlineData(3, 2000)]
        [InlineData(4, 4000)]
        [InlineData(5, 5000)]
        [InlineData(6, 5000)]
        [InlineData(500, 5000)]
        public void Retries_back_off_up_to_a_ceiling(int attempt, int expectedMs)
        {
            _ = BrowserRelay.DefaultBackoff(attempt).Should().Be(TimeSpan.FromMilliseconds(expectedMs));
        }

        [Fact]
        public async Task The_relay_writes_only_frames_to_its_output()
        {
            (Task _, Stream browser, FakeHost host) = await StartAsync();
            Stream hostEnd = await host.AcceptNextAsync();
            await WriteAsync(browser, Hello);
            _ = await ReadAsync(hostEnd);
            await WriteAsync(hostEnd, "{}");
            await WriteAsync(hostEnd, "[]");

            string? a = await ReadAsync(browser);
            string? b = await ReadAsync(browser);

            _ = (a, b).Should().Be(("{}", "[]"), "anything else on standard output would corrupt the browser's framing");
        }
    }
}
