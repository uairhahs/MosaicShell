using System.Text.RegularExpressions;

namespace MosaicShell.Core.Update;

/// <summary>
/// Date+build release tags: <c>yyyy.M.d-b{N}</c> (optional leading <c>v</c>).
/// Used by GitHub release CI, assembly informational version, and update checks.
/// </summary>
public readonly record struct HostBuildVersionParts(int Year, int Month, int Day, int Build)
{
    public string Format() => $"{Year}.{Month}.{Day}-b{Build}";
}

public static class HostBuildVersionPolicy
{
    /// <summary>Unstamped local/debug builds.</summary>
    public const string LocalDevLabel = "0.0.0-dev";

    private static readonly Regex ReleaseTag = new(
        @"^v?(?<y>\d{4})\.(?<m>\d{1,2})\.(?<d>\d{1,2})-b(?<b>\d+)$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool TryParse(string? raw, out HostBuildVersionParts parts)
    {
        parts = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var text = Normalize(raw);
        var match = ReleaseTag.Match(text);
        if (!match.Success)
            return false;

        parts = new HostBuildVersionParts(
            int.Parse(match.Groups["y"].Value),
            int.Parse(match.Groups["m"].Value),
            int.Parse(match.Groups["d"].Value),
            int.Parse(match.Groups["b"].Value));
        return true;
    }

    /// <summary>Negative if <paramref name="left"/> is older; positive if newer; 0 if equal.</summary>
    public static int Compare(string? left, string? right)
    {
        var a = Normalize(left);
        var b = Normalize(right);
        if (a.Equals(b, StringComparison.OrdinalIgnoreCase))
            return 0;

        var aParsed = TryParse(a, out var aParts);
        var bParsed = TryParse(b, out var bParts);
        if (aParsed && bParsed)
            return CompareParsed(aParts, bParts);

        if (IsLocalDev(a) && bParsed)
            return -1;
        if (aParsed && IsLocalDev(b))
            return 1;

        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsNewer(string? latest, string? current) =>
        Compare(latest, current) > 0;

    /// <summary>
    /// Maps a date-build tag to a 4-part numeric string for PE / Inno VersionInfoVersion.
    /// Example: <c>2026.8.23-b1</c> → <c>2026.8.23.1</c>.
    /// </summary>
    public static string ToVersionInfoVersion(string? tag)
    {
        if (!TryParse(tag, out var parts))
            return "0.0.0.0";
        return $"{parts.Year}.{parts.Month}.{parts.Day}.{parts.Build}";
    }

    public static bool IsLocalDev(string? label) =>
        string.IsNullOrWhiteSpace(label)
        || Normalize(label).Equals(LocalDevLabel, StringComparison.OrdinalIgnoreCase);

    private static int CompareParsed(HostBuildVersionParts left, HostBuildVersionParts right)
    {
        var dateCompare = left.Year.CompareTo(right.Year);
        if (dateCompare != 0) return dateCompare;

        dateCompare = left.Month.CompareTo(right.Month);
        if (dateCompare != 0) return dateCompare;

        dateCompare = left.Day.CompareTo(right.Day);
        if (dateCompare != 0) return dateCompare;

        return left.Build.CompareTo(right.Build);
    }

    private static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return LocalDevLabel;

        var text = raw.Trim();
        var plus = text.IndexOf('+');
        if (plus >= 0)
            text = text[..plus];
        return text.TrimStart('v', 'V');
    }
}
