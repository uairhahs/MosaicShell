using System.Text.Json;
using MosaicShell.Core.Runtime;

namespace MosaicShell.Core.Install;

public sealed class ModuleInstallProgress
{
    public required string Stage { get; init; }
    public string? Detail { get; init; }
}

/// <summary>
/// Installs modules from the repo's native <c>Tiles/{Id}/</c> stub only.
/// Runtime code lives under <c>host/</c>; install copies metadata + <c>module.native.json</c>.
/// </summary>
public sealed class ModuleInstaller
{
    public Task InstallAsync(
        string moduleId,
        IProgress<ModuleInstallProgress>? progress = null,
        CancellationToken ct = default,
        string? sourceTreeRoot = null)
    {
        ct.ThrowIfCancellationRequested();
        AppPaths.EnsureLayout();

        if (TryInstallFromSourceTree(moduleId, progress, sourceTreeRoot))
            return Task.CompletedTask;

        throw new InvalidOperationException(
            $"No native install stub for '{moduleId}'. Expected Tiles/{moduleId}/module.native.json " +
            "in the MosaicShell repo. Clone the repo and run Mosaicist from the dev tree, or copy Tiles/ into your install root.");
    }

    public bool TryInstallFromSourceTree(
        string moduleId,
        IProgress<ModuleInstallProgress>? progress = null,
        string? repoRoot = null)
    {
        var root = repoRoot ?? FindRepoRoot();
        if (root is null) return false;

        var candidates = new[]
        {
            Path.Combine(root, "Tiles", moduleId),
            Path.Combine(root, moduleId),
        };

        var source = candidates.FirstOrDefault(Directory.Exists);
        if (source is null || !IsNativeModuleStub(source)) return false;

        progress?.Report(new ModuleInstallProgress { Stage = "local", Detail = source });
        var dest = Path.Combine(AppPaths.ModulesDirectory, moduleId);
        if (Directory.Exists(dest))
            Directory.Delete(dest, recursive: true);
        CopyDirectory(source, dest);

        var marker = new
        {
            Id = moduleId,
            InstalledUtc = DateTime.UtcNow,
            Source = source,
            Runtime = "avalonia"
        };
        File.WriteAllText(
            Path.Combine(dest, "module.json"),
            JsonSerializer.Serialize(marker, new JsonSerializerOptions { WriteIndented = true }));
        ModuleManifest.WriteDefault(moduleId);

        progress?.Report(new ModuleInstallProgress
        {
            Stage = "done",
            Detail = $"{dest} (native stub)"
        });
        return true;
    }

    /// <summary>Native modules ship a stub folder with module.native.json (or native.marker).</summary>
    public static bool IsNativeModuleStub(string dir) =>
        File.Exists(Path.Combine(dir, "module.native.json"))
        || File.Exists(Path.Combine(dir, "native.marker"));

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "Tiles"))
                && File.Exists(Path.Combine(dir.FullName, "host", "MosaicShell.sln")))
                return dir.FullName;
            if (File.Exists(Path.Combine(dir.FullName, "MosaicShell.sln"))
                && Directory.Exists(Path.Combine(dir.Parent?.FullName ?? "", "Tiles")))
                return dir.Parent!.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, file);
            var target = Path.Combine(dest, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }
}
