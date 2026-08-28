using System.Security.Cryptography;
using FluentAssertions;
using MosaicShell.Core.Install;

namespace MosaicShell.Core.Tests
{
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
            byte[] payload = "hello-mosaic"u8.ToArray();
            string sha = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
            StaticHandler handler = new(payload);
            ReleaseDownloader dl = new(new HttpClient(handler));

            string path = await dl.DownloadAsync(
                new ReleaseAsset
                {
                    Url = "https://example.test/a.bin",
                    Sha256 = sha,
                    FileName = "a.bin"
                },
                AppPaths.CacheDirectory);

            _ = File.Exists(path).Should().BeTrue();
            _ = (await File.ReadAllBytesAsync(path)).Should().Equal(payload);
        }

        [Fact]
        public async Task Download_with_bad_sha256_deletes_file_and_throws()
        {
            StaticHandler handler = new("payload"u8.ToArray());
            ReleaseDownloader dl = new(new HttpClient(handler));

            Func<Task<string>> act = async () => await dl.DownloadAsync(
                new ReleaseAsset
                {
                    Url = "https://example.test/b.bin",
                    Sha256 = new string('0', 64),
                    FileName = "b.bin"
                },
                AppPaths.CacheDirectory);

            _ = await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*SHA-256*");
            _ = File.Exists(Path.Combine(AppPaths.CacheDirectory, "b.bin")).Should().BeFalse();
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
}
