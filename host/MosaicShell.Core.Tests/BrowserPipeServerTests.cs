using System.Diagnostics;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using FluentAssertions;
using MosaicShell.Core.Services.BrowserBridge;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The relay reaches the Host over a per-user named pipe. These use real pipes with a unique name per test:
    /// what matters is that a relay that vanishes, floods or lies cannot take the Host down, and that a second
    /// user's relay cannot reach it at all.
    /// </summary>
    public sealed class BrowserPipeServerTests : IDisposable
    {
        private const string Hello = """{"type":"hello","protocol":1,"extensionVersion":"0.1.0","browser":"edge"}""";
        private const string Session = """{"type":"session","tabId":7,"windowId":1,"origin":"https://music.youtube.com","title":"Humid","playbackState":"playing","audible":true}""";

        private readonly string _pipeName = $"MosaicShell.Test.{Guid.NewGuid():N}";
        private readonly List<IDisposable> _disposables = [];

        public void Dispose()
        {
            foreach (IDisposable d in _disposables)
            {
                d.Dispose();
            }
        }

        private (BrowserSessionHub Hub, BrowserPipeServer Server) Start(int maxConnections = 8)
        {
            BrowserSessionHub hub = new(TimeProvider.System, maxConnections);
            BrowserPipeServer server = new(_pipeName, hub, maxConnections + 1);
            _disposables.Add(server);
            server.Start();
            return (hub, server);
        }

        private async Task<NamedPipeClientStream> ConnectAsync()
        {
            NamedPipeClientStream client = new(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            _disposables.Add(client);
            await client.ConnectAsync(5000);
            return client;
        }

        private static async Task WriteFrameAsync(Stream stream, string json)
        {
            await stream.WriteAsync(NativeMessagingFraming.Encode(Encoding.UTF8.GetBytes(json)));
        }

        private static async Task<string?> ReadFrameAsync(Stream stream)
        {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            byte[]? frame = await NativeMessagingFraming.ReadFrameAsync(stream, 1024 * 1024, cts.Token);
            return frame is null ? null : Encoding.UTF8.GetString(frame);
        }

        private static async Task<bool> WaitUntilAsync(Func<bool> condition, int timeoutMs = 5000)
        {
            Stopwatch clock = Stopwatch.StartNew();
            while (clock.ElapsedMilliseconds < timeoutMs)
            {
                if (condition())
                {
                    return true;
                }

                await Task.Delay(10);
            }

            return condition();
        }

        [Fact]
        public void The_pipe_name_is_per_user_and_stable()
        {
            string name = BrowserPipeServer.PipeNameForCurrentUser();

            _ = name.Should().StartWith("MosaicShell.BrowserMedia.");
            _ = name.Length.Should().BeGreaterThan("MosaicShell.BrowserMedia.".Length);
            _ = BrowserPipeServer.PipeNameForCurrentUser().Should().Be(name);
        }

        [Fact]
        public async Task A_hello_is_accepted_and_answered_with_a_resync()
        {
            (BrowserSessionHub hub, _) = Start();
            NamedPipeClientStream client = await ConnectAsync();

            await WriteFrameAsync(client, Hello);

            _ = (await ReadFrameAsync(client)).Should().Be("""{"type":"resync"}""");
            _ = hub.ConnectionCount.Should().Be(1);
        }

        [Fact]
        public async Task A_session_sent_over_the_pipe_reaches_the_hub()
        {
            (BrowserSessionHub hub, _) = Start();
            NamedPipeClientStream client = await ConnectAsync();
            await WriteFrameAsync(client, Hello);
            await WriteFrameAsync(client, Session);

            bool arrived = await WaitUntilAsync(() => hub.Select(null) is not null);

            _ = arrived.Should().BeTrue();
            _ = hub.Select(null)!.Report.Title.Should().Be("Humid");
        }

        [Fact]
        public async Task Two_frames_in_one_write_are_both_handled()
        {
            (BrowserSessionHub hub, _) = Start();
            NamedPipeClientStream client = await ConnectAsync();

            await client.WriteAsync(new[]
            {
                NativeMessagingFraming.Encode(Encoding.UTF8.GetBytes(Hello)),
                NativeMessagingFraming.Encode(Encoding.UTF8.GetBytes(Session)),
            }.SelectMany(b => b).ToArray());

            _ = (await WaitUntilAsync(() => hub.Select(null) is not null)).Should().BeTrue();
        }

        [Fact]
        public async Task A_relay_that_disconnects_takes_its_tabs_with_it()
        {
            (BrowserSessionHub hub, _) = Start();
            NamedPipeClientStream client = await ConnectAsync();
            await WriteFrameAsync(client, Hello);
            await WriteFrameAsync(client, Session);
            _ = await WaitUntilAsync(() => hub.Select(null) is not null);

            await client.DisposeAsync();

            _ = (await WaitUntilAsync(() => hub.ConnectionCount == 0)).Should().BeTrue();
            _ = hub.Sessions.Should().BeEmpty();
        }

        [Fact]
        public async Task A_relay_that_dies_mid_frame_does_not_stop_the_server()
        {
            (BrowserSessionHub hub, _) = Start();
            NamedPipeClientStream broken = await ConnectAsync();
            await broken.WriteAsync(new byte[] { 0x10, 0x00 });
            await broken.DisposeAsync();
            _ = await WaitUntilAsync(() => hub.ConnectionCount == 0);

            NamedPipeClientStream next = await ConnectAsync();
            await WriteFrameAsync(next, Hello);
            await WriteFrameAsync(next, Session);

            _ = (await WaitUntilAsync(() => hub.Select(null) is not null)).Should().BeTrue();
        }

        [Fact]
        public async Task A_frame_longer_than_the_limit_drops_the_connection()
        {
            (BrowserSessionHub hub, _) = Start();
            NamedPipeClientStream client = await ConnectAsync();
            await WriteFrameAsync(client, Hello);
            _ = await ReadFrameAsync(client);

            await client.WriteAsync(BitConverter.GetBytes((uint)(BrowserProtocol.MaxMessageBytes + 1)));

            _ = (await ReadFrameAsync(client)).Should().BeNull("the Host closed the pipe");
            _ = (await WaitUntilAsync(() => hub.ConnectionCount == 0)).Should().BeTrue();
        }

        [Fact]
        public async Task A_connection_over_the_hub_cap_is_closed_straight_away()
        {
            (BrowserSessionHub hub, _) = Start(maxConnections: 1);
            NamedPipeClientStream first = await ConnectAsync();
            await WriteFrameAsync(first, Hello);
            _ = await ReadFrameAsync(first);

            NamedPipeClientStream second = await ConnectAsync();

            _ = (await ReadFrameAsync(second)).Should().BeNull();
            _ = hub.ConnectionCount.Should().Be(1);
        }

        [Fact]
        public async Task A_command_from_the_hub_arrives_at_the_relay_as_a_frame()
        {
            (BrowserSessionHub hub, _) = Start();
            NamedPipeClientStream client = await ConnectAsync();
            await WriteFrameAsync(client, Hello);
            _ = await ReadFrameAsync(client);
            await WriteFrameAsync(client, Session);
            _ = await WaitUntilAsync(() => hub.Select(null) is not null);

            bool sent = hub.SendCommand(hub.Select(null)!, BrowserCommandAction.Like);

            _ = sent.Should().BeTrue();
            _ = (await ReadFrameAsync(client)).Should().Be("""{"type":"command","tabId":7,"action":"like"}""");
        }

        [Fact]
        public async Task Sending_to_a_relay_that_is_not_reading_never_blocks_the_Host()
        {
            (BrowserSessionHub hub, _) = Start();
            NamedPipeClientStream client = await ConnectAsync();
            await WriteFrameAsync(client, Hello);
            await WriteFrameAsync(client, Session);
            _ = await WaitUntilAsync(() => hub.Select(null) is not null);
            BrowserSessionEntry entry = hub.Select(null)!;

            Stopwatch clock = Stopwatch.StartNew();
            for (int i = 0; i < 5000; i++)
            {
                _ = hub.SendCommand(entry, BrowserCommandAction.Like);
            }

            _ = clock.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2), "queueing a message never waits on the relay");
        }

        [Fact]
        public async Task The_pipe_grants_access_to_the_current_user_only()
        {
            _ = Start();
            NamedPipeClientStream client = await ConnectAsync();
            using WindowsIdentity me = WindowsIdentity.GetCurrent();

            List<PipeAccessRule> rules = [.. client.GetAccessControl()
                .GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier))
                .Cast<PipeAccessRule>()
                .Where(r => r.AccessControlType == AccessControlType.Allow)];

            _ = rules.Should().NotBeEmpty();
            _ = rules.Select(r => r.IdentityReference.Value).Should().OnlyContain(sid => sid == me.User!.Value,
                "PipeOptions.CurrentUserOnly must stay on; the pipe drives what the flyout shows and carries commands");
        }

        [Fact]
        public async Task Disposing_the_server_closes_connections_and_stops_accepting()
        {
            (BrowserSessionHub hub, BrowserPipeServer server) = Start();
            NamedPipeClientStream client = await ConnectAsync();
            await WriteFrameAsync(client, Hello);
            _ = await ReadFrameAsync(client);

            server.Dispose();

            _ = (await ReadFrameAsync(client)).Should().BeNull();
            _ = (await WaitUntilAsync(() => hub.ConnectionCount == 0)).Should().BeTrue();

            using NamedPipeClientStream late = new(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            Func<Task> connect = async () => await late.ConnectAsync(300);
            _ = await connect.Should().ThrowAsync<TimeoutException>();
        }
    }
}
