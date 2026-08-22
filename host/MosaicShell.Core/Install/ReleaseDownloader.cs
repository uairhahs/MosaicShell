using System.Security.Cryptography;
using System.Text.Json;

namespace MosaicShell.Core.Install;

public sealed class ReleaseAsset
{
    public required string Url { get; init; }
    public string? Sha256 { get; init; }
    public string? FileName { get; init; }
}

/// <summary>
/// Downloads release assets to disk and verifies SHA-256 when provided.
/// Never executes downloaded content (no iex / script piping).
/// </summary>
public sealed class ReleaseDownloader
{
    private readonly HttpClient _http;

    public ReleaseDownloader(HttpClient? http = null)
    {
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("MosaicShell-Mosaicist/0.1");
    }

    public async Task<string> DownloadAsync(ReleaseAsset asset, string destinationDirectory, CancellationToken ct = default)
    {
        Directory.CreateDirectory(destinationDirectory);
        var name = asset.FileName
                   ?? Path.GetFileName(new Uri(asset.Url).AbsolutePath)
                   ?? "download.bin";
        var path = Path.Combine(destinationDirectory, name);

        await using (var remote = await _http.GetStreamAsync(asset.Url, ct))
        await using (var local = File.Create(path))
            await remote.CopyToAsync(local, ct);

        if (!string.IsNullOrWhiteSpace(asset.Sha256))
        {
            var hash = await ComputeSha256Async(path, ct);
            if (!hash.Equals(asset.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(path);
                throw new InvalidOperationException(
                    $"SHA-256 mismatch for {name}. Expected {asset.Sha256}, got {hash}.");
            }
        }

        return path;
    }

    public static async Task<string> ComputeSha256Async(string filePath, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public async Task InstallModuleFromZipAsync(string zipPath, string moduleId, CancellationToken ct = default)
    {
        AppPaths.EnsureLayout();
        var dest = Path.Combine(AppPaths.ModulesDirectory, moduleId);
        if (Directory.Exists(dest))
            Directory.Delete(dest, recursive: true);

        await Task.Run(() => System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, dest), ct);

        var marker = new { Id = moduleId, InstalledUtc = DateTime.UtcNow };
        await File.WriteAllTextAsync(
            Path.Combine(dest, "module.json"),
            JsonSerializer.Serialize(marker, new JsonSerializerOptions { WriteIndented = true }),
            ct);
    }
}
