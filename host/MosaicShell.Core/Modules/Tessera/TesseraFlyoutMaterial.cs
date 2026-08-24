namespace MosaicShell.Core.Modules.Tessera;

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

        var hints = osAcrylicEligible
            ? (IReadOnlyList<string>)["AcrylicBlur", "Transparent"]
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
        if (payload is null) return true;
        if (!payload.TryGetValue("acrylic", out var raw) || string.IsNullOrWhiteSpace(raw))
            return true;
        return raw is not ("0" or "false" or "False" or "off" or "Off");
    }

    public static TesseraFlyoutMaterial FromPayload(
        IReadOnlyDictionary<string, string>? payload,
        string? styleId = null,
        string? kind = null) =>
        Create(
            UseAcrylicFromPayload(payload),
            osAcrylicEligible: OsAcrylicEligibleFromPayload(payload, styleId, kind));

    /// <summary>
    /// Single-shell (<see cref="TesseraOsAcrylicTrialPolicy"/>) or stacked N-window
    /// (<see cref="TesseraOsAcrylicStackedPolicy"/>) OS acrylic trial.
    /// Status kinds never take stacked acrylic eligibility.
    /// </summary>
    public static bool OsAcrylicEligibleFromPayload(
        IReadOnlyDictionary<string, string>? payload,
        string? styleId = null,
        string? kind = null) =>
        TesseraOsAcrylicTrialPolicy.IsEligibleFromPayload(payload, styleId)
        || (!TesseraStatusFlyoutPolicy.MustUseDedicatedSingleWindow(kind)
            && TesseraOsAcrylicStackedPolicy.UseMultiWindowFromPayload(payload, styleId, kind));
}
