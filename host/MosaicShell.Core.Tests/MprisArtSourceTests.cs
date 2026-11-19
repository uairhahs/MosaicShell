using System.Diagnostics;
using System.Net;
using FluentAssertions;
using MosaicShell.Core.Services.BrowserBridge;
using MosaicShell.Core.Services.Mpris;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// mpris:artUrl comes from any process on the session bus. Local files are read off the caller's thread, only when
    /// they are regular files within the size cap and really are images; https goes through the browser artwork
    /// fetcher and its public-address rule; every other scheme is ignored. TryGet never blocks on I/O.
    /// </summary>
    public sealed class MprisArtSourceTests : IDisposable
    {
        private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);
        private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0, 1, 2, 3, 4];
        private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, 0x4A, 0x46, 0x49, 0x46, 0, 1, 9, 9];

        private readonly string _dir = Path.Combine(Path.GetTempPath(), "mosaic-mpris-art-" + Guid.NewGuid().ToString("N"));
        private readonly StubHttpHandler _http;
        private readonly MprisArtSource _art;
        private int _fetched;

        public MprisArtSourceTests()
        {
            _ = Directory.CreateDirectory(_dir);
            _http = new StubHttpHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Png) }));
            _art = new MprisArtSource(new BrowserArtworkFetcher(_http));
            _art.Fetched += (_, _) => Interlocked.Increment(ref _fetched);
        }

        public void Dispose()
        {
            _art.Dispose();
            Directory.Delete(_dir, recursive: true);
        }

        [Fact]
        public async Task A_local_image_is_read_in_the_background_and_then_served()
        {
            string url = WriteFile("cover.png", Png);

            _ = _art.TryGet(url).Should().BeNull("the first call only starts the read");
            await _art.IdleAsync();

            _ = _art.TryGet(url).Should().Equal(Png);
            _ = _fetched.Should().Be(1);
        }

        [Fact]
        public async Task A_cached_local_image_is_not_read_again_while_the_file_is_unchanged()
        {
            string url = WriteFile("cover.png", Png);
            _ = _art.TryGet(url);
            await _art.IdleAsync();

            _ = _art.TryGet(url);
            _ = _art.TryGet(url);
            await _art.IdleAsync();

            _ = _art.FileReads.Should().Be(1);
        }

        [Fact]
        public async Task A_file_rewritten_at_the_same_path_is_read_again()
        {
            // Some players reuse one path (such as /tmp/cover.jpg) for every track.
            string url = WriteFile("cover.jpg", Png);
            _ = _art.TryGet(url);
            await _art.IdleAsync();

            _ = WriteFile("cover.jpg", Jpeg, DateTime.UtcNow.AddMinutes(1));
            _ = _art.TryGet(url).Should().BeNull("the cached bytes belong to the old file");
            await _art.IdleAsync();

            _ = _art.TryGet(url).Should().Equal(Jpeg);
            _ = _fetched.Should().Be(2);
        }

        [Fact]
        public async Task A_percent_encoded_path_is_decoded()
        {
            string url = WriteFile("My Album Art.png", Png);
            _ = url.Should().Contain("%20");

            _ = _art.TryGet(url);
            await _art.IdleAsync();

            _ = _art.TryGet(url).Should().Equal(Png);
        }

        [Fact]
        public async Task A_local_file_that_is_not_an_image_is_refused_and_not_read_again_until_it_changes()
        {
            string url = WriteFile("notes.png", "not an image at all"u8.ToArray());

            _ = _art.TryGet(url);
            await _art.IdleAsync();
            _ = _art.TryGet(url).Should().BeNull();
            await _art.IdleAsync();

            _ = _art.FileReads.Should().Be(1);
            _ = _fetched.Should().Be(0);

            _ = WriteFile("notes.png", Png, DateTime.UtcNow.AddMinutes(1));
            _ = _art.TryGet(url);
            await _art.IdleAsync();
            _ = _art.TryGet(url).Should().Equal(Png);
        }

        [Fact]
        public async Task A_local_file_over_the_size_cap_is_never_read()
        {
            byte[] big = new byte[MprisArtSource.MaxBytes + 1];
            Png.CopyTo(big, 0);
            string url = WriteFile("huge.png", big);

            _ = _art.TryGet(url).Should().BeNull();
            await _art.IdleAsync();

            _ = _art.TryGet(url).Should().BeNull();
            _ = _art.FileReads.Should().Be(0);
        }

        [Fact]
        public async Task A_missing_file_an_empty_file_and_a_directory_are_ignored()
        {
            string missing = new Uri(Path.Combine(_dir, "gone.png")).AbsoluteUri;
            string empty = WriteFile("empty.png", []);
            string directory = new Uri(_dir).AbsoluteUri;

            foreach (string url in new[] { missing, empty, directory })
            {
                _ = _art.TryGet(url).Should().BeNull(url);
            }

            await _art.IdleAsync();
            _ = _art.FileReads.Should().Be(0);
        }

        [UnixFact]
        public async Task A_fifo_is_never_opened()
        {
            // Opening a FIFO for reading blocks until a writer appears; a player naming one must not hang a thread.
            string path = Path.Combine(_dir, "pipe.png");
            using (Process mkfifo = Process.Start("mkfifo", [path]))
            {
                await mkfifo.WaitForExitAsync();
                _ = mkfifo.ExitCode.Should().Be(0);
            }

            _ = _art.TryGet(new Uri(path).AbsoluteUri).Should().BeNull();

            Task idle = _art.IdleAsync();
            _ = (await Task.WhenAny(idle, Task.Delay(Patience))).Should().BeSameAs(idle);
            _ = _art.FileReads.Should().Be(0);
        }

        [Fact]
        public async Task Https_art_is_fetched_through_the_browser_artwork_fetcher()
        {
            const string url = "https://i.scdn.co/image/abc";
            TaskCompletionSource arrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _art.Fetched += (_, _) => arrived.TrySetResult();

            _ = _art.TryGet(url).Should().BeNull();
            await arrived.Task.WaitAsync(Patience);

            _ = _art.TryGet(url).Should().Equal(Png);
            _ = _http.Calls.Should().Be(1);
        }

        [Theory]
        [InlineData("http://i.scdn.co/image/abc")]
        [InlineData("https://192.168.1.10/cover.jpg")]
        [InlineData("https://localhost/cover.jpg")]
        [InlineData("ftp://example.com/cover.jpg")]
        [InlineData("data:image/png;base64,iVBORw0KGgo=")]
        [InlineData("/tmp/cover.png")]
        [InlineData("not a url")]
        [InlineData("")]
        public async Task Other_urls_are_ignored_without_any_request(string url)
        {
            _ = _art.TryGet(url).Should().BeNull();
            await _art.IdleAsync();
            await Task.Delay(50);

            _ = _http.Calls.Should().Be(0);
            _ = _art.FileReads.Should().Be(0);
        }

        [Fact]
        public void A_unc_file_url_is_ignored()
        {
            // On Windows a UNC path would reach out over the network (and offer the user's credentials).
            _ = _art.TryGet("file://fileserver/share/cover.png").Should().BeNull();
            _ = _art.FileReads.Should().Be(0);
        }

        [Fact]
        public async Task Only_a_bounded_number_of_local_images_is_kept()
        {
            List<string> urls = [];
            for (int i = 0; i <= MprisArtSource.MaxCachedFiles; i++)
            {
                string url = WriteFile($"cover{i}.png", Png);
                urls.Add(url);
                _ = _art.TryGet(url);
                await _art.IdleAsync();
            }

            _ = _art.TryGet(urls[0]).Should().BeNull("the oldest entry was evicted");
            _ = _art.TryGet(urls[^1]).Should().Equal(Png);
        }

        private string WriteFile(string name, byte[] bytes, DateTime? lastWriteUtc = null)
        {
            string path = Path.Combine(_dir, name);
            File.WriteAllBytes(path, bytes);
            if (lastWriteUtc is { } stamp)
            {
                File.SetLastWriteTimeUtc(path, stamp);
            }

            return new Uri(path).AbsoluteUri;
        }
    }
}
