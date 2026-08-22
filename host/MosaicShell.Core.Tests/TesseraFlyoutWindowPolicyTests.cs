using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class TesseraFlyoutWindowPolicyTests
{
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
    public void Soft_frost_prefers_winui_composition_over_dxgi_black_clear()
    {
        TesseraFlyoutWindowPolicy.SoftFrostHwndReady.Should().BeTrue();
        TesseraFlyoutWindowPolicy.PreferWinUiCompositionForSoftFrost.Should().BeTrue();
    }

    [Fact]
    public void Soft_frost_composition_fallback_alpha_is_zero()
    {
        TesseraFlyoutWindowPolicy.SoftFrostHwndReady.Should().BeTrue();
        TesseraFlyoutWindowPolicy.SoftFrostCompositionFallbackAlpha.Should().Be((byte)0);

        foreach (var frost in new[] { true, false })
        {
            TesseraFlyoutWindowPolicy
                .ResolveCompositionFallbackAlpha(TesseraFlyoutMaterialFactory.Create(frost))
                .Should().Be((byte)0, "mocha ≥170 fallback matte-slabs Soft frost");
        }
    }

    [Fact]
    public void Soft_frost_hwnd_skips_opaque_lwa_alpha()
    {
        TesseraFlyoutWindowPolicy.SoftFrostHwndReady.Should().BeTrue();
        TesseraFlyoutWindowPolicy.MustApplyPresentableLayeredAlpha.Should().BeFalse(
            "LWA_ALPHA=255 makes the HWND opaque and blocks glass");
        TesseraFlyoutWindowPolicy.PresentableLayeredAlpha.Should().Be((byte)255);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Soft_frost_hwnd_honors_material_transparent_hints(bool softFrost)
    {
        var material = TesseraFlyoutMaterialFactory.Create(softFrost);
        material.TransparencyHints.Should().Contain("Transparent");

        TesseraFlyoutWindowPolicy.SoftFrostHwndReady.Should().BeTrue();
        TesseraFlyoutWindowPolicy.MustRequestOpaqueToolWindow.Should().BeFalse();

        var hints = TesseraFlyoutWindowPolicy.ResolveTransparencyHints(material);
        hints.Should().Equal("Transparent");
        hints.Should().NotContain("AcrylicBlur");
    }

    [Fact]
    public void Window_background_brush_is_transparent_for_soft_frost()
    {
        TesseraFlyoutWindowPolicy.WindowBackgroundBrushIsTransparent.Should().BeTrue();
    }

    [Fact]
    public void Host_must_not_wrap_flyout_in_debug_title_chrome()
    {
        TesseraFlyoutWindowPolicy.ForbidDebugTitleChrome.Should().BeTrue();
    }
}
