using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class TesseraFlyoutWindowPolicyTests
    {
        /// <summary>Single ship-gate fact, SoftFrost must not flip off silently.</summary>
        [Fact]
        public void Ship_gate_soft_frost_hwnd_is_on()
        {
            _ = TesseraFlyoutWindowPolicy.SoftFrostHwndReady.Should().BeTrue();
            _ = TesseraFlyoutWindowPolicy.PreferWinUiCompositionForSoftFrost.Should().BeTrue();
            _ = TesseraFlyoutWindowPolicy.MustApplyPresentableLayeredAlpha.Should().BeFalse(
                "LWA_ALPHA=255 makes the HWND opaque and blocks glass");
            _ = TesseraFlyoutWindowPolicy.WindowBackgroundBrushIsTransparent.Should().BeTrue();
            _ = TesseraFlyoutWindowPolicy.MustRequestOpaqueToolWindow.Should().BeFalse();
            _ = TesseraFlyoutWindowPolicy.SoftFrostCompositionFallbackAlpha.Should().Be(0);
            _ = TesseraFlyoutWindowPolicy.PresentableLayeredAlpha.Should().Be(255);
            _ = TesseraFlyoutWindowPolicy.ForbidDebugTitleChrome.Should().BeTrue();
            _ = TesseraFlyoutWindowPolicy.HideUntilCompositionReady.Should().BeTrue(
                "SoftFrost cold Show must hide until layout or Try now flashes a black HWND");
        }

        [Fact]
        public void Soft_frost_must_hide_until_composition_ready()
        {
            // Transparent HWND paints black for 1+ frames before WinUI composition settles
            // (Try now / cold Show). Host must Show at Opacity 0, finish layout, then reveal.
            _ = TesseraFlyoutWindowPolicy.SoftFrostHwndReady.Should().BeTrue();
            _ = TesseraFlyoutWindowPolicy.HideUntilCompositionReady.Should().BeTrue();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Opaque_recovery_shell_alpha_stays_presentable(bool softFrost)
        {
            TesseraFlyoutMaterial material = TesseraFlyoutMaterialFactory.Create(softFrost);
            byte alpha = TesseraFlyoutWindowPolicy.ResolveWindowBackgroundAlpha(material);
            _ = alpha.Should().Be(material.ShellAlpha);
            _ = alpha.Should().BeGreaterThanOrEqualTo(TesseraFlyoutWindowPolicy.MinPresentableFallbackAlpha);
        }

        [Fact]
        public void Composition_fallback_alpha_is_zero_while_soft_frost_hwnd_ship_gate_on()
        {
            // Behavior follows SoftFrostHwndReady, not the material acrylic flag.
            foreach (bool frost in new[] { true, false })
            {
                _ = TesseraFlyoutWindowPolicy
                    .ResolveCompositionFallbackAlpha(TesseraFlyoutMaterialFactory.Create(frost))
                    .Should().Be(0);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Resolve_transparency_hints_never_request_os_acrylic(bool softFrostMaterial)
        {
            TesseraFlyoutMaterial material = TesseraFlyoutMaterialFactory.Create(softFrostMaterial);
            _ = material.TransparencyHints.Should().Contain("Transparent");
            _ = material.TransparencyHints.Should().NotContain("AcrylicBlur");
            _ = material.TransparencyHints.Should().NotContain("Blur");

            // Ship SoftFrost: opaque tool window off → honor material Transparent hints.
            _ = TesseraFlyoutWindowPolicy.MustRequestOpaqueToolWindow.Should().BeFalse();
            _ = TesseraFlyoutWindowPolicy.ResolveTransparencyHints(material).Should().Equal("Transparent");
        }
    }
}
