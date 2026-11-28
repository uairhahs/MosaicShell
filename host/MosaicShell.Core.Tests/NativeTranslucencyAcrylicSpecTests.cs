using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Windows acrylic is blur plus tint, luminosity and noise, done by the OS. KWin's blur is only the blur, so on KDE
    /// the card paints the rest itself, or it reads as a flat dark fill (reported 2026-11-22).
    /// </summary>
    public sealed class NativeTranslucencyAcrylicSpecTests
    {
        [Theory]
        [InlineData(NativeTranslucencyBackend.KWinBlur, true)]
        [InlineData(NativeTranslucencyBackend.WindowsAcrylic, true)]
        [InlineData(NativeTranslucencyBackend.None, false)]
        public void Every_blur_backend_draws_only_the_blur_so_the_app_paints_the_layers(
            NativeTranslucencyBackend backend, bool expected)
        {
            _ = NativeTranslucencyAcrylicSpec.AppPaintsAcrylicLayers(backend).Should().Be(expected);
        }

        [Theory]
        [InlineData(NativeTranslucencyBackend.KWinBlur, true)]
        [InlineData(NativeTranslucencyBackend.WindowsAcrylic, false)]
        [InlineData(NativeTranslucencyBackend.None, false)]
        public void Only_KWin_gets_the_plain_veil_and_edge_Windows_shows_its_acrylic_bare(
            NativeTranslucencyBackend backend, bool expected)
        {
            // Asked 2026-11-28: Native Translucency on Windows is Windows' own material, with no shade painted over it.
            _ = NativeTranslucencyAcrylicSpec.PaintsPlainVeil(backend).Should().Be(expected);
        }

        [Theory]
        [InlineData(NativeTranslucencyBackend.KWinBlur, true)]
        [InlineData(NativeTranslucencyBackend.WindowsAcrylic, false)]
        public void Frosted_has_its_light_edge_on_KWin_only(NativeTranslucencyBackend backend, bool expected)
        {
            // Windows cuts the window with a hard-edged region and the flyout scale puts the line between pixels: at one
            // unit it smeared over two or three, at one pixel it still read harsh. Asked 2026-11-28: no edge on Windows.
            _ = NativeTranslucencyAcrylicSpec.FrostedHasEdge(backend).Should().Be(expected);
        }

        [Fact]
        public void Frosted_text_shadow_is_lighter_and_tighter_than_the_plain_glass_one()
        {
            // At the plain glass's 0.6 and 6 the frosted text wore a dark halo that read as an old Aero glow (2026-11-28).
            // Frosted has its shade under the text, so it needs less.
            _ = NativeTranslucencyAcrylicSpec.ContentShadowOpacityFor(frosted: true).Should().BeInRange(0.2, 0.4);
            _ = NativeTranslucencyAcrylicSpec.ContentShadowBlurFor(frosted: true).Should().BeInRange(2, 4);
            _ = NativeTranslucencyAcrylicSpec.ContentShadowOpacityFor(frosted: false).Should().Be(NativeTranslucencyAcrylicSpec.ContentShadowOpacity);
            _ = NativeTranslucencyAcrylicSpec.ContentShadowBlurFor(frosted: false).Should().Be(NativeTranslucencyAcrylicSpec.ContentShadowBlur);
        }

        [Fact]
        public void The_tint_lets_the_blur_through_but_keeps_text_readable()
        {
            // Glass, not a slab: thin layers let the colours behind through. At 45% the dark tint greyed the blur out,
            // and at 20% shade plus 14% frost it still held back too much light. Text reads by its own shadow instead.
            double shade = NativeTranslucencyAcrylicSpec.TintAlpha / 255.0;
            double frost = NativeTranslucencyAcrylicSpec.FrostAlpha / 255.0;

            _ = shade.Should().BeInRange(0.05, 0.15);
            _ = frost.Should().BeInRange(0.03, 0.06, "at 7% Frosted read too white");
            _ = NativeTranslucencyAcrylicSpec.ContentShadowOpacity.Should().BeInRange(0.4, 0.8);
            _ = NativeTranslucencyAcrylicSpec.ContentShadowBlur.Should().BeInRange(4, 10, "a soft halo, not a hard drop");
            _ = NativeTranslucencyAcrylicSpec.EdgeAlpha.Should().BeGreaterThan(64, "brighter than the other materials' edge");
            _ = (NativeTranslucencyAcrylicSpec.TintR, NativeTranslucencyAcrylicSpec.TintG, NativeTranslucencyAcrylicSpec.TintB)
                .Should().Be((TesseraFocusDimPolicy.CrustR, TesseraFocusDimPolicy.CrustG, TesseraFocusDimPolicy.CrustB),
                    "the tint is Tessera's crust, as the frost is");
        }

        [Fact]
        public void Noise_and_highlight_are_faint_texture_not_a_pattern()
        {
            _ = NativeTranslucencyAcrylicSpec.NoiseAlpha.Should().BeInRange(1, 6, "dithering against banding, not a visible grain");
            _ = NativeTranslucencyAcrylicSpec.HighlightTopAlpha.Should().BeInRange(10, 40);
            _ = NativeTranslucencyAcrylicSpec.HighlightBottomAlpha.Should().BeLessThan(NativeTranslucencyAcrylicSpec.HighlightTopAlpha);
            _ = NativeTranslucencyAcrylicSpec.HighlightExtent.Should().BeInRange(0.2f, 0.6f);
        }
    }
}
