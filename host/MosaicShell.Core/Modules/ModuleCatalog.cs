namespace MosaicShell.Core.Modules;

public enum ModuleKind
{
    /// <summary>Armed background host (flyouts / hotkeys / idle). Prefer CapabilityDaemon.</summary>
    Capability = 0,
    /// <summary>Always-visible desktop surface via TileRuntime.</summary>
    Widget = 1,
    /// <summary>Both armed host and optional overlay.</summary>
    Hybrid = 2,
    /// <summary>Legacy alias for Capability (catalog / older callers).</summary>
    Module = Capability,
}

public sealed record ModuleInfo(
    string Id,
    string DisplayName,
    string Description,
    ModuleKind Kind);

/// <summary>
/// First-party seed catalog plus modules discovered from installed
/// <c>Modules/{id}/module.manifest.json</c> folders.
/// </summary>
public static class ModuleCatalog
{
    /// <summary>Built-in first-party modules (install stubs / Hub seed).</summary>
    public static IReadOnlyList<ModuleInfo> BuiltIns { get; } =
    [
        new("Tessera", "Tessera", "Volume / brightness / media flyouts. Arm, then use system keys (replaces OS OSD while Host runs).", ModuleKind.Capability),
        new("Mixdeck", "Mixdeck", "Per-app audio mixer. Arm, then press Ctrl+Alt+M (default) or Tessera Pixel.", ModuleKind.Capability),
        new("Inlay", "Inlay", "Start-menu launcher. Arm, then press Ctrl+Alt+I (default) for pins + search.", ModuleKind.Capability),
        new("Slate", "Slate", "Idle clock overlay. Arm, then wait for the idle timeout (default 5 min).", ModuleKind.Capability),
        new("Chord", "Chord", "Macro app launcher. Arm, then press Ctrl+Alt+K (default) for named actions.", ModuleKind.Capability),
        new("Substrate", "Substrate", "Quick-settings shade. Arm, then press Ctrl+Alt+Q (default) for mute / volume / brightness.", ModuleKind.Capability),
        new("Chrono", "Chrono", "Clock collection with multiple styles.", ModuleKind.Widget),
        new("Phono", "Phono", "Media player widget.", ModuleKind.Widget),
        new("Pulse", "Pulse", "Audio visualizer.", ModuleKind.Widget),
        new("Canvas", "Canvas", "Minimal plain-text information widget.", ModuleKind.Widget),
    ];

    /// <summary>Built-ins plus any installed external manifests (catalog order: built-ins first).</summary>
    public static IReadOnlyList<ModuleInfo> All => EnumerateAll().ToList();

    public static IEnumerable<ModuleInfo> Modules => All.Where(m =>
        m.Kind is ModuleKind.Capability or ModuleKind.Hybrid);
    public static IEnumerable<ModuleInfo> Widgets => All.Where(m => m.Kind == ModuleKind.Widget);
    public static IEnumerable<ModuleInfo> Capabilities => Modules;

    public static bool IsCapability(string moduleId) =>
        TryGet(moduleId, out var info) && info is not null &&
        info.Kind is ModuleKind.Capability or ModuleKind.Hybrid;

    public static bool TryGet(string moduleId, out ModuleInfo? info)
    {
        info = BuiltIns.FirstOrDefault(m => m.Id.Equals(moduleId, StringComparison.OrdinalIgnoreCase));
        if (info is not null)
            return true;

        info = TryFromInstalledManifest(moduleId);
        return info is not null;
    }

    public static bool IsInstalled(string moduleId) =>
        Directory.Exists(Path.Combine(AppPaths.ModulesDirectory, moduleId));

    /// <summary>Built-ins, then directories under Modules/ with a readable manifest (or folder name fallback).</summary>
    public static IEnumerable<ModuleInfo> EnumerateAll()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in BuiltIns)
        {
            seen.Add(m.Id);
            yield return m;
        }

        foreach (var discovered in DiscoverInstalled(seen))
            yield return discovered;
    }

    private static IEnumerable<ModuleInfo> DiscoverInstalled(HashSet<string> seen)
    {
        string root;
        try
        {
            root = AppPaths.ModulesDirectory;
        }
        catch
        {
            yield break;
        }

        if (!Directory.Exists(root))
            yield break;

        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            ModuleInfo? info = null;
            try
            {
                var id = Path.GetFileName(dir);
                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                    continue;
                info = TryFromInstalledManifest(id)
                       ?? new ModuleInfo(id, id, "Installed module (no catalog entry).", ModuleKind.Capability);
            }
            catch
            {
                continue;
            }

            if (info is not null)
                yield return info;
        }
    }

    private static ModuleInfo? TryFromInstalledManifest(string moduleId)
    {
        var manifest = Runtime.ModuleManifest.TryLoad(moduleId);
        if (manifest is null)
            return null;

        var id = string.IsNullOrWhiteSpace(manifest.Id) ? moduleId : manifest.Id;
        var name = string.IsNullOrWhiteSpace(manifest.DisplayName) ? id : manifest.DisplayName!;
        var kind = ParseKind(manifest.Kind);
        var description = string.IsNullOrWhiteSpace(manifest.Description)
            ? $"Installed module ({kind})."
            : manifest.Description!;
        return new ModuleInfo(id, name, description, kind);
    }

    internal static ModuleKind ParseKind(string? kind) =>
        kind?.Trim().ToLowerInvariant() switch
        {
            "widget" => ModuleKind.Widget,
            "hybrid" => ModuleKind.Hybrid,
            _ => ModuleKind.Capability
        };
}
