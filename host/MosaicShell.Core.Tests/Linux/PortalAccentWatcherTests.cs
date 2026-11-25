using FluentAssertions;
using MosaicShell.Core.Services.Linux;
using MosaicShell.Core.Services.Xdg;
using Tmds.DBus.Protocol;
using static MosaicShell.Core.Tests.TestWaits;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary>
    /// The desktop's accent colour on Linux from the XDG desktop portal's <c>org.freedesktop.appearance accent-color</c>,
    /// as read on taizo: (ddd) 0.207843 0.517647 0.894118 from GNOME (#3584E4) and 0.741176 0.576471 0.976471 from
    /// Plasma (#BD93F9). Until now Linux always used the Windows fallback blue, since the system accent came only from
    /// DwmGetColorizationColor.
    /// </summary>
    public sealed class PortalAccentWatcherTests : IAsyncLifetime
    {
        private PrivateDBus? _dbus;
        private FakePortalSettings? _portal;
        private PortalAccentWatcher? _watcher;
        private int _changes;

        public async Task InitializeAsync()
        {
            if (new LinuxBusFactAttribute().Skip is not null)
            {
                return;
            }

            _dbus = PrivateDBus.Start();
            _portal = await FakePortalSettings.StartAsync(_dbus.Address, (0.207843, 0.517647, 0.894118));
            _watcher = new PortalAccentWatcher(_dbus.Address);
            _watcher.Changed += (_, _) => Interlocked.Increment(ref _changes);
        }

        public Task DisposeAsync()
        {
            _watcher?.Dispose();
            _portal?.Dispose();
            _dbus?.Dispose();
            return Task.CompletedTask;
        }

        private async Task StartAndWaitAsync()
        {
            _watcher!.Start();
            _ = (await EventuallyAsync(() => _watcher.IsListening)).Should().BeTrue("the change watch should be in place");
        }

        [Theory]
        [InlineData(0.207843, 0.517647, 0.894118, 0x35, 0x84, 0xE4)]
        [InlineData(0.741176, 0.576471, 0.976471, 0xBD, 0x93, 0xF9)]
        [InlineData(0.0, 1.0, 0.5, 0, 255, 128)]
        public void Portal_doubles_become_bytes(double r, double g, double b, int er, int eg, int eb)
        {
            _ = PortalAccentPolicy.FromRgb(r, g, b).Should().Be(((byte)er, (byte)eg, (byte)eb));
        }

        [Theory]
        [InlineData(-1.0, 0.5, 0.5)]
        [InlineData(0.5, 1.5, 0.5)]
        [InlineData(double.NaN, 0.5, 0.5)]
        public void Values_outside_zero_to_one_mean_the_desktop_sets_no_accent(double r, double g, double b)
        {
            _ = PortalAccentPolicy.FromRgb(r, g, b).Should().BeNull();
        }

        [Fact]
        public void Only_the_appearance_accent_is_the_accent()
        {
            _ = PortalAccentPolicy.Touches("org.freedesktop.appearance", "accent-color").Should().BeTrue();
            _ = PortalAccentPolicy.Touches("org.freedesktop.appearance", "color-scheme").Should().BeFalse();
            _ = PortalAccentPolicy.Touches("org.gnome.desktop.interface", "accent-color").Should().BeFalse();
        }

        [LinuxBusFact]
        public async Task Start_reads_the_current_accent()
        {
            await StartAndWaitAsync();

            _ = (await EventuallyAsync(() => _watcher!.Current is not null)).Should().BeTrue();
            _ = _watcher!.Current.Should().Be(((byte)0x35, (byte)0x84, (byte)0xE4));
        }

        [LinuxBusFact]
        public async Task A_change_on_the_desktop_follows_and_raises()
        {
            await StartAndWaitAsync();
            _ = await EventuallyAsync(() => _watcher!.Current is not null);
            int before = Volatile.Read(ref _changes);

            _portal!.Change(0.741176, 0.576471, 0.976471);

            _ = (await EventuallyAsync(() => _watcher!.Current == (0xBD, 0x93, 0xF9))).Should().BeTrue();
            _ = Volatile.Read(ref _changes).Should().BeGreaterThan(before);
        }

        [LinuxBusFact]
        public async Task Other_settings_changing_leave_the_accent_alone()
        {
            await StartAndWaitAsync();
            _ = await EventuallyAsync(() => _watcher!.Current is not null);

            _portal!.ChangeOther("org.freedesktop.appearance", "color-scheme");
            _portal.Change(0.0, 1.0, 0.5);

            _ = (await EventuallyAsync(() => _watcher!.Current == (0, 255, 128))).Should().BeTrue();
        }

        [LinuxBusFact]
        public async Task An_accent_the_desktop_clears_falls_back()
        {
            await StartAndWaitAsync();
            _ = await EventuallyAsync(() => _watcher!.Current is not null);

            _portal!.Change(-1, -1, -1);

            _ = (await EventuallyAsync(() => _watcher!.Current is null)).Should().BeTrue();
        }

        [LinuxBusFact]
        public async Task A_version_one_portal_without_ReadOne_is_read_through_Read()
        {
            _portal!.SupportsReadOne = false;

            await StartAndWaitAsync();

            _ = (await EventuallyAsync(() => _watcher!.Current is not null)).Should().BeTrue();
            _ = _watcher!.Current.Should().Be(((byte)0x35, (byte)0x84, (byte)0xE4));
        }

        [LinuxBusFact]
        public async Task Without_a_bus_there_is_no_accent_and_nothing_throws()
        {
            using PortalAccentWatcher watcher = new(address: "");

            watcher.Start();
            await Task.Delay(100);

            _ = watcher.Current.Should().BeNull();
            _ = watcher.IsListening.Should().BeFalse();
        }

        /// <summary>The portal frontend's org.freedesktop.portal.Settings: ReadOne, Read (version 1) and SettingChanged.</summary>
        private sealed class FakePortalSettings : IPathMethodHandler, IDisposable
        {
            private readonly DBusConnection _connection;
            private (double R, double G, double B) _accent;

            private FakePortalSettings(DBusConnection connection, (double, double, double) accent)
            {
                _connection = connection;
                _accent = accent;
            }

            public bool SupportsReadOne { get; set; } = true;

            public string Path => PortalAccentPolicy.ObjectPath;
            public bool HandlesChildPaths => false;

            public static async Task<FakePortalSettings> StartAsync(string address, (double, double, double) accent)
            {
                DBusConnection connection = new(address);
                await connection.ConnectAsync();
                FakePortalSettings fake = new(connection, accent);
                connection.AddMethodHandler(fake);
                await connection.RequestNameAsync(PortalAccentPolicy.BusName);
                return fake;
            }

            private static VariantValue Rgb((double R, double G, double B) c)
            {
                return VariantValue.Struct(VariantValue.Double(c.R), VariantValue.Double(c.G), VariantValue.Double(c.B));
            }

            public void Change(double r, double g, double b)
            {
                _accent = (r, g, b);
                Emit(PortalAccentPolicy.Namespace, PortalAccentPolicy.Key, Rgb(_accent));
            }

            public void ChangeOther(string ns, string key)
            {
                Emit(ns, key, VariantValue.UInt32(1));
            }

            private void Emit(string ns, string key, VariantValue value)
            {
                using MessageWriter w = _connection.GetMessageWriter();
                w.WriteSignalHeader(null, PortalAccentPolicy.ObjectPath, PortalAccentPolicy.Interface, PortalAccentPolicy.SettingChangedMember, "ssv");
                w.WriteString(ns);
                w.WriteString(key);
                w.WriteVariant(value);
                _ = _connection.TrySendMessage(w.CreateMessage());
            }

            public ValueTask HandleMethodAsync(MethodContext context)
            {
                string member = context.Request.MemberAsString ?? "";
                Reader body = context.Request.GetBodyReader();
                string ns = body.ReadString();
                string key = body.ReadString();
                bool accent = ns == PortalAccentPolicy.Namespace && key == PortalAccentPolicy.Key;
                if (member == "ReadOne" && SupportsReadOne && accent)
                {
                    using MessageWriter w = context.CreateReplyWriter("v");
                    w.WriteVariant(Rgb(_accent));
                    context.Reply(w.CreateMessage());
                    return ValueTask.CompletedTask;
                }

                if (member == "Read" && accent)
                {
                    // Version 1 wraps the value in one more variant.
                    using MessageWriter w = context.CreateReplyWriter("v");
                    w.WriteVariant(VariantValue.Variant(Rgb(_accent)));
                    context.Reply(w.CreateMessage());
                    return ValueTask.CompletedTask;
                }

                context.ReplyUnknownMethodError();
                return ValueTask.CompletedTask;
            }

            public void Dispose()
            {
                _connection.Dispose();
            }
        }
    }
}
