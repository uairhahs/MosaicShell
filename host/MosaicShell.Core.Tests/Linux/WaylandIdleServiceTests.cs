using System.Buffers.Binary;
using System.Net.Sockets;
using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Kde;
using MosaicShell.Core.Services.Linux;
using MosaicShell.Core.Services.Wayland;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary>
    /// <see cref="WaylandIdleService"/> against a stand-in compositor on a Unix socket that lists a seat and (optionally)
    /// <c>ext_idle_notifier_v1</c>, records the requests, and sends idled and resumed on demand.
    /// </summary>
    public sealed class WaylandIdleServiceTests : IDisposable
    {
        private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

        private readonly string _dir = Path.Combine(Path.GetTempPath(), "mosaic-idle-" + Guid.NewGuid().ToString("N"));

        public WaylandIdleServiceTests()
        {
            _ = Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            Directory.Delete(_dir, recursive: true);
        }

        private static async Task<bool> EventuallyAsync(Func<bool> condition)
        {
            DateTime until = DateTime.UtcNow + Patience;
            while (DateTime.UtcNow < until)
            {
                if (condition())
                {
                    return true;
                }

                await Task.Delay(20);
            }

            return condition();
        }

        [Fact]
        public async Task Start_asks_for_an_input_idle_notification_at_the_threshold()
        {
            using FakeIdleCompositor kwin = FakeIdleCompositor.Start(_dir, notifierVersion: 2);
            using WaylandIdleService idle = new(kwin.SocketPath) { Threshold = TimeSpan.FromSeconds(90) };

            idle.Start();

            _ = (await EventuallyAsync(() => kwin.Notifications.Count == 1)).Should().BeTrue();
            _ = kwin.Notifications[0].Should().Be((2, 90_000u));
            _ = idle.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task A_version_1_notifier_gets_the_plain_idle_notification()
        {
            using FakeIdleCompositor kwin = FakeIdleCompositor.Start(_dir, notifierVersion: 1);
            using WaylandIdleService idle = new(kwin.SocketPath);

            idle.Start();

            _ = (await EventuallyAsync(() => kwin.Notifications.Count == 1)).Should().BeTrue();
            _ = kwin.Notifications[0].Opcode.Should().Be(1);
        }

        [Fact]
        public async Task Idled_raises_once_per_idle_stretch()
        {
            // As Windows: one event when the threshold is reached, and again only after input in between.
            using FakeIdleCompositor kwin = FakeIdleCompositor.Start(_dir, notifierVersion: 2);
            using WaylandIdleService idle = new(kwin.SocketPath);
            int raised = 0;
            idle.IdleThresholdReached += (_, _) => Interlocked.Increment(ref raised);
            idle.Start();
            _ = (await EventuallyAsync(() => kwin.Notifications.Count == 1)).Should().BeTrue();

            kwin.SendIdled();
            kwin.SendIdled();
            _ = (await EventuallyAsync(() => raised == 1)).Should().BeTrue();
            _ = idle.IdleTime.Should().BeGreaterThanOrEqualTo(idle.Threshold);
            kwin.SendResumed();
            _ = (await EventuallyAsync(() => idle.IdleTime == TimeSpan.Zero)).Should().BeTrue();
            kwin.SendIdled();

            _ = (await EventuallyAsync(() => raised == 2)).Should().BeTrue();
            await Task.Delay(100);
            _ = raised.Should().Be(2);
        }

        [Fact]
        public async Task A_new_threshold_while_running_replaces_the_notification()
        {
            using FakeIdleCompositor kwin = FakeIdleCompositor.Start(_dir, notifierVersion: 2);
            using WaylandIdleService idle = new(kwin.SocketPath) { Threshold = TimeSpan.FromSeconds(60) };
            idle.Start();
            _ = (await EventuallyAsync(() => kwin.Notifications.Count == 1)).Should().BeTrue();

            idle.Threshold = TimeSpan.FromSeconds(120);

            _ = (await EventuallyAsync(() => kwin.Notifications.Count == 2)).Should().BeTrue();
            _ = kwin.Notifications[1].TimeoutMs.Should().Be(120_000u);
            _ = kwin.Destroyed.Should().Equal(kwin.NotificationIds[0]);
        }

        [Fact]
        public async Task Stop_closes_the_connection_and_later_events_are_not_raised()
        {
            using FakeIdleCompositor kwin = FakeIdleCompositor.Start(_dir, notifierVersion: 2);
            using WaylandIdleService idle = new(kwin.SocketPath);
            int raised = 0;
            idle.IdleThresholdReached += (_, _) => Interlocked.Increment(ref raised);
            idle.Start();
            _ = (await EventuallyAsync(() => kwin.Notifications.Count == 1)).Should().BeTrue();

            idle.Stop();

            _ = (await EventuallyAsync(() => kwin.ClientClosed)).Should().BeTrue();
            _ = idle.IsActive.Should().BeFalse();
            _ = raised.Should().Be(0);
        }

        [Fact]
        public async Task A_compositor_without_the_notifier_leaves_it_inactive_and_disconnected()
        {
            // GNOME's Mutter does not offer ext_idle_notifier_v1.
            using FakeIdleCompositor mutter = FakeIdleCompositor.Start(_dir, notifierVersion: 0);
            using WaylandIdleService idle = new(mutter.SocketPath);

            idle.Start();

            _ = (await EventuallyAsync(() => mutter.ClientClosed)).Should().BeTrue();
            _ = idle.IsActive.Should().BeFalse();
            _ = idle.IdleTime.Should().Be(TimeSpan.Zero);
        }

        [Fact]
        public void No_compositor_leaves_it_inactive_without_throwing()
        {
            using WaylandIdleService idle = new(Path.Combine(_dir, "missing"));

            idle.Start();
            idle.Stop();

            _ = idle.IsActive.Should().BeFalse();
        }

        [Fact]
        public void Platform_defaults_on_linux_take_idle_from_the_compositor()
        {
            if (!OperatingSystem.IsLinux())
            {
                return;
            }

            using HostServices hub = HostServices.CreatePlatformDefaults();

            _ = hub.Idle.Should().BeOfType<WaylandIdleService>();
        }
    }

    /// <summary>A Wayland compositor with a seat and, unless its version is 0, the idle notifier.</summary>
    internal sealed class FakeIdleCompositor : IDisposable
    {
        private readonly Socket _listener;
        private readonly uint _notifierVersion;
        private readonly Lock _gate = new();
        private readonly List<(ushort Opcode, uint TimeoutMs)> _notifications = [];
        private readonly List<uint> _notificationIds = [];
        private readonly List<uint> _destroyed = [];
        private Socket? _client;
        private volatile bool _clientClosed;

        private FakeIdleCompositor(string socketPath, Socket listener, uint notifierVersion)
        {
            SocketPath = socketPath;
            _listener = listener;
            _notifierVersion = notifierVersion;
        }

        public string SocketPath { get; }
        public bool ClientClosed => _clientClosed;

        public IReadOnlyList<(ushort Opcode, uint TimeoutMs)> Notifications
        {
            get { lock (_gate) { return [.. _notifications]; } }
        }

        public IReadOnlyList<uint> NotificationIds
        {
            get { lock (_gate) { return [.. _notificationIds]; } }
        }

        public IReadOnlyList<uint> Destroyed
        {
            get { lock (_gate) { return [.. _destroyed]; } }
        }

        public static FakeIdleCompositor Start(string dir, uint notifierVersion)
        {
            string path = Path.Combine(dir, "wayland-test");
            Socket listener = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            listener.Bind(new UnixDomainSocketEndPoint(path));
            listener.Listen(1);
            FakeIdleCompositor fake = new(path, listener, notifierVersion);
            _ = Task.Run(fake.ServeAsync);
            return fake;
        }

        public void SendIdled()
        {
            _ = _client!.Send(KWinKeystatePolicy.Message(NotificationIds[^1], 0, []));
        }

        public void SendResumed()
        {
            _ = _client!.Send(KWinKeystatePolicy.Message(NotificationIds[^1], 1, []));
        }

        private async Task ServeAsync()
        {
            try
            {
                Socket client = await _listener.AcceptAsync();
                _client = client;
                byte[] buffer = new byte[4096];
                int filled = 0;
                while (true)
                {
                    int read = await client.ReceiveAsync(buffer.AsMemory(filled));
                    if (read == 0)
                    {
                        _clientClosed = true;
                        return;
                    }

                    filled += read;
                    int offset = 0;
                    List<WaylandMessage> requests = [];
                    while (KWinKeystatePolicy.TryRead(buffer.AsSpan(offset, filled - offset), out WaylandMessage? m, out int used))
                    {
                        offset += used;
                        requests.Add(m!);
                    }

                    Buffer.BlockCopy(buffer, offset, buffer, 0, filled - offset);
                    filled -= offset;
                    foreach (WaylandMessage request in requests)
                    {
                        Answer(client, request);
                    }
                }
            }
            catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
            {
                _clientClosed = true;
            }
        }

        private void Answer(Socket client, WaylandMessage request)
        {
            switch (request.ObjectId, request.Opcode)
            {
                case (KWinKeystatePolicy.DisplayId, 1):
                    _ = client.Send(Global(10, "wl_seat", 9));
                    if (_notifierVersion > 0)
                    {
                        _ = client.Send(Global(20, ExtIdleNotifyPolicy.NotifierInterface, _notifierVersion));
                    }

                    break;

                case (KWinKeystatePolicy.DisplayId, 0):
                    _ = client.Send(KWinKeystatePolicy.Message(KWinKeystatePolicy.SyncCallbackId, 0, KWinKeystatePolicy.U32(1)));
                    break;

                case (ExtIdleNotifyPolicy.NotifierId, 1 or 2):
                    lock (_gate)
                    {
                        _notificationIds.Add(BinaryPrimitives.ReadUInt32LittleEndian(request.Args));
                        _notifications.Add((request.Opcode, BinaryPrimitives.ReadUInt32LittleEndian(request.Args.AsSpan(4))));
                    }

                    break;

                case (_, 0) when request.ObjectId >= ExtIdleNotifyPolicy.FirstNotificationId:
                    lock (_gate)
                    {
                        _destroyed.Add(request.ObjectId);
                    }

                    break;
            }
        }

        private static byte[] Global(uint name, string iface, uint version)
        {
            return KWinKeystatePolicy.Message(KWinKeystatePolicy.RegistryId, 0,
                [.. KWinKeystatePolicy.U32(name), .. KWinKeystatePolicy.String(iface), .. KWinKeystatePolicy.U32(version)]);
        }

        public void Dispose()
        {
            _client?.Dispose();
            _listener.Dispose();
        }
    }
}
