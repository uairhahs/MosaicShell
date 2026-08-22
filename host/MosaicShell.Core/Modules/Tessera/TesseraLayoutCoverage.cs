using MosaicShell.Core.Styles;
namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// Maps Tessera StyleCatalog ids to Host layout maturity (Phase C1).
/// <see cref="IsPolished"/> styles have dedicated Avalonia layouts; <see cref="IsApproximate"/> remain lighter.
/// <see cref="IsLayoutFidelitySignedOff"/> is manual visual sign-off vs YourFlyouts refs.
/// <c>tessera_layout_fidelity</c> stays false until <see cref="AllLayoutFidelitySignedOff"/> (no deviated ids).
/// </summary>
public static class TesseraLayoutCoverage
{
    private static readonly HashSet<string> Polished = new(StringComparer.OrdinalIgnoreCase)
    {
        "Fluent", "Win11", "Center", "Pixel", "Simple", "Modern", "Amber", "Gnome", "CoreUI"
    };

    private static readonly HashSet<string> Approximate = new(StringComparer.OrdinalIgnoreCase)
    {
        "Smouti", "Plainext"
    };

    /// <summary>Signed off manually; refs under <c>.local/Tessera/original</c>.</summary>
    private static readonly HashSet<string> LayoutFidelitySignedOff = new(StringComparer.OrdinalIgnoreCase)
    {
        "Amber", "Center", "CoreUI", "Fluent", "Gnome", "Modern", "Pixel", "Plainext", "Simple", "Win11"
    };

    /// <summary>Still deviate from refs; compare targets in <c>.local/Tessera/deviated/</c>.</summary>
    private static readonly HashSet<string> LayoutFidelityDeviated = new(StringComparer.OrdinalIgnoreCase)
    {
        "Smouti"
    };

    public static bool IsPolished(string styleId) => Polished.Contains(styleId);

    public static bool IsApproximate(string styleId) => Approximate.Contains(styleId);

    public static bool IsLayoutFidelitySignedOff(string styleId) => LayoutFidelitySignedOff.Contains(styleId);

    public static bool IsLayoutFidelityDeviated(string styleId) => LayoutFidelityDeviated.Contains(styleId);

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
        !styleId.Equals("Pixel", StringComparison.OrdinalIgnoreCase);
}
