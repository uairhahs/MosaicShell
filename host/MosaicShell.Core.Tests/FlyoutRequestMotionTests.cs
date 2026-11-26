using FluentAssertions;
using MosaicShell.Core.Capabilities;

namespace MosaicShell.Core.Tests
{
    public class FlyoutRequestMotionTests
    {
        private static readonly FlyoutRequest Spring = new(
            "Tessera", "media", "ModernFlyouts", AutoDismissMs: 4000,
            Payload: new Dictionary<string, string> { ["mediaTitle"] = "Old" },
            Ani: 1, AniDir: "Left", AniEase: "OutElastic", AniSteps: 40, AnimationDisplacement: 160);

        private static readonly FlyoutRequest Fade = new(
            "Tessera", "vol", "Fluent", AutoDismissMs: 2500,
            Payload: new Dictionary<string, string> { ["mediaTitle"] = "New" },
            Ani: 0, AniDir: "Right", AniEase: "OutCubic", AniSteps: 20, AnimationDisplacement: 30);

        [Fact]
        public void A_slot_takes_every_motion_field_of_the_new_request()
        {
            FlyoutRequest slot = FlyoutRequestMotion.Adopt(Spring, Fade);
            _ = FlyoutRequestMotion.SameMotion(slot, Fade).Should().BeTrue();
            _ = slot.Ani.Should().Be(0);
            _ = slot.AniDir.Should().Be("Right");
            _ = slot.AniEase.Should().Be("OutCubic");
            _ = slot.AniSteps.Should().Be(20);
            _ = slot.AnimationDisplacement.Should().Be(30);
        }

        [Fact]
        public void A_slot_keeps_its_own_content_kind_style_and_timing()
        {
            FlyoutRequest slot = FlyoutRequestMotion.Adopt(Spring, Fade);
            _ = slot.Kind.Should().Be("media");
            _ = slot.StyleId.Should().Be("ModernFlyouts");
            _ = slot.AutoDismissMs.Should().Be(4000);
            _ = slot.Payload.Should().BeSameAs(Spring.Payload);
        }

        [Fact]
        public void Every_motion_field_is_compared()
        {
            _ = FlyoutRequestMotion.SameMotion(Spring, Spring with { Ani = 2 }).Should().BeFalse();
            _ = FlyoutRequestMotion.SameMotion(Spring, Spring with { AniDir = "Top" }).Should().BeFalse();
            _ = FlyoutRequestMotion.SameMotion(Spring, Spring with { AniEase = "OutCubic" }).Should().BeFalse();
            _ = FlyoutRequestMotion.SameMotion(Spring, Spring with { AniSteps = 10 }).Should().BeFalse();
            _ = FlyoutRequestMotion.SameMotion(Spring, Spring with { AnimationDisplacement = 30 }).Should().BeFalse();
            _ = FlyoutRequestMotion.SameMotion(Spring, Spring with { Payload = null }).Should().BeTrue();
        }
    }
}
