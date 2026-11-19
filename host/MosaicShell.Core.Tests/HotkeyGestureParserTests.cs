using FluentAssertions;
using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    /// <summary>Parsing and formatting of the "Ctrl+Alt+K" gestures that capability hotkeys are stored and shown as.</summary>
    public class HotkeyGestureParserTests
    {
        [Theory]
        [InlineData("Ctrl+Alt+K", ModifierKeys.Control | ModifierKeys.Alt, 'K')]
        [InlineData("ctrl+alt+k", ModifierKeys.Control | ModifierKeys.Alt, 'K')]
        [InlineData("Control+Shift+1", ModifierKeys.Control | ModifierKeys.Shift, '1')]
        [InlineData("Win+Shift+Z", ModifierKeys.Win | ModifierKeys.Shift, 'Z')]
        [InlineData("Windows+X", ModifierKeys.Win, 'X')]
        [InlineData("Meta+X", ModifierKeys.Win, 'X')]
        [InlineData("Cmd+X", ModifierKeys.Win, 'X')]
        [InlineData("  Ctrl + Alt + K  ", ModifierKeys.Control | ModifierKeys.Alt, 'K')]
        [InlineData("Ctrl+A", ModifierKeys.Control, 'A')]
        [InlineData("Ctrl+Z", ModifierKeys.Control, 'Z')]
        [InlineData("Ctrl+0", ModifierKeys.Control, '0')]
        [InlineData("Ctrl+9", ModifierKeys.Control, '9')]
        [InlineData("Ctrl+Space", ModifierKeys.Control, 0x20)]
        [InlineData("Alt+Esc", ModifierKeys.Alt, 0x1B)]
        [InlineData("Alt+Escape", ModifierKeys.Alt, 0x1B)]
        [InlineData("Ctrl+Tab", ModifierKeys.Control, 0x09)]
        [InlineData("Ctrl+Enter", ModifierKeys.Control, 0x0D)]
        [InlineData("Ctrl+Return", ModifierKeys.Control, 0x0D)]
        [InlineData("Ctrl+F1", ModifierKeys.Control, 0x70)]
        [InlineData("Ctrl+F12", ModifierKeys.Control, 0x7B)]
        public void A_valid_gesture_parses_to_its_modifiers_and_virtual_key(string gesture, ModifierKeys mods, int vk)
        {
            HotkeyGestureParser.TryParse(gesture, out ModifierKeys parsedMods, out int parsedVk).Should().BeTrue();

            parsedMods.Should().Be(mods);
            parsedVk.Should().Be(vk);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("+")]
        [InlineData("K")]
        [InlineData("F5")]
        [InlineData("Space")]
        [InlineData("Ctrl+")]
        [InlineData("Ctrl+Alt")]
        [InlineData("Ctrl+!")]
        [InlineData("Ctrl+@")]
        [InlineData("Ctrl+[")]
        [InlineData("Ctrl+/")]
        [InlineData("Ctrl+:")]
        [InlineData("Ctrl+F13")]
        [InlineData("Ctrl+Home")]
        [InlineData("Foo+K")]
        public void A_gesture_without_a_modifier_or_a_known_key_is_rejected(string? gesture)
        {
            HotkeyGestureParser.TryParse(gesture!, out _, out _).Should().BeFalse();
        }

        [Theory]
        [InlineData(ModifierKeys.Control, 'K', "Ctrl+K")]
        [InlineData(ModifierKeys.Control | ModifierKeys.Alt, 'K', "Ctrl+Alt+K")]
        [InlineData(ModifierKeys.Win | ModifierKeys.Shift | ModifierKeys.Alt | ModifierKeys.Control, '7', "Ctrl+Alt+Shift+Win+7")]
        [InlineData(ModifierKeys.Alt, 0x20, "Alt+Space")]
        [InlineData(ModifierKeys.Alt, 0x1B, "Alt+Esc")]
        [InlineData(ModifierKeys.Alt, 0x09, "Alt+Tab")]
        [InlineData(ModifierKeys.Alt, 0x0D, "Alt+Enter")]
        [InlineData(ModifierKeys.Control, 0x70, "Ctrl+F1")]
        [InlineData(ModifierKeys.Control, 0x7B, "Ctrl+F12")]
        [InlineData(ModifierKeys.Control, 'A', "Ctrl+A")]
        [InlineData(ModifierKeys.Control, 'Z', "Ctrl+Z")]
        [InlineData(ModifierKeys.Control, '0', "Ctrl+0")]
        [InlineData(ModifierKeys.Control, '9', "Ctrl+9")]
        [InlineData(ModifierKeys.Control, '@', "Ctrl+0x40")]
        [InlineData(ModifierKeys.Control, '[', "Ctrl+0x5B")]
        [InlineData(ModifierKeys.Control, '/', "Ctrl+0x2F")]
        [InlineData(ModifierKeys.Control, ':', "Ctrl+0x3A")]
        [InlineData(ModifierKeys.Control, 0x2E, "Ctrl+0x2E")]
        public void Format_orders_modifiers_the_same_way_every_time(ModifierKeys mods, int vk, string expected)
        {
            HotkeyGestureParser.Format(mods, vk).Should().Be(expected);
        }

        [Theory]
        [InlineData("alt+ctrl+k", "Ctrl+Alt+K")]
        [InlineData("Win+Alt+Shift+Ctrl+f5", "Ctrl+Alt+Shift+Win+F5")]
        [InlineData("  Ctrl+Alt+K ", "Ctrl+Alt+K")]
        public void Normalize_rewrites_a_valid_gesture_in_canonical_form(string gesture, string expected)
        {
            HotkeyGestureParser.Normalize(gesture).Should().Be(expected);
        }

        [Fact]
        public void Normalize_leaves_an_unparseable_gesture_alone_apart_from_trimming()
        {
            HotkeyGestureParser.Normalize("  not a gesture ").Should().Be("not a gesture");
        }

        [Theory]
        [InlineData("Ctrl+Alt+K")]
        [InlineData("Ctrl+Alt+Shift+Win+F12")]
        public void A_normalized_gesture_parses_back_to_the_same_text(string gesture)
        {
            _ = HotkeyGestureParser.TryParse(gesture, out ModifierKeys mods, out int vk);

            HotkeyGestureParser.Format(mods, vk).Should().Be(gesture);
        }

        [Theory]
        [InlineData("Win+S", true)]
        [InlineData("win+s", true)]
        [InlineData("Windows+S", true)]
        [InlineData("Win+Tab", true)]
        [InlineData("Ctrl+Esc", true)]
        [InlineData("Win+J", false)]
        [InlineData("Ctrl+Alt+S", false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        public void Gestures_the_shell_owns_are_recognised(string gesture, bool reserved)
        {
            HotkeyGestureParser.IsLikelyOsReserved(gesture).Should().Be(reserved);
        }

        [Theory]
        [InlineData("inlay", "Ctrl+Alt+I")]
        [InlineData("Chord", "Ctrl+Alt+K")]
        [InlineData("SUBSTRATE", "Ctrl+Alt+Q")]
        [InlineData("mixdeck", "Ctrl+Alt+M")]
        [InlineData("anything-else", "Ctrl+Alt+O")]
        public void Each_module_has_a_safe_default_gesture(string moduleId, string expected)
        {
            HotkeyGestureParser.SafeDefault(moduleId).Should().Be(expected);
        }

        [Theory]
        [InlineData("Inlay", "alt+ctrl+j", "Ctrl+Alt+J")]
        [InlineData("Inlay", "Win+S", "Ctrl+Alt+I")]
        [InlineData("Chord", "garbage", "Ctrl+Alt+K")]
        [InlineData("Chord", "", "Ctrl+Alt+K")]
        [InlineData("Chord", "K", "Ctrl+Alt+K")]
        [InlineData("Unknown", null, "Ctrl+Alt+O")]
        public void A_gesture_that_cannot_be_registered_is_replaced_by_the_modules_default(string moduleId, string? gesture, string expected)
        {
            HotkeyGestureParser.EnsureRegisterable(moduleId, gesture!).Should().Be(expected);
        }
    }
}
