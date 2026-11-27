using MosaicShell.Core.Capabilities.Platform;

namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// The layers Frosted paints over a blur-only backdrop (KWin) to turn it into frosted glass: a light shade, white
    /// frost, a soft highlight from the top, a crisp edge, barely any grain and a shadow under the content. Native
    /// Translucency there is the plain blur under <see cref="PlainTintAlpha"/>. Windows draws them as part of AcrylicBlur. KWin's blur effect draws only the blur, so on KDE the
    /// card paints them over it; Host reads these values, never parallel numbers.
    /// </summary>
    public static class NativeTranslucencyAcrylicSpec
    {
        /// <summary>Tessera's crust, as the frost uses.</summary>
        public const byte TintR = TesseraFocusDimPolicy.CrustR;
        public const byte TintG = TesseraFocusDimPolicy.CrustG;
        public const byte TintB = TesseraFocusDimPolicy.CrustB;

        /// <summary>
        /// Native Translucency's flat veil over the blur: a light neutral grey at about 10%. Over KWin only plain alpha
        /// reaches the desktop, so a dark tint can only darken the blur and keep its colour (it read as saturated, not as
        /// glass); a light grey lifts dark backdrops and washes strong colours a little, as a translucent pane does.
        /// The text reads by the content shadow, as on Frosted.
        /// </summary>
        public const byte PlainTintR = 0xD4;

        public const byte PlainTintG = 0xD4;

        public const byte PlainTintB = 0xDC;

        public const byte PlainTintAlpha = 26;

        /// <summary>About 12%: a light shade under the frost. Legibility comes from the content's shadow, not from here.</summary>
        public const byte TintAlpha = 30;

        /// <summary>White frost over the shade, about 7%: the glass reads light and lets the colours behind through.</summary>
        public const byte FrostAlpha = 18;

        /// <summary>A crisp light edge, the line where the pane catches the light.</summary>
        public const byte EdgeAlpha = 110;

        /// <summary>
        /// A soft dark shadow behind the card's text and icons. Thin glass over a bright wallpaper leaves white text with
        /// too little contrast; the shadow gives it back without darkening the whole pane.
        /// </summary>
        public const double ContentShadowOpacity = 0.6;

        public const double ContentShadowBlur = 6;

        public const double ContentShadowOffsetY = 1;

        /// <summary>Overlay grain, barely there: enough to break up banding in the blur, not a visible texture.</summary>
        public const byte NoiseAlpha = 3;

        /// <summary>White added at the card's top edge, fading to <see cref="HighlightBottomAlpha"/>.</summary>
        public const byte HighlightTopAlpha = 24;

        public const byte HighlightBottomAlpha = 0;

        /// <summary>How far down the card (fraction of its height) the highlight fades out.</summary>
        public const float HighlightExtent = 0.45f;

        /// <summary>
        /// Whether the card's text and icons carry the content shadow: over a blur-only backdrop both finishes are light
        /// glass, so the content needs its own contrast. Windows' acrylic tint keeps its own.
        /// </summary>
        public static bool ShadowsContent(NativeTranslucencyBackend backend)
        {
            return AppPaintsAcrylicLayers(backend);
        }

        /// <summary>Whether the OS leaves the acrylic layers to the app.</summary>
        public static bool AppPaintsAcrylicLayers(NativeTranslucencyBackend backend)
        {
            return backend == NativeTranslucencyBackend.KWinBlur;
        }
    }
}
