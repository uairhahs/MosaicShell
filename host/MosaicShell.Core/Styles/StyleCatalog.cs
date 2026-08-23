namespace MosaicShell.Core.Styles;

public sealed record StyleDescriptor(string ModuleId, string StyleId, string DisplayName)
{
    public override string ToString() => DisplayName;
}

/// <summary>Built-in JaxCore style/layout ids for native Avalonia recreations.</summary>
public static class StyleCatalog
{
    private static readonly Dictionary<string, string[]> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Tessera"] =
        [
            "Meter", "Square", "CoreUI", "Fluent", "Gnome", "ModernFlyouts",
            "MaterialYou", "PlainText", "Compact", "Radial", "Windows11"
        ],
        ["Chrono"] =
        [
            "16", "Arc", "Square", "CircTech", "Graph", "Light", "Measure", "Smart", "Tech", "Text"
        ],
        ["Phono"] =
        [
            "16", "BigCirc", "Blur", "Card", "Square", "DoubleCirc", "Fortnite",
            "MIUI", "ModernFlyouts", "Neumorphism", "Side", "Compact", "Windows11"
        ],
        ["Pulse"] =
        [
            "Boxes", "Chroma", "Circ", "DEFAULT", "Gradient", "Layered",
            "Regular", "Screen", "Smooth", "Subtle", "Tech"
        ],
        ["Mixdeck"] = ["Square", "Fluent", "Fluent10", "Fluent11", "Rounded", "Solid"],
        ["Inlay"] = ["ClassicWavy", "Flat", "SideBar", "Windows11"],
        ["Chord"] = ["Bottom", "Square", "Expand", "Spin", "VectorSlide"],
        ["Slate"] = ["Square", "CoreUI", "CustomGroup", "CustomPaper", "CustomVideo", "JD", "Ninety", "String"],
        ["Substrate"] = ["DEFAULT"],
        ["Canvas"] = ["DEFAULT", "Compact"],
    };

    public static IReadOnlyList<string> IdsFor(string moduleId)
    {
        if (Map.TryGetValue(moduleId, out var ids))
            return ids;

        var manifest = MosaicShell.Core.Runtime.ModuleManifest.TryLoad(moduleId);
        if (manifest?.Styles is { Count: > 0 })
            return manifest.Styles;

        return Array.Empty<string>();
    }

    public static IReadOnlyList<StyleDescriptor> For(string moduleId) =>
        IdsFor(moduleId)
            .Select(id => new StyleDescriptor(moduleId, id, StyleIds.DisplayName(id)))
            .ToList();

    public static bool IsValid(string moduleId, string styleId) =>
        IdsFor(moduleId).Contains(StyleIds.Normalize(styleId), StringComparer.OrdinalIgnoreCase)
        || IdsFor(moduleId).Contains(styleId, StringComparer.OrdinalIgnoreCase);

    public static string DefaultFor(string moduleId)
    {
        var manifest = MosaicShell.Core.Runtime.ModuleManifest.TryLoad(moduleId);
        if (!string.IsNullOrWhiteSpace(manifest?.DefaultStyle))
            return StyleIds.Normalize(manifest!.DefaultStyle!);
        return IdsFor(moduleId).FirstOrDefault() ?? "DEFAULT";
    }
}
