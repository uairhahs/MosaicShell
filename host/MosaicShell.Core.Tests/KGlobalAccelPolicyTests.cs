using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Kde;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Which KDE global shortcuts open which shell flyout. The set mirrors what
    /// <see cref="ShellFlyoutTriggerDecoder"/> accepts from Windows' SHELLHOOK, so the same key opens the same flyout on
    /// both platforms. Names are as captured live from KGlobalAccel on Plasma (2026-11-22).
    /// </summary>
    public sealed class KGlobalAccelPolicyTests
    {
        [Theory]
        [InlineData("mediacontrol", "playpausemedia", ShellFlyoutKind.Media)]
        [InlineData("mediacontrol", "stopmedia", ShellFlyoutKind.Media)]
        [InlineData("mediacontrol", "nextmedia", ShellFlyoutKind.Media)]
        [InlineData("mediacontrol", "previousmedia", ShellFlyoutKind.Media)]
        [InlineData("kmix", "increase_volume", ShellFlyoutKind.Volume)]
        [InlineData("kmix", "decrease_volume", ShellFlyoutKind.Volume)]
        [InlineData("kmix", "mute", ShellFlyoutKind.Volume)]
        [InlineData("kmix", "increase_volume_small", ShellFlyoutKind.Volume)]
        [InlineData("kmix", "decrease_volume_small", ShellFlyoutKind.Volume)]
        [InlineData("org_kde_powerdevil", "Increase Screen Brightness", ShellFlyoutKind.Brightness)]
        [InlineData("org_kde_powerdevil", "Decrease Screen Brightness", ShellFlyoutKind.Brightness)]
        [InlineData("org_kde_powerdevil", "Increase Screen Brightness Small", ShellFlyoutKind.Brightness)]
        [InlineData("org_kde_powerdevil", "Decrease Screen Brightness Small", ShellFlyoutKind.Brightness)]
        public void Flyout_keys_map_to_their_flyout(string component, string shortcut, ShellFlyoutKind expected)
        {
            _ = KGlobalAccelPolicy.TryDecode(component, shortcut, out ShellFlyoutKind kind).Should().BeTrue();
            _ = kind.Should().Be(expected);
        }

        [Theory]
        // Windows' decoder has no separate play, pause or seek keys, nor a player-volume key.
        [InlineData("mediacontrol", "playmedia")]
        [InlineData("mediacontrol", "pausemedia")]
        [InlineData("mediacontrol", "seekforwardmedia")]
        [InlineData("mediacontrol", "mediavolumeup")]
        // Microphone and keyboard backlight are not the output volume or the screen.
        [InlineData("kmix", "mic_mute")]
        [InlineData("kmix", "increase_microphone_volume")]
        [InlineData("org_kde_powerdevil", "Increase Keyboard Brightness")]
        [InlineData("org_kde_powerdevil", "Sleep")]
        // A shortcut name is only meaningful inside its own component.
        [InlineData("kwin", "mute")]
        [InlineData("kmix", "playpausemedia")]
        [InlineData("", "")]
        public void Other_shortcuts_open_nothing(string component, string shortcut)
        {
            _ = KGlobalAccelPolicy.TryDecode(component, shortcut, out _).Should().BeFalse();
        }

        [Fact]
        public void Names_are_matched_exactly()
        {
            _ = KGlobalAccelPolicy.TryDecode("KMix", "mute", out _).Should().BeFalse();
            _ = KGlobalAccelPolicy.TryDecode("org_kde_powerdevil", "increase screen brightness", out _).Should().BeFalse();
        }
    }
}
