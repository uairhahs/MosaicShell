namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// Flyout material policy. Soft frost = translucent crust + chrome edge blend.
    /// OS AcrylicBlur is opt-in via <see cref="TesseraOsAcrylicTrialPolicy"/> (default Skia frost).
    /// </summary>
    public sealed record TesseraFlyoutMaterial(
        bool UseSoftFrost,
        IReadOnlyList<string> TransparencyHints,
        byte ShellAlpha,
        bool ShouldLockClientSize,
        bool UseEdgeBlend);

    public static class TesseraFlyoutMaterialFactory
    {
        public const byte SoftFrostShellAlpha = 188;
        public const byte SolidShellAlpha = 232;

        /// <param name="useAcrylic">Legacy setting name - means soft frost tint, not OS acrylic.</param>
        /// <param name="osAcrylicEligible">When true, request AcrylicBlur with Transparent fallback.</param>
        public static TesseraFlyoutMaterial Create(bool useAcrylic, bool osAcrylicEligible = false)
        {
            if (!useAcrylic)
            {
                return new TesseraFlyoutMaterial(
                    UseSoftFrost: false,
                    TransparencyHints: ["Transparent"],
                    ShellAlpha: SolidShellAlpha,
                    ShouldLockClientSize: false,
                    UseEdgeBlend: false);
            }

            IReadOnlyList<string> hints = osAcrylicEligible
                ? ["AcrylicBlur", "Transparent"]
                : ["Transparent"];

            return new TesseraFlyoutMaterial(
                UseSoftFrost: true,
                TransparencyHints: hints,
                ShellAlpha: SoftFrostShellAlpha,
                ShouldLockClientSize: false,
                UseEdgeBlend: true);
        }

        /// <summary>Payload key <c>acrylic</c>: "0" off, anything else / missing → soft frost on.</summary>
        public static bool UseAcrylicFromPayload(IReadOnlyDictionary<string, string>? payload)
        {
            return payload is null || !payload.TryGetValue("acrylic", out string? raw) || string.IsNullOrWhiteSpace(raw) || raw is not ("0" or "false" or "False" or "off" or "Off");
        }

        public static TesseraFlyoutMaterial FromPayload(
            IReadOnlyDictionary<string, string>? payload,
            string? styleId = null,
            string? kind = null)
        {
            // A style whose spec calls for matte chrome only (MaterialYou's pills, PlainText's
            // slant-clipped card) must never touch OS Acrylic or the Skia soft-frost glass,
            // regardless of settings - the unpainted window area around its shape has no local
            // background to fall back on if a glass material fails to render, unlike every other
            // style. The window itself still uses plain Transparent (true per-pixel alpha, not an
            // OS material) so the desktop shows through those gaps exactly as before; only the two
            // glass paths are excluded, not window transparency itself.
            return TesseraFlyoutTweenTargetCatalog.ResolveProfile(styleId).RequiresMatteChrome
                ? Create(useAcrylic: false)
                : Create(
                    UseAcrylicFromPayload(payload),
                    osAcrylicEligible: OsAcrylicEligibleFromPayload(payload, styleId, kind));
        }

        /// <summary>
        /// Single-shell (<see cref="TesseraOsAcrylicTrialPolicy"/>) or stacked N-window
        /// (<see cref="TesseraOsAcrylicStackedPolicy"/>) OS acrylic trial.
        /// Status kinds never take stacked acrylic eligibility.
        /// </summary>
        public static bool OsAcrylicEligibleFromPayload(
            IReadOnlyDictionary<string, string>? payload,
            string? styleId = null,
            string? kind = null)
        {
            return TesseraOsAcrylicTrialPolicy.IsEligibleFromPayload(payload, styleId)
            || (!TesseraStatusFlyoutPolicy.MustUseDedicatedSingleWindow(kind)
                && TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(payload, styleId, kind));
        }
    }
}
