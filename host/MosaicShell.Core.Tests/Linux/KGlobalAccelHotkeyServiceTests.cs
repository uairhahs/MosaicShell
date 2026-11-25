using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Kde;
using MosaicShell.Core.Services.Linux;
using Tmds.DBus.Protocol;
using static MosaicShell.Core.Tests.TestWaits;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary>
    /// <see cref="KGlobalAccelHotkeyService"/> against a real dbus-daemon, with a stand-in KGlobalAccel that answers as the
    /// live one did on Plasma 6.6 (2026-11-24): setShortcut hands back the key it bound, or 0 for a key another
    /// component holds, and presses arrive on /component/mosaicshell.
    /// </summary>
    public sealed class KGlobalAccelHotkeyServiceTests : IAsyncLifetime
    {
        private const int CtrlAltM = 0x0C00004D;
        private PrivateDBus? _dbus;
        private FakeKGlobalAccelRegistry? _kglobalaccel;
        private KGlobalAccelHotkeyService? _hotkeys;

        public async Task InitializeAsync()
        {
            if (new LinuxBusFactAttribute().Skip is not null)
            {
                return;
            }

            _dbus = PrivateDBus.Start();
            _kglobalaccel = await FakeKGlobalAccelRegistry.StartAsync(_dbus.Address);
            _hotkeys = new KGlobalAccelHotkeyService(_dbus.Address);
        }

        public Task DisposeAsync()
        {
            _hotkeys?.Dispose();
            _kglobalaccel?.Dispose();
            _dbus?.Dispose();
            return Task.CompletedTask;
        }

        [LinuxBusFact]
        public async Task A_free_key_binds_and_its_press_runs_the_callback()
        {
            int pressed = 0;

            bool registered = _hotkeys!.Register("cap:Mixdeck", ModifierKeys.Control | ModifierKeys.Alt, 'M', () => Interlocked.Increment(ref pressed));
            _kglobalaccel!.Press("cap:Mixdeck");

            _ = registered.Should().BeTrue();
            _ = _kglobalaccel.Bound.Should().Contain(("cap:Mixdeck", CtrlAltM));
            _ = (await EventuallyAsync(() => Volatile.Read(ref pressed) == 1)).Should().BeTrue();
        }

        [LinuxBusFact]
        public void A_key_another_component_holds_fails_and_leaves_no_action()
        {
            _ = _kglobalaccel!.Taken.Add(CtrlAltM);

            bool registered = _hotkeys!.Register("cap:Mixdeck", ModifierKeys.Control | ModifierKeys.Alt, 'M', () => { });

            _ = registered.Should().BeFalse();
            _ = _kglobalaccel.Actions.Should().BeEmpty();
        }

        [LinuxBusFact]
        public async Task Unregister_frees_the_key_and_stops_the_callback()
        {
            int pressed = 0;
            _ = _hotkeys!.Register("cap:Mixdeck", ModifierKeys.Control | ModifierKeys.Alt, 'M', () => Interlocked.Increment(ref pressed));

            _hotkeys.Unregister("cap:Mixdeck");
            _kglobalaccel!.Press("cap:Mixdeck");
            await Task.Delay(200);

            _ = _kglobalaccel.Actions.Should().BeEmpty();
            _ = Volatile.Read(ref pressed).Should().Be(0);
        }

        [LinuxBusFact]
        public void Dispose_frees_every_key()
        {
            _ = _hotkeys!.Register("cap:Mixdeck", ModifierKeys.Control | ModifierKeys.Alt, 'M', () => { });
            _ = _hotkeys.Register("cap:Inlay", ModifierKeys.Control | ModifierKeys.Alt, 'I', () => { });

            _hotkeys.Dispose();

            _ = _kglobalaccel!.Actions.Should().BeEmpty();
        }

        [LinuxBusFact]
        public async Task A_press_from_anyone_but_KGlobalAccel_is_ignored()
        {
            int pressed = 0;
            _ = _hotkeys!.Register("cap:Mixdeck", ModifierKeys.Control | ModifierKeys.Alt, 'M', () => Interlocked.Increment(ref pressed));
            using FakeKGlobalAccel impostor = await FakeKGlobalAccel.StartAsync(_dbus!.Address, busName: null);

            impostor.Press(KGlobalAccelHotkeyPolicy.ComponentUnique, "cap:Mixdeck");
            await Task.Delay(200);

            _ = Volatile.Read(ref pressed).Should().Be(0);
        }

        [Fact]
        public void No_bus_binds_nothing_without_throwing()
        {
            using KGlobalAccelHotkeyService hotkeys = new(address: "");

            _ = hotkeys.Register("cap:Mixdeck", ModifierKeys.Control | ModifierKeys.Alt, 'M', () => { }).Should().BeFalse();
        }

        [LinuxBusFact]
        public void Platform_defaults_on_linux_take_hotkeys_from_kglobalaccel()
        {
            using HostServices hub = HostServices.CreatePlatformDefaults();

            _ = hub.Hotkeys.Should().BeOfType<KGlobalAccelHotkeyService>();
        }
    }

    /// <summary>KGlobalAccel's registry calls as the live daemon answers them, plus component presses.</summary>
    internal sealed class FakeKGlobalAccelRegistry : IPathMethodHandler, IDisposable
    {
        private readonly Lock _gate = new();
        private readonly HashSet<string> _actions = [];
        private readonly List<(string, int)> _bound = [];
        private DBusConnection? _connection;

        public string Path => KGlobalAccelHotkeyPolicy.Path;
        public bool HandlesChildPaths => false;

        /// <summary>Keys another component holds; setShortcut answers 0 for them.</summary>
        public HashSet<int> Taken { get; } = [];

        public string[] Actions
        {
            get
            {
                lock (_gate)
                {
                    return [.. _actions];
                }
            }
        }

        public (string, int)[] Bound
        {
            get
            {
                lock (_gate)
                {
                    return [.. _bound];
                }
            }
        }

        public static async Task<FakeKGlobalAccelRegistry> StartAsync(string address)
        {
            FakeKGlobalAccelRegistry fake = new();
            DBusConnection connection = new(address);
            await connection.ConnectAsync();
            connection.AddMethodHandler(fake);
            await connection.RequestNameAsync(KGlobalAccelPolicy.BusName);
            fake._connection = connection;

            // RequestNameAsync completes on this connection's receive loop; leave it, or the test body runs there and a
            // blocking Register stops the stand-in from answering.
            await Task.Yield();
            return fake;
        }

        public ValueTask HandleMethodAsync(MethodContext context)
        {
            Reader r = context.Request.GetBodyReader();
            switch (context.Request.MemberAsString)
            {
                case "doRegister":
                    {
                        string[] id = r.ReadArrayOfString();
                        lock (_gate)
                        {
                            _ = _actions.Add(id[1]);
                        }

                        context.Reply(context.CreateReplyWriter(null).CreateMessage());
                        break;
                    }

                case "setShortcut":
                    {
                        string[] id = r.ReadArrayOfString();
                        int key = r.ReadArrayOfInt32()[0];
                        bool free = !Taken.Contains(key);
                        if (free)
                        {
                            lock (_gate)
                            {
                                _bound.Add((id[1], key));
                            }
                        }

                        using MessageWriter w = context.CreateReplyWriter("ai");
                        int[] bound = [free ? key : 0];
                        w.WriteArray(bound);
                        context.Reply(w.CreateMessage());
                        break;
                    }

                case "unregister":
                    {
                        _ = r.ReadString();
                        string action = r.ReadString();
                        bool removed;
                        lock (_gate)
                        {
                            removed = _actions.Remove(action);
                        }

                        using MessageWriter w = context.CreateReplyWriter("b");
                        w.WriteBool(removed);
                        context.Reply(w.CreateMessage());
                        break;
                    }

                default:
                    context.ReplyUnknownMethodError();
                    break;
            }

            return ValueTask.CompletedTask;
        }

        public void Press(string action)
        {
            using MessageWriter w = _connection!.GetMessageWriter();
            w.WriteSignalHeader(null, KGlobalAccelHotkeyPolicy.ComponentPath, KGlobalAccelPolicy.ComponentInterface, KGlobalAccelPolicy.PressedMember, "ssx");
            w.WriteString(KGlobalAccelHotkeyPolicy.ComponentUnique);
            w.WriteString(action);
            w.WriteInt64(0);
            _ = _connection.TrySendMessage(w.CreateMessage());
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
