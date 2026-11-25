using MosaicShell.Core.Capabilities.Platform;

namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// The layers that turn a plain backdrop blur into acrylic: a translucent tint, faint noise and a soft highlight
    /// from the top. Windows draws them as part of AcrylicBlur. KWin's blur effect draws only the blur, so on KDE the
    /// card paints them over it; Host reads these values, never parallel numbers.
    /// </summary>
    public static class NativeTranslucencyAcrylicSpec
    {
        /// <summary>Tessera's crust, as the frost uses.</summary>
        public const byte TintR = TesseraFocusDimPolicy.CrustR;
        public const byte TintG = TesseraFocusDimPolicy.CrustG;
        public const byte TintB = TesseraFocusDimPolicy.CrustB;

        /// <summary>About 45%: the blurred desktop shows, white text stays readable over a bright wallpaper.</summary>
        public const byte TintAlpha = 115;

        /// <summary>Overlay grain, the texture that tells acrylic from a smooth gradient.</summary>
        public const byte NoiseAlpha = 14;

        /// <summary>White added at the card's top edge, fading to <see cref="HighlightBottomAlpha"/>.</summary>
        public const byte HighlightTopAlpha = 24;

        public const byte HighlightBottomAlpha = 0;

        /// <summary>How far down the card (fraction of its height) the highlight fades out.</summary>
        public const float HighlightExtent = 0.45f;

        /// <summary>Whether the OS leaves the acrylic layers to the app.</summary>
        public static bool AppPaintsAcrylicLayers(NativeTranslucencyBackend backend)
        {
            return backend == NativeTranslucencyBackend.KWinBlur;
        }
    }
}
