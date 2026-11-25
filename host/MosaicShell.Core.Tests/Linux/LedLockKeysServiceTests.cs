using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Linux;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary><see cref="LedLockKeysService"/> against a stand-in LED folder, and once against the real one.</summary>
    public sealed class LedLockKeysServiceTests
    {
        private sealed class FakeLeds : ILedReader
        {
            public List<(string Name, int Brightness)> Leds { get; } =
            [
                ("input3::capslock", 0),
                ("input3::numlock", 1),
                ("input3::scrolllock", 0),
            ];

            public IReadOnlyList<(string Name, int Brightness)> Read()
            {
                return [.. Leds];
            }

            public void Set(string name, int brightness)
            {
                int i = Leds.FindIndex(l => l.Name == name);
                Leds[i] = (name, brightness);
            }
        }

        [Fact]
        public void Start_takes_the_current_state_without_raising()
        {
            FakeLeds leds = new();
            using LedLockKeysService service = new(leds, pollInterval: Timeout.InfiniteTimeSpan);
            List<LockKeyState> seen = [];
            service.Changed += (_, s) => seen.Add(s);

            service.Start();

            _ = service.IsActive.Should().BeTrue();
            _ = service.Num.IsOn.Should().BeTrue();
            _ = service.Caps.IsOn.Should().BeFalse();
            _ = seen.Should().BeEmpty();
        }

        [Fact]
        public void Each_edge_is_raised_once_for_its_key()
        {
            FakeLeds leds = new();
            using LedLockKeysService service = new(leds, pollInterval: Timeout.InfiniteTimeSpan);
            List<LockKeyState> seen = [];
            service.Changed += (_, s) => seen.Add(s);
            service.Start();

            leds.Set("input3::capslock", 1);
            service.Poll();
            service.Poll();
            leds.Set("input3::numlock", 0);
            leds.Set("input3::capslock", 0);
            service.Poll();

            _ = seen.Should().Equal(
                new LockKeyState(LockKeyKind.CapsLock, true),
                new LockKeyState(LockKeyKind.CapsLock, false),
                new LockKeyState(LockKeyKind.NumLock, false));
        }

        [Fact]
        public void Without_lock_LEDs_it_is_inactive_and_every_key_reads_off()
        {
            FakeLeds leds = new();
            leds.Leds.Clear();
            using LedLockKeysService service = new(leds, pollInterval: Timeout.InfiniteTimeSpan);

            service.Start();

            _ = service.IsActive.Should().BeFalse();
            _ = service.Caps.IsOn.Should().BeFalse();
        }

        [Fact]
        public void It_runs_while_started_or_subscribed_like_the_other_lock_key_services()
        {
            using LedLockKeysService service = new(new FakeLeds(), pollInterval: Timeout.InfiniteTimeSpan);
            static void handler(object? sender, LockKeyState state) { }

            service.Changed += handler;
            _ = service.IsRunning.Should().BeTrue("a subscriber starts it");
            service.Start();
            service.Changed -= handler;
            _ = service.IsRunning.Should().BeTrue("still started");
            service.Stop();
            _ = service.IsRunning.Should().BeFalse();
        }

        [Fact]
        public void The_real_LED_folder_reads_without_throwing()
        {
            if (!OperatingSystem.IsLinux())
            {
                return;
            }

            IReadOnlyList<(string Name, int Brightness)> leds = new SysClassLedReader().Read();

            _ = leds.Should().OnlyContain(l => l.Brightness >= 0);
        }
    }
}
