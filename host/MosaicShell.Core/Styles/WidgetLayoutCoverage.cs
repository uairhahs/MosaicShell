namespace MosaicShell.Core.Styles;

public static class ChronoLayoutCoverage
{
    private static readonly HashSet<string> Flagship = new(StringComparer.OrdinalIgnoreCase) { "Center" };
    private static readonly HashSet<string> ChromeOnly = new(StringComparer.OrdinalIgnoreCase)
    {
        "Text", "Minimal", "Tech", "CircTech", "Light", "16", "Arc", "Graph", "Measure", "Smart"
    };

    public static bool IsFlagship(string styleId) => Flagship.Contains(styleId);
    public static bool IsChromeOnly(string styleId) => ChromeOnly.Contains(styleId);

    public static bool CoversCatalog() =>
        StyleCatalog.IdsFor("Chrono").All(id => Flagship.Contains(id) || ChromeOnly.Contains(id));
}

public static class PhonoLayoutCoverage
{
    private static readonly HashSet<string> Flagship = new(StringComparer.OrdinalIgnoreCase) { "Simple" };
    private static readonly HashSet<string> Distinct = new(StringComparer.OrdinalIgnoreCase)
    {
        "Center", "Win11", "Card", "BigCirc", "DoubleCirc", "Side"
    };
    private static readonly HashSet<string> ChromeOnly = new(StringComparer.OrdinalIgnoreCase)
    {
        "16", "Blur", "Fortnite", "MIUI", "Modern", "Neumorphism"
    };

    public static bool IsFlagship(string styleId) => Flagship.Contains(styleId);
    public static bool CoversCatalog() =>
        StyleCatalog.IdsFor("Phono").All(id => Flagship.Contains(id) || Distinct.Contains(id) || ChromeOnly.Contains(id));
}

public static class PulseLayoutCoverage
{
    private static readonly HashSet<string> Flagship = new(StringComparer.OrdinalIgnoreCase) { "Regular" };
    private static readonly HashSet<string> Variant = new(StringComparer.OrdinalIgnoreCase)
    {
        "Circ", "Gradient", "Chroma", "Boxes", "DEFAULT", "Layered", "Screen", "Smooth", "Subtle", "Tech"
    };

    public static bool IsFlagship(string styleId) => Flagship.Contains(styleId);
    public static bool CoversCatalog() =>
        StyleCatalog.IdsFor("Pulse").All(id => Flagship.Contains(id) || Variant.Contains(id));
}

public static class CanvasLayoutCoverage
{
    private static readonly HashSet<string> Flagship = new(StringComparer.OrdinalIgnoreCase) { "DEFAULT" };
    private static readonly HashSet<string> Variant = new(StringComparer.OrdinalIgnoreCase) { "Compact" };

    public static bool IsFlagship(string styleId) => Flagship.Contains(styleId);
    public static bool CoversCatalog() =>
        StyleCatalog.IdsFor("Canvas").All(id => Flagship.Contains(id) || Variant.Contains(id));
}

public static class MixdeckLayoutCoverage
{
    private static readonly HashSet<string> Flagship = new(StringComparer.OrdinalIgnoreCase) { "Fluent" };
    private static readonly HashSet<string> Chrome = new(StringComparer.OrdinalIgnoreCase)
    {
        "Center", "Fluent10", "Fluent11", "Rounded", "Solid"
    };

    public static bool IsFlagship(string styleId) => Flagship.Contains(styleId);
    public static bool CoversCatalog() =>
        StyleCatalog.IdsFor("Mixdeck").All(id => Flagship.Contains(id) || Chrome.Contains(id));
}

public static class InlayLayoutCoverage
{
    private static readonly HashSet<string> Flagship = new(StringComparer.OrdinalIgnoreCase) { "Win11" };
    private static readonly HashSet<string> Chrome = new(StringComparer.OrdinalIgnoreCase)
    {
        "ClassicWavy", "Flat", "SideBar"
    };

    public static bool IsFlagship(string styleId) => Flagship.Contains(styleId);
    public static bool CoversCatalog() =>
        StyleCatalog.IdsFor("Inlay").All(id => Flagship.Contains(id) || Chrome.Contains(id));
}

public static class ChordLayoutCoverage
{
    private static readonly HashSet<string> Distinct = new(StringComparer.OrdinalIgnoreCase)
    {
        "Center", "Bottom", "Expand", "VectorSlide", "Spin"
    };

    public static bool CoversCatalog() =>
        StyleCatalog.IdsFor("Chord").All(id => Distinct.Contains(id));
}

public static class SubstrateLayoutCoverage
{
    public static bool CoversCatalog() =>
        StyleCatalog.IdsFor("Substrate").Contains("DEFAULT", StringComparer.OrdinalIgnoreCase);
}

public static class SlateLayoutCoverage
{
    private static readonly HashSet<string> Implemented = new(StringComparer.OrdinalIgnoreCase)
    {
        "Center", "Ninety", "String"
    };
    private static readonly HashSet<string> Deferred = new(StringComparer.OrdinalIgnoreCase)
    {
        "CustomGroup", "CustomPaper", "CustomVideo", "JD", "CoreUI"
    };

    public static bool CoversCatalog() =>
        StyleCatalog.IdsFor("Slate").All(id => Implemented.Contains(id) || Deferred.Contains(id));
}
