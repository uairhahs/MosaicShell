using FluentAssertions;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests;

public class LockKeyInputPolicyTests
{
    [Theory]
    [InlineData(0x14, true)]
    [InlineData(0x90, true)]
    [InlineData(0x91, true)]
    [InlineData(0x41, false)]
    [InlineData(0xAD, false)]
    public void IsToggleVirtualKey_matches_caps_num_scroll(int vk, bool expected) =>
        LockKeyInputPolicy.IsToggleVirtualKey(vk).Should().Be(expected);

    [Theory]
    [InlineData(0, 0x0100, 0x14, true)]
    [InlineData(0, 0x0101, 0x14, true)]
    [InlineData(0, 0x0100, 0x41, false)]
    [InlineData(-1, 0x0100, 0x14, false)]
    public void ShouldSampleFromHook_filters_ncode_msg_and_vk(int nCode, int msg, int vk, bool expected) =>
        LockKeyInputPolicy.ShouldSampleFromHook(nCode, msg, vk).Should().Be(expected);

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(unchecked((short)0x8001), true)]
    [InlineData(unchecked((short)0x8000), false)]
    public void IsToggleBitOn_reads_low_bit(short state, bool expected) =>
        LockKeyInputPolicy.IsToggleBitOn(state).Should().Be(expected);

    [Theory]
    [InlineData(0x0100, true)]
    [InlineData(0x0104, true)]
    [InlineData(0x0101, false)]
    public void IsToggleKeyDown_matches_keydown_msgs(int msg, bool expected) =>
        LockKeyInputPolicy.IsToggleKeyDown(msg).Should().Be(expected);

    [Fact]
    public void KindFromVirtualKey_maps_toggle_keys()
    {
        LockKeyInputPolicy.KindFromVirtualKey(0x14).Should().Be(LockKeyKind.CapsLock);
        LockKeyInputPolicy.KindFromVirtualKey(0x90).Should().Be(LockKeyKind.NumLock);
        LockKeyInputPolicy.KindFromVirtualKey(0x91).Should().Be(LockKeyKind.ScrollLock);
        LockKeyInputPolicy.KindFromVirtualKey(0x41).Should().BeNull();
    }

    [Fact]
    public void Poll_and_post_toggle_delays_are_positive()
    {
        LockKeyInputPolicy.PollIntervalMs.Should().BePositive();
        LockKeyInputPolicy.PostToggleSampleDelayMs.Should().BePositive();
    }

    [Fact]
    public void Lock_keys_must_use_dedicated_pump_without_stale_GetKeyState_poll()
    {
        LockKeyInputPolicy.MustUseDedicatedMessagePump.Should().BeTrue();
        LockKeyInputPolicy.MustNotPollGetKeyStateOnPumpThread.Should().BeTrue();
    }
}
