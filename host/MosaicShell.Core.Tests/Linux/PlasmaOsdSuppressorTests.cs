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

        private async Task<FakeKWinScripting> StartWithKWinAsync()
        {
            FakeKWinScripting kwin = await FakeKWinScripting.StartAsync(_dbus!.Address);
            await StartAndWaitAsync();
            _ = (await EventuallyAsync(() => kwin.Calls().Contains("start"))).Should().BeTrue("the script should be started");
            return kwin;
        }

        private async Task<bool> GuardOwnedAsync()
        {
            MessageBuffer message;
            using (MessageWriter w = _caller!.GetMessageWriter())
            {
                w.WriteMethodCallHeader("org.freedesktop.DBus", "/org/freedesktop/DBus", "org.freedesktop.DBus", "NameHasOwner", "s");
                w.WriteString(KWinOsdScriptPolicy.GuardBusName);
                message = w.CreateMessage();
            }

            return await _caller.CallMethodAsync(message, static (m, _) => m.GetBodyReader().ReadBool());
        }

        private Task<bool> AskGuardAsync()
        {
            MessageBuffer message;
            using (MessageWriter w = _caller!.GetMessageWriter())
            {
                w.WriteMethodCallHeader(
                    KWinOsdScriptPolicy.GuardBusName, KWinOsdScriptPolicy.GuardPath, KWinOsdScriptPolicy.GuardInterface, KWinOsdScriptPolicy.GuardMethod);
                message = w.CreateMessage();
            }

            return _caller.CallMethodAsync(message, static (m, _) => m.GetBodyReader().ReadBool());
        }

        [LinuxBusFact]
        public async Task Start_replaces_any_stale_script_then_loads_and_starts_the_guard()
        {
            using FakeKWinScripting kwin = await StartWithKWinAsync();

            _ = kwin.Calls().Should().Equal(
                "unloadScript " + KWinOsdScriptPolicy.ScriptName,
                "loadScript " + KWinOsdScriptPolicy.ScriptName,
                "start");
            _ = kwin.LoadedScript.Should().Be(KWinOsdScriptPolicy.Script);
            _ = (await GuardOwnedAsync()).Should().BeTrue();
        }

        [LinuxBusFact]
        public async Task The_guard_keeps_a_volume_popup_hidden_but_not_a_layout_popup()
        {
            using FakeKWinScripting kwin = await StartWithKWinAsync();

            await VolumeAsync(40);
            _ = (await EventuallyAsync(() => _shell!.Calls().Contains("hide"))).Should().BeTrue("the volume call was seen");
            _ = (await AskGuardAsync()).Should().BeTrue();

            await KeyboardLayoutAsync();
            await VolumeAsync(41);
            await KeyboardLayoutAsync();
            // Calls from one sender arrive in order: two hides prove the last layout call was seen before asking.
            _ = (await EventuallyAsync(() => _shell!.Calls().Count(static c => c == "hide") >= 2)).Should().BeTrue();
            await Task.Delay(50);
            _ = (await AskGuardAsync()).Should().BeFalse();
        }

        [LinuxBusFact]
        public async Task Nothing_is_kept_hidden_before_any_popup()
        {
            using FakeKWinScripting kwin = await StartWithKWinAsync();

            _ = (await AskGuardAsync()).Should().BeFalse();
        }

        [LinuxBusFact]
        public async Task Stop_unloads_the_script_and_gives_up_the_guard()
        {
            using FakeKWinScripting kwin = await StartWithKWinAsync();

            _suppressor!.Stop();

            _ = (await EventuallyAsync(() => kwin.Calls().Count(static c => c.StartsWith("unloadScript", StringComparison.Ordinal)) == 2))
                .Should().BeTrue("Stop should unload the script");
            bool owned = true;
            DateTime until = DateTime.UtcNow + Patience;
            while (owned && DateTime.UtcNow < until)
            {
                owned = await GuardOwnedAsync();
                if (owned)
                {
                    await Task.Delay(20);
                }
            }

            _ = owned.Should().BeFalse("Stop should give up the guard name");
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

    /// <summary>
    /// KWin's /Scripting: records loadScript (with the script file's text), unloadScript and start, as KWin 6.6 names
    /// them, and answers each with what KWin returns.
    /// </summary>
    internal sealed class FakeKWinScripting : IPathMethodHandler, IDisposable
    {
        private readonly DBusConnection _connection;
        private readonly List<string> _calls = [];
#pragma warning disable IDE0032 // LoadedScript reads under the lock the D-Bus handler writes under; an auto property would not.
        private string? _loadedScript;
#pragma warning restore IDE0032

        private FakeKWinScripting(DBusConnection connection)
        {
            _connection = connection;
        }

        public string Path => KWinOsdScriptPolicy.ScriptingPath;
        public bool HandlesChildPaths => false;

        public string? LoadedScript
        {
            get
            {
                lock (_calls)
                {
                    return _loadedScript;
                }
            }
        }

        public static async Task<FakeKWinScripting> StartAsync(string address)
        {
            DBusConnection connection = new(address);
            await connection.ConnectAsync();
            FakeKWinScripting kwin = new(connection);
            connection.AddMethodHandler(kwin);
            await connection.RequestNameAsync(KWinOsdScriptPolicy.KWinBusName);
            return kwin;
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
            string member = context.Request.MemberAsString ?? "";
            Reader reader = context.Request.GetBodyReader();
            using MessageWriter w = context.CreateReplyWriter(member switch
            {
                "loadScript" => "i",
                "unloadScript" => "b",
                _ => null,
            });
            switch (member)
            {
                case "loadScript":
                    string file = reader.ReadString();
                    string name = reader.ReadString();
                    lock (_calls)
                    {
                        _calls.Add("loadScript " + name);
                        _loadedScript = File.ReadAllText(file);
                    }

                    w.WriteInt32(0);
                    break;
                case "unloadScript":
                    string unloaded = reader.ReadString();
                    lock (_calls)
                    {
                        _calls.Add("unloadScript " + unloaded);
                    }

                    w.WriteBool(true);
                    break;
                default:
                    lock (_calls)
                    {
                        _calls.Add(member);
                    }

                    break;
            }

            context.Reply(w.CreateMessage());
            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
