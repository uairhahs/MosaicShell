using System.Runtime.InteropServices;
using FluentAssertions;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// <see cref="WindowsLockKeysService"/> on the real Raw Input listener. These never press a key, so they do not
    /// flip the user's toggles; a real press was checked once by a throwaway test (2026-11-24).
    /// </summary>
    public class WindowsLockKeysServiceTests
    {
        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        private static bool On(int vk)
        {
            return LockKeyInputPolicy.IsToggleBitOn(GetKeyState(vk));
        }

        [WindowsFact]
        public void Start_listens_and_takes_the_current_toggles_quietly()
        {
            using WindowsLockKeysService keys = new();
            int raised = 0;

            keys.Start();
            keys.Changed += (_, _) => raised++;

            _ = keys.IsActive.Should().BeTrue();
            _ = keys.Caps.IsOn.Should().Be(On(LockKeyInputPolicy.VkCapital));
            _ = keys.Num.IsOn.Should().Be(On(LockKeyInputPolicy.VkNumlock));
            _ = keys.Scroll.IsOn.Should().Be(On(LockKeyInputPolicy.VkScroll));
            _ = raised.Should().Be(0);
            keys.Stop();
        }

        [WindowsFact]
        public void It_listens_while_started_or_subscribed_and_stops_after_the_last()
        {
            using WindowsLockKeysService keys = new();
            static void Handler(object? sender, LockKeyState state) { }

            keys.Start();
            keys.Changed += Handler;
            keys.Stop();
            _ = keys.IsActive.Should().BeTrue("a subscriber still wants edges");

            keys.Changed -= Handler;
            _ = keys.IsActive.Should().BeFalse();
        }

        [WindowsFact]
        public void It_can_listen_again_after_stopping()
        {
            // Raw input registration is per process: a stop removes it, and the next start must register again.
            using WindowsLockKeysService keys = new();
            keys.Start();
            keys.Stop();

            keys.Start();

            _ = keys.IsActive.Should().BeTrue();
            keys.Stop();
        }
    }
}
