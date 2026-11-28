using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Kde;
using MosaicShell.Core.Services.Linux;
using Tmds.DBus.Protocol;
using static MosaicShell.Core.Tests.TestWaits;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary>
    /// <see cref="KGlobalAccelShellFlyoutTriggers"/> against a real dbus-daemon, with a stand-in for KWin's
    /// KGlobalAccel that emits shortcut signals in the shape captured live on Plasma: path /component/&lt;name&gt;,
    /// body (component, shortcut, timestamp 0), Pressed then Released for each key.
    /// </summary>
    public sealed class KGlobalAccelShellFlyoutTriggersTests : IAsyncLifetime
    {
        private PrivateDBus? _dbus;
        private FakeKGlobalAccel? _kglobalaccel;
        private KGlobalAccelShellFlyoutTriggers? _triggers;
        private readonly List<ShellFlyoutKind> _seen = [];

        public async Task InitializeAsync()
        {
            if (new LinuxBusFactAttribute().Skip is not null)
            {
                return;
            }

            _dbus = PrivateDBus.Start();
            _kglobalaccel = await FakeKGlobalAccel.StartAsync(_dbus.Address, KGlobalAccelPolicy.BusName);
            _triggers = new KGlobalAccelShellFlyoutTriggers(_dbus.Address);
            _triggers.Triggered += (_, kind) => { lock (_seen) { _seen.Add(kind); } };
        }

        public Task DisposeAsync()
        {
            _triggers?.Dispose();
            _kglobalaccel?.Dispose();
            _dbus?.Dispose();
            return Task.CompletedTask;
        }

        private ShellFlyoutKind[] Seen()
        {
            lock (_seen)
            {
                return [.. _seen];
            }
        }

        private async Task StartAndWaitAsync()
        {
            _triggers!.Start();
            _ = (await EventuallyAsync(() => _triggers.IsActive)).Should().BeTrue("the signal watch should be in place");
        }

        [LinuxBusFact]
        public async Task A_flyout_key_press_raises_its_flyout_kind()
        {
            await StartAndWaitAsync();

            _kglobalaccel!.Press("mediacontrol", "playpausemedia");
            _kglobalaccel.Press("kmix", "increase_volume");
            _kglobalaccel.Press("org_kde_powerdevil", "Decrease Screen Brightness");

            _ = (await EventuallyAsync(() => Seen().Length == 3)).Should().BeTrue();
            _ = Seen().Should().Equal(ShellFlyoutKind.Media, ShellFlyoutKind.Volume, ShellFlyoutKind.Brightness);
        }

        [LinuxBusFact]
        public async Task Releases_and_other_shortcuts_raise_nothing()
        {
            await StartAndWaitAsync();

            _kglobalaccel!.Release("kmix", "mute");
            _kglobalaccel.Press("kmix", "mic_mute");
            _kglobalaccel.Press("kwin", "Window Close");

            // Signals from one sender arrive in order, so once this one is seen the others have been handled.
            _kglobalaccel.Press("mediacontrol", "nextmedia");
            _ = (await EventuallyAsync(() => Seen().Length >= 1)).Should().BeTrue();
            _ = Seen().Should().Equal(ShellFlyoutKind.Media);
        }

        [LinuxBusFact]
        public async Task Only_the_real_kglobalaccel_is_believed()
        {
            using FakeKGlobalAccel impostor = await FakeKGlobalAccel.StartAsync(_dbus!.Address, busName: null);
            await StartAndWaitAsync();

            impostor.Press("kmix", "mute");
            _kglobalaccel!.Press("mediacontrol", "stopmedia");

            _ = (await EventuallyAsync(() => Seen().Length >= 1)).Should().BeTrue();
            await Task.Delay(200);
            _ = Seen().Should().Equal(ShellFlyoutKind.Media);
        }

        [LinuxBusFact]
        public async Task Stop_silences_the_source_and_Start_resumes_it()
        {
            await StartAndWaitAsync();
            _triggers!.Stop();
            _ = _triggers.IsActive.Should().BeFalse();

            _kglobalaccel!.Press("kmix", "mute");
            await Task.Delay(300);
            _ = Seen().Should().BeEmpty();

            await StartAndWaitAsync();
            _kglobalaccel.Press("kmix", "mute");
            _ = (await EventuallyAsync(() => Seen().Length == 1)).Should().BeTrue();
        }

        [LinuxBusFact]
        public async Task An_unreachable_bus_leaves_the_source_inactive_without_throwing()
        {
            string missing = "unix:path=" + Path.Combine(Path.GetTempPath(), "mosaic-no-bus-" + Guid.NewGuid().ToString("N"));
            using KGlobalAccelShellFlyoutTriggers triggers = new(missing);
            using KGlobalAccelShellFlyoutTriggers noAddress = new(address: "");

            triggers.Start();
            noAddress.Start();
            await Task.Delay(300);

            _ = triggers.IsActive.Should().BeFalse();
            _ = noAddress.IsActive.Should().BeFalse();
        }

        [LinuxBusFact]
        public async Task Dispose_during_connect_leaves_nothing_running()
        {
            KGlobalAccelShellFlyoutTriggers triggers = new(_dbus!.Address);
            int raised = 0;
            triggers.Triggered += (_, _) => Interlocked.Increment(ref raised);

            triggers.Start();
            triggers.Dispose();
            await Task.Delay(300);
            _kglobalaccel!.Press("kmix", "mute");
            await Task.Delay(300);

            _ = triggers.IsActive.Should().BeFalse();
            _ = Volatile.Read(ref raised).Should().Be(0);
        }

        [LinuxBusFact]
        public void Platform_defaults_on_linux_take_flyout_triggers_from_kglobalaccel()
        {
            using HostServices hub = HostServices.CreatePlatformDefaults();

            // KGlobalAccel on Plasma only (LinuxDesktopRoutesTests); this checks the defaults take the desktop's route,
            // whatever session the tests run in (CI's runner has none).
            bool plasma = Services.Xdg.XdgCurrentDesktop.IsPlasma(Services.Xdg.XdgCurrentDesktop.Current);
            _ = hub.ShellFlyoutTriggers.Should().BeOfType(
                plasma ? typeof(KGlobalAccelShellFlyoutTriggers) : typeof(NullShellFlyoutTriggerSource));
            bool gnome = Services.Xdg.XdgCurrentDesktop.IsGnome(Services.Xdg.XdgCurrentDesktop.Current);
            _ = hub.OsdSuppressor.Should().BeOfType(
                gnome ? typeof(GnomeOsdSuppressor) : typeof(PlasmaOsdSuppressor),
                "the desktop's own volume and brightness pop-ups are hidden while Tessera is armed");
        }
    }

    /// <summary>Emits KGlobalAccel's component signals, from <c>org.kde.kglobalaccel</c> or from an unnamed peer.</summary>
    internal sealed class FakeKGlobalAccel : IDisposable
    {
        private readonly DBusConnection _connection;

        private FakeKGlobalAccel(DBusConnection connection)
        {
            _connection = connection;
        }

        public static async Task<FakeKGlobalAccel> StartAsync(string address, string? busName)
        {
            DBusConnection connection = new(address);
            await connection.ConnectAsync();
            if (busName is not null)
            {
                await connection.RequestNameAsync(busName);
            }

            return new FakeKGlobalAccel(connection);
        }

        public void Press(string component, string shortcut)
        {
            Emit(component, shortcut, KGlobalAccelPolicy.PressedMember);
        }

        public void Release(string component, string shortcut)
        {
            Emit(component, shortcut, "globalShortcutReleased");
        }

        private void Emit(string component, string shortcut, string member)
        {
            using MessageWriter w = _connection.GetMessageWriter();
            w.WriteSignalHeader(null, "/component/" + component, KGlobalAccelPolicy.ComponentInterface, member, "ssx");
            w.WriteString(component);
            w.WriteString(shortcut);
            w.WriteInt64(0);
            _ = _connection.TrySendMessage(w.CreateMessage());
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
