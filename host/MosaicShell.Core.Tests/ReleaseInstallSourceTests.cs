using System.Net;
using System.Text;
using FluentAssertions;
using MosaicShell.Core.Install;

namespace MosaicShell.Core.Tests;

public class ReleaseInstallSourceTests
{
    [Fact]
    public async Task ResolveLatest_picks_MosaicShell_zip_asset()
    {
        var json = """
            {
              "tag_name": "2026.8.23-b42",
              "assets": [
                { "name": "notes.txt", "browser_download_url": "https://example.test/notes.txt" },
                { "name": "MosaicShell-2026.8.23-b42.zip", "browser_download_url": "https://example.test/MosaicShell-2026.8.23-b42.zip" }
              ]
            }
            """;
        var handler = new MapHandler(new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["https://api.github.com/repos/uairhahs/MosaicShell/releases/latest"] =
                Encoding.UTF8.GetBytes(json),
        });
        var source = new ReleaseInstallSource(new HttpClient(handler));
        var info = await source.ResolveLatestAsync();
        info.TagName.Should().Be("2026.8.23-b42");
        info.ZipFileName.Should().Be("MosaicShell-2026.8.23-b42.zip");
        info.ZipUrl.Should().Contain("MosaicShell-2026.8.23-b42.zip");
    }

    [Fact]
    public async Task DownloadAndExtractLatest_produces_bundle_root()
    {
        var staging = Path.Combine(Path.GetTempPath(), "ms-relsrc-" + Guid.NewGuid().ToString("N"));
        var work = Path.Combine(Path.GetTempPath(), "ms-reldl-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(staging);
            Directory.CreateDirectory(Path.Combine(staging, "Host"));
            Directory.CreateDirectory(Path.Combine(staging, "Mosaicist"));
            Directory.CreateDirectory(Path.Combine(staging, "Tiles", "Canvas"));
            File.WriteAllText(Path.Combine(staging, "Host", "MosaicShell.Host.exe"), "h");
            File.WriteAllText(Path.Combine(staging, "Mosaicist", "Mosaicist.exe"), "m");
            File.WriteAllText(Path.Combine(staging, "Tiles", "Canvas", "module.native.json"), "{}");
            File.WriteAllText(Path.Combine(staging, "VERSION.txt"), "2026.8.23-b9");

            var zipPath = Path.Combine(Path.GetTempPath(), "ms-relzip-" + Guid.NewGuid().ToString("N") + ".zip");
            System.IO.Compression.ZipFile.CreateFromDirectory(staging, zipPath);
            var zipBytes = await File.ReadAllBytesAsync(zipPath);

            var json = """
                {
                  "tag_name": "2026.8.23-b9",
                  "assets": [
                    { "name": "MosaicShell-2026.8.23-b9.zip", "browser_download_url": "https://example.test/MosaicShell-2026.8.23-b9.zip" }
                  ]
                }
                """;
            var handler = new MapHandler(new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["https://api.github.com/repos/uairhahs/MosaicShell/releases/latest"] =
                    Encoding.UTF8.GetBytes(json),
                ["https://example.test/MosaicShell-2026.8.23-b9.zip"] = zipBytes,
            });

            var source = new ReleaseInstallSource(new HttpClient(handler));
            var (bundle, release) = await source.DownloadAndExtractLatestAsync(work);
            release.TagName.Should().Be("2026.8.23-b9");
            ReleaseBundleLayout.IsBundleRoot(bundle).Should().BeTrue();
            File.Exists(Path.Combine(bundle, "VERSION.txt")).Should().BeTrue();
        }
        finally
        {
            try { Directory.Delete(staging, recursive: true); } catch { /* ignore */ }
            try { Directory.Delete(work, recursive: true); } catch { /* ignore */ }
        }
    }

    private sealed class MapHandler(Dictionary<string, byte[]> map) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var key = request.RequestUri?.ToString() ?? "";
            if (!map.TryGetValue(key, out var body))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent($"missing {key}"),
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(body),
            });
        }
    }
}
