namespace MosaicShell.Core.Modules;

/// <summary>Material icon kind name + Mocha tint for hub cards and tile rows.</summary>
public readonly record struct HubGlyph(string Kind, string TintHex);

/// <summary>
/// Maps hub surfaces onto Material.Icons kind names (PascalCase).
/// Host parses <see cref="HubGlyph.Kind"/> to <c>MaterialIconKind</c>.
/// </summary>
public static class HubGlyphCatalog
{
    public static HubGlyph ForModule(string moduleId) => moduleId?.Trim().ToLowerInvariant() switch
    {
        "tessera" => new("VolumeHigh", "#89B4FA"),
        "mixdeck" => new("TuneVertical", "#CBA6F7"),
        "inlay" => new("Apps", "#94E2D5"),
        "slate" => new("ClockOutline", "#F9E2AF"),
        "chord" => new("KeyboardOutline", "#FAB387"),
        "substrate" => new("Tune", "#A6E3A1"),
        "chrono" => new("Clock", "#74C7EC"),
        "phono" => new("MusicNote", "#F5C2E7"),
        "pulse" => new("Equalizer", "#F38BA8"),
        "canvas" => new("CardTextOutline", "#B4BEFE"),
        _ => new("Puzzle", "#A6ADC8"),
    };

    public static HubGlyph ForHomeCard(string targetPage) => targetPage?.Trim() switch
    {
        "Welcome" => new("PartyPopper", "#89DCEB"),
        "Tiles" => new("Widgets", "#89B4FA"),
        "About" => new("InformationOutline", "#A6E3A1"),
        _ => new("Apps", "#A6ADC8"),
    };
}
