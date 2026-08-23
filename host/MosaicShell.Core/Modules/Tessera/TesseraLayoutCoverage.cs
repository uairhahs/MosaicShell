using MosaicShell.Core.Styles;
namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// Maps Tessera StyleCatalog ids to Host layout maturity (Phase C1).
/// <see cref="IsPolished"/> styles have dedicated Avalonia layouts; <see cref="IsApproximate"/> remain lighter.
/// <see cref="IsLayoutFidelitySignedOff"/> is manual visual sign-off vs YourFlyouts refs.
/// <c>tessera_layout_fidelity</c> is true when <see cref="AllLayoutFidelitySignedOff"/> (no deviated ids).
/// </summary>
public static class TesseraLayoutCoverage
{
    /// <summary>In-repo Host proofs: <c>.github/res/Tessera/{StyleId}.png</c>.</summary>
    public const string LayoutFidelityProofRelativeDirectory = ".github/res/Tessera";

    private static readonly HashSet<string> Polished = new(StringComparer.OrdinalIgnoreCase)
    {
        StyleIds.Fluent, StyleIds.Windows11, StyleIds.Square, StyleIds.MaterialYou,
        StyleIds.Compact, StyleIds.ModernFlyouts, StyleIds.Meter, StyleIds.Gnome, StyleIds.CoreUI
    };

    private static readonly HashSet<string> Approximate = new(StringComparer.OrdinalIgnoreCase)
    {
        StyleIds.Radial, StyleIds.PlainText
    };

    /// <summary>Signed off manually; Host proofs under <see cref="LayoutFidelityProofRelativeDirectory"/>.</summary>
    private static readonly HashSet<string> LayoutFidelitySignedOff = new(StringComparer.OrdinalIgnoreCase)
    {
        StyleIds.Meter, StyleIds.Square, StyleIds.CoreUI, StyleIds.Fluent, StyleIds.Gnome,
        StyleIds.ModernFlyouts, StyleIds.MaterialYou, StyleIds.PlainText, StyleIds.Compact,
        StyleIds.Windows11, StyleIds.Radial
    };

    /// <summary>Still deviate from refs. Empty when every catalog id is signed off.</summary>
    private static readonly HashSet<string> LayoutFidelityDeviated = new(StringComparer.OrdinalIgnoreCase);

    public static bool IsPolished(string styleId) => Polished.Contains(StyleIds.Normalize(styleId));

    public static bool IsApproximate(string styleId) => Approximate.Contains(StyleIds.Normalize(styleId));

    public static bool IsLayoutFidelitySignedOff(string styleId) =>
        LayoutFidelitySignedOff.Contains(StyleIds.Normalize(styleId));

    public static bool IsLayoutFidelityDeviated(string styleId) =>
        LayoutFidelityDeviated.Contains(StyleIds.Normalize(styleId));

    public static bool AllLayoutFidelitySignedOff()
    {
        var ids = StyleCatalog.IdsFor("Tessera");
        return ids.Count > 0 && ids.All(IsLayoutFidelitySignedOff);
    }

    public static bool CoversCatalog()
    {
        var ids = StyleCatalog.IdsFor("Tessera");
        if (ids.Count == 0) return false;
        return ids.All(id => Polished.Contains(id) || Approximate.Contains(id));
    }

    public static bool CoversLayoutFidelity()
    {
        var ids = StyleCatalog.IdsFor("Tessera");
        if (ids.Count == 0) return false;
        return ids.All(id => IsLayoutFidelitySignedOff(id) || IsLayoutFidelityDeviated(id));
    }

    /// <summary>Styles that embed media controls in-layout (no Modern-style stacked strip on volume).</summary>
    public static bool UsesStackedMediaStrip(string styleId) =>
        !StyleIds.Normalize(styleId).Equals(StyleIds.MaterialYou, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Meter’s volume chrome is a glass meter with no glyph — Host must register a live percent
    /// so Patch/ApplyLive has readable feedback (otherwise the pill reads as a static block).
    /// </summary>
    public static bool RequiresLiveVolumePercentLabel(string styleId) =>
        StyleIds.Normalize(styleId).Equals(StyleIds.Meter, StringComparison.OrdinalIgnoreCase);
}
