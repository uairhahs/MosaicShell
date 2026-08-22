namespace MosaicShell.Core.Styles;

/// <summary>
/// Maps Tessera StyleCatalog ids to Host layout maturity (Phase C1).
/// <see cref="IsPolished"/> styles have dedicated Avalonia layouts; <see cref="IsApproximate"/> remain lighter.
/// <c>tessera_layout_fidelity</c> stays false until screenshot-level proofs exist for all ids.
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

    public static bool IsPolished(string styleId) => Polished.Contains(styleId);

    public static bool IsApproximate(string styleId) => Approximate.Contains(styleId);

    public static bool CoversCatalog()
    {
        var ids = StyleCatalog.IdsFor("Tessera");
        if (ids.Count == 0) return false;
        return ids.All(id => Polished.Contains(id) || Approximate.Contains(id));
    }
}
