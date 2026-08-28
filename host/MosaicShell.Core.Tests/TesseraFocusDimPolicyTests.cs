using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class TesseraFocusDimPolicyTests
    {
        [Fact]
        public void Overlay_alpha_is_subtle()
        {
            _ = TesseraFocusDimPolicy.OverlayAlpha.Should().BeInRange(56, 80);
        }

        [Fact]
        public void Payload_defaults_to_enabled()
        {
            _ = TesseraFocusDimPolicy.EnabledFromPayload(null).Should().BeTrue();
            _ = TesseraFocusDimPolicy.EnabledFromPayload(new Dictionary<string, string>()).Should().BeTrue();
        }

        [Fact]
        public void Payload_zero_disables()
        {
            _ = TesseraFocusDimPolicy.EnabledFromPayload(
                new Dictionary<string, string> { ["focusDim"] = "0" }).Should().BeFalse();
        }

        [Fact]
        public void Must_never_block_input_or_linger_on_click()
        {
            _ = TesseraFocusDimPolicy.MustPassThroughInput.Should().BeTrue();
            _ = TesseraFocusDimPolicy.InstantDismissOnOutsideClick.Should().BeTrue();
            _ = TesseraFocusDimPolicy.InstantDismissMustCloseFocusDim.Should().BeTrue();
            _ = TesseraFocusDimPolicy.ShouldCloseFocusDimOnTransientDismiss().Should().BeTrue();
        }

        [Fact]
        public void Dim_uses_constant_layered_alpha_not_composition_fallback_brush()
        {
            // High-alpha Transparent FallbackBrush under RedirectionSurface paints a black fill.
            _ = TesseraFocusDimPolicy.UseConstantLayeredAlpha.Should().BeTrue();
            _ = TesseraFocusDimPolicy.MaxCompositionFallbackAlpha.Should().Be(0);
            _ = TesseraFocusDimPolicy.ResolveLayeredAlpha()
                .Should().Be(TesseraFocusDimPolicy.OverlayAlpha);
            _ = TesseraFocusDimPolicy.ResolveLayeredAlpha()
                .Should().BeLessThan(100);
        }

        [Fact]
        public void Crust_rgb_is_mocha_base()
        {
            _ = TesseraFocusDimPolicy.CrustR.Should().Be(0x11);
            _ = TesseraFocusDimPolicy.CrustG.Should().Be(0x11);
            _ = TesseraFocusDimPolicy.CrustB.Should().Be(0x1b);
        }
    }
}
