using FluentAssertions;
using MosaicShell.Core.Services.Kde;
using MosaicShell.Core.Services.Linux;
using Tmds.DBus.Protocol;
using static MosaicShell.Core.Tests.TestWaits;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary>
    /// <see cref="KdeScreenBrightness"/> on a real dbus-daemon against a stand-in PowerDevil with an external monitor
    /// listed first and the built-in panel second, as on the user's machine (2026-11-22).
    /// </summary>
    public sealed class KdeScreenBrightnessTests : IAsyncLifetime
    {
        private PrivateDBus? _dbus;
        private FakePowerDevil? _powerDevil;
        private KdeScreenBrightness? _brightness;
        private int _changes;

        public async Task InitializeAsync()
        {
            if (new LinuxBusFactAttribute().Skip is not null)
            {
                return;
            }

            _dbus = PrivateDBus.Start();
            _powerDevil = await FakePowerDevil.StartAsync(_dbus.Address,
                new KdeDisplay("display0", IsInternal: false, Brightness: 8500, MaxBrightness: 10000),
                new KdeDisplay("display1", IsInternal: true, Brightness: 4500, MaxBrightness: 10000));
        }

        public Task DisposeAsync()
        {
            _brightness?.Dispose();
            _powerDevil?.Dispose();
            _dbus?.Dispose();
            return Task.CompletedTask;
        }

        private async Task<KdeScreenBrightness> StartAsync()
        {
            _brightness = new KdeScreenBrightness(_dbus!.Address);
            _brightness.Changed += (_, _) => Interlocked.Increment(ref _changes);
            _brightness.Start();
            _ = (await EventuallyAsync(() => _brightness.IsSupported)).Should().BeTrue();
            return _brightness;
        }

        private int Changes => Volatile.Read(ref _changes);

        [LinuxBusFact]
        public async Task Reads_the_built_in_panel_even_when_it_is_listed_second()
        {
            KdeScreenBrightness brightness = await StartAsync();

            _ = brightness.Brightness.Should().BeApproximately(0.45, 1e-9);
        }

        [LinuxBusFact]
        public async Task Setting_moves_every_display_by_the_same_step_without_plasmas_popup()
        {
            KdeScreenBrightness brightness = await StartAsync();

            brightness.Brightness = 0.6;

            _ = brightness.Brightness.Should().BeApproximately(0.6, 1e-9, "the slider must not jump back");
            _ = (await EventuallyAsync(() => _powerDevil!.Adjustments().Count == 1)).Should().BeTrue();
            (double delta, uint flags) = _powerDevil!.Adjustments()[0];
            _ = delta.Should().BeApproximately(0.15, 1e-9);
            _ = flags.Should().Be(KdeScreenBrightnessPolicy.SuppressIndicatorFlag);
        }

        [LinuxBusFact]
        public async Task A_change_from_the_keys_updates_the_value_and_raises_Changed()
        {
            KdeScreenBrightness brightness = await StartAsync();

            _powerDevil!.Change("display1", 3000);

            _ = (await EventuallyAsync(() => Changes == 1)).Should().BeTrue();
            _ = brightness.Brightness.Should().BeApproximately(0.3, 1e-9);
        }

        [LinuxBusFact]
        public async Task A_change_on_another_display_alone_is_not_a_change_of_the_reference()
        {
            KdeScreenBrightness brightness = await StartAsync();

            _powerDevil!.Change("display0", 2000);
            _powerDevil.Change("display1", 4600);

            _ = (await EventuallyAsync(() => Changes >= 1)).Should().BeTrue();
            await Task.Delay(100);
            _ = Changes.Should().Be(1);
            _ = brightness.Brightness.Should().BeApproximately(0.46, 1e-9);
        }

        [LinuxBusFact]
        public async Task No_Changed_before_Start_or_after_Stop()
        {
            KdeScreenBrightness brightness = await StartAsync();
            brightness.Stop();

            _powerDevil!.Change("display1", 2000);

            _ = (await EventuallyAsync(() => Math.Abs(brightness.Brightness - 0.2) < 1e-9)).Should().BeTrue(
                "the value stays current for the flyout even when nobody listens");
            _ = Changes.Should().Be(0);
        }

        [LinuxBusFact]
        public async Task Losing_the_panel_picks_the_next_display()
        {
            KdeScreenBrightness brightness = await StartAsync();

            _powerDevil!.Remove("display1");

            _ = (await EventuallyAsync(() => Math.Abs(brightness.Brightness - 0.85) < 1e-9)).Should().BeTrue();
        }

        [LinuxBusFact]
        public async Task Without_powerdevil_brightness_is_unsupported_and_nothing_throws()
        {
            _powerDevil!.Dispose();
            _powerDevil = null;
            using KdeScreenBrightness brightness = new(_dbus!.Address);

            brightness.Start();
            brightness.Brightness = 0.5;
            await Task.Delay(300);

            _ = brightness.IsSupported.Should().BeFalse();
            _ = brightness.Brightness.Should().Be(0);
        }
    }

    /// <summary>
    /// PowerDevil's org.kde.ScreenBrightness: the display list, each display's properties, AdjustBrightnessRatio (which
    /// moves every display and signals each), and the BrightnessChanged, DisplayAdded and DisplayRemoved signals.
    /// </summary>
    internal sealed class FakePowerDevil : IPathMethodHandler, IDisposable
    {
        private readonly DBusConnection _connection;
        private readonly Lock _gate = new();
        private readonly List<KdeDisplay> _displays;
        private readonly List<(double Delta, uint Flags)> _adjustments = [];

        private FakePowerDevil(DBusConnection connection, IEnumerable<KdeDisplay> displays)
        {
            _connection = connection;
            _displays = [.. displays];
        }

        public string Path => KdeScreenBrightnessPolicy.ObjectPath;
        public bool HandlesChildPaths => true;

        public static async Task<FakePowerDevil> StartAsync(string address, params KdeDisplay[] displays)
        {
            DBusConnection connection = new(address);
            await connection.ConnectAsync();
            FakePowerDevil fake = new(connection, displays);
            connection.AddMethodHandler(fake);
            await connection.RequestNameAsync(KdeScreenBrightnessPolicy.BusName);
            return fake;
        }

        public IReadOnlyList<(double Delta, uint Flags)> Adjustments()
        {
            lock (_gate)
            {
                return [.. _adjustments];
            }
        }

        /// <summary>A brightness change from elsewhere (the keys), signalled as PowerDevil does.</summary>
        public void Change(string name, int brightness)
        {
            KdeDisplay display;
            lock (_gate)
            {
                int i = _displays.FindIndex(d => d.DbusName == name);
                display = _displays[i] = _displays[i] with { Brightness = brightness };
            }

            EmitBrightnessChanged(display);
        }

        public void Remove(string name)
        {
            lock (_gate)
            {
                _ = _displays.RemoveAll(d => d.DbusName == name);
            }

            using MessageWriter w = _connection.GetMessageWriter();
            w.WriteSignalHeader(null, KdeScreenBrightnessPolicy.ObjectPath, KdeScreenBrightnessPolicy.Interface, "DisplayRemoved", "s");
            w.WriteString(name);
            _ = _connection.TrySendMessage(w.CreateMessage());
        }

        private void EmitBrightnessChanged(KdeDisplay display)
        {
            using MessageWriter w = _connection.GetMessageWriter();
            w.WriteSignalHeader(null, KdeScreenBrightnessPolicy.ObjectPath, KdeScreenBrightnessPolicy.Interface, "BrightnessChanged", "siss");
            w.WriteString(display.DbusName);
            w.WriteInt32(display.Brightness);
            w.WriteString(display.IsInternal ? "(internal)" : "External");
            w.WriteString("brightness_key");
            _ = _connection.TrySendMessage(w.CreateMessage());
        }

        public ValueTask HandleMethodAsync(MethodContext context)
        {
            Message request = context.Request;
            string path = request.PathAsString ?? "";
            string member = request.MemberAsString ?? "";
            Reader body = request.GetBodyReader();
            lock (_gate)
            {
                if (path == KdeScreenBrightnessPolicy.ObjectPath && member == "Get")
                {
                    _ = body.ReadString();
                    string property = body.ReadString();
                    if (property != "DisplaysDBusNames")
                    {
                        context.ReplyError("org.freedesktop.DBus.Error.UnknownProperty", property);
                        return ValueTask.CompletedTask;
                    }

                    using MessageWriter w = context.CreateReplyWriter("v");
                    w.WriteVariant(VariantValue.Array(_displays.Select(static d => d.DbusName).ToArray()));
                    context.Reply(w.CreateMessage());
                    return ValueTask.CompletedTask;
                }

                if (path == KdeScreenBrightnessPolicy.ObjectPath && member == "AdjustBrightnessRatio")
                {
                    double delta = body.ReadDouble();
                    uint flags = body.ReadUInt32();
                    _adjustments.Add((delta, flags));
                    using MessageWriter w = context.CreateReplyWriter(null);
                    context.Reply(w.CreateMessage());
                    return ValueTask.CompletedTask;
                }

                KdeDisplay? display = _displays.FirstOrDefault(d => KdeScreenBrightnessPolicy.DisplayPath(d.DbusName) == path);
                if (display is not null && member == "GetAll")
                {
                    using MessageWriter w = context.CreateReplyWriter("a{sv}");
                    ArrayStart dict = w.WriteDictionaryStart();
                    w.WriteDictionaryEntryStart();
                    w.WriteString("Brightness");
                    w.WriteVariant(VariantValue.Int32(display.Brightness));
                    w.WriteDictionaryEntryStart();
                    w.WriteString("MaxBrightness");
                    w.WriteVariant(VariantValue.Int32(display.MaxBrightness));
                    w.WriteDictionaryEntryStart();
                    w.WriteString("IsInternal");
                    w.WriteVariant(VariantValue.Bool(display.IsInternal));
                    w.WriteDictionaryEnd(dict);
                    context.Reply(w.CreateMessage());
                    return ValueTask.CompletedTask;
                }
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
