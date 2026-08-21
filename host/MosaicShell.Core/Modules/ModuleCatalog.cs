namespace MosaicShell.Core.Modules;

public enum ModuleKind
{
    Module,
    Widget
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
        new("Tessera", "Tessera", "System flyouts for volume, brightness, and media.", ModuleKind.Module),
        new("Mixdeck", "Mixdeck", "Per-app audio mixer overlay.", ModuleKind.Module),
        new("Inlay", "Inlay", "Start-menu style launcher and shortcuts.", ModuleKind.Module),
        new("Slate", "Slate", "Idle / lock screen surface.", ModuleKind.Module),
        new("Chord", "Chord", "Keyboard-driven app launcher.", ModuleKind.Module),
        new("Substrate", "Substrate", "Notification shade / control center.", ModuleKind.Module),
        new("Chrono", "Chrono", "Clock collection with multiple styles.", ModuleKind.Widget),
        new("Phono", "Phono", "Media player widget.", ModuleKind.Widget),
        new("Pulse", "Pulse", "Audio visualizer.", ModuleKind.Widget),
        new("Canvas", "Canvas", "Minimal plain-text information widget.", ModuleKind.Widget),
    ];

    public static IEnumerable<ModuleInfo> Modules => All.Where(m => m.Kind == ModuleKind.Module);
    public static IEnumerable<ModuleInfo> Widgets => All.Where(m => m.Kind == ModuleKind.Widget);

    public static bool TryGet(string moduleId, out ModuleInfo? info)
    {
        info = All.FirstOrDefault(m => m.Id.Equals(moduleId, StringComparison.OrdinalIgnoreCase));
        return info is not null;
    }

    public static bool IsInstalled(string moduleId) =>
        Directory.Exists(Path.Combine(AppPaths.ModulesDirectory, moduleId));
}
