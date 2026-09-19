using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    public class TesseraFlyoutMaterialTests
    {
        [Fact]
        public void Soft_frost_never_requests_os_acrylic_or_blur()
        {
            TesseraFlyoutMaterial m = TesseraFlyoutMaterialFactory.Create(useAcrylic: true);
            _ = m.UseSoftFrost.Should().BeTrue();
            _ = m.TransparencyHints.Should().Equal("Transparent");
            _ = m.TransparencyHints.Should().NotContain("AcrylicBlur");
            _ = m.TransparencyHints.Should().NotContain("Blur");
        }

        [Fact]
        public void Soft_frost_uses_edge_blend()
        {
            TesseraFlyoutMaterial m = TesseraFlyoutMaterialFactory.Create(useAcrylic: true);
            _ = m.UseEdgeBlend.Should().BeTrue();
            _ = m.ShouldLockClientSize.Should().BeFalse();
        }

        [Fact]
        public void Solid_mode_is_Transparent_only_without_edge_blend()
        {
            TesseraFlyoutMaterial m = TesseraFlyoutMaterialFactory.Create(useAcrylic: false);
            _ = m.UseSoftFrost.Should().BeFalse();
            _ = m.UseEdgeBlend.Should().BeFalse();
            _ = m.TransparencyHints.Should().Equal("Transparent");
        }

        [Fact]
        public void Soft_frost_shell_alpha_is_translucent_not_see_through()
        {
            TesseraFlyoutMaterial m = TesseraFlyoutMaterialFactory.Create(useAcrylic: true);
            _ = m.ShellAlpha.Should().BeInRange(170, 210);
        }

        [Fact]
        public void Solid_shell_alpha_is_more_opaque()
        {
            TesseraFlyoutMaterial m = TesseraFlyoutMaterialFactory.Create(useAcrylic: false);
            _ = m.ShellAlpha.Should().BeGreaterThanOrEqualTo(220);
        }

        [Fact]
        public void Payload_acrylic_flag_parses()
        {
            _ = TesseraFlyoutMaterialFactory.UseAcrylicFromPayload(
                new Dictionary<string, string> { ["acrylic"] = "1" }).Should().BeTrue();
            _ = TesseraFlyoutMaterialFactory.UseAcrylicFromPayload(
                new Dictionary<string, string> { ["acrylic"] = "0" }).Should().BeFalse();
            _ = TesseraFlyoutMaterialFactory.UseAcrylicFromPayload(null).Should().BeTrue();
        }

        [Fact]
        public void Other_styles_still_use_soft_frost_by_default()
        {
            TesseraFlyoutMaterial m = TesseraFlyoutMaterialFactory.FromPayload(
                null, MosaicShell.Core.Styles.StyleIds.Fluent, "vol");
            _ = m.UseSoftFrost.Should().BeTrue();
        }

        /// <summary>
        /// Every style whose chrome leaves real unpainted window area with no local background to
        /// fall back on - MaterialYou's inter-pill gaps, PlainText's slant-clipped card sliver -
        /// must never touch OS Acrylic or the Skia soft-frost glass, regardless of settings.
        /// Confirmed as a real, reproducible defect for MaterialYou (2026-09-07): AcrylicBlur
        /// reported success identically on two monitors while only one actually composited it,
        /// leaving raw desktop passthrough in the unpainted gap on the other. The window itself
        /// stays plain <c>Transparent</c> either way - true per-pixel alpha, not an OS material -
        /// so the desktop still shows through those gaps exactly as intended; only the two glass
        /// paths are excluded.
        /// <para>
        /// Gnome/Meter/Compact/ModernFlyouts have a visually similar single-shell fallback gap
        /// (a <c>StackPanel</c> with real <c>Spacing</c> between the volume and media pills when
        /// H3 stacked multi-window isn't engaged), but unlike MaterialYou/PlainText they also
        /// have a legitimate, tested, working stacked-window acrylic mode
        /// (<c>SupportsStackedOsAcrylic: true</c>, each HWND is one pill with no internal gap) -
        /// see <c>TesseraOsAcrylicStackedPolicyTests.Stacked_volume_media_requests_acrylic_blur_on_each_hwnd</c>.
        /// Setting <c>RequiresMatteChrome</c> on them blocks that legitimate mode too, since the
        /// field is a blanket per-style switch with no notion of which render path is actually in
        /// play; confirmed as a regression, not applied. Their gap needs a narrower fix scoped to
        /// the single-shell fallback specifically, not this list.
        /// </para>
        /// </summary>
        public static TheoryData<string> MatteChromeOnlyStyles =>
        [
            MosaicShell.Core.Styles.StyleIds.MaterialYou,
            MosaicShell.Core.Styles.StyleIds.PlainText,
        ];

        [Theory]
        [MemberData(nameof(MatteChromeOnlyStyles))]
        public void Matte_chrome_style_declares_it_on_the_profile(string styleId)
        {
            _ = TesseraFlyoutTweenTargetCatalog.ResolveProfile(styleId).RequiresMatteChrome.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(MatteChromeOnlyStyles))]
        public void Matte_chrome_style_never_uses_glass_regardless_of_acrylic_payload(string styleId)
        {
            foreach (string? acrylicPayloadValue in new string?[] { "1", "0", null })
            {
                Dictionary<string, string>? payload = acrylicPayloadValue is null
                    ? null
                    : new Dictionary<string, string> { ["acrylic"] = acrylicPayloadValue };

                TesseraFlyoutMaterial m = TesseraFlyoutMaterialFactory.FromPayload(payload, styleId, "vol");
                _ = m.UseSoftFrost.Should().BeFalse();
                _ = m.TransparencyHints.Should().Equal("Transparent");
                _ = m.TransparencyHints.Should().NotContain("AcrylicBlur");
                _ = m.UseEdgeBlend.Should().BeFalse();
            }
        }

        [Theory]
        [MemberData(nameof(MatteChromeOnlyStyles))]
        public void Matte_chrome_style_never_reports_os_acrylic_eligible(string styleId)
        {
            // Deterministic overload: bypasses HostLaunchOptions/settings-file reads so this
            // isolates the per-style exclusion from environment-dependent trial state.
            _ = TesseraOsAcrylicTrialPolicy.IsEligible(
                    payload: null,
                    trialRequested: true,
                    osSupportsWinUiAcrylic: true,
                    styleId: styleId)
                .Should().BeFalse();
        }
    }
}
