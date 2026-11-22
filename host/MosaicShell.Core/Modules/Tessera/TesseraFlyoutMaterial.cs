using MosaicShell.Core.Capabilities.Platform;

namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// Flyout material policy. Soft frost = translucent crust + chrome edge blend.
    /// Native Translucency (the OS blurs the desktop behind the card) is opt-in via
    /// <see cref="TesseraNativeTranslucencyPolicy"/> (default Skia frost).
    /// </summary>
    /// <remarks>
    /// <c>UseNativeTranslucency</c>: the OS draws the material behind the window. Windows does it through the AcrylicBlur
    /// hint; on KWin the hints stay Transparent and the Host sets the blur region, so this flag, not the hints, says
    /// which windows are translucent.
    /// </remarks>
    public sealed record TesseraFlyoutMaterial(
        bool UseSoftFrost,
        IReadOnlyList<string> TransparencyHints,
        byte ShellAlpha,
        bool ShouldLockClientSize,
        bool UseEdgeBlend,
        bool UseNativeTranslucency = false)
    {
        /// <summary>
        /// Whether a window built with <paramref name="other"/> already has this material. The transparency hints, the
        /// shell alpha and the OS blur are set when the window is built; a live patch does not change them, so a
        /// request whose material differs must rebuild the window.
        /// </summary>
        public bool SameWindowMaterial(TesseraFlyoutMaterial other)
        {
            return UseSoftFrost == other.UseSoftFrost
                && ShellAlpha == other.ShellAlpha
                && UseEdgeBlend == other.UseEdgeBlend
                && UseNativeTranslucency == other.UseNativeTranslucency
                && TransparencyHints.SequenceEqual(other.TransparencyHints, StringComparer.OrdinalIgnoreCase);
        }
    }

    public static class TesseraFlyoutMaterialFactory
    {
        public const byte SoftFrostShellAlpha = 188;
        public const byte SolidShellAlpha = 232;

        /// <param name="useAcrylic">Legacy setting name - means soft frost tint, not native translucency.</param>
        /// <param name="nativeTranslucencyEligible">When true, ask the backend for the OS material.</param>
        /// <param name="backend">
        /// Which OS draws it; null for this process's <see cref="NativeTranslucencyPlatform.Backend"/>. Windows asks for
        /// AcrylicBlur with a Transparent fallback; KWin needs only Transparent.
        /// </param>
        public static TesseraFlyoutMaterial Create(
            bool useAcrylic,
            bool nativeTranslucencyEligible = false,
            NativeTranslucencyBackend? backend = null)
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

            NativeTranslucencyBackend native = nativeTranslucencyEligible
                ? backend ?? NativeTranslucencyPlatform.Backend
                : NativeTranslucencyBackend.None;
            IReadOnlyList<string> hints = native == NativeTranslucencyBackend.WindowsAcrylic
                ? ["AcrylicBlur", "Transparent"]
                : ["Transparent"];

            return new TesseraFlyoutMaterial(
                UseSoftFrost: true,
                TransparencyHints: hints,
                ShellAlpha: SoftFrostShellAlpha,
                ShouldLockClientSize: false,
                UseEdgeBlend: true,
                UseNativeTranslucency: native != NativeTranslucencyBackend.None);
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
            // slant-clipped card) must never touch Native Translucency or the Skia soft-frost glass,
            // regardless of settings - the unpainted window area around its shape has no local
            // background to fall back on if a glass material fails to render, unlike every other
            // style. The window itself still uses plain Transparent (true per-pixel alpha, not an
            // OS material) so the desktop shows through those gaps exactly as before; only the two
            // glass paths are excluded, not window transparency itself.
            return TesseraFlyoutTweenTargetCatalog.ResolveProfile(styleId).RequiresMatteChrome
                ? Create(useAcrylic: false)
                : Create(
                    UseAcrylicFromPayload(payload),
                    nativeTranslucencyEligible: NativeTranslucencyEligibleFromPayload(payload, styleId, kind));
        }

        /// <summary>
        /// Single-shell (<see cref="TesseraNativeTranslucencyPolicy"/>) or stacked N-window
        /// (<see cref="TesseraNativeTranslucencyStackedPolicy"/>) native translucency trial.
        /// Status kinds never take stacked acrylic eligibility.
        /// </summary>
        public static bool NativeTranslucencyEligibleFromPayload(
            IReadOnlyDictionary<string, string>? payload,
            string? styleId = null,
            string? kind = null)
        {
            return TesseraNativeTranslucencyPolicy.IsEligibleFromPayload(payload, styleId)
            || (!TesseraStatusFlyoutPolicy.MustUseDedicatedSingleWindow(kind)
                && TesseraNativeTranslucencyStackedPolicy.UseMultiWindowFromPayload(payload, styleId, kind));
        }
    }
}
