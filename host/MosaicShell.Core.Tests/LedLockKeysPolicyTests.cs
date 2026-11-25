using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Leds;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Caps, Num and Scroll Lock read from the keyboard LEDs in <c>/sys/class/leds</c>, for desktops without KWin's
    /// keystate protocol (GNOME). Names as found on taizo: input3, input17, input23 and input46 each carry
    /// <c>::capslock</c>, <c>::numlock</c> and <c>::scrolllock</c>, beside unrelated LEDs (<c>::compose</c>, network ports).
    /// </summary>
    public sealed class LedLockKeysPolicyTests
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
    }
}
