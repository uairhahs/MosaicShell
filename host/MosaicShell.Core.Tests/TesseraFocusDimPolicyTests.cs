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
            _ = TesseraFocusDimPolicy.InstantDismissMustCloseFocusDim.Should().BeTrue();
            _ = TesseraFocusDimPolicy.ShouldCloseFocusDimOnTransientDismiss().Should().BeTrue();
        }

        [Fact]
        public void Dim_uses_constant_layered_alpha_not_composition_fallback_brush()
        {
            // High-alpha Transparent FallbackBrush under RedirectionSurface paints a black fill.
            _ = TesseraFocusDimPolicy.UseConstantLayeredAlpha.Should().BeTrue();
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

        [Fact]
        public void Only_windows_can_draw_the_dim_translucent_and_click_through()
        {
            // Elsewhere the Win32 layered alpha and WS_EX_TRANSPARENT calls do nothing, and the dim became an opaque
            // full-screen sheet that hid the flyouts and swallowed clicks (KDE, 2026-11-22).
            _ = TesseraFocusDimPolicy.PlatformCanDim.Should().Be(OperatingSystem.IsWindows());
        }

        [Theory]
        [InlineData(true, null, true)]
        [InlineData(true, "0", false)]
        [InlineData(false, null, false)]
        [InlineData(false, "1", false)]
        public void The_dim_shows_only_where_the_platform_can_draw_it_and_the_user_wants_it(
            bool platformCanDim, string? setting, bool expected)
        {
            Dictionary<string, string>? payload = setting is null ? null : new() { ["focusDim"] = setting };

            _ = TesseraFocusDimPolicy.ShouldShow(payload, platformCanDim).Should().Be(expected);
        }
    }
}
