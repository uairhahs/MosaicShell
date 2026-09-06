using MosaicShell.Core.Runtime;

namespace MosaicShell.Core.Styles
{
    public sealed record StyleDescriptor(string ModuleId, string StyleId, string DisplayName)
    {
        public override string ToString()
        {
            return DisplayName;
        }
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
            if (Map.TryGetValue(moduleId, out string[]? ids))
            {
                return ids;
            }

            ModuleManifest? manifest = ModuleManifest.TryLoad(moduleId);
            return manifest?.Styles is { Count: > 0 } ? manifest.Styles : (IReadOnlyList<string>)[];
        }

        public static IReadOnlyList<StyleDescriptor> For(string moduleId)
        {
            return [.. IdsFor(moduleId).Select(id => new StyleDescriptor(moduleId, id, StyleIds.DisplayName(id)))];
        }

        public static bool IsValid(string moduleId, string styleId)
        {
            return IdsFor(moduleId).Contains(StyleIds.Normalize(styleId), StringComparer.OrdinalIgnoreCase)
            || IdsFor(moduleId).Contains(styleId, StringComparer.OrdinalIgnoreCase);
        }

        public static string DefaultFor(string moduleId)
        {
            ModuleManifest? manifest = ModuleManifest.TryLoad(moduleId);
            return !string.IsNullOrWhiteSpace(manifest?.DefaultStyle)
                ? StyleIds.Normalize(manifest.DefaultStyle)
                : IdsFor(moduleId).FirstOrDefault() ?? "DEFAULT";
        }
    }
}
