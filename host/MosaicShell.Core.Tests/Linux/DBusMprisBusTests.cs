using FluentAssertions;
using MosaicShell.Core.Services.Linux;
using MosaicShell.Core.Services.Mpris;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary>
    /// <see cref="DBusMprisBus"/> against a real dbus-daemon and a player exported over the wire, so name ownership,
    /// signal routing from unique names, property encodings and command arguments are all exercised for real.
    /// </summary>
    public sealed class DBusMprisBusTests : IAsyncLifetime
    {
        private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

        private PrivateDBus? _dbus;
        private FakeMprisPlayer? _player;
        private DBusMprisBus? _bus;

        public async Task InitializeAsync()
        {
            if (new LinuxBusFactAttribute().Skip is not null)
            {
                return;
            }

            _dbus = PrivateDBus.Start();
            _player = new FakeMprisPlayer("fake.instance42");
            _player.SetMetadata("Song One", "Band", 200_000_000);
            await _player.PublishAsync(_dbus.Address);
            _bus = new DBusMprisBus(_dbus.Address);
            await _bus.ConnectAsync();
        }

        public async Task DisposeAsync()
        {
            _bus?.Dispose();
            if (_player is not null)
            {
                await _player.DisposeAsync();
            }

            _dbus?.Dispose();
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

        [LinuxBusFact]
        public async Task Lists_and_reads_a_player_over_the_wire()
        {
            IReadOnlyList<string> players = await _bus!.ListPlayersAsync();
            MprisPlayerState? state = await _bus.ReadPlayerAsync(_player!.BusName);

            _ = players.Should().Equal(_player.BusName);
            _ = state!.Title.Should().Be("Song One");
            _ = state.Artists.Should().Equal("Band");
            _ = state.LengthMicros.Should().Be(200_000_000);
            _ = state.PositionMicros.Should().Be(10_000_000);
            _ = state.TrackId.Should().Be("/fake/track/1");
            _ = state.DesktopEntry.Should().Be("fake-player");
            _ = state.PlaybackStatus.Should().Be(MprisPlaybackStatus.Playing);
            _ = state.OwnerPid.Should().Be(Environment.ProcessId, "the fake player runs in the test process");
        }

        [LinuxBusFact]
        public async Task A_vanished_player_reads_as_null()
        {
            _ = await _bus!.ListPlayersAsync();

            MprisPlayerState? state = await _bus.ReadPlayerAsync("org.mpris.MediaPlayer2.nobody");

            _ = state.Should().BeNull();
        }

        [LinuxBusFact]
        public async Task Properties_changed_from_a_unique_name_is_routed_to_the_well_known_name()
        {
            _ = await _bus!.ListPlayersAsync();
            List<MprisPlayerSignal> signals = [];
            _bus.PlayerChanged += (_, s) => { lock (signals) { signals.Add(s); } };

            _player!.SetMetadata("Song Two", "Band", 180_000_000);
            _player.EmitPropertiesChanged("Metadata");

            _ = (await EventuallyAsync(() => { lock (signals) { return signals.Count > 0; } })).Should().BeTrue();
            lock (signals)
            {
                _ = signals[0].Should().Be(new MprisPlayerSignal(_player.BusName, Seeked: false));
            }
        }

        [LinuxBusFact]
        public async Task Seeked_is_routed_as_a_seek()
        {
            _ = await _bus!.ListPlayersAsync();
            List<MprisPlayerSignal> signals = [];
            _bus.PlayerChanged += (_, s) => { lock (signals) { signals.Add(s); } };

            _player!.EmitSeeked(42_000_000);

            _ = (await EventuallyAsync(() => { lock (signals) { return signals.Any(s => s.Seeked); } })).Should().BeTrue();
            _ = (await _bus.ReadPositionAsync(_player.BusName)).Should().Be(42_000_000);
        }

        [LinuxBusFact]
        public async Task Players_arriving_and_leaving_are_announced()
        {
            int changes = 0;
            _bus!.PlayersChanged += (_, _) => Interlocked.Increment(ref changes);

            await using FakeMprisPlayer second = new("second");
            second.SetMetadata("Other", "Someone", 1);
            await second.PublishAsync(_dbus!.Address);
            _ = (await EventuallyAsync(() => Volatile.Read(ref changes) >= 1)).Should().BeTrue("arrival");
            _ = (await _bus.ListPlayersAsync()).Should().Contain(second.BusName);

            await second.UnpublishAsync();
            _ = (await EventuallyAsync(() => Volatile.Read(ref changes) >= 2)).Should().BeTrue("departure");
            _ = (await _bus.ListPlayersAsync()).Should().NotContain(second.BusName);
        }

        [LinuxBusFact]
        public async Task Commands_reach_the_player_with_their_arguments()
        {
            string name = _player!.BusName;

            await _bus!.PlayPauseAsync(name);
            await _bus.NextAsync(name);
            await _bus.PreviousAsync(name);
            await _bus.SetPositionAsync(name, "/fake/track/1", 42_500_000);
            await _bus.SeekByAsync(name, -5_000_000);
            await _bus.SetShuffleAsync(name, true);
            await _bus.SetLoopStatusAsync(name, "Track");

            _ = _player.SnapshotCalls().Should().Equal(
                "PlayPause",
                "Next",
                "Previous",
                "SetPosition /fake/track/1 42500000",
                "Seek -5000000",
                "Set Shuffle True",
                "Set LoopStatus Track");
        }

        [LinuxBusFact]
        public async Task The_media_service_over_the_bus_shows_and_follows_the_player()
        {
            MprisMediaSessionService service = new(_bus!, new NoArt());
            _bus = null; // the service owns and disposes it
            using (service)
            {
                await service.Initialization;
                _ = service.Current!.Title.Should().Be("Song One");
                _ = service.Current.AppId.Should().Be("fake-player");
                int changed = 0;
                service.Changed += (_, _) => Interlocked.Increment(ref changed);

                _player!.SetMetadata("Song Two", "Band", 180_000_000, "/fake/track/2");
                _player.EmitPropertiesChanged("Metadata");

                _ = (await EventuallyAsync(() => service.Current?.Title == "Song Two")).Should().BeTrue();
                _ = Volatile.Read(ref changed).Should().Be(1);

                await service.SeekAsync(30);
                _ = _player.SnapshotCalls().Should().Equal("SetPosition /fake/track/2 30000000");
            }
        }

        [LinuxBusFact]
        public async Task Disposing_while_connecting_leaves_no_connection_behind()
        {
            // Services are disposed whenever the Host or Worker shuts down, which can be before the bus has connected.
            // A connection that finishes connecting after Dispose would live on with nobody to close it.
            using DBusConnection observer = new(_dbus!.Address);
            await observer.ConnectAsync();
            int before = await ClientCountAsync(observer);

            DBusMprisBus bus = new(_dbus.Address);
            Task connect = bus.ConnectAsync();
            bus.Dispose();

            // ConnectAsync must end (as disposed), not wait forever on a handshake that will never finish.
            Func<Task> connecting = () => connect.WaitAsync(Patience);
            _ = await connecting.Should().ThrowAsync<ObjectDisposedException>();

            int after = await ClientCountAsync(observer);
            for (DateTime until = DateTime.UtcNow + Patience; after != before && DateTime.UtcNow < until; after = await ClientCountAsync(observer))
            {
                await Task.Delay(20);
            }

            _ = after.Should().Be(before, "the half-open connection must be closed by Dispose");
        }

        /// <summary>Clients on the bus, by unique name (":1.N").</summary>
        private static async Task<int> ClientCountAsync(DBusConnection connection)
        {
            string[] names = await connection.ListServicesAsync();
            return names.Count(n => n.StartsWith(':'));
        }

        private sealed class NoArt : IMprisArtSource
        {
            public event EventHandler? Fetched { add { } remove { } }

            public byte[]? TryGet(string url)
            {
                return null;
            }

            public void Dispose() { }
        }
    }
}
