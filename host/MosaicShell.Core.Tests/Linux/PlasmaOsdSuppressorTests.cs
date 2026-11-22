using FluentAssertions;
using MosaicShell.Core.Services.Kde;
using MosaicShell.Core.Services.Linux;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary>
    /// <see cref="PlasmaOsdSuppressor"/> on a real dbus-daemon: a stand-in plasmashell exports /org/kde/osdService,
    /// a stand-in plasma-pa or PowerDevil calls it as they do on Plasma, and the suppressor must answer each volume or
    /// brightness pop-up with hide, and nothing else.
    /// </summary>
    public sealed class PlasmaOsdSuppressorTests : IAsyncLifetime
    {
        private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

        private PrivateDBus? _dbus;
        private FakeOsdService? _shell;
        private DBusConnection? _caller;
        private PlasmaOsdSuppressor? _suppressor;

        public async Task InitializeAsync()
        {
            if (new LinuxBusFactAttribute().Skip is not null)
            {
                return;
            }

            _dbus = PrivateDBus.Start();
            _shell = await FakeOsdService.StartAsync(_dbus.Address);
            _caller = new DBusConnection(_dbus.Address);
            await _caller.ConnectAsync();
            _suppressor = new PlasmaOsdSuppressor(_dbus.Address);
        }

        public Task DisposeAsync()
        {
            _suppressor?.Dispose();
            _caller?.Dispose();
            _shell?.Dispose();
            _dbus?.Dispose();
            return Task.CompletedTask;
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

        private async Task StartAndWaitAsync()
        {
            _suppressor!.Start();
            _ = (await EventuallyAsync(() => _suppressor.IsActive)).Should().BeTrue("the watch should be in place");
        }

        private Task VolumeAsync(int percent)
        {
            MessageBuffer message;
            using (MessageWriter w = _caller!.GetMessageWriter())
            {
                w.WriteMethodCallHeader(PlasmaOsdPolicy.BusName, PlasmaOsdPolicy.ObjectPath, PlasmaOsdPolicy.Interface, "volumeChanged", "ii");
                w.WriteInt32(percent);
                w.WriteInt32(100);
                message = w.CreateMessage();
            }

            return _caller.CallMethodAsync(message);
        }

        private Task KeyboardLayoutAsync()
        {
            MessageBuffer message;
            using (MessageWriter w = _caller!.GetMessageWriter())
            {
                w.WriteMethodCallHeader(PlasmaOsdPolicy.BusName, PlasmaOsdPolicy.ObjectPath, PlasmaOsdPolicy.Interface, "kbdLayoutChanged", "s");
                w.WriteString("us");
                message = w.CreateMessage();
            }

            return _caller.CallMethodAsync(message);
        }

        private Task BrightnessAsync(int percent)
        {
            MessageBuffer message;
            using (MessageWriter w = _caller!.GetMessageWriter())
            {
                w.WriteMethodCallHeader(PlasmaOsdPolicy.BusName, PlasmaOsdPolicy.ObjectPath, PlasmaOsdPolicy.Interface, "brightnessChanged", "i");
                w.WriteInt32(percent);
                message = w.CreateMessage();
            }

            return _caller.CallMethodAsync(message);
        }

        [LinuxBusFact]
        public async Task A_volume_popup_is_hidden_right_after_it_is_shown()
        {
            await StartAndWaitAsync();

            await VolumeAsync(54);

            _ = (await EventuallyAsync(() => _shell!.Calls().Contains("hide"))).Should().BeTrue();
            _ = _shell!.Calls().Should().Equal("volumeChanged", "hide");
        }

        [LinuxBusFact]
        public async Task A_brightness_popup_is_hidden_too()
        {
            await StartAndWaitAsync();

            await BrightnessAsync(35);

            _ = (await EventuallyAsync(() => _shell!.Calls().Contains("hide"))).Should().BeTrue();
        }

        [LinuxBusFact]
        public async Task Other_popups_are_left_alone()
        {
            await StartAndWaitAsync();

            await KeyboardLayoutAsync();
            await VolumeAsync(10);

            // Calls from one sender arrive in order, so the volume hide proves the layout call was already seen.
            _ = (await EventuallyAsync(() => _shell!.Calls().Contains("hide"))).Should().BeTrue();
            _ = _shell!.Calls().Should().Equal("kbdLayoutChanged", "volumeChanged", "hide");
        }

        [LinuxBusFact]
        public async Task Nothing_is_hidden_after_stop()
        {
            await StartAndWaitAsync();
            _suppressor!.Stop();
            _ = _suppressor.IsActive.Should().BeFalse();

            await VolumeAsync(20);
            await Task.Delay(300);

            _ = _shell!.Calls().Should().Equal("volumeChanged");
        }

        [LinuxBusFact]
        public async Task SuppressOnce_hides_whatever_is_showing()
        {
            await StartAndWaitAsync();

            _suppressor!.SuppressOnce();

            _ = (await EventuallyAsync(() => _shell!.Calls().Contains("hide"))).Should().BeTrue();
        }

        [LinuxBusFact]
        public async Task Without_a_bus_it_stays_inactive_and_never_throws()
        {
            string missing = "unix:path=" + Path.Combine(Path.GetTempPath(), "mosaic-no-bus-" + Guid.NewGuid().ToString("N"));
            using PlasmaOsdSuppressor suppressor = new(missing);

            suppressor.Start();
            suppressor.SuppressOnce();
            suppressor.SuppressBurst();
            await Task.Delay(300);

            _ = suppressor.IsActive.Should().BeFalse();
        }
    }

    /// <summary>plasmashell's /org/kde/osdService: records every call, answers each with an empty reply.</summary>
    internal sealed class FakeOsdService : IPathMethodHandler, IDisposable
    {
        private readonly DBusConnection _connection;
        private readonly List<string> _calls = [];

        private FakeOsdService(DBusConnection connection)
        {
            _connection = connection;
        }

        public string Path => PlasmaOsdPolicy.ObjectPath;
        public bool HandlesChildPaths => false;

        public static async Task<FakeOsdService> StartAsync(string address)
        {
            DBusConnection connection = new(address);
            await connection.ConnectAsync();
            FakeOsdService shell = new(connection);
            connection.AddMethodHandler(shell);
            await connection.RequestNameAsync(PlasmaOsdPolicy.BusName);
            return shell;
        }

        public string[] Calls()
        {
            lock (_calls)
            {
                return [.. _calls];
            }
        }

        public ValueTask HandleMethodAsync(MethodContext context)
        {
            lock (_calls)
            {
                _calls.Add(context.Request.MemberAsString ?? "");
            }

            using MessageWriter w = context.CreateReplyWriter(null);
            context.Reply(w.CreateMessage());
            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
