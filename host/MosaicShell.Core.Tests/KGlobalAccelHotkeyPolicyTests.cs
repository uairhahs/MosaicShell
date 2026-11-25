using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Kde;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Capability hotkeys on KDE are actions of one KGlobalAccel component, bound by Qt key code. The codes below are what
    /// KGlobalAccel reported live on Plasma 6.6 (2026-11-24): Alt+F4 is 150994995 and Ctrl+Alt+Shift+F11 251658298.
    /// </summary>
    public sealed class KGlobalAccelHotkeyPolicyTests
    {
        [Theory]
        [InlineData(ModifierKeys.Control | ModifierKeys.Alt, 'M', 0x0C00004D)]
        [InlineData(ModifierKeys.Alt, 0x73, 150994995)]
        [InlineData(ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, 0x7A, 251658298)]
        [InlineData(ModifierKeys.Win | ModifierKeys.Shift, '7', 0x12000037)]
        [InlineData(ModifierKeys.Control, 0x20, 0x04000020)]
        [InlineData(ModifierKeys.Control, 0x1B, 0x05000000)]
        [InlineData(ModifierKeys.Control, 0x09, 0x05000001)]
        [InlineData(ModifierKeys.Control, 0x0D, 0x05000004)]
        [InlineData(ModifierKeys.Control, 0x70, 0x05000030)]
        public void Every_gesture_the_parser_makes_has_its_Qt_key(ModifierKeys modifiers, int virtualKey, int expected)
        {
            _ = KGlobalAccelHotkeyPolicy.TryQtKey(modifiers, virtualKey, out int key).Should().BeTrue();
            _ = key.Should().Be(expected);
        }

        [Theory]
        [InlineData(ModifierKeys.Control, 0x2E)]
        [InlineData(ModifierKeys.None, 'M')]
        public void A_key_with_no_Qt_code_or_no_modifier_is_refused(ModifierKeys modifiers, int virtualKey)
        {
            // A bare letter would take that letter from every app, as RegisterHotKey's callers never ask for either.
            _ = KGlobalAccelHotkeyPolicy.TryQtKey(modifiers, virtualKey, out _).Should().BeFalse();
        }

        [Fact]
        public void A_capability_hotkey_is_an_action_named_for_what_it_opens()
        {
            // System Settings > Shortcuts lists it under MosaicShell, so it reads as the action, not the internal id.
            _ = KGlobalAccelHotkeyPolicy.ActionId("cap:Mixdeck").Should().Equal("mosaicshell", "cap:Mixdeck", "MosaicShell", "Open Mixdeck");
            _ = KGlobalAccelHotkeyPolicy.ActionId("other").Should().Equal("mosaicshell", "other", "MosaicShell", "other");
        }

        [Theory]
        [InlineData(new[] { 0x0C00004D }, true)]
        [InlineData(new[] { 0 }, false)]
        [InlineData(new int[0], false)]
        public void The_key_is_ours_only_when_KGlobalAccel_hands_it_back(int[] assigned, bool expected)
        {
            // Live: a key another component holds comes back as 0, and that component keeps it.
            _ = KGlobalAccelHotkeyPolicy.Took(0x0C00004D, assigned).Should().Be(expected);
        }
    }
}
