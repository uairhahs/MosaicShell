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
            // Under 40% the blur of a bright wallpaper washes out white text; over 55% it reads as a solid slab again.
            double opacity = NativeTranslucencyAcrylicSpec.TintAlpha / 255.0;

            _ = opacity.Should().BeInRange(0.40, 0.55);
            _ = (NativeTranslucencyAcrylicSpec.TintR, NativeTranslucencyAcrylicSpec.TintG, NativeTranslucencyAcrylicSpec.TintB)
                .Should().Be((TesseraFocusDimPolicy.CrustR, TesseraFocusDimPolicy.CrustG, TesseraFocusDimPolicy.CrustB),
                    "the tint is Tessera's crust, as the frost is");
        }

        [Fact]
        public void Noise_and_highlight_are_faint_texture_not_a_pattern()
        {
            _ = NativeTranslucencyAcrylicSpec.NoiseAlpha.Should().BeInRange(8, 24);
            _ = NativeTranslucencyAcrylicSpec.HighlightTopAlpha.Should().BeInRange(10, 40);
            _ = NativeTranslucencyAcrylicSpec.HighlightBottomAlpha.Should().BeLessThan(NativeTranslucencyAcrylicSpec.HighlightTopAlpha);
            _ = NativeTranslucencyAcrylicSpec.HighlightExtent.Should().BeInRange(0.2f, 0.6f);
        }
    }
}
