using System.Text.Json;
using MosaicShell.Core.Modules;

namespace MosaicShell.Core.Runtime;

public sealed record TileSessionState(
    string ModuleId,
    int X,
    int Y,
    double Width,
    double Height);

public sealed class SessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string StorePath => Path.Combine(AppPaths.ConfigDirectory, "sessions.json");

    public static IReadOnlyList<TileSessionState> Load()
    {
        AppPaths.EnsureLayout();
        if (!File.Exists(StorePath)) return [];
        try
        {
            var json = File.ReadAllText(StorePath);
            return JsonSerializer.Deserialize<List<TileSessionState>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static void Save(IEnumerable<TileSessionState> sessions)
    {
        AppPaths.EnsureLayout();
        File.WriteAllText(StorePath, JsonSerializer.Serialize(sessions.ToList(), JsonOptions));
    }
}

public static class ModuleSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string PathFor(string moduleId) =>
        Path.Combine(AppPaths.ConfigDirectory, "modules", moduleId + ".json");

    public static T Load<T>(string moduleId, Func<T> factory) where T : class
    {
        AppPaths.EnsureLayout();
        Directory.CreateDirectory(Path.Combine(AppPaths.ConfigDirectory, "modules"));
        var path = PathFor(moduleId);
        if (!File.Exists(path))
        {
            var created = factory();
            Save(moduleId, created);
            return created;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions) ?? factory();
        }
        catch
        {
            return factory();
        }
    }

    public static void Save<T>(string moduleId, T settings)
    {
        AppPaths.EnsureLayout();
        Directory.CreateDirectory(Path.Combine(AppPaths.ConfigDirectory, "modules"));
        File.WriteAllText(PathFor(moduleId), JsonSerializer.Serialize(settings, JsonOptions));
    }

    public static void Delete(string moduleId)
    {
        var path = PathFor(moduleId);
        if (File.Exists(path)) File.Delete(path);
    }
}

public static class ModuleUninstaller
{
    public static bool Uninstall(string moduleId, ITileRuntime? runtime = null)
    {
        if (!ModuleCatalog.TryGet(moduleId, out _))
            return false;

        runtime?.Stop(moduleId);
        ModuleSettingsStore.Delete(moduleId);

        var dir = Path.Combine(AppPaths.ModulesDirectory, moduleId);
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);

        // Drop from persisted sessions
        var remaining = SessionStore.Load().Where(s => !s.ModuleId.Equals(moduleId, StringComparison.OrdinalIgnoreCase));
        SessionStore.Save(remaining);
        return true;
    }
}

public sealed class ModuleManifest
{
    public string Id { get; set; } = "";
    public string Version { get; set; } = "0.0.0";
    public string? DisplayName { get; set; }
    public Dictionary<string, string>? DefaultSettings { get; set; }

    public static string PathInModule(string moduleId) =>
        Path.Combine(AppPaths.ModulesDirectory, moduleId, "module.manifest.json");

    public static ModuleManifest? TryLoad(string moduleId)
    {
        var path = PathInModule(moduleId);
        if (!File.Exists(path)) return null;
        try
        {
            return JsonSerializer.Deserialize<ModuleManifest>(File.ReadAllText(path));
        }
        catch
        {
            return null;
        }
    }

    public static void WriteDefault(string moduleId, string? displayName = null)
    {
        var path = PathInModule(moduleId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var manifest = new ModuleManifest
        {
            Id = moduleId,
            Version = "1.0.0",
            DisplayName = displayName ?? moduleId
        };
        File.WriteAllText(path, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
    }
}
