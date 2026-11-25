using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// Manual Win11 native translucency evaluation sign-off (H2 single-shell + H3 stacked N-window).
    /// Visual acceptance only; alpha still ships Skia frost unless
    /// <see cref="Capabilities.Platform.HostLaunchOptions.TesseraNativeTranslucencyTrialFlag"/> is set.
    /// H4 ship/kill (persisted setting or kill-switch) is a separate decision.
    /// </summary>
    public static class TesseraNativeTranslucencySignOffPolicy
    {

        /// <summary>H2: single-shell NativeTranslucency on Win11 (--tessera-native-translucency, showMediaStrip=0).</summary>
        public const bool H2Win11EvalSignedOff = true;

        /// <summary>H3 phase 1: N-window stacked volume+media acrylic on Win11.</summary>
        public const bool H3StackedVolumeMediaSignedOff = true;

        /// <summary>Both H2 and H3 manual Win11 rows passed. Does not imply H4 ship.</summary>
        public static bool Win11EvalComplete =>
            H2Win11EvalSignedOff && H3StackedVolumeMediaSignedOff;

        /// <summary>
        /// KDE Plasma (KWin blur behind the card's rounded region). On, so the Hub offers it there and the saved choice
        /// is authoritative while its look is compared with Windows.
        /// Turn off to fall back to the launch flag only, as Windows worked before H2.
        /// </summary>
        public static bool KdeEvalSignedOff => true;

        /// <summary>The sign-off that governs this OS: Windows' evaluation on Windows, KDE's elsewhere.</summary>
        public static bool EvalCompleteForThisPlatform =>
            OperatingSystem.IsWindows() ? Win11EvalComplete : KdeEvalSignedOff;

        /// <summary>
        /// The Hub offers Native Translucency: this platform is signed off and this system has a backend (not GNOME,
        /// not KWin with blur off). On Linux the backend is found shortly after start; see
        /// <see cref="Capabilities.Platform.NativeTranslucencyPlatform.Changed"/>.
        /// </summary>
        public static bool HubOffersNativeTranslucency =>
            EvalCompleteForThisPlatform && Capabilities.Platform.NativeTranslucencyPlatform.IsAvailable;

        private static readonly HashSet<string> H3StackedStylesSignedOff = new(StringComparer.OrdinalIgnoreCase)
        {
            StyleIds.Meter,
            StyleIds.Gnome,
            StyleIds.Compact,
            StyleIds.ModernFlyouts,
        };

        public static bool IsH3StackedStyleSignedOff(string? styleId)
        {
            return H3StackedStylesSignedOff.Contains(StyleIds.Normalize(styleId ?? string.Empty));
        }

        /// <summary>
        /// Every style that Host can present as N-window stacked acrylic under H3 is signed off.
        /// </summary>
        public static bool AllH3StackedStylesSignedOff()
        {
            foreach (string id in StyleCatalog.IdsFor("Tessera"))
            {
                if (TesseraStackedPlacementPolicy.SupportsStackedNativeTranslucency(id)
                    && !IsH3StackedStyleSignedOff(id))
                {
                    return false;
                }
            }

            return H3StackedStylesSignedOff.Count > 0;
        }
    }
}
