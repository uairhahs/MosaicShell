using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// Manual Win11 OS acrylic evaluation sign-off (H2 single-shell + H3 stacked N-window).
    /// Visual acceptance only; alpha still ships Skia frost unless
    /// <see cref="Capabilities.Platform.HostLaunchOptions.TesseraOsAcrylicTrialFlag"/> is set.
    /// H4 ship/kill (persisted setting or kill-switch) is a separate decision.
    /// </summary>
    public static class TesseraOsAcrylicSignOffPolicy
    {
        /// <summary>UTC date of formal Win11 acrylic eval sign-off.</summary>
        public const string SignedOffDate = "2026-08-23";

        /// <summary>H2: single-shell OsAcrylic on Win11 (--tessera-os-acrylic, showMediaStrip=0).</summary>
        public const bool H2Win11EvalSignedOff = true;

        /// <summary>H3 phase 1: N-window stacked volume+media acrylic on Win11.</summary>
        public const bool H3StackedVolumeMediaSignedOff = true;

        /// <summary>Both H2 and H3 manual Win11 rows passed. Does not imply H4 ship.</summary>
        public static bool Win11EvalComplete =>
            H2Win11EvalSignedOff && H3StackedVolumeMediaSignedOff;

        /// <summary>Scratch eval notes (gitignored): .local/Tessera/os-acrylic-eval/</summary>
        public const string EvalScratchRelativeDirectory = ".local/Tessera/os-acrylic-eval";

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
                if (TesseraStackedPlacementPolicy.SupportsStackedOsAcrylic(id)
                    && !IsH3StackedStyleSignedOff(id))
                {
                    return false;
                }
            }

            return H3StackedStylesSignedOff.Count > 0;
        }
    }
}
