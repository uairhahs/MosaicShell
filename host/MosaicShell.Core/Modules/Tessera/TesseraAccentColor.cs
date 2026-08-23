using System.Globalization;

namespace MosaicShell.Core.Modules.Tessera;

/// <summary>Parse Tessera appearance accent (#RRGGBB). Empty = use system accent.</summary>
public static class TesseraAccentColor
{
    public static bool IsConfigured(string? input) => TryParse(input, out _, out _, out _);

    public static bool TryParse(string? input, out byte r, out byte g, out byte b)
    {
        r = g = b = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var s = input.Trim();
        if (s.StartsWith('#'))
            s = s[1..];

        if (s.Length != 6
            || !uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
            return false;

        r = (byte)((rgb >> 16) & 0xFF);
        g = (byte)((rgb >> 8) & 0xFF);
        b = (byte)(rgb & 0xFF);
        return true;
    }

    public static string NormalizeOrEmpty(string? input)
    {
        if (!TryParse(input, out var r, out var g, out var b))
            return "";
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    /// <summary>Quick-pick circles in Tessera appearance. First entry is system accent (empty hex).</summary>
    public static IReadOnlyList<TesseraAccentPreset> Presets { get; } =
    [
        new("System", ""),
        new("Windows blue", "#0273CD"),
        new("Blue", "#89B4FA"),
        new("Sapphire", "#74C7EC"),
        new("Teal", "#94E2D5"),
        new("Green", "#A6E3A1"),
        new("Yellow", "#F9E2AF"),
        new("Peach", "#FAB387"),
        new("Red", "#F38BA8"),
        new("Mauve", "#CBA6F7"),
        new("Pink", "#F5C2E7"),
        new("Lavender", "#B4BEFE"),
    ];

    public static bool MatchesPreset(string? currentHex, TesseraAccentPreset preset) =>
        string.Equals(NormalizeOrEmpty(currentHex), NormalizeOrEmpty(preset.Hex), StringComparison.OrdinalIgnoreCase);
}

public readonly record struct TesseraAccentPreset(string Name, string Hex)
{
    public bool IsSystem => string.IsNullOrEmpty(Hex);
}
