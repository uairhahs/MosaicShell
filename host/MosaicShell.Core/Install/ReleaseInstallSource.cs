using System.IO.Compression;
using System.Text.Json;

namespace MosaicShell.Core.Install;

public sealed class GitHubReleaseInfo
{
    public required string TagName { get; init; }
    public required string ZipUrl { get; init; }
    public required string ZipFileName { get; init; }
    public string? Sha256 { get; init; }
}

/// <summary>
/// Resolves and downloads the latest MosaicShell release zip from GitHub.
/// Never executes downloaded content; only extracts the verified archive.
/// </summary>
public sealed class ReleaseInstallSource
{
    public const string DefaultRepository = "uairhahs/MosaicShell";

    private readonly HttpClient _http;
    private readonly ReleaseDownloader _downloader;

    public ReleaseInstallSource(HttpClient? http = null)
    {
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("MosaicShell-Installer/0.1");
        if (!_http.DefaultRequestHeaders.Accept.Any())
            _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        _downloader = new ReleaseDownloader(_http);
    }

    public async Task<GitHubReleaseInfo> ResolveLatestAsync(
        string? repository = null,
        CancellationToken ct = default)
    {
        var repo = string.IsNullOrWhiteSpace(repository) ? DefaultRepository : repository.Trim();
        var url = $"https://api.github.com/repos/{repo}/releases/latest";
        await using var stream = await _http.GetStreamAsync(url, ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;
        var tag = root.GetProperty("tag_name").GetString()
                  ?? throw new InvalidOperationException("Release JSON missing tag_name.");

        if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Release JSON missing assets.");

        JsonElement? chosen = null;
        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? "";
            if (!name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                continue;
            if (name.StartsWith("MosaicShell-", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("MosaicShell-Host-", StringComparison.OrdinalIgnoreCase))
            {
                chosen = asset;
                break;
            }
        }

        if (chosen is null)
            throw new InvalidOperationException($"No MosaicShell-*.zip asset on release {tag}.");

        var zipName = chosen.Value.GetProperty("name").GetString()!;
        var zipUrl = chosen.Value.GetProperty("browser_download_url").GetString()
                     ?? throw new InvalidOperationException("Asset missing browser_download_url.");

        return new GitHubReleaseInfo
        {
            TagName = tag,
            ZipUrl = zipUrl,
            ZipFileName = zipName,
        };
    }

    /// <summary>
    /// Downloads the latest release zip, extracts it, and returns the bundle root path.
    /// </summary>
    public async Task<(string BundleRoot, GitHubReleaseInfo Release)> DownloadAndExtractLatestAsync(
        string destinationDirectory,
        string? repository = null,
        IProgress<HostInstallProgress>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report(new HostInstallProgress { Stage = "resolve", Detail = repository ?? DefaultRepository });
        var release = await ResolveLatestAsync(repository, ct);

        Directory.CreateDirectory(destinationDirectory);
        progress?.Report(new HostInstallProgress { Stage = "download", Detail = release.ZipFileName, Fraction = 0.2 });
        var zipPath = await _downloader.DownloadAsync(
            new ReleaseAsset
            {
                Url = release.ZipUrl,
                FileName = release.ZipFileName,
                Sha256 = release.Sha256,
            },
            destinationDirectory,
            ct);

        var extractDir = Path.Combine(destinationDirectory, "extract-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(extractDir);
        progress?.Report(new HostInstallProgress { Stage = "extract", Detail = extractDir, Fraction = 0.6 });
        await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, extractDir), ct);

        var bundle = ReleaseBundleLayout.TryFindRoot(extractDir)
                     ?? ReleaseBundleLayout.TryFindRoot(Path.Combine(extractDir, Path.GetFileNameWithoutExtension(release.ZipFileName)))
                     ?? (ReleaseBundleLayout.IsBundleRoot(extractDir) ? extractDir : null);

        if (bundle is null)
        {
            // Zip may flatten contents at extractDir root
            if (Directory.Exists(Path.Combine(extractDir, HostInstallLayoutSpec.TilesFolder)))
                bundle = extractDir;
        }

        if (bundle is null || !Directory.Exists(Path.Combine(bundle, HostInstallLayoutSpec.TilesFolder)))
            throw new InvalidOperationException(
                "Downloaded release zip did not contain a MosaicShell bundle (Host*/Mosaicist/Tiles).");

        progress?.Report(new HostInstallProgress { Stage = "ready", Detail = bundle, Fraction = 1 });
        return (bundle, release);
    }
}
