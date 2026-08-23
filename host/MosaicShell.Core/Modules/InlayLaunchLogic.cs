namespace MosaicShell.Core.Modules;

/// <summary>Search/filter logic for Inlay overlay (shared with Host + tests).</summary>
public static class InlayLaunchLogic
{
    public static IReadOnlyList<LaunchTarget> BuildTargets(
        string? filter,
        IReadOnlyList<string> pins,
        int maxResults = 28)
    {
        var q = filter?.Trim() ?? "";
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<LaunchTarget>();

        foreach (var pin in pins)
        {
            if (string.IsNullOrWhiteSpace(pin)) continue;
            if (LaunchTargetCatalog.TryResolveLabel(pin, out var target, out var display))
            {
                if (!MatchesQuery(q, display, target, "Pinned")) continue;
                if (seen.Add(target))
                    results.Add(new LaunchTarget(display, target, "Pinned"));
            }
        }

        foreach (var t in LaunchTargetCatalog.Search(q, maxResults * 2))
        {
            if (seen.Add(t.Target))
                results.Add(t);
            if (results.Count >= maxResults) break;
        }

        return results;
    }

    public static bool TryLaunchFromQuery(string? query)
    {
        if (!LaunchTargetCatalog.TryResolveLabel(query, out var target, out _)) return false;
        if (string.IsNullOrWhiteSpace(target)) return false;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool MatchesQuery(string q, string display, string target, string group)
    {
        if (string.IsNullOrEmpty(q)) return true;
        return display.Contains(q, StringComparison.OrdinalIgnoreCase)
               || target.Contains(q, StringComparison.OrdinalIgnoreCase)
               || group.Contains(q, StringComparison.OrdinalIgnoreCase);
    }
}
