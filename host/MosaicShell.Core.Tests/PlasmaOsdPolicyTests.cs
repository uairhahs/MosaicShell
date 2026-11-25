using FluentAssertions;
using MosaicShell.Core.Services.Kde;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Which of Plasma's own pop-ups MosaicShell hides while Tessera is armed: the ones Tessera replaces. Calls as
    /// captured on Plasma 6.8 (2026-11-22): plasma-pa sends volumeChanged(percent, max) for volume and for mute;
    /// PowerDevil sends screenBrightnessChanged per screen it changes.
    /// </summary>
    public sealed class PlasmaOsdPolicyTests
    {
        [Theory]
        [InlineData("volumeChanged")]
        [InlineData("screenBrightnessChanged")]
        [InlineData("brightnessChanged")]
        public void The_volume_and_brightness_popups_are_hidden(string member)
        {
            _ = PlasmaOsdPolicy.ShouldHide(member).Should().BeTrue();
        }

        [Theory]
        [InlineData("keyboardBrightnessChanged")]
        [InlineData("microphoneVolumeChanged")]
        [InlineData("mediaPlayerVolumeChanged")]
        [InlineData("kbdLayoutChanged")]
        [InlineData("showText")]
        [InlineData("hide")]
        [InlineData("")]
        public void Popups_tessera_does_not_replace_stay(string member)
        {
            _ = PlasmaOsdPolicy.ShouldHide(member).Should().BeFalse();
        }

        [Fact]
        public void The_watched_calls_are_exactly_the_hidden_ones()
        {
            _ = PlasmaOsdPolicy.HiddenMembers.Should().BeEquivalentTo("volumeChanged", "screenBrightnessChanged", "brightnessChanged");
            _ = PlasmaOsdPolicy.HiddenMembers.Should().OnlyContain(member => PlasmaOsdPolicy.ShouldHide(member));
        }
    }
}
