using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;
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

        [Theory]
        [InlineData(FlyoutIngressDecision.ColdBuild)]
        [InlineData(FlyoutIngressDecision.CloseThenColdBuild)]
        [InlineData(FlyoutIngressDecision.ReviveHidden)]
        [InlineData(FlyoutIngressDecision.RecreateHwnd)]
        [InlineData(FlyoutIngressDecision.RebuildInPlace)]
        [InlineData(FlyoutIngressDecision.RebuildStructure)]
        [InlineData(FlyoutIngressDecision.ReviveDuringExit)]
        public void Every_ingress_that_shows_or_rebuilds_the_flyout_syncs_the_dim(FlyoutIngressDecision decision)
        {
            // A revived flyout skipped the dim: transient dismiss closes it, and only a cold build or rebuild opened
            // it again, so every press after the first came without one (KDE, 2026-11-23).
            _ = TesseraFocusDimPolicy.MustSyncOnIngress(decision).Should().BeTrue();
        }

        [Theory]
        [InlineData(FlyoutIngressDecision.PatchActiveSession)]
        [InlineData(FlyoutIngressDecision.PatchVisibleCoalesced)]
        public void A_patch_of_a_showing_session_leaves_its_dim_alone(FlyoutIngressDecision decision)
        {
            _ = TesseraFocusDimPolicy.MustSyncOnIngress(decision).Should().BeFalse();
        }

        [Fact]
        public void Every_ingress_decision_has_a_dim_answer()
        {
            // A new decision must be added to one of the two theories above, not fall through unnoticed.
            FlyoutIngressDecision[] patches = [FlyoutIngressDecision.PatchActiveSession, FlyoutIngressDecision.PatchVisibleCoalesced];
            foreach (FlyoutIngressDecision decision in Enum.GetValues<FlyoutIngressDecision>())
            {
                _ = TesseraFocusDimPolicy.MustSyncOnIngress(decision).Should().Be(!patches.Contains(decision), decision.ToString());
            }

            _ = Enum.GetValues<FlyoutIngressDecision>().Should().HaveCount(9);
        }

        [Theory]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        [InlineData(false, false, false)]
        [InlineData(false, true, false)]
        public void The_dim_never_outlives_every_tessera_flyout(bool dimOpen, bool anyTesseraFlyoutShowing, bool expected)
        {
            // A request during a stacked exit re-opened the dim, then the slots finished their exit and hid; nothing
            // closed the dim, which stayed over the desktop with no flyout above it (KDE, 2026-11-23).
            _ = TesseraFocusDimPolicy.ShouldCloseOrphanedDim(dimOpen, anyTesseraFlyoutShowing).Should().Be(expected);
        }

        [Theory]
        [InlineData(FocusDimRoute.X11ArgbInputShape, "1", true)]
        [InlineData(FocusDimRoute.X11ArgbInputShape, "0", true)]
        [InlineData(FocusDimRoute.Win32LayeredAlpha, "1", true)]
        [InlineData(FocusDimRoute.Win32LayeredAlpha, "0", false)]
        [InlineData(FocusDimRoute.None, "1", false)]
        public void On_x11_the_layer_shows_with_the_dim_off_too_as_the_outside_click_hitbox(
            FocusDimRoute route, string focusDim, bool expected)
        {
            // Wayland gives the Host no clicks on other windows, so on X11 the layer catches them (the click is not
            // passed on, as with a popup menu). Windows keeps its hook and shows the layer only for the dim.
            Dictionary<string, string> payload = new() { ["focusDim"] = focusDim };

            _ = TesseraFocusDimPolicy.ShouldShowLayer(payload, route).Should().Be(expected);
            _ = TesseraFocusDimPolicy.LayerIsOutsideClickHitbox(route).Should().Be(route == FocusDimRoute.X11ArgbInputShape);
        }

        [Theory]
        [InlineData(true, TesseraFocusDimPolicy.OverlayAlpha)]
        [InlineData(false, 0)]
        public void With_the_dim_off_the_hitbox_layer_is_fully_transparent(bool dimEnabled, byte expected)
        {
            _ = TesseraFocusDimPolicy.ResolveLayerAlpha(FocusDimRoute.X11ArgbInputShape, dimEnabled).Should().Be(expected);
        }

        [Theory]
        [InlineData(FocusDimRoute.X11ArgbInputShape, true)]
        [InlineData(FocusDimRoute.Win32LayeredAlpha, false)]
        [InlineData(FocusDimRoute.None, false)]
        public void On_x11_the_hitbox_layer_covers_every_screen(FocusDimRoute route, bool expected)
        {
            // A layer on the flyout's screen alone let a click on another display reach the app there and leave the
            // flyout up (KDE, two displays, 2026-11-23). Windows' hook already sees clicks on every screen.
            _ = TesseraFocusDimPolicy.LayerCoversEveryScreen(route).Should().Be(expected);
        }

        [Theory]
        [InlineData(true, true, TesseraFocusDimPolicy.OverlayAlpha)]
        [InlineData(true, false, 0)]
        [InlineData(false, true, 0)]
        [InlineData(false, false, 0)]
        public void Only_the_flyouts_screen_is_dimmed_the_others_are_bare_hitbox(
            bool dimEnabled, bool onFlyoutScreen, byte expected)
        {
            // Windows dims only the flyout's monitor; the extra X11 layers catch clicks without dimming other screens.
            _ = TesseraFocusDimPolicy.ResolveLayerAlpha(FocusDimRoute.X11ArgbInputShape, dimEnabled, onFlyoutScreen)
                .Should().Be(expected);
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

        [Theory]
        [InlineData(FocusDimRoute.Win32LayeredAlpha, FlyoutRestack.Win32InsertAfter)]
        [InlineData(FocusDimRoute.X11ArgbInputShape, FlyoutRestack.X11RestackWindow)]
        [InlineData(FocusDimRoute.None, FlyoutRestack.None)]
        public void Every_platform_with_a_layer_can_put_the_flyout_back_above_it(FocusDimRoute route, FlyoutRestack expected)
        {
            // A layer shown after the flyout stacks on top of it, and on X11 it is the outside-click hitbox: every click
            // on that flyout then dismissed it (KDE: a kind switch rebuilt the hidden window, showed it, then made the
            // layer; 14 of 137 outside-click dismisses landed inside the flyout).
            _ = TesseraFocusDimPolicy.ResolveRestack(route).Should().Be(expected);
        }
    }
}
