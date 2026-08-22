using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class TesseraFocusDimPolicyTests
{
    [Fact]
    public void Overlay_alpha_is_subtle()
    {
        TesseraFocusDimPolicy.OverlayAlpha.Should().BeInRange((byte)56, (byte)80);
    }

    [Fact]
    public void Payload_defaults_to_enabled()
    {
        TesseraFocusDimPolicy.EnabledFromPayload(null).Should().BeTrue();
        TesseraFocusDimPolicy.EnabledFromPayload(new Dictionary<string, string>()).Should().BeTrue();
    }

    [Fact]
    public void Payload_zero_disables()
    {
        TesseraFocusDimPolicy.EnabledFromPayload(
            new Dictionary<string, string> { ["focusDim"] = "0" }).Should().BeFalse();
    }

    [Fact]
    public void Must_never_block_input_or_linger_on_click()
    {
        TesseraFocusDimPolicy.MustPassThroughInput.Should().BeTrue();
        TesseraFocusDimPolicy.InstantDismissOnOutsideClick.Should().BeTrue();
    }

    [Fact]
    public void Dim_uses_constant_layered_alpha_not_composition_fallback_brush()
    {
        // High-alpha Transparent FallbackBrush under RedirectionSurface paints a black fill.
        TesseraFocusDimPolicy.UseConstantLayeredAlpha.Should().BeTrue();
        TesseraFocusDimPolicy.MaxCompositionFallbackAlpha.Should().Be((byte)0);
        TesseraFocusDimPolicy.ResolveLayeredAlpha()
            .Should().Be(TesseraFocusDimPolicy.OverlayAlpha);
        TesseraFocusDimPolicy.ResolveLayeredAlpha()
            .Should().BeLessThan((byte)100);
    }

    [Fact]
    public void Crust_rgb_is_mocha_base()
    {
        TesseraFocusDimPolicy.CrustR.Should().Be(0x11);
        TesseraFocusDimPolicy.CrustG.Should().Be(0x11);
        TesseraFocusDimPolicy.CrustB.Should().Be(0x1b);
    }
}
