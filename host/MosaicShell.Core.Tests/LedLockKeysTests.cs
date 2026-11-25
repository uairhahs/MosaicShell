using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Leds;
using MosaicShell.Core.Services.Linux;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Caps, Num and Scroll Lock read from the keyboard LEDs in <c>/sys/class/leds</c>, for desktops without KWin's
    /// keystate protocol (GNOME). Names as found on taizo: input3, input17, input23 and input46 each carry
    /// <c>::capslock</c>, <c>::numlock</c> and <c>::scrolllock</c>, beside unrelated LEDs (<c>::compose</c>, network ports).
    /// </summary>
    public sealed class LedLockKeysTests
    {
        [Theory]
        [InlineData("input17::capslock", LockKeyKind.CapsLock)]
        [InlineData("input3::numlock", LockKeyKind.NumLock)]
        [InlineData("input46::scrolllock", LockKeyKind.ScrollLock)]
        public void A_keyboards_lock_LED_names_its_key(string name, LockKeyKind expected)
        {
            _ = LedLockKeysPolicy.KeyFor(name).Should().Be(expected);
        }

        [Theory]
        [InlineData("input17::compose")]
        [InlineData("input17::kana")]
        [InlineData("enp162s0-0::lan")]
        [InlineData("platform::micmute")]
        [InlineData("capslock")]
        public void Other_LEDs_are_not_lock_keys(string name)
        {
            _ = LedLockKeysPolicy.KeyFor(name).Should().BeNull();
        }

        [Fact]
        public void A_key_is_on_when_any_keyboard_lights_it()
        {
            (string, int)[] leds =
            [
                ("input3::capslock", 0),
                ("input17::capslock", 1),
                ("input3::numlock", 0),
                ("input17::numlock", 0),
                ("enp162s0-0::lan", 1),
            ];

            (bool caps, bool num, bool scroll, bool any) = LedLockKeysPolicy.Resolve(leds);

            _ = caps.Should().BeTrue();
            _ = num.Should().BeFalse();
            _ = scroll.Should().BeFalse();
            _ = any.Should().BeTrue("there is a lock LED to read");
        }

        [Fact]
        public void Without_lock_LEDs_there_is_nothing_to_read()
        {
            (_, _, _, bool any) = LedLockKeysPolicy.Resolve([("enp162s0-0::lan", 1)]);

            _ = any.Should().BeFalse();
        }

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
