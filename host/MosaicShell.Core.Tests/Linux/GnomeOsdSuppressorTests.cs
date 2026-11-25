using FluentAssertions;
using MosaicShell.Core.Services.Gnome;
using MosaicShell.Core.Services.Kde;
using MosaicShell.Core.Services.Linux;
using Tmds.DBus.Protocol;
using static MosaicShell.Core.Tests.TestWaits;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary>
    /// <see cref="GnomeOsdSuppressor"/> against a private bus, a throwaway data folder and a stand-in for gsettings:
    /// Start installs and enables the extension and owns the guard name; Stop gives the name up, so Shell's pop-ups
    /// come back.
    /// </summary>
    public sealed class GnomeOsdSuppressorTests : IAsyncLifetime
    {
        private PrivateDBus? _dbus;
        private DBusConnection? _observer;
        private FakeGnomeSettings? _settings;
        private GnomeOsdSuppressor? _suppressor;
        private string? _dataHome;

        public async Task InitializeAsync()
        {
            if (new LinuxBusFactAttribute().Skip is not null)
            {
                return;
            }

            _dataHome = Directory.CreateTempSubdirectory("mosaic-gnome-ext-").FullName;
            _dbus = PrivateDBus.Start();
            _observer = new DBusConnection(_dbus.Address);
            await _observer.ConnectAsync();
            _settings = new FakeGnomeSettings();
            _suppressor = new GnomeOsdSuppressor(_dbus.Address, _dataHome, _settings);
        }

        public Task DisposeAsync()
        {
            _suppressor?.Dispose();
            _observer?.Dispose();
            _dbus?.Dispose();
            if (_dataHome is not null)
            {
                Directory.Delete(_dataHome, recursive: true);
            }

            return Task.CompletedTask;
        }

        private async Task<bool> GuardOwnedAsync()
        {
            MessageBuffer message;
            MessageWriter writer = _observer!.GetMessageWriter();
            try
            {
                writer.WriteMethodCallHeader("org.freedesktop.DBus", "/org/freedesktop/DBus", "org.freedesktop.DBus", "NameHasOwner", "s");
                writer.WriteString(KWinOsdScriptPolicy.GuardBusName);
                message = writer.CreateMessage();
            }
            finally
            {
                writer.Dispose();
            }

            return await _observer.CallMethodAsync(message, static (m, _) => m.GetBodyReader().ReadBool());
        }

        [LinuxBusFact]
        public async Task Start_installs_the_extension_files()
        {
            _suppressor!.Start();

            string dir = GnomeOsdGuardExtension.InstallDirectory(_dataHome!);
            _ = (await EventuallyAsync(() => Task.FromResult(File.Exists(Path.Combine(dir, "extension.js"))))).Should().BeTrue();
            _ = (await File.ReadAllTextAsync(Path.Combine(dir, "extension.js"))).Should().Be(GnomeOsdGuardExtension.ExtensionJs);
            _ = (await File.ReadAllTextAsync(Path.Combine(dir, "metadata.json"))).Should().Be(GnomeOsdGuardExtension.MetadataJson);
        }

        [LinuxBusFact]
        public async Task Start_enables_the_extension_once()
        {
            _suppressor!.Start();
            _ = await EventuallyAsync(() => Task.FromResult(_settings!.Writes.Count == 1));

            _ = _settings!.EnabledExtensions.Should().Be("['" + GnomeOsdGuardExtension.Uuid + "']");

            _suppressor.Stop();
            _suppressor.Start();
            await Task.Delay(200);
            _ = _settings.Writes.Should().HaveCount(1, "it is already in the list");
        }

        [LinuxBusFact]
        public async Task The_guard_name_is_owned_while_started_and_given_up_on_Stop()
        {
            _suppressor!.Start();
            _ = (await EventuallyAsync(GuardOwnedAsync)).Should().BeTrue();
            _ = (await EventuallyAsync(() => Task.FromResult(_suppressor.IsActive))).Should().BeTrue();

            _suppressor.Stop();

            _ = (await EventuallyAsync(async () => !await GuardOwnedAsync())).Should().BeTrue();
            _ = _suppressor.IsActive.Should().BeFalse();
        }

        [LinuxBusFact]
        public async Task With_user_extensions_turned_off_it_installs_but_does_not_switch_them_on()
        {
            _settings!.UserExtensionsDisabled = true;

            _suppressor!.Start();
            _ = await EventuallyAsync(GuardOwnedAsync);
            await Task.Delay(100);

            _ = _settings.Writes.Should().BeEmpty("the user turned extensions off; MosaicShell does not overrule that");
        }

        [LinuxBusFact]
        public void Without_a_bus_nothing_throws()
        {
            using GnomeOsdSuppressor suppressor = new("", _dataHome!, new FakeGnomeSettings());

            suppressor.Start();
            suppressor.SuppressOnce();
            suppressor.SuppressBurst();
            suppressor.Stop();

            _ = suppressor.IsActive.Should().BeFalse();
        }

        private sealed class FakeGnomeSettings : IGnomeSettings
        {
            public string EnabledExtensions { get; private set; } = "@as []";
            public bool UserExtensionsDisabled { get; set; }
            public List<string> Writes { get; } = [];

            public string? Get(string schema, string key)
            {
                return (schema, key) switch
                {
                    (GnomeOsdGuardExtension.ShellSchema, GnomeOsdGuardExtension.EnabledKey) => EnabledExtensions,
                    (GnomeOsdGuardExtension.ShellSchema, GnomeOsdGuardExtension.DisableUserExtensionsKey) => UserExtensionsDisabled ? "true" : "false",
                    _ => null,
                };
            }

            public bool Set(string schema, string key, string value)
            {
                Writes.Add($"{schema} {key} {value}");
                if (key == GnomeOsdGuardExtension.EnabledKey)
                {
                    EnabledExtensions = value;
                }

                return true;
            }
        }
    }
}
