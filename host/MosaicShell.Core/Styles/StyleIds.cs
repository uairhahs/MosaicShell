namespace MosaicShell.Core.Styles;

/// <summary>
/// Canonical style ids shared across tile catalogs. Legacy YourFlyouts / JaxCore names
/// normalize here so saved settings and Host switches stay compatible.
/// </summary>
public static class StyleIds
{
    public const string MaterialYou = "MaterialYou";
    public const string Fluent = "Fluent";
    public const string Windows11 = "Windows11";
    public const string Gnome = "Gnome";
    public const string CoreUI = "CoreUI";
    public const string ModernFlyouts = "ModernFlyouts";
    public const string Meter = "Meter";
    public const string Square = "Square";
    public const string Compact = "Compact";
    public const string PlainText = "PlainText";
    public const string Radial = "Radial";

    private static readonly Dictionary<string, string> LegacyToCanonical =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Pixel"] = MaterialYou,
            ["Win11"] = Windows11,
            ["Modern"] = ModernFlyouts,
            ["Amber"] = Meter,
            ["Center"] = Square,
            ["Simple"] = Compact,
            ["Plainext"] = PlainText,
            ["Smouti"] = Radial,
            [MaterialYou] = MaterialYou,
            [Fluent] = Fluent,
            [Windows11] = Windows11,
            [Gnome] = Gnome,
            [CoreUI] = CoreUI,
            [ModernFlyouts] = ModernFlyouts,
            [Meter] = Meter,
            [Square] = Square,
            [Compact] = Compact,
            [PlainText] = PlainText,
            [Radial] = Radial,
        };

    private static readonly Dictionary<string, string> DisplayNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [MaterialYou] = "Material You",
            [Fluent] = "Fluent",
            [Windows11] = "Windows 11",
            [Gnome] = "GNOME",
            [CoreUI] = "CoreUI",
            [ModernFlyouts] = "Modern Flyouts",
            [Meter] = "Meter",
            [Square] = "Square",
            [Compact] = "Compact",
            [PlainText] = "Plain Text",
            [Radial] = "Radial",
        };

    /// <summary>Map legacy or current id to the canonical StyleCatalog id.</summary>
    public static string Normalize(string? styleId)
    {
        if (string.IsNullOrWhiteSpace(styleId))
            return "";
        var trimmed = styleId.Trim();
        return LegacyToCanonical.TryGetValue(trimmed, out var canonical) ? canonical : trimmed;
    }

    /// <summary>UI label for a style id (after normalize). Unknown ids return themselves.</summary>
    public static string DisplayName(string? styleId)
    {
        var id = Normalize(styleId);
        if (string.IsNullOrEmpty(id))
            return "";
        return DisplayNames.TryGetValue(id, out var name) ? name : id;
    }

    /// <summary>
    /// If <paramref name="settings"/> has a writable string <c>Style</c> property with a legacy id,
    /// rewrite it to the canonical id. Returns true when the object was changed (caller should persist).
    /// </summary>
    public static bool TryMigratePersistedStyle(object settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var prop = settings.GetType().GetProperty("Style");
        if (prop is null || prop.PropertyType != typeof(string) || !prop.CanRead || !prop.CanWrite)
            return false;

        var current = prop.GetValue(settings) as string;
        var normalized = Normalize(current);
        if (string.Equals(current, normalized, StringComparison.Ordinal))
            return false;

        prop.SetValue(settings, normalized);
        return true;
    }
}
