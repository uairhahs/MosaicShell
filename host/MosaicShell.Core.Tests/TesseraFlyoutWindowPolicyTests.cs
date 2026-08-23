using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class TesseraFlyoutWindowPolicyTests
{
    /// <summary>Single ship-gate fact — SoftFrost must not flip off silently.</summary>
    [Fact]
    public void Ship_gate_soft_frost_hwnd_is_on()
    {
        TesseraFlyoutWindowPolicy.SoftFrostHwndReady.Should().BeTrue();
        TesseraFlyoutWindowPolicy.PreferWinUiCompositionForSoftFrost.Should().BeTrue();
        TesseraFlyoutWindowPolicy.MustApplyPresentableLayeredAlpha.Should().BeFalse(
            "LWA_ALPHA=255 makes the HWND opaque and blocks glass");
        TesseraFlyoutWindowPolicy.WindowBackgroundBrushIsTransparent.Should().BeTrue();
        TesseraFlyoutWindowPolicy.MustRequestOpaqueToolWindow.Should().BeFalse();
        TesseraFlyoutWindowPolicy.SoftFrostCompositionFallbackAlpha.Should().Be((byte)0);
        TesseraFlyoutWindowPolicy.PresentableLayeredAlpha.Should().Be((byte)255);
        TesseraFlyoutWindowPolicy.ForbidDebugTitleChrome.Should().BeTrue();
        TesseraFlyoutWindowPolicy.HideUntilCompositionReady.Should().BeTrue(
            "SoftFrost cold Show must hide until layout or Try now flashes a black HWND");
    }

    [Fact]
    public void Soft_frost_must_hide_until_composition_ready()
    {
        // Transparent HWND paints black for 1+ frames before WinUI composition settles
        // (Try now / cold Show). Host must Show at Opacity 0, finish layout, then reveal.
        TesseraFlyoutWindowPolicy.SoftFrostHwndReady.Should().BeTrue();
        TesseraFlyoutWindowPolicy.HideUntilCompositionReady.Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Opaque_recovery_shell_alpha_stays_presentable(bool softFrost)
    {
        var material = TesseraFlyoutMaterialFactory.Create(softFrost);
        var alpha = TesseraFlyoutWindowPolicy.ResolveWindowBackgroundAlpha(material);
        alpha.Should().Be(material.ShellAlpha);
        alpha.Should().BeGreaterThanOrEqualTo(TesseraFlyoutWindowPolicy.MinPresentableFallbackAlpha);
    }

    [Fact]
    public void Composition_fallback_alpha_is_zero_while_soft_frost_hwnd_ship_gate_on()
    {
        // Behavior follows SoftFrostHwndReady, not the material acrylic flag.
        foreach (var frost in new[] { true, false })
        {
            TesseraFlyoutWindowPolicy
                .ResolveCompositionFallbackAlpha(TesseraFlyoutMaterialFactory.Create(frost))
                .Should().Be((byte)0);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Resolve_transparency_hints_never_request_os_acrylic(bool softFrostMaterial)
    {
        var material = TesseraFlyoutMaterialFactory.Create(softFrostMaterial);
        material.TransparencyHints.Should().Contain("Transparent");
        material.TransparencyHints.Should().NotContain("AcrylicBlur");
        material.TransparencyHints.Should().NotContain("Blur");

        // Ship SoftFrost: opaque tool window off → honor material Transparent hints.
        TesseraFlyoutWindowPolicy.MustRequestOpaqueToolWindow.Should().BeFalse();
        TesseraFlyoutWindowPolicy.ResolveTransparencyHints(material).Should().Equal("Transparent");
    }
}
