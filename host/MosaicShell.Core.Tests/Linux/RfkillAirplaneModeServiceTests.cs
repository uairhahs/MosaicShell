using System.Collections.Concurrent;
using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Linux;
using MosaicShell.Core.Services.Rfkill;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary><see cref="RfkillAirplaneModeService"/> against a stand-in <c>/dev/rfkill</c>, and once against the real one.</summary>
    public sealed class RfkillAirplaneModeServiceTests
    {
        private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

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

        private static RfkillEvent Radio(uint idx, byte op, bool blocked)
        {
            return new RfkillEvent(idx, 1, op, SoftBlocked: blocked, HardBlocked: false);
        }

        /// <summary>The kernel's queue: events waiting now, and a wait that a later event or a wake ends.</summary>
        private sealed class FakeRfkill : IRfkillReader
        {
            private readonly ConcurrentQueue<RfkillEvent> _events = new();
            private readonly SemaphoreSlim _signal = new(0);
            private volatile bool _woken;

            public FakeRfkill(params RfkillEvent[] present)
            {
                foreach (RfkillEvent ev in present)
                {
                    _events.Enqueue(ev);
                }
            }

            public bool Disposed { get; private set; }

            public void Push(RfkillEvent ev)
            {
                _events.Enqueue(ev);
                _ = _signal.Release();
            }

            public bool TryRead(out RfkillEvent ev)
            {
                return _events.TryDequeue(out ev);
            }

            public bool WaitReadable()
            {
                _signal.Wait();
                return !_woken;
            }

            public void Wake()
            {
                _woken = true;
                _ = _signal.Release();
            }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        [Fact]
        public void Start_takes_the_current_radios_without_raising()
        {
            FakeRfkill rfkill = new(Radio(0, RfkillPolicy.OpAdd, blocked: true));
            using RfkillAirplaneModeService airplane = new(() => rfkill);
            int raised = 0;
            airplane.Changed += (_, _) => raised++;

            airplane.Start();

            _ = airplane.IsSupported.Should().BeTrue();
            _ = airplane.IsEnabled.Should().BeTrue();
            _ = raised.Should().Be(0);
        }

        [Fact]
        public async Task Changed_is_raised_when_airplane_mode_flips()
        {
            FakeRfkill rfkill = new(Radio(0, RfkillPolicy.OpAdd, blocked: false));
            using RfkillAirplaneModeService airplane = new(() => rfkill);
            int raised = 0;
            airplane.Changed += (_, _) => Interlocked.Increment(ref raised);
            airplane.Start();

            rfkill.Push(Radio(0, RfkillPolicy.OpChange, blocked: true));

            _ = (await EventuallyAsync(() => raised == 1)).Should().BeTrue();
            _ = airplane.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public async Task A_radio_change_that_leaves_airplane_mode_as_it_was_is_not_raised()
        {
            FakeRfkill rfkill = new(Radio(0, RfkillPolicy.OpAdd, blocked: false), Radio(1, RfkillPolicy.OpAdd, blocked: false));
            using RfkillAirplaneModeService airplane = new(() => rfkill);
            int raised = 0;
            airplane.Changed += (_, _) => Interlocked.Increment(ref raised);
            airplane.Start();

            rfkill.Push(Radio(0, RfkillPolicy.OpChange, blocked: true));
            rfkill.Push(Radio(1, RfkillPolicy.OpChange, blocked: true));

            _ = (await EventuallyAsync(() => raised == 1)).Should().BeTrue("only the second change turns every radio off");
            await Task.Delay(100);
            _ = raised.Should().Be(1);
        }

        [Fact]
        public void Without_radios_it_is_unsupported()
        {
            FakeRfkill rfkill = new();
            using RfkillAirplaneModeService airplane = new(() => rfkill);

            airplane.Start();

            _ = airplane.IsSupported.Should().BeFalse();
            _ = airplane.IsEnabled.Should().BeFalse();
        }

        [Fact]
        public void Without_rfkill_it_is_unsupported_and_nothing_throws()
        {
            using RfkillAirplaneModeService airplane = new(() => throw new IOException("no /dev/rfkill"));

            airplane.Start();
            airplane.Stop();

            _ = airplane.IsSupported.Should().BeFalse();
        }

        [Fact]
        public async Task Stop_wakes_the_reader_closes_it_and_later_events_are_not_raised()
        {
            FakeRfkill rfkill = new(Radio(0, RfkillPolicy.OpAdd, blocked: false));
            using RfkillAirplaneModeService airplane = new(() => rfkill);
            int raised = 0;
            airplane.Changed += (_, _) => Interlocked.Increment(ref raised);
            airplane.Start();

            airplane.Stop();

            _ = (await EventuallyAsync(() => rfkill.Disposed)).Should().BeTrue();
            rfkill.Push(Radio(0, RfkillPolicy.OpChange, blocked: true));
            await Task.Delay(100);
            _ = raised.Should().Be(0);
        }

        [Fact]
        public void The_real_rfkill_starts_and_stops_cleanly()
        {
            if (!OperatingSystem.IsLinux())
            {
                return;
            }

            using RfkillAirplaneModeService airplane = new();

            airplane.Start();
            airplane.Stop();

            bool radios = Directory.Exists("/sys/class/rfkill") && Directory.EnumerateFileSystemEntries("/sys/class/rfkill").Any();
            _ = airplane.IsSupported.Should().Be(radios);
        }

        [Fact]
        public void Platform_defaults_on_linux_watch_rfkill_or_on_Plasma_its_setting()
        {
            if (!OperatingSystem.IsLinux())
            {
                return;
            }

            using HostServices hub = HostServices.CreatePlatformDefaults();

            bool plasma = Services.Kde.PlasmaAirplanePolicy.IsPlasmaSession(Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP"));
            _ = hub.Airplane.Should().BeOfType(plasma ? typeof(PlasmaAirplaneModeService) : typeof(RfkillAirplaneModeService));
        }
    }
}
