using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests;

public class TesseraFlyoutGlassPolicyTests
{
    [Fact]
    public void Glass_background_must_not_claim_available_size()
    {
        TesseraFlyoutGlassPolicy.GlassBackgroundClaimsAvailableSize.Should().BeFalse(
            "claiming available height Y-stretches Amber/CoreUI/Fluent/Win11 Stretch tracks");
    }

    [Fact]
    public void Live_backdrop_pixel_sampling_stays_forbidden()
    {
        TesseraFlyoutGlassPolicy.ForbidLiveBackdropPixelSampling.Should().BeTrue(
            "shared-backdrop scaffold stays dormant; GDI/self-capture blanks Soft frost");
    }

    [Fact]
    public void Config_or_export_preview_may_use_embedded_simple_shells()
    {
        TesseraFlyoutGlassPolicy.PreviewMayUseEmbeddedSimple.Should().BeTrue();
        TesseraFlyoutGlassPolicy.ShouldUseEmbeddedPreviewBuild(isConfigOrExportPreview: true)
            .Should().BeTrue();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Embedded_preview_for_live_depends_on_soft_frost_hwnd(
        bool softFrostHwndReady,
        bool expectEmbedded)
    {
        TesseraFlyoutGlassPolicy
            .ShouldUseEmbeddedPreviewBuild(isConfigOrExportPreview: false, softFrostHwndReady)
            .Should().Be(expectEmbedded);
    }

    [Theory]
    [InlineData(true, true, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public void Backdrop_blur_stays_off_while_sampling_forbidden(
        bool softFrostHwndReady,
        bool settingsWantBlur,
        bool expectEnabled)
    {
        TesseraFlyoutGlassPolicy.ForbidLiveBackdropPixelSampling.Should().BeTrue();
        TesseraFlyoutGlassPolicy
            .ShouldEnableBackdropBlur(softFrostHwndReady, settingsWantBlur)
            .Should().Be(expectEnabled);
        TesseraFlyoutGlassPolicy
            .ShouldAllowGdiScreenCapture(softFrostHwndReady, settingsWantBlur)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(true, true, TesseraFlyoutGlassMode.SkiaFallback)]
    [InlineData(true, false, TesseraFlyoutGlassMode.SkiaFallback)]
    [InlineData(false, true, TesseraFlyoutGlassMode.EmbeddedSimple)]
    [InlineData(false, false, TesseraFlyoutGlassMode.EmbeddedSimple)]
    public void Resolve_live_mode_matches_soft_frost_stage(
        bool softFrostHwndReady,
        bool useBackdropBlur,
        TesseraFlyoutGlassMode expected)
    {
        TesseraFlyoutGlassPolicy
            .ResolveLiveMode(softFrostHwndReady, useBackdropBlur)
            .Should().Be(expected);
    }

    [Fact]
    public void Soft_frost_material_still_asks_for_transparent_not_os_acrylic()
    {
        // R5: OS Acrylic/Mica stays shelved, material contract forces Transparent only.
        var m = TesseraFlyoutMaterialFactory.Create(useAcrylic: true);
        m.TransparencyHints.Should().Equal("Transparent");
        m.TransparencyHints.Should().NotContain("AcrylicBlur");
        m.TransparencyHints.Should().NotContain("Blur");
        m.TransparencyHints.Should().NotContain("Mica");
    }
}
