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
    public void Soft_frost_hwnd_ready_uses_skia_not_presentable_matte()
    {
        TesseraFlyoutWindowPolicy.SoftFrostHwndReady.Should().BeTrue();
        TesseraFlyoutGlassPolicy.PreferPresentableShellUntilSoftFrostHwnd.Should().BeTrue();

        TesseraFlyoutGlassPolicy.ShouldUseEmbeddedPreviewBuild(isConfigOrExportPreview: false)
            .Should().BeFalse();

        // Pixel sampling forbidden → SkiaFallback (translucent frost), not GDI SkiaBackdrop.
        TesseraFlyoutGlassPolicy
            .ResolveLiveMode(softFrostHwndReady: true, useBackdropBlur: true)
            .Should().Be(TesseraFlyoutGlassMode.SkiaFallback);
    }

    [Fact]
    public void Live_backdrop_pixel_sampling_is_forbidden_to_avoid_black_self_capture()
    {
        TesseraFlyoutGlassPolicy.ForbidLiveBackdropPixelSampling.Should().BeTrue();
        TesseraFlyoutGlassPolicy
            .ShouldEnableBackdropBlur(softFrostHwndReady: true, settingsWantBlur: true)
            .Should().BeFalse();
        TesseraFlyoutGlassPolicy
            .ShouldAllowGdiScreenCapture(softFrostHwndReady: true, settingsWantBlur: true)
            .Should().BeFalse();
    }

    [Fact]
    public void Opaque_hwnd_recovery_still_prefers_presentable_shell()
    {
        TesseraFlyoutGlassPolicy
            .ShouldUseEmbeddedPreviewBuild(isConfigOrExportPreview: false, softFrostHwndReady: false)
            .Should().BeTrue();

        TesseraFlyoutGlassPolicy
            .ResolveLiveMode(softFrostHwndReady: false, useBackdropBlur: true)
            .Should().Be(TesseraFlyoutGlassMode.EmbeddedSimple);
    }

    [Fact]
    public void Config_or_export_preview_may_use_embedded_simple_shells()
    {
        TesseraFlyoutGlassPolicy.PreviewMayUseEmbeddedSimple.Should().BeTrue();
        TesseraFlyoutGlassPolicy.ShouldUseEmbeddedPreviewBuild(isConfigOrExportPreview: true)
            .Should().BeTrue();
    }

    [Fact]
    public void Backdrop_blur_requires_soft_frost_settings_and_sampling_allowed()
    {
        TesseraFlyoutGlassPolicy
            .ShouldEnableBackdropBlur(softFrostHwndReady: false, settingsWantBlur: true)
            .Should().BeFalse();

        TesseraFlyoutGlassPolicy
            .ShouldEnableBackdropBlur(softFrostHwndReady: true, settingsWantBlur: false)
            .Should().BeFalse();

        // Sampling currently forbidden — Soft frost uses translucent Skia frost only.
        TesseraFlyoutGlassPolicy.ForbidLiveBackdropPixelSampling.Should().BeTrue();
        TesseraFlyoutGlassPolicy
            .ShouldEnableBackdropBlur(softFrostHwndReady: true, settingsWantBlur: true)
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
        var m = TesseraFlyoutMaterialFactory.Create(useAcrylic: true);
        m.TransparencyHints.Should().Equal("Transparent");
        m.TransparencyHints.Should().NotContain("AcrylicBlur");
        m.TransparencyHints.Should().NotContain("Blur");
    }
}
