using System.Net.Sockets;
using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Kde;
using MosaicShell.Core.Services.Linux;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary>
    /// <see cref="KWinKeystateLockKeys"/> against a stand-in compositor on a Unix socket that answers as KWin does: the
    /// registry lists its globals (keystate among them) before the sync's done, and fetchStates sends Caps, Num and
    /// Scroll Lock in turn.
    /// </summary>
    public sealed class KWinKeystateLockKeysTests : IDisposable
    {
        private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

        private readonly string _dir = Path.Combine(Path.GetTempPath(), "mosaic-keystate-" + Guid.NewGuid().ToString("N"));

        public KWinKeystateLockKeysTests()
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
        public async Task The_first_states_are_taken_quietly_and_each_edge_is_raised()
        {
            using FakeCompositor kwin = FakeCompositor.Start(_dir, offerKeystate: true, capsLocked: true);
            using KWinKeystateLockKeys keys = new(kwin.SocketPath);
            List<LockKeyState> seen = [];
            keys.Changed += (_, s) => { lock (seen) { seen.Add(s); } };

            _ = (await EventuallyAsync(() => keys.Caps.IsOn)).Should().BeTrue("fetchStates reported Caps Lock locked");
            kwin.Send(0, 0);
            kwin.Send(1, 2);
            kwin.Send(1, 2);

            _ = (await EventuallyAsync(() => { lock (seen) { return seen.Count == 2; } })).Should().BeTrue();
            await Task.Delay(100);
            lock (seen)
            {
                _ = seen.Should().Equal(new LockKeyState(LockKeyKind.CapsLock, false), new LockKeyState(LockKeyKind.NumLock, true));
            }

            _ = keys.IsActive.Should().BeTrue();
            _ = kwin.BoundVersion.Should().Be(1, "version 5 would report every modifier press too");
        }

        [Fact]
        public async Task Stop_closes_the_connection_and_Start_opens_a_new_one()
        {
            using FakeCompositor kwin = FakeCompositor.Start(_dir, offerKeystate: true, capsLocked: false);
            using KWinKeystateLockKeys keys = new(kwin.SocketPath);

            keys.Start();
            _ = (await EventuallyAsync(() => keys.IsActive)).Should().BeTrue();
            keys.Stop();

            _ = (await EventuallyAsync(() => kwin.ClientClosed)).Should().BeTrue("nothing is listening, so nothing stays connected");
            _ = keys.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task A_compositor_without_keystate_leaves_it_inactive_and_disconnected()
        {
            // GNOME: the sync's done arrives with no keystate global.
            using FakeCompositor mutter = FakeCompositor.Start(_dir, offerKeystate: false, capsLocked: false);
            using KWinKeystateLockKeys keys = new(mutter.SocketPath);

            keys.Start();

            _ = (await EventuallyAsync(() => mutter.ClientClosed)).Should().BeTrue();
            _ = keys.IsActive.Should().BeFalse();
            _ = keys.Caps.IsOn.Should().BeFalse();
        }

        [Fact]
        public void No_compositor_leaves_it_inactive_without_throwing()
        {
            using KWinKeystateLockKeys keys = new(Path.Combine(_dir, "missing"));

            keys.Start();
            keys.Stop();

            _ = keys.IsActive.Should().BeFalse();
        }

        [Fact]
        public void Platform_defaults_on_linux_take_lock_keys_from_kwin_on_Plasma_and_the_LEDs_elsewhere()
        {
            if (!OperatingSystem.IsLinux())
            {
                return;
            }

            using HostServices hub = HostServices.CreatePlatformDefaults();

            bool plasma = Services.Xdg.XdgCurrentDesktop.IsPlasma(Services.Xdg.XdgCurrentDesktop.Current);
            _ = hub.LockKeys.Should().BeOfType(plasma ? typeof(KWinKeystateLockKeys) : typeof(LedLockKeysService));
        }
    }

    /// <summary>A Wayland compositor that knows only the registry, sync and keystate, enough for one client.</summary>
    internal sealed class FakeCompositor : IDisposable
    {
        private readonly Socket _listener;
        private readonly bool _offerKeystate;
        private readonly bool _capsLocked;
        private Socket? _client;
        private volatile bool _clientClosed;

        private FakeCompositor(string socketPath, Socket listener, bool offerKeystate, bool capsLocked)
        {
            SocketPath = socketPath;
            _listener = listener;
            _offerKeystate = offerKeystate;
            _capsLocked = capsLocked;
        }

        public string SocketPath { get; }
        public bool ClientClosed => _clientClosed;
        public uint BoundVersion { get; private set; }

        public static FakeCompositor Start(string dir, bool offerKeystate, bool capsLocked)
        {
            string path = Path.Combine(dir, "wayland-test");
            Socket listener = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            listener.Bind(new UnixDomainSocketEndPoint(path));
            listener.Listen(1);
            FakeCompositor fake = new(path, listener, offerKeystate, capsLocked);
            _ = Task.Run(fake.ServeAsync);
            return fake;
        }

        /// <summary>Pushes keystate.stateChanged(key, state) to the client.</summary>
        public void Send(uint key, uint state)
        {
            _ = _client!.Send(KWinKeystatePolicy.Message(KWinKeystatePolicy.KeystateId, 0, [.. KWinKeystatePolicy.U32(key), .. KWinKeystatePolicy.U32(state)]));
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
                    List<Services.Kde.WaylandMessage> requests = [];
                    while (KWinKeystatePolicy.TryRead(buffer.AsSpan(offset, filled - offset), out Services.Kde.WaylandMessage? m, out int used))
                    {
                        offset += used;
                        requests.Add(m!);
                    }

                    Buffer.BlockCopy(buffer, offset, buffer, 0, filled - offset);
                    filled -= offset;
                    foreach (Services.Kde.WaylandMessage request in requests)
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

        private void Answer(Socket client, Services.Kde.WaylandMessage request)
        {
            switch (request.ObjectId, request.Opcode)
            {
                case (KWinKeystatePolicy.DisplayId, 1):
                    _ = client.Send(Global(10, "wl_seat", 10));
                    if (_offerKeystate)
                    {
                        _ = client.Send(Global(62, KWinKeystatePolicy.Interface, 5));
                    }

                    break;

                case (KWinKeystatePolicy.DisplayId, 0):
                    _ = client.Send(KWinKeystatePolicy.Message(KWinKeystatePolicy.SyncCallbackId, 0, KWinKeystatePolicy.U32(1)));
                    break;

                case (KWinKeystatePolicy.RegistryId, 0):
                    {
                        // name, interface string (22 bytes padded to 24), version, new id.
                        BoundVersion = BitConverter.ToUInt32(request.Args, 4 + 4 + 24);
                        break;
                    }

                case (KWinKeystatePolicy.KeystateId, 0):
                    Send(0, _capsLocked ? 2u : 0u);
                    Send(1, 0);
                    Send(2, 0);
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
