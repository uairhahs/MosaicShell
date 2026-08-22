using System.Security.Cryptography;
using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Install;

namespace MosaicShell.Core.Tests;

public class ReleaseDownloaderTests : IDisposable
{
    private readonly string _home;

    public ReleaseDownloaderTests()
    {
        _home = Path.Combine(Path.GetTempPath(), "ms-dl-" + Guid.NewGuid().ToString("N"));
        AppPaths.SetRootOverride(_home);
        AppPaths.EnsureLayout();
    }

    public void Dispose()
    {
        AppPaths.ClearRootOverride();
        try { Directory.Delete(_home, recursive: true); } catch { /* ignore */ }
    }

    [Fact]
    public async Task Download_with_matching_sha256_succeeds()
    {
        var payload = "hello-mosaic"u8.ToArray();
        var sha = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
        var handler = new StaticHandler(payload);
        var dl = new ReleaseDownloader(new HttpClient(handler));

        var path = await dl.DownloadAsync(
            new ReleaseAsset
            {
                Url = "https://example.test/a.bin",
                Sha256 = sha,
                FileName = "a.bin"
            },
            AppPaths.CacheDirectory);

        File.Exists(path).Should().BeTrue();
        (await File.ReadAllBytesAsync(path)).Should().Equal(payload);
    }

    [Fact]
    public async Task Download_with_bad_sha256_deletes_file_and_throws()
    {
        var handler = new StaticHandler("payload"u8.ToArray());
        var dl = new ReleaseDownloader(new HttpClient(handler));

        var act = async () => await dl.DownloadAsync(
            new ReleaseAsset
            {
                Url = "https://example.test/b.bin",
                Sha256 = new string('0', 64),
                FileName = "b.bin"
            },
            AppPaths.CacheDirectory);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*SHA-256*");
        File.Exists(Path.Combine(AppPaths.CacheDirectory, "b.bin")).Should().BeFalse();
    }

    private sealed class StaticHandler(byte[] body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(body)
            });
        }
    }
}
