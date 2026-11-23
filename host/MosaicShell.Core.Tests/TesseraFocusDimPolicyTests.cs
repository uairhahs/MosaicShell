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

        [Theory]
        [InlineData(true, false, false, false, FocusDimRoute.Win32LayeredAlpha)]
        [InlineData(true, false, true, true, FocusDimRoute.Win32LayeredAlpha)]
        [InlineData(false, true, true, true, FocusDimRoute.X11ArgbInputShape)]
        [InlineData(false, true, true, false, FocusDimRoute.None)]
        [InlineData(false, true, false, true, FocusDimRoute.None)]
        [InlineData(false, false, true, true, FocusDimRoute.None)]
        public void Each_platform_gets_the_route_that_draws_a_translucent_click_through_dim(
            bool isWindows, bool isLinux, bool hasX11Display, bool x11Composited, FocusDimRoute expected)
        {
            // Without a compositor an ARGB window's alpha is ignored and the dim is an opaque sheet that hides the
            // flyouts and swallows clicks, as it was on KDE before any route existed (2026-11-22).
            _ = TesseraFocusDimPolicy.ResolveRoute(isWindows, isLinux, hasX11Display, x11Composited).Should().Be(expected);
        }

        [Theory]
        [InlineData(FocusDimRoute.None, false)]
        [InlineData(FocusDimRoute.Win32LayeredAlpha, true)]
        [InlineData(FocusDimRoute.X11ArgbInputShape, true)]
        public void The_dim_can_show_on_any_route_but_none(FocusDimRoute route, bool expected)
        {
            _ = TesseraFocusDimPolicy.CanDim(route).Should().Be(expected);
        }

        [Fact]
        public void On_x11_the_window_itself_carries_the_dim_alpha()
        {
            // X11 has no per-window constant alpha the compositor honors for every window, so the transparent window's
            // background brush carries the same alpha that LWA_ALPHA applies on Windows.
            _ = TesseraFocusDimPolicy.PaintsAlphaInWindow(FocusDimRoute.X11ArgbInputShape).Should().BeTrue();
            _ = TesseraFocusDimPolicy.ResolveBackgroundAlpha(FocusDimRoute.X11ArgbInputShape)
                .Should().Be(TesseraFocusDimPolicy.OverlayAlpha);
        }

        [Fact]
        public void On_windows_the_background_stays_opaque_under_layered_alpha()
        {
            _ = TesseraFocusDimPolicy.PaintsAlphaInWindow(FocusDimRoute.Win32LayeredAlpha).Should().BeFalse();
            _ = TesseraFocusDimPolicy.ResolveBackgroundAlpha(FocusDimRoute.Win32LayeredAlpha).Should().Be(255);
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
