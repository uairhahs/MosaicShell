using FluentAssertions;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// <see cref="WindowsHotkeyService"/> against the real user32 hotkey table. RegisterHotKey refuses a window owned by
    /// another thread (ERROR_WINDOW_OF_OTHER_THREAD, 1408), and capabilities register from whichever thread arms them,
    /// never the service's message thread. Ctrl+Alt+Shift+F11 is used because nothing in Windows or the Host holds it.
    /// </summary>
    public class WindowsHotkeyServiceTests
    {
        private const int VkF11 = 0x7A;
        private const ModifierKeys Mods = ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift;

        [WindowsFact]
        public void A_free_key_registers_from_a_thread_other_than_the_message_thread()
        {
            using WindowsHotkeyService hotkeys = new();

            bool registered = hotkeys.Register("probe", Mods, VkF11, () => { });

            _ = registered.Should().BeTrue();
        }

        [WindowsFact]
        public void Unregister_frees_the_key_for_the_next_registration()
        {
            using WindowsHotkeyService first = new();
            _ = first.Register("probe", Mods, VkF11, () => { }).Should().BeTrue();

            first.Unregister("probe");

            using WindowsHotkeyService second = new();
            _ = second.Register("probe", Mods, VkF11, () => { }).Should().BeTrue();
        }

        [WindowsFact]
        public void Dispose_frees_every_key_it_held()
        {
            WindowsHotkeyService first = new();
            _ = first.Register("probe", Mods, VkF11, () => { }).Should().BeTrue();

            first.Dispose();

            using WindowsHotkeyService second = new();
            _ = second.Register("probe", Mods, VkF11, () => { }).Should().BeTrue();
        }

        [WindowsFact]
        public void A_key_another_registration_holds_is_refused()
        {
            using WindowsHotkeyService first = new();
            using WindowsHotkeyService second = new();
            _ = first.Register("probe", Mods, VkF11, () => { }).Should().BeTrue();

            _ = second.Register("probe", Mods, VkF11, () => { }).Should().BeFalse();
        }
    }
}
