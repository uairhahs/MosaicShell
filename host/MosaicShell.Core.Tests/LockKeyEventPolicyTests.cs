using FluentAssertions;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Windows lock keys from Raw Input instead of a 100 ms GetKeyState poll. Probe on Windows 11 (2026-11-24): the toggle
    /// bit already shows the new state when the key's WM_INPUT arrives (make: 1, then make: 0), injected presses
    /// included, so each keyboard event resamples the three toggles.
    /// </summary>
    public class LockKeyEventPolicyTests
    {
        [Fact]
        public void The_listener_sinks_keyboard_raw_input_from_every_app()
        {
            // Generic desktop page, keyboard usage, RIDEV_INPUTSINK: delivered while another window has focus.
            _ = LockKeyEventPolicy.RawUsagePage.Should().Be(0x01);
            _ = LockKeyEventPolicy.RawUsageKeyboard.Should().Be(0x06);
            _ = LockKeyEventPolicy.RidevInputSink.Should().Be(0x100u);
        }

        [Theory]
        [InlineData(1u, true)]
        [InlineData(0u, false)]
        [InlineData(2u, false)]
        public void Only_keyboard_raw_input_resamples(uint rawInputType, bool expected)
        {
            // RIM_TYPEKEYBOARD is 1; mouse (0) and HID (2) never touch the toggles.
            _ = LockKeyEventPolicy.ShouldResampleOnRawInput(rawInputType).Should().Be(expected);
        }

        [Theory]
        [InlineData(0x1, true)]
        [InlineData(0x3, true)]
        [InlineData(0x8, true)]
        [InlineData(0x7, false)]
        [InlineData(0x5, false)]
        public void A_return_to_the_session_resamples(int wtsEvent, bool expected)
        {
            // Raw input stops on the secure desktop, where a toggle can still change: resample on console or remote
            // connect and on unlock, never on lock or logon.
            _ = LockKeyEventPolicy.ShouldResampleOnSessionChange(wtsEvent).Should().Be(expected);
        }

        [Fact]
        public void Unchanged_toggles_raise_nothing()
        {
            _ = LockKeyEventPolicy.Edges((true, false, false), (true, false, false)).Should().BeEmpty();
        }

        [Fact]
        public void Each_toggle_that_flipped_raises_one_edge_in_caps_num_scroll_order()
        {
            IReadOnlyList<LockKeyState> edges = LockKeyEventPolicy.Edges((false, true, false), (true, false, true));

            _ = edges.Should().Equal(
                new LockKeyState(LockKeyKind.CapsLock, true),
                new LockKeyState(LockKeyKind.NumLock, false),
                new LockKeyState(LockKeyKind.ScrollLock, true));
        }

        [Fact]
        public void Changed_is_raised_on_the_listener_thread()
        {
            // Marshaling to the Host UI thread belongs in TesseraCapability, as with the poll it replaces.
            _ = LockKeyEventPolicy.MustInvokeChangedSynchronously.Should().BeTrue();
        }
    }
}
