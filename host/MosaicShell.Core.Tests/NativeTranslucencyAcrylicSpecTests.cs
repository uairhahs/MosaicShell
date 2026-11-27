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
        [InlineData(NativeTranslucencyBackend.WindowsAcrylic, false)]
        [InlineData(NativeTranslucencyBackend.None, false)]
        public void Only_a_blur_only_backend_needs_the_app_to_paint_the_acrylic_layers(
            NativeTranslucencyBackend backend, bool expected)
        {
            _ = NativeTranslucencyAcrylicSpec.AppPaintsAcrylicLayers(backend).Should().Be(expected);
        }

        [Fact]
        public void The_tint_lets_the_blur_through_but_keeps_text_readable()
        {
            // Glass, not a slab: thin layers let the colours behind through. At 45% the dark tint greyed the blur out,
            // and at 20% shade plus 14% frost it still held back too much light. Text reads by its own shadow instead.
            double shade = NativeTranslucencyAcrylicSpec.TintAlpha / 255.0;
            double frost = NativeTranslucencyAcrylicSpec.FrostAlpha / 255.0;

            _ = shade.Should().BeInRange(0.05, 0.15);
            _ = frost.Should().BeInRange(0.03, 0.10);
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
