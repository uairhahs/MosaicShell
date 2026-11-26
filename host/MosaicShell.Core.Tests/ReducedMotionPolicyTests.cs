using FluentAssertions;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.HostPlatform;

namespace MosaicShell.Core.Tests
{
    public class ReducedMotionPolicyTests
    {
        [Fact]
        public void Windows_animation_effects_off_means_reduced()
        {
            _ = ReducedMotionPolicy.FromWindowsAnimationEffects(animationEffectsOn: false).Should().BeTrue();
            _ = ReducedMotionPolicy.FromWindowsAnimationEffects(animationEffectsOn: true).Should().BeFalse();
        }

        [Theory]
        [InlineData("false", true)]
        [InlineData("false\n", true)]
        [InlineData("true", false)]
        [InlineData(null, false)]
        [InlineData("", false)]
        public void Gnome_enable_animations_false_means_reduced(string? value, bool reduced)
        {
            _ = ReducedMotionPolicy.FromGnomeEnableAnimations(value).Should().Be(reduced);
        }

        [Theory]
        [InlineData("[KDE]\nAnimationDurationFactor=0\n", true)]
        [InlineData("[General]\nX=1\n[KDE]\r\nLookAndFeelPackage=a\r\nAnimationDurationFactor=0\r\n", true)]
        [InlineData("[KDE]\nAnimationDurationFactor=0.5\n", false)]
        [InlineData("[Other]\nAnimationDurationFactor=0\n", false)]
        [InlineData("[KDE]\nLookAndFeelPackage=a\n", false)]
        [InlineData(null, false)]
        public void Plasma_instant_animation_speed_means_reduced(string? kdeglobals, bool reduced)
        {
            _ = ReducedMotionPolicy.FromKdeGlobals(kdeglobals).Should().Be(reduced);
        }

        [Fact]
        public void Reduced_motion_turns_every_flyout_into_a_plain_fade()
        {
            FlyoutRequest slide = new("Tessera", "vol", Ani: 2);
            _ = ReducedMotionPolicy.ApplyTo(slide, reduced: true).Ani.Should().Be(0);
            _ = ReducedMotionPolicy.ApplyTo(slide, reduced: false).Should().BeSameAs(slide);
        }

        [Fact]
        public void Previews_hold_still_unless_pointed_at_and_buttons_do_not_sink()
        {
            _ = ReducedMotionPolicy.PreviewPlays(reduced: true, pointerOver: false).Should().BeFalse();
            _ = ReducedMotionPolicy.PreviewPlays(reduced: true, pointerOver: true).Should().BeTrue();
            _ = ReducedMotionPolicy.PreviewPlays(reduced: false, pointerOver: false).Should().BeTrue();
            _ = ReducedMotionPolicy.PressSinks(reduced: true).Should().BeFalse();
            _ = ReducedMotionPolicy.PressSinks(reduced: false).Should().BeTrue();
        }
    }
}
