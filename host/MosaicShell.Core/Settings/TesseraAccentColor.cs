using System.Globalization;

namespace MosaicShell.Core.Settings;

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

        r = (byte)(rgb >> 16);
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
}
