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

public static class ModuleCatalog
{
    /// <summary>Canonical tile list (same order as CoreWebResources SkinList).</summary>
    public static IReadOnlyList<ModuleInfo> All { get; } =
    [
        new("Tessera", "Tessera", "System flyouts for volume, brightness, and media.", ModuleKind.Capability),
        new("Mixdeck", "Mixdeck", "Per-app audio mixer overlay.", ModuleKind.Capability),
        new("Inlay", "Inlay", "Start-menu style launcher and shortcuts.", ModuleKind.Capability),
        new("Slate", "Slate", "Idle / lock screen surface.", ModuleKind.Capability),
        new("Chord", "Chord", "Keyboard-driven app launcher.", ModuleKind.Capability),
        new("Substrate", "Substrate", "Notification shade / control center.", ModuleKind.Capability),
        new("Chrono", "Chrono", "Clock collection with multiple styles.", ModuleKind.Widget),
        new("Phono", "Phono", "Media player widget.", ModuleKind.Widget),
        new("Pulse", "Pulse", "Audio visualizer.", ModuleKind.Widget),
        new("Canvas", "Canvas", "Minimal plain-text information widget.", ModuleKind.Widget),
    ];

    public static IEnumerable<ModuleInfo> Modules => All.Where(m =>
        m.Kind is ModuleKind.Capability or ModuleKind.Hybrid);
    public static IEnumerable<ModuleInfo> Widgets => All.Where(m => m.Kind == ModuleKind.Widget);
    public static IEnumerable<ModuleInfo> Capabilities => Modules;

    public static bool IsCapability(string moduleId) =>
        TryGet(moduleId, out var info) && info is not null &&
        info.Kind is ModuleKind.Capability or ModuleKind.Hybrid;

    public static bool TryGet(string moduleId, out ModuleInfo? info)
    {
        info = All.FirstOrDefault(m => m.Id.Equals(moduleId, StringComparison.OrdinalIgnoreCase));
        return info is not null;
    }

    public static bool IsInstalled(string moduleId) =>
        Directory.Exists(Path.Combine(AppPaths.ModulesDirectory, moduleId));
}
