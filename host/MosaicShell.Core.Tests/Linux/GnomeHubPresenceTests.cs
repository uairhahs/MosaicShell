using FluentAssertions;
using MosaicShell.Core.HubShell;
using MosaicShell.Core.Services.Gnome;
using MosaicShell.Core.Services.Linux;
using Tmds.DBus.Protocol;
using static MosaicShell.Core.Tests.TestWaits;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary>
    /// <see cref="GnomeHubPresence"/> against a private bus and a throwaway data folder: while the Host runs it owns the
    /// presence name the Shell extension watches (so the top bar shows MosaicShell), installs the extension, and the
    /// panel menu's Open and Quit reach the Host. Disposing gives the name up, so the icon goes.
    /// </summary>
    public sealed class GnomeHubPresenceTests : IAsyncLifetime
    {
        private PrivateDBus? _dbus;
        private DBusConnection? _observer;
        private GnomeHubPresence? _presence;
        private string? _dataHome;
        private int _shown;
        private int _quit;
        private readonly List<(string Id, bool On)> _switched = [];

        public async Task InitializeAsync()
        {
            if (new LinuxBusFactAttribute().Skip is not null)
            {
                return;
            }

            _dataHome = Directory.CreateTempSubdirectory("mosaic-gnome-hub-").FullName;
            _dbus = PrivateDBus.Start();
            _observer = new DBusConnection(_dbus.Address);
            await _observer.ConnectAsync();
            _presence = new GnomeHubPresence(
                () => _shown++,
                () => _quit++,
                _dbus.Address,
                _dataHome,
                new QuietGnomeSettings(),
                () => [new TrayModule("Tessera", "Tessera", true), new TrayModule("Acme", "Acme Clock", false)],
                (id, on) =>
                {
                    lock (_switched)
                    {
                        _switched.Add((id, on));
                    }
                });
        }

        public Task DisposeAsync()
        {
            _presence?.Dispose();
            _observer?.Dispose();
            _dbus?.Dispose();
            if (_dataHome is not null)
            {
                Directory.Delete(_dataHome, recursive: true);
            }

            return Task.CompletedTask;
        }

        private Task<bool> OwnedAsync()
        {
            return DBusCalls.CallAsync(_observer!, DBusCalls.BusName, DBusCalls.BusPath, DBusCalls.BusName, "NameHasOwner", "s",
                (ref w) => w.WriteString(GnomeHubPresencePolicy.BusName), static (m, _) => m.GetBodyReader().ReadBool());
        }

        private Task CallAsync(string method)
        {
            return DBusCalls.CallAsync(_observer!, GnomeHubPresencePolicy.BusName, GnomeHubPresencePolicy.ObjectPath,
                GnomeHubPresencePolicy.Interface, method);
        }

        [LinuxBusFact]
        public async Task Start_owns_the_presence_name_and_installs_the_extension()
        {
            _presence!.Start();

            _ = (await EventuallyAsync(OwnedAsync)).Should().BeTrue();
            string js = Path.Combine(GnomeOsdGuardExtension.InstallDirectory(_dataHome!), "extension.js");
            _ = (await File.ReadAllTextAsync(js)).Should().Be(GnomeOsdGuardExtension.ExtensionJs);
        }

        [LinuxBusFact]
        public async Task The_panel_menus_Open_and_Quit_reach_the_Host()
        {
            _presence!.Start();
            _ = (await EventuallyAsync(OwnedAsync)).Should().BeTrue();

            await CallAsync(GnomeHubPresencePolicy.ShowHubMethod);
            await CallAsync(GnomeHubPresencePolicy.QuitMethod);

            _ = (await EventuallyAsync(() => Task.FromResult(_shown == 1 && _quit == 1))).Should().BeTrue();
        }

        [LinuxBusFact]
        public async Task The_panel_menu_reads_the_modules_with_their_state()
        {
            _presence!.Start();
            _ = (await EventuallyAsync(OwnedAsync)).Should().BeTrue();

            List<TrayModule> modules = await DBusCalls.CallAsync(_observer!, GnomeHubPresencePolicy.BusName,
                GnomeHubPresencePolicy.ObjectPath, GnomeHubPresencePolicy.Interface, GnomeHubPresencePolicy.ModulesMethod,
                null, null, static (m, _) =>
                {
                    Reader r = m.GetBodyReader();
                    List<TrayModule> list = [];
                    ArrayEnd end = r.ReadArrayStart(DBusType.Struct);
                    while (r.HasNext(end))
                    {
                        r.AlignStruct();
                        list.Add(new TrayModule(r.ReadString(), r.ReadString(), r.ReadBool()));
                    }

                    return list;
                });

            _ = modules.Should().Equal(new TrayModule("Tessera", "Tessera", true), new TrayModule("Acme", "Acme Clock", false));
        }

        [LinuxBusFact]
        public async Task A_panel_menu_switch_reaches_the_Host()
        {
            _presence!.Start();
            _ = (await EventuallyAsync(OwnedAsync)).Should().BeTrue();

            await DBusCalls.CallAsync(_observer!, GnomeHubPresencePolicy.BusName, GnomeHubPresencePolicy.ObjectPath,
                GnomeHubPresencePolicy.Interface, GnomeHubPresencePolicy.SetModuleOnMethod, "sb", (ref w) =>
                {
                    w.WriteString("Acme");
                    w.WriteBool(true);
                });

            lock (_switched)
            {
                _ = _switched.Should().Equal([("Acme", true)]);
            }
        }

        [LinuxBusFact]
        public async Task Disposing_gives_the_name_up_so_the_icon_goes()
        {
            _presence!.Start();
            _ = (await EventuallyAsync(OwnedAsync)).Should().BeTrue();

            _presence.Dispose();

            _ = (await EventuallyAsync(async () => !await OwnedAsync())).Should().BeTrue();
        }

        private sealed class QuietGnomeSettings : IGnomeSettings
        {
            public string? Get(string schema, string key)
            {
                return key == GnomeOsdGuardExtension.EnabledKey ? "@as []" : "false";
            }

            public bool Set(string schema, string key, string value)
            {
                return true;
            }
        }
    }
}
