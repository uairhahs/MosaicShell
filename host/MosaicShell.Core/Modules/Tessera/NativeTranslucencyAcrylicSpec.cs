using MosaicShell.Core.Capabilities.Platform;

namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// The layers Frosted paints over the OS blur to turn it into frosted glass: a light shade, white frost, a soft
    /// highlight from the top, a crisp edge, barely any grain and a shadow under the content. Native Translucency is the
    /// plain blur, under <see cref="PlainTintAlpha"/> on KWin and bare on Windows (<see cref="PaintsPlainVeil"/>). Both backends draw only the blur: KWin's blur effect, and on Windows
    /// the blur Avalonia's AcrylicBlur level asks the compositor for (a Gaussian blur of the host backdrop, with no tint,
    /// luminosity or noise). Windows' own acrylic material (DWM's transient window backdrop) would carry those, but it
    /// draws only its solid fallback for a window that is never activated, as a flyout is (spike 2026-11-28). So the card
    /// paints them on both; Host reads these values, never parallel numbers.
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

        /// <summary>
        /// White frost over the shade, about 5%: the glass reads light and lets the colours behind through. At 7% it read
        /// too white (2026-11-28).
        /// </summary>
        public const byte FrostAlpha = 12;

        /// <summary>A crisp light edge, the line where the pane catches the light.</summary>
        public const byte EdgeAlpha = 110;

        /// <summary>
        /// The plain material's edge, a faint white line (drawn over, not blended): enough to find the pane's outline, far
        /// softer than Frosted's <see cref="EdgeAlpha"/>.
        /// </summary>
        public const byte PlainEdgeAlpha = 36;

        /// <summary>
        /// A soft dark shadow behind the card's text and icons. Thin glass over a bright wallpaper leaves white text with
        /// too little contrast; the shadow gives it back without darkening the whole pane.
        /// </summary>
        public const double ContentShadowOpacity = 0.6;

        public const double ContentShadowBlur = 6;

        public const double ContentShadowOffsetY = 1;

        /// <summary>
        /// Frosted's lighter text shadow: its shade already sits under the text, and at the plain glass's values the
        /// text wore a dark halo that read as an old Aero glow (2026-11-28).
        /// </summary>
        public const double FrostedContentShadowOpacity = 0.3;

        public const double FrostedContentShadowBlur = 3;

        public static double ContentShadowOpacityFor(bool frosted)
        {
            return frosted ? FrostedContentShadowOpacity : ContentShadowOpacity;
        }

        public static double ContentShadowBlurFor(bool frosted)
        {
            return frosted ? FrostedContentShadowBlur : ContentShadowBlur;
        }

        /// <summary>Overlay grain, barely there: enough to break up banding in the blur, not a visible texture.</summary>
        public const byte NoiseAlpha = 3;

        /// <summary>White added at the card's top edge, fading to <see cref="HighlightBottomAlpha"/>.</summary>
        public const byte HighlightTopAlpha = 18;

        public const byte HighlightBottomAlpha = 0;

        /// <summary>How far down the card (fraction of its height) the highlight fades out.</summary>
        public const float HighlightExtent = 0.45f;

        /// <summary>
        /// Whether the card's text and icons carry the content shadow: over a blur-only backdrop both finishes are light
        /// glass, so the content needs its own contrast.
        /// </summary>
        public static bool ShadowsContent(NativeTranslucencyBackend backend)
        {
            return AppPaintsAcrylicLayers(backend);
        }

        /// <summary>
        /// Whether Native Translucency paints <see cref="PlainTintAlpha"/>'s veil and <see cref="PlainEdgeAlpha"/>'s edge
        /// over the blur. Only on KWin: on Windows the plain material is Windows' own, shown bare (asked 2026-11-28).
        /// </summary>
        public static bool PaintsPlainVeil(NativeTranslucencyBackend backend)
        {
            return backend == NativeTranslucencyBackend.KWinBlur;
        }

        /// <summary>
        /// Whether Frosted draws its light edge (<see cref="EdgeAlpha"/>). Not on Windows: the window's hard-edged
        /// region and the flyout scale left it smeared or harsh however it was sized (asked 2026-11-28).
        /// </summary>
        public static bool FrostedHasEdge(NativeTranslucencyBackend backend)
        {
            return backend != NativeTranslucencyBackend.WindowsAcrylic;
        }

        /// <summary>Whether the OS leaves the acrylic layers to the app: every backend draws only the blur.</summary>
        public static bool AppPaintsAcrylicLayers(NativeTranslucencyBackend backend)
        {
            return backend != NativeTranslucencyBackend.None;
        }
    }
}
