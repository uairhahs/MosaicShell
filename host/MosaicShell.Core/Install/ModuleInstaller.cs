using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using MosaicShell.Core.Runtime;

namespace MosaicShell.Core.Install;

public sealed class ModuleInstallProgress
{
    public required string Stage { get; init; }
    public string? Detail { get; init; }
}

/// <summary>
/// Installs modules from a local source tree, a local .rmskin/.zip, or a GitHub release asset.
/// Never executes downloaded scripts.
/// </summary>
public sealed class ModuleInstaller
{
    private readonly ReleaseDownloader _downloader;
    private readonly HttpClient _http;

    public ModuleInstaller(HttpClient? http = null)
    {
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("MosaicShell-Mosaicist/0.1");
        _downloader = new ReleaseDownloader(_http);
    }

    public async Task InstallAsync(
        string moduleId,
        IProgress<ModuleInstallProgress>? progress = null,
        CancellationToken ct = default,
        string? sourceTreeRoot = null)
    {
        AppPaths.EnsureLayout();

        if (TryInstallFromSourceTree(moduleId, progress, sourceTreeRoot))
            return;

        progress?.Report(new ModuleInstallProgress { Stage = "resolve", Detail = "Looking up GitHub release…" });
        var assetUrl = await ResolveLatestAssetUrlAsync(moduleId, ct);
        progress?.Report(new ModuleInstallProgress { Stage = "download", Detail = assetUrl });

        var downloaded = await _downloader.DownloadAsync(
            new ReleaseAsset
            {
                Url = assetUrl,
                FileName = $"{moduleId}-latest.rmskin"
            },
            AppPaths.CacheDirectory,
            ct);

        await InstallPackageAsync(downloaded, moduleId, progress, ct);
    }

    public async Task InstallPackageAsync(
        string packagePath,
        string moduleId,
        IProgress<ModuleInstallProgress>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report(new ModuleInstallProgress { Stage = "extract", Detail = packagePath });
        var work = Path.Combine(AppPaths.CacheDirectory, $"extract-{moduleId}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(work);

        try
        {
            // .rmskin is already a zip archive, just extract in place (no temp copy; avoids file locks).
            var unpack = Path.Combine(work, "unpacked");
            await Task.Run(() => ZipFile.ExtractToDirectory(packagePath, unpack), ct);

            var skinSource = FindSkinRoot(unpack, moduleId)
                             ?? throw new InvalidOperationException(
                                 $"Could not find skin folder for '{moduleId}' inside package.");

            var dest = Path.Combine(AppPaths.ModulesDirectory, moduleId);
            if (Directory.Exists(dest))
                Directory.Delete(dest, recursive: true);

            progress?.Report(new ModuleInstallProgress { Stage = "copy", Detail = dest });
            CopyDirectory(skinSource, dest);

            var marker = new
            {
                Id = moduleId,
                InstalledUtc = DateTime.UtcNow,
                Source = packagePath
            };
            await File.WriteAllTextAsync(
                Path.Combine(dest, "module.json"),
                JsonSerializer.Serialize(marker, new JsonSerializerOptions { WriteIndented = true }),
                ct);
            ModuleManifest.WriteDefault(moduleId);

            progress?.Report(new ModuleInstallProgress { Stage = "done", Detail = dest });
        }
        finally
        {
            try { Directory.Delete(work, recursive: true); } catch { /* ignore */ }
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
        if (source is null) return false;

        progress?.Report(new ModuleInstallProgress { Stage = "local", Detail = source });
        var dest = Path.Combine(AppPaths.ModulesDirectory, moduleId);
        if (Directory.Exists(dest))
            Directory.Delete(dest, recursive: true);
        CopyDirectory(source, dest);

        var marker = new
        {
            Id = moduleId,
            InstalledUtc = DateTime.UtcNow,
            Source = source
        };
        File.WriteAllText(
            Path.Combine(dest, "module.json"),
            JsonSerializer.Serialize(marker, new JsonSerializerOptions { WriteIndented = true }));
        ModuleManifest.WriteDefault(moduleId);

        progress?.Report(new ModuleInstallProgress { Stage = "done", Detail = dest });
        return true;
    }

    private async Task<string> ResolveLatestAssetUrlAsync(string moduleId, CancellationToken ct)
    {
        var orgs = new[] { "uairhahs", "MosaicShell" };
        Exception? last = null;
        foreach (var org in orgs)
        {
            try
            {
                var url = $"https://api.github.com/repos/{org}/{moduleId}/releases/latest";
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Accept.ParseAdd("application/vnd.github+json");
                using var res = await _http.SendAsync(req, ct);
                if (!res.IsSuccessStatusCode)
                {
                    last = new HttpRequestException($"{(int)res.StatusCode} from {url}");
                    continue;
                }

                await using var stream = await res.Content.ReadAsStreamAsync(ct);
                var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, cancellationToken: ct)
                              ?? throw new InvalidOperationException("Empty release payload.");
                var asset = release.Assets?.FirstOrDefault(a =>
                                a.Name?.EndsWith(".rmskin", StringComparison.OrdinalIgnoreCase) == true)
                            ?? release.Assets?.FirstOrDefault();
                if (asset?.BrowserDownloadUrl is null)
                    throw new InvalidOperationException($"No downloadable asset on {org}/{moduleId} latest release.");
                return asset.BrowserDownloadUrl;
            }
            catch (Exception ex)
            {
                last = ex;
            }
        }

        throw new InvalidOperationException(
            $"Could not resolve a release for '{moduleId}'. Last error: {last?.Message}");
    }

    private static string? FindSkinRoot(string unpackRoot, string moduleId)
    {
        var skins = Path.Combine(unpackRoot, "Skins");
        if (Directory.Exists(skins))
        {
            var direct = Path.Combine(skins, moduleId);
            if (Directory.Exists(direct)) return direct;

            var tiles = Path.Combine(skins, "Tiles", moduleId);
            if (Directory.Exists(tiles)) return tiles;

            foreach (var dir in Directory.GetDirectories(skins))
            {
                if (Path.GetFileName(dir).Equals(moduleId, StringComparison.OrdinalIgnoreCase))
                    return dir;
                var nested = Path.Combine(dir, moduleId);
                if (Directory.Exists(nested)) return nested;
            }
        }

        var named = Directory.GetDirectories(unpackRoot, moduleId, SearchOption.AllDirectories)
            .FirstOrDefault(LooksLikeSkinRoot);
        if (named is not null) return named;

        var directRoot = Path.Combine(unpackRoot, moduleId);
        return Directory.Exists(directRoot) ? directRoot : null;
    }

    private static bool LooksLikeSkinRoot(string dir) =>
        File.Exists(Path.Combine(dir, "Main.ini"))
        || Directory.Exists(Path.Combine(dir, "Main"))
        || Directory.Exists(Path.Combine(dir, "@Resources"))
        || Directory.GetFiles(dir, "*.ini", SearchOption.AllDirectories).Length > 0;

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "Tiles"))
                && File.Exists(Path.Combine(dir.FullName, "RunMosaicist.ps1")))
                return dir.FullName;
            if (Directory.Exists(Path.Combine(dir.FullName, "Tiles"))
                && File.Exists(Path.Combine(dir.FullName, "host", "MosaicShell.sln")))
                return dir.FullName;
            // host/MosaicShell.Host/bin/... → walk to repo
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

    private sealed class GitHubRelease
    {
        [JsonPropertyName("assets")]
        public List<GitHubAsset>? Assets { get; set; }
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }
    }
}
