using System.Globalization;

namespace MosaicShell.Core.Modules;

/// <summary>ARGB tuple Host materializes into brushes. Never assign hex strings to IBrush.</summary>
public readonly record struct HostChromeArgb(byte A, byte R, byte G, byte B);

/// <summary>
/// Hub scrollbar chrome. Fluent's expanded bar (arrows + ~12px track) is too loud on Mocha;
/// Host must use a slim auto-hiding overlay thumb.
/// </summary>
public static class HostScrollbarChromeSpec
{
    public const double Thickness = 8;
    public const string ThumbHex = "#585B70";
    public const string ThumbHoverHex = "#6C7086";
    public const string ThumbPressedHex = "#7F849C";
    public const string TrackHex = "#00000000";
    public const bool AutoHide = true;
    public const bool HideLineButtons = true;

    public static HostChromeArgb Thumb { get; } = MustParse(ThumbHex);
    public static HostChromeArgb ThumbHover { get; } = MustParse(ThumbHoverHex);
    public static HostChromeArgb ThumbPressed { get; } = MustParse(ThumbPressedHex);
    public static HostChromeArgb Track { get; } = MustParse(TrackHex);

    public static bool TryParseArgb(string? hex, out byte a, out byte r, out byte g, out byte b)
    {
        a = r = g = b = 0;
        if (string.IsNullOrWhiteSpace(hex))
            return false;

        var s = hex.Trim();
        if (s.StartsWith('#'))
            s = s[1..];

        if (s.Length == 6)
        {
            if (!uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
                return false;
            a = 255;
            r = (byte)((rgb >> 16) & 0xFF);
            g = (byte)((rgb >> 8) & 0xFF);
            b = (byte)(rgb & 0xFF);
            return true;
        }

        if (s.Length == 8
            && uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
        {
            a = (byte)((argb >> 24) & 0xFF);
            r = (byte)((argb >> 16) & 0xFF);
            g = (byte)((argb >> 8) & 0xFF);
            b = (byte)(argb & 0xFF);
            return true;
        }

        return false;
    }

    private static HostChromeArgb MustParse(string hex)
    {
        if (!TryParseArgb(hex, out var a, out var r, out var g, out var b))
            throw new InvalidOperationException($"HostScrollbarChromeSpec hex '{hex}' is not #RRGGBB or #AARRGGBB.");
        return new HostChromeArgb(a, r, g, b);
    }
}
