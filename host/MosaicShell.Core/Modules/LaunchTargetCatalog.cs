namespace MosaicShell.Core.Modules;

/// <summary>Searchable launch targets for Inlay pins / Chord actions.</summary>
public sealed record LaunchTarget(string DisplayName, string Target, string Group);

public static class LaunchTargetCatalog
{
    private static IReadOnlyList<LaunchTarget>? _cached;

    public static IReadOnlyList<LaunchTarget> All()
    {
        if (_cached is not null) return _cached;
        var list = new List<LaunchTarget>();
        list.AddRange(BuiltIns);
        list.AddRange(ScanStartMenu());
        _cached = list
            .GroupBy(t => t.Target, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(t => t.Group)
            .ThenBy(t => t.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return _cached;
    }

    public static bool TryResolveLabel(string? labelOrTarget, out string target, out string displayName)
    {
        target = "";
        displayName = "";
        if (string.IsNullOrWhiteSpace(labelOrTarget)) return false;
        var text = labelOrTarget.Trim();

        foreach (var t in All())
        {
            var label = $"{t.DisplayName}  ({t.Target})";
            if (label.Equals(text, StringComparison.OrdinalIgnoreCase) ||
                t.Target.Equals(text, StringComparison.OrdinalIgnoreCase) ||
                t.DisplayName.Equals(text, StringComparison.OrdinalIgnoreCase))
            {
                target = t.Target;
                displayName = t.DisplayName;
                return true;
            }
        }

        // Allow free-form paths / URIs the user typed.
        target = text;
        displayName = Path.GetFileNameWithoutExtension(text);
        if (string.IsNullOrWhiteSpace(displayName)) displayName = text;
        return true;
    }

    public static IEnumerable<string> DisplayLabels() =>
        All().Select(t => $"{t.DisplayName}  ({t.Target})");

    /// <summary>Filter catalog by display name, target path, or group.</summary>
    public static IEnumerable<LaunchTarget> Search(string? query, int maxResults = 32)
    {
        var q = query?.Trim() ?? "";
        var all = All();
        if (string.IsNullOrEmpty(q))
            return all.Take(maxResults);

        return all.Where(t =>
                t.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase)
                || t.Target.Contains(q, StringComparison.OrdinalIgnoreCase)
                || t.Group.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults);
    }

    private static readonly LaunchTarget[] BuiltIns =
    [
        new("Notepad", "notepad", "Apps"),
        new("Calculator", "calc", "Apps"),
        new("File Explorer", "explorer", "Apps"),
        new("Command Prompt", "cmd", "Apps"),
        new("Windows Terminal", "wt", "Apps"),
        new("PowerShell", "powershell", "Apps"),
        new("Paint", "mspaint", "Apps"),
        new("Snipping Tool", "snippingtool", "Apps"),
        new("Task Manager", "taskmgr", "Apps"),
        new("Control Panel", "control", "Apps"),
        new("Run dialog", "shell:::{2559a1f3-21d7-11d4-bdaf-00c04f60b9f0}", "Shell"),
        new("Settings", "ms-settings:", "Settings"),
        new("Settings - System", "ms-settings:system", "Settings"),
        new("Settings - Display", "ms-settings:display", "Settings"),
        new("Settings - Sound", "ms-settings:sound", "Settings"),
        new("Settings - Bluetooth", "ms-settings:bluetooth", "Settings"),
        new("Settings - Network", "ms-settings:network", "Settings"),
        new("Settings - Personalization", "ms-settings:personalization", "Settings"),
        new("Settings - Apps", "ms-settings:appsfeatures", "Settings"),
        new("Settings - Privacy", "ms-settings:privacy", "Settings"),
        new("Settings - Update", "ms-settings:windowsupdate", "Settings"),
    ];

    private static IEnumerable<LaunchTarget> ScanStartMenu()
    {
        var roots = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs")
        };

        foreach (var root in roots.Where(Directory.Exists))
        {
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(root, "*.lnk", SearchOption.AllDirectories);
            }
            catch
            {
                continue;
            }

            foreach (var file in files.Take(400))
            {
                string name;
                try { name = Path.GetFileNameWithoutExtension(file); }
                catch { continue; }
                if (string.IsNullOrWhiteSpace(name)) continue;
                if (name.StartsWith("uninstall", StringComparison.OrdinalIgnoreCase)) continue;
                yield return new LaunchTarget(name, file, "Start Menu");
            }
        }
    }
}
