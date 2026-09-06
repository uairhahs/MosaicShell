using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class TesseraFlyoutGlassPolicyTests
    {
        [Fact]
        public void Glass_background_must_not_claim_available_size()
        {
            _ = TesseraFlyoutGlassPolicy.GlassBackgroundClaimsAvailableSize.Should().BeFalse(
                "claiming available height Y-stretches Amber/CoreUI/Fluent/Win11 Stretch tracks");
        }

        [Fact]
        public void Live_backdrop_pixel_sampling_stays_forbidden()
        {
            _ = TesseraFlyoutGlassPolicy.ForbidLiveBackdropPixelSampling.Should().BeTrue(
                "shared-backdrop scaffold stays dormant; GDI/self-capture blanks Soft frost");
        }

        [Fact]
        public void Config_or_export_preview_may_use_embedded_simple_shells()
        {
            _ = TesseraFlyoutGlassPolicy.PreviewMayUseEmbeddedSimple.Should().BeTrue();
            _ = TesseraFlyoutGlassPolicy.ShouldUseEmbeddedPreviewBuild(isConfigOrExportPreview: true)
                .Should().BeTrue();
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void Embedded_preview_for_live_depends_on_soft_frost_hwnd(
            bool softFrostHwndReady,
            bool expectEmbedded)
        {
            _ = TesseraFlyoutGlassPolicy
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
            _ = TesseraFlyoutGlassPolicy.ForbidLiveBackdropPixelSampling.Should().BeTrue();
            _ = TesseraFlyoutGlassPolicy
                .ShouldEnableBackdropBlur(softFrostHwndReady, settingsWantBlur)
                .Should().Be(expectEnabled);
            _ = TesseraFlyoutGlassPolicy
                .ShouldAllowGdiScreenCapture(softFrostHwndReady, settingsWantBlur)
                .Should().BeFalse();
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        public void Simulated_backdrop_blur_follows_user_setting_when_hwnd_ready(
            bool softFrostHwndReady,
            bool settingsWantBlur,
            bool expectSimulated)
        {
            _ = TesseraFlyoutGlassPolicy
                .ShouldUseSimulatedBackdropBlur(softFrostHwndReady, settingsWantBlur)
                .Should().Be(expectSimulated);
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
            _ = TesseraFlyoutGlassPolicy
                .ResolveLiveMode(softFrostHwndReady, useBackdropBlur)
                .Should().Be(expected);
        }

        [Fact]
        public void Default_frost_material_asks_for_transparent_not_os_acrylic()
        {
            TesseraFlyoutMaterial m = TesseraFlyoutMaterialFactory.Create(useAcrylic: true);
            _ = m.TransparencyHints.Should().Equal("Transparent");
            _ = m.TransparencyHints.Should().NotContain("AcrylicBlur");
            _ = m.TransparencyHints.Should().NotContain("Blur");
            _ = m.TransparencyHints.Should().NotContain("Mica");
        }

        [Theory]
        [InlineData(TesseraFlyoutGlassMode.EmbeddedSimple, false)]
        [InlineData(TesseraFlyoutGlassMode.SkiaFallback, true)]
        [InlineData(TesseraFlyoutGlassMode.OsAcrylic, true)]
        [InlineData(TesseraFlyoutGlassMode.SkiaBackdrop, true)]
        public void Meter_inner_skia_glass_suppressed_on_live_flyout_shells(
            TesseraFlyoutGlassMode mode,
            bool expectSuppressed)
        {
            _ = TesseraFlyoutGlassPolicy.SuppressMeterInnerSkiaGlass(mode).Should().Be(expectSuppressed);
        }
    }
}
