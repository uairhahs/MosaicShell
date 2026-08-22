namespace MosaicShell.Core.Styles;

public sealed record StyleDescriptor(string ModuleId, string StyleId, string DisplayName);

/// <summary>Built-in JaxCore style/layout ids for native Avalonia recreations.</summary>
public static class StyleCatalog
{
    private static readonly Dictionary<string, string[]> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Tessera"] =
        [
            "Amber", "Center", "CoreUI", "Fluent", "Gnome", "Modern",
            "Pixel", "Plainext", "Simple", "Smouti", "Win11"
        ],
        ["Chrono"] =
        [
            "16", "Arc", "Center", "CircTech", "Graph", "Light", "Measure", "Smart", "Tech", "Text"
        ],
        ["Phono"] =
        [
            "16", "BigCirc", "Blur", "Card", "Center", "DoubleCirc", "Fortnite",
            "MIUI", "Modern", "Neumorphism", "Side", "Simple", "Win11"
        ],
        ["Pulse"] =
        [
            "Boxes", "Chroma", "Circ", "DEFAULT", "Gradient", "Layered",
            "Regular", "Screen", "Smooth", "Subtle", "Tech"
        ],
        ["Mixdeck"] = ["Center", "Fluent", "Fluent10", "Fluent11", "Rounded", "Solid"],
        ["Inlay"] = ["ClassicWavy", "Flat", "SideBar", "Win11"],
        ["Chord"] = ["Bottom", "Center", "Expand", "Spin", "VectorSlide"],
        ["Slate"] = ["Center", "CoreUI", "CustomGroup", "CustomPaper", "CustomVideo", "JD", "Ninety", "String"],
        ["Substrate"] = ["DEFAULT"],
        ["Canvas"] = ["DEFAULT", "Compact"],
    };

    public static IReadOnlyList<string> IdsFor(string moduleId) =>
        Map.TryGetValue(moduleId, out var ids) ? ids : Array.Empty<string>();

    public static IReadOnlyList<StyleDescriptor> For(string moduleId) =>
        IdsFor(moduleId).Select(id => new StyleDescriptor(moduleId, id, id)).ToList();

    public static bool IsValid(string moduleId, string styleId) =>
        IdsFor(moduleId).Contains(styleId, StringComparer.OrdinalIgnoreCase);

    public static string DefaultFor(string moduleId) =>
        IdsFor(moduleId).FirstOrDefault() ?? "DEFAULT";
}
