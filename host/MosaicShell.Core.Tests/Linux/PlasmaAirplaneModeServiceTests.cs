using System.Text;
using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Kde;
using MosaicShell.Core.Services.Linux;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary>
    /// <see cref="PlasmaAirplaneModeService"/> against a real dbus-daemon and a throwaway plasma-nm file, with a stand-in
    /// for plasma-nm that writes the file and then sends KConfig's ConfigChanged in the shape captured live on Plasma 6:
    /// path /plasma_nm, body a{saay} of {"General": ["AirplaneModeEnabled"]}.
    /// </summary>
    public sealed class PlasmaAirplaneModeServiceTests : IAsyncLifetime
    {
        private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

        private PrivateDBus? _dbus;
        private FakePlasmaNm? _plasma;
        private FakeRadios? _radios;
        private PlasmaAirplaneModeService? _service;
        private string? _dir;
        private int _changes;

        private string ConfigFile => Path.Combine(_dir!, PlasmaAirplanePolicy.ConfigFileName);

        public async Task InitializeAsync()
        {
            if (new LinuxBusFactAttribute().Skip is not null)
            {
                return;
            }

            _dir = Directory.CreateTempSubdirectory("mosaic-plasma-nm-").FullName;
            _dbus = PrivateDBus.Start();
            _plasma = await FakePlasmaNm.StartAsync(_dbus.Address);
            _radios = new FakeRadios { IsSupported = true };
            _service = new PlasmaAirplaneModeService(_radios, ConfigFile, _dbus.Address);
            _service.Changed += (_, _) => Interlocked.Increment(ref _changes);
        }

        public Task DisposeAsync()
        {
            _service?.Dispose();
            _plasma?.Dispose();
            _dbus?.Dispose();
            if (_dir is not null)
            {
                Directory.Delete(_dir, recursive: true);
            }

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

        private void WriteState(bool on)
        {
            // plasma-nm's file as found on taizo, written whole and renamed in as KConfig does.
            string temp = ConfigFile + ".tmp";
            File.WriteAllText(temp, "[General]\nAirplaneModeEnabled=" + (on ? "true" : "false") + "\nHotspotName=x\n");
            File.Move(temp, ConfigFile, overwrite: true);
        }

        private async Task StartAndWaitAsync()
        {
            _service!.Start();
            _ = (await EventuallyAsync(() => _service.IsListening)).Should().BeTrue("the signal watch should be in place");
        }

        /// <summary>Signals from one sender arrive in order: once this toggle is seen, everything sent before it was handled.</summary>
        private async Task FlushWithToggleAsync(int changesBefore)
        {
            bool target = !_service!.IsEnabled;
            WriteState(target);
            _plasma!.Notify(PlasmaAirplanePolicy.Group, PlasmaAirplanePolicy.Key);
            _ = (await EventuallyAsync(() => Volatile.Read(ref _changes) == changesBefore + 1)).Should().BeTrue();
            _ = _service.IsEnabled.Should().Be(target);
        }

        [LinuxBusFact]
        public async Task Start_takes_the_current_state_without_raising()
        {
            WriteState(on: true);

            await StartAndWaitAsync();

            _ = _service!.IsEnabled.Should().BeTrue();
            _ = Volatile.Read(ref _changes).Should().Be(0);
        }

        [LinuxBusFact]
        public async Task Turning_airplane_mode_on_and_off_in_Plasma_raises_each_flip()
        {
            WriteState(on: false);
            await StartAndWaitAsync();

            WriteState(on: true);
            _plasma!.Notify(PlasmaAirplanePolicy.Group, PlasmaAirplanePolicy.Key);
            _ = (await EventuallyAsync(() => Volatile.Read(ref _changes) == 1)).Should().BeTrue();
            _ = _service!.IsEnabled.Should().BeTrue();

            WriteState(on: false);
            _plasma.Notify(PlasmaAirplanePolicy.Group, PlasmaAirplanePolicy.Key);
            _ = (await EventuallyAsync(() => Volatile.Read(ref _changes) == 2)).Should().BeTrue();
            _ = _service.IsEnabled.Should().BeFalse();
        }

        [LinuxBusFact]
        public async Task A_change_to_another_key_group_or_file_is_not_a_toggle()
        {
            WriteState(on: false);
            await StartAndWaitAsync();

            // The file says on, but no signal names the airplane key, so it is not re-read.
            WriteState(on: true);
            _plasma!.Notify(PlasmaAirplanePolicy.Group, "HotspotName");
            _plasma.Notify("Other", PlasmaAirplanePolicy.Key);
            _plasma.Notify(PlasmaAirplanePolicy.Group, PlasmaAirplanePolicy.Key, path: "/kdeglobals");
            await Task.Delay(200);
            _ = Volatile.Read(ref _changes).Should().Be(0);
            _ = _service!.IsEnabled.Should().BeFalse();
        }

        [LinuxBusFact]
        public async Task A_signal_that_leaves_the_state_as_it_was_raises_nothing()
        {
            WriteState(on: true);
            await StartAndWaitAsync();

            _plasma!.Notify(PlasmaAirplanePolicy.Group, PlasmaAirplanePolicy.Key);
            await FlushWithToggleAsync(changesBefore: 0);

            _ = Volatile.Read(ref _changes).Should().Be(1, "only the flush's own flip counts");
        }

        [LinuxBusFact]
        public async Task No_file_is_off_until_Plasma_first_writes_it()
        {
            await StartAndWaitAsync();
            _ = _service!.IsEnabled.Should().BeFalse();

            WriteState(on: true);
            _plasma!.Notify(PlasmaAirplanePolicy.Group, PlasmaAirplanePolicy.Key);

            _ = (await EventuallyAsync(() => _service.IsEnabled)).Should().BeTrue();
        }

        [LinuxBusFact]
        public async Task Support_follows_the_radios_which_start_and_stop_with_it()
        {
            await StartAndWaitAsync();
            _ = _radios!.Started.Should().BeTrue();
            _ = _service!.IsSupported.Should().BeTrue();

            _radios.IsSupported = false;
            _ = _service.IsSupported.Should().BeFalse("without a radio there is no airplane mode to show, as on Windows");

            _service.Stop();
            _ = _radios.Started.Should().BeFalse();
        }

        [LinuxBusFact]
        public async Task A_radio_change_alone_is_not_a_toggle()
        {
            WriteState(on: false);
            await StartAndWaitAsync();

            _radios!.Raise();
            await Task.Delay(100);

            _ = Volatile.Read(ref _changes).Should().Be(0, "Plasma's key is the state, not the radios");
        }

        [LinuxBusFact]
        public async Task Stop_silences_the_watch_and_Start_resumes_it()
        {
            WriteState(on: false);
            await StartAndWaitAsync();
            _service!.Stop();
            _ = _service.IsListening.Should().BeFalse();

            WriteState(on: true);
            _plasma!.Notify(PlasmaAirplanePolicy.Group, PlasmaAirplanePolicy.Key);
            await Task.Delay(200);
            _ = Volatile.Read(ref _changes).Should().Be(0);

            await StartAndWaitAsync();
            _ = _service.IsEnabled.Should().BeTrue("Start reads the state again");
            await FlushWithToggleAsync(changesBefore: 0);
        }

        [LinuxBusFact]
        public void Without_a_bus_it_still_reads_the_file_and_never_throws()
        {
            WriteState(on: true);
            using PlasmaAirplaneModeService service = new(new FakeRadios(), ConfigFile, address: "");

            service.Start();

            _ = service.IsEnabled.Should().BeTrue();
            _ = service.IsListening.Should().BeFalse();
        }

        /// <summary>The radio source the service asks whether there is anything to switch off.</summary>
        private sealed class FakeRadios : IAirplaneModeService
        {
            public bool IsSupported { get; set; }

            public bool IsEnabled => false;

            public bool Started { get; private set; }

            public event EventHandler? Changed;

            public void Raise()
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }

            public void Start()
            {
                Started = true;
            }

            public void Stop()
            {
                Started = false;
            }

            public void Dispose()
            {
                Stop();
            }
        }

        /// <summary>Sends KConfig's change signal as plasmashell does after rewriting plasma-nm.</summary>
        private sealed class FakePlasmaNm : IDisposable
        {
            private readonly DBusConnection _connection;

            private FakePlasmaNm(DBusConnection connection)
            {
                _connection = connection;
            }

            public static async Task<FakePlasmaNm> StartAsync(string address)
            {
                DBusConnection connection = new(address);
                await connection.ConnectAsync();
                return new FakePlasmaNm(connection);
            }

            public void Notify(string group, string key, string path = PlasmaAirplanePolicy.NotifyPath)
            {
                using MessageWriter w = _connection.GetMessageWriter();
                w.WriteSignalHeader(null, path, PlasmaAirplanePolicy.NotifyInterface, PlasmaAirplanePolicy.NotifyMember, "a{saay}");
                ArrayStart dict = w.WriteDictionaryStart();
                w.WriteDictionaryEntryStart();
                w.WriteString(group);
                ArrayStart keys = w.WriteArrayStart(DBusType.Array);
                w.WriteArray(Encoding.UTF8.GetBytes(key));
                w.WriteArrayEnd(keys);
                w.WriteDictionaryEnd(dict);
                _ = _connection.TrySendMessage(w.CreateMessage());
            }

            public void Dispose()
            {
                _connection.Dispose();
            }
        }
    }
}
