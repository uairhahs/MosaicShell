using FluentAssertions;
using MosaicShell.Core.Services.Gnome;
using MosaicShell.Core.Services.Linux;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary>
    /// <see cref="MutterIdleService"/> against a real dbus-daemon, with a stand-in for Mutter's
    /// org.gnome.Mutter.IdleMonitor as probed on GNOME 51: AddIdleWatch(t) and AddUserActiveWatch() return watch ids,
    /// RemoveWatch(u) drops one, and WatchFired(u) is signalled from /org/gnome/Mutter/IdleMonitor/Core. An idle watch
    /// fires each time the user has been idle that long; a user-active watch fires once, on the next input.
    /// </summary>
    public sealed class MutterIdleServiceTests : IAsyncLifetime
    {
        private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

        private PrivateDBus? _dbus;
        private FakeMutterIdle? _mutter;
        private MutterIdleService? _idle;
        private int _reached;

        public async Task InitializeAsync()
        {
            if (new LinuxBusFactAttribute().Skip is not null)
            {
                return;
            }

            _dbus = PrivateDBus.Start();
            _mutter = await FakeMutterIdle.StartAsync(_dbus.Address);
            _idle = new MutterIdleService(_dbus.Address) { Threshold = TimeSpan.FromSeconds(90) };
            _idle.IdleThresholdReached += (_, _) => Interlocked.Increment(ref _reached);
        }

        public Task DisposeAsync()
        {
            _idle?.Dispose();
            _mutter?.Dispose();
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
            _idle!.Start();
            _ = (await EventuallyAsync(() => _idle.IsActive)).Should().BeTrue("the idle watch should be in place");
        }

        [Fact]
        public void The_watch_interval_is_the_threshold_in_milliseconds_and_never_zero()
        {
            _ = MutterIdlePolicy.WatchIntervalMs(TimeSpan.FromSeconds(90)).Should().Be(90_000UL);
            _ = MutterIdlePolicy.WatchIntervalMs(TimeSpan.Zero).Should().Be(1UL);
        }

        [Theory]
        [InlineData("GNOME", true)]
        [InlineData("ubuntu:GNOME", true)]
        [InlineData("KDE", false)]
        [InlineData(null, false)]
        public void Mutter_is_the_idle_source_in_a_GNOME_session(string? desktop, bool expected)
        {
            _ = MutterIdlePolicy.IsGnomeSession(desktop).Should().Be(expected);
        }

        [LinuxBusFact]
        public async Task Start_asks_Mutter_for_an_idle_watch_at_the_threshold()
        {
            await StartAndWaitAsync();

            _ = _mutter!.IdleWatches().Should().Equal(90_000UL);
        }

        [LinuxBusFact]
        public async Task Reaching_the_threshold_raises_once_per_idle_stretch()
        {
            await StartAndWaitAsync();
            uint idle = _mutter!.LastIdleWatchId;

            _mutter.Fire(idle);
            _ = (await EventuallyAsync(() => Volatile.Read(ref _reached) == 1)).Should().BeTrue();
            _mutter.Fire(idle);
            await Task.Delay(200);

            _ = Volatile.Read(ref _reached).Should().Be(1, "no input came between the two");
            _ = _idle!.IdleTime.Should().BeGreaterThanOrEqualTo(TimeSpan.FromSeconds(90));
        }

        [LinuxBusFact]
        public async Task Input_after_an_idle_stretch_lets_the_next_one_raise_again()
        {
            await StartAndWaitAsync();
            uint idle = _mutter!.LastIdleWatchId;
            _mutter.Fire(idle);
            _ = (await EventuallyAsync(() => _mutter.LastActiveWatchId != 0)).Should().BeTrue("it waits for input once idle");

            _mutter.Fire(_mutter.LastActiveWatchId);
            _ = (await EventuallyAsync(() => _idle!.IdleTime == TimeSpan.Zero)).Should().BeTrue();
            _mutter.Fire(idle);

            _ = (await EventuallyAsync(() => Volatile.Read(ref _reached) == 2)).Should().BeTrue();
        }

        [LinuxBusFact]
        public async Task Another_clients_watch_firing_is_ignored()
        {
            await StartAndWaitAsync();

            _mutter!.Fire(_mutter.LastIdleWatchId + 100);
            await Task.Delay(200);

            _ = Volatile.Read(ref _reached).Should().Be(0);
        }

        [LinuxBusFact]
        public async Task A_new_threshold_replaces_the_watch()
        {
            await StartAndWaitAsync();
            uint first = _mutter!.LastIdleWatchId;

            _idle!.Threshold = TimeSpan.FromSeconds(30);

            _ = (await EventuallyAsync(() => _mutter.IdleWatches().Count == 2)).Should().BeTrue();
            _ = _mutter.IdleWatches()[1].Should().Be(30_000UL);
            _ = (await EventuallyAsync(() => _mutter.Removed().Contains(first))).Should().BeTrue();
        }

        [LinuxBusFact]
        public async Task Stop_silences_the_watch()
        {
            await StartAndWaitAsync();
            uint idle = _mutter!.LastIdleWatchId;

            _idle!.Stop();
            _mutter.Fire(idle);
            await Task.Delay(200);

            _ = Volatile.Read(ref _reached).Should().Be(0);
            _ = _idle.IsActive.Should().BeFalse();
        }

        [LinuxBusFact]
        public async Task Without_a_bus_it_stays_inactive_and_never_throws()
        {
            using MutterIdleService idle = new(address: "");

            idle.Start();
            await Task.Delay(100);

            _ = idle.IsActive.Should().BeFalse();
            _ = idle.IdleTime.Should().Be(TimeSpan.Zero);
        }

        /// <summary>Mutter's idle monitor: watch ids from 1 up, the intervals asked for, and WatchFired on demand.</summary>
        private sealed class FakeMutterIdle : IPathMethodHandler, IDisposable
        {
            private readonly DBusConnection _connection;
            private readonly Lock _gate = new();
            private readonly List<ulong> _idleWatches = [];
            private readonly List<uint> _removed = [];
            private uint _nextId = 1;

            private FakeMutterIdle(DBusConnection connection)
            {
                _connection = connection;
            }

            public string Path => MutterIdlePolicy.ObjectPath;
            public bool HandlesChildPaths => false;
            public uint LastIdleWatchId { get; private set; }
            public uint LastActiveWatchId { get; private set; }

            public static async Task<FakeMutterIdle> StartAsync(string address)
            {
                DBusConnection connection = new(address);
                await connection.ConnectAsync();
                FakeMutterIdle fake = new(connection);
                connection.AddMethodHandler(fake);
                await connection.RequestNameAsync(MutterIdlePolicy.BusName);
                return fake;
            }

            public IReadOnlyList<ulong> IdleWatches()
            {
                lock (_gate)
                {
                    return [.. _idleWatches];
                }
            }

            public IReadOnlyList<uint> Removed()
            {
                lock (_gate)
                {
                    return [.. _removed];
                }
            }

            public void Fire(uint id)
            {
                using MessageWriter w = _connection.GetMessageWriter();
                w.WriteSignalHeader(null, MutterIdlePolicy.ObjectPath, MutterIdlePolicy.Interface, MutterIdlePolicy.WatchFiredMember, "u");
                w.WriteUInt32(id);
                _ = _connection.TrySendMessage(w.CreateMessage());
            }

            public ValueTask HandleMethodAsync(MethodContext context)
            {
                Message request = context.Request;
                Reader body = request.GetBodyReader();
                lock (_gate)
                {
                    switch (request.MemberAsString)
                    {
                        case MutterIdlePolicy.AddIdleWatchMember:
                            _idleWatches.Add(body.ReadUInt64());
                            LastIdleWatchId = _nextId++;
                            Reply(context, LastIdleWatchId);
                            return ValueTask.CompletedTask;
                        case MutterIdlePolicy.AddUserActiveWatchMember:
                            LastActiveWatchId = _nextId++;
                            Reply(context, LastActiveWatchId);
                            return ValueTask.CompletedTask;
                        case MutterIdlePolicy.RemoveWatchMember:
                            _removed.Add(body.ReadUInt32());
                            using (MessageWriter w = context.CreateReplyWriter(null))
                            {
                                context.Reply(w.CreateMessage());
                            }

                            return ValueTask.CompletedTask;
                    }
                }

                context.ReplyUnknownMethodError();
                return ValueTask.CompletedTask;
            }

            private static void Reply(MethodContext context, uint id)
            {
                using MessageWriter w = context.CreateReplyWriter("u");
                w.WriteUInt32(id);
                context.Reply(w.CreateMessage());
            }

            public void Dispose()
            {
                _connection.Dispose();
            }
        }
    }
}
