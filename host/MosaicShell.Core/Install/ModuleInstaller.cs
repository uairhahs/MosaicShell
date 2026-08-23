using System.IO.Compression;
using System.Text.Json;
using MosaicShell.Core.Runtime;

namespace MosaicShell.Core.Install;

public sealed class ModuleInstallProgress
{
    public required string Stage { get; init; }
    public string? Detail { get; init; }
}

/// <summary>
/// Installs modules from repo <c>Tiles/{Id}/</c> stubs, a packaged folder, or a zip
/// containing <c>module.manifest.json</c> (+ optional DLLs).
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
            "in the MosaicShell repo, or use InstallFromPackageAsync for a folder/zip package.");
    }

    /// <summary>
    /// Install from a directory or .zip that contains <c>module.manifest.json</c>
    /// (and optionally module.dll / capability.dll / tile.dll).
    /// </summary>
    public Task InstallFromPackageAsync(
        string packagePath,
        IProgress<ModuleInstallProgress>? progress = null,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        AppPaths.EnsureLayout();

        if (string.IsNullOrWhiteSpace(packagePath))
            throw new ArgumentException("Package path is required.", nameof(packagePath));

        var full = Path.GetFullPath(packagePath);
        progress?.Report(new ModuleInstallProgress { Stage = "package", Detail = full });

        string staging;
        var cleanupStaging = false;
        if (File.Exists(full) && full.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            staging = Path.Combine(Path.GetTempPath(), "mosaic-pkg-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(staging);
            ZipFile.ExtractToDirectory(full, staging);
            cleanupStaging = true;
        }
        else if (Directory.Exists(full))
        {
            staging = full;
        }
        else
        {
            throw new FileNotFoundException("Package folder or zip not found.", full);
        }

        try
        {
            var manifestPath = FindManifest(staging)
                ?? throw new InvalidOperationException(
                    "Package must contain module.manifest.json (at root or one level down).");

            var sourceDir = Path.GetDirectoryName(manifestPath)!;
            var manifest = JsonSerializer.Deserialize<ModuleManifest>(File.ReadAllText(manifestPath))
                           ?? throw new InvalidOperationException("Could not parse module.manifest.json.");
            var moduleId = string.IsNullOrWhiteSpace(manifest.Id)
                ? Path.GetFileName(sourceDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                : manifest.Id;
            if (string.IsNullOrWhiteSpace(moduleId))
                throw new InvalidOperationException("module.manifest.json must set Id.");

            var dest = Path.Combine(AppPaths.ModulesDirectory, moduleId);
            if (Directory.Exists(dest))
                Directory.Delete(dest, recursive: true);
            CopyDirectory(sourceDir, dest);

            // Ensure destination has a manifest with Id set.
            manifest.Id = moduleId;
            File.WriteAllText(
                Path.Combine(dest, "module.manifest.json"),
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

            var marker = new
            {
                Id = moduleId,
                InstalledUtc = DateTime.UtcNow,
                Source = full,
                Runtime = "avalonia",
                Package = true
            };
            File.WriteAllText(
                Path.Combine(dest, "module.json"),
                JsonSerializer.Serialize(marker, new JsonSerializerOptions { WriteIndented = true }));

            progress?.Report(new ModuleInstallProgress { Stage = "done", Detail = dest });
            return Task.CompletedTask;
        }
        finally
        {
            if (cleanupStaging)
            {
                try { Directory.Delete(staging, recursive: true); } catch { /* ignore */ }
            }
        }
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

    private static string? FindManifest(string root)
    {
        var direct = Path.Combine(root, "module.manifest.json");
        if (File.Exists(direct))
            return direct;

        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            var nested = Path.Combine(dir, "module.manifest.json");
            if (File.Exists(nested))
                return nested;
        }

        return null;
    }

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
