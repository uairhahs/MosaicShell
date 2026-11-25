using MosaicShell.Core.Services.BrowserBridge;

namespace MosaicShell.Core.Services.Mpris
{
    /// <summary>
    /// Album art named by <c>mpris:artUrl</c>, which any process on the session bus can set to anything. A local file
    /// is read off the caller's thread, only when it is a non-empty regular file within <see cref="MaxBytes"/> and its
    /// bytes are a supported image. https goes through <see cref="BrowserArtworkFetcher"/>, so only public addresses
    /// are reached. Every other scheme (http, data, UNC file URLs) is ignored.
    /// </summary>
    public sealed class MprisArtSource : IMprisArtSource
    {
        public const int MaxBytes = BrowserArtworkFetcher.MaxBytes;

        /// <summary>Local images kept; a player shows one at a time, so a few cover switching between players.</summary>
        public const int MaxCachedFiles = 8;

        private readonly BrowserArtworkFetcher _web;
        private readonly Lock _gate = new();

        // Url to the file's stamp when it was read, and its bytes (null when refused at that stamp).
        private readonly Dictionary<string, (FileStamp Stamp, byte[]? Bytes)> _files = new(StringComparer.Ordinal);
        private readonly Queue<string> _insertion = new();
        private readonly Dictionary<string, Task> _reading = new(StringComparer.Ordinal);
        private int _fileReads;
        private bool _disposed;

        /// <param name="web">The https fetcher; this source owns and disposes it. Tests pass one with a stub handler.</param>
        public MprisArtSource(BrowserArtworkFetcher? web = null)
        {
            _web = web ?? new BrowserArtworkFetcher();
            _web.Fetched += OnWebFetched;
        }

        public event EventHandler? Fetched;

        /// <summary>Local files opened so far. For tests.</summary>
        internal int FileReads => Volatile.Read(ref _fileReads);

        public byte[]? TryGet(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                return null;
            }

            if (uri.IsFile)
            {
                return uri.IsUnc ? null : TryGetFile(url, uri.LocalPath);
            }

            if (uri.Scheme != Uri.UriSchemeHttps)
            {
                return null;
            }

            byte[]? image = _web.TryGet(url);
            if (image is null)
            {
                _web.Request(url);
            }

            return image;
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
            }

            _web.Fetched -= OnWebFetched;
            _web.Dispose();
        }

        /// <summary>Completes when the local file reads started so far have finished. For tests.</summary>
        internal Task IdleAsync()
        {
            lock (_gate)
            {
                return Task.WhenAll([.. _reading.Values]);
            }
        }

        private static FileStamp? Stat(string path)
        {
            try
            {
                // Length is 0 for FIFOs, sockets and device nodes, so this also keeps them from ever being opened (a
                // FIFO open blocks until a writer appears). FileInfo.Exists is false for a directory.
                FileInfo info = new(path);
                return info.Exists && info.Length is > 0 and <= MaxBytes
                    ? new FileStamp(info.Length, info.LastWriteTimeUtc)
                    : null;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                return null;
            }
        }

        private static byte[]? ReadImage(string path)
        {
            try
            {
                using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using MemoryStream buffer = new();
                byte[] chunk = new byte[16 * 1024];
                int read;
                while ((read = stream.Read(chunk)) > 0)
                {
                    if (buffer.Length + read > MaxBytes)
                    {
                        return null;
                    }

                    buffer.Write(chunk, 0, read);
                }

                byte[] bytes = buffer.ToArray();
                return BrowserArtworkPolicy.LooksLikeSupportedImage(bytes) ? bytes : null;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        private byte[]? TryGetFile(string url, string path)
        {
            if (Stat(path) is not { } stamp)
            {
                return null;
            }

            lock (_gate)
            {
                if (_disposed)
                {
                    return null;
                }

                if (_files.TryGetValue(url, out (FileStamp Stamp, byte[]? Bytes) known) && known.Stamp == stamp)
                {
                    return known.Bytes;
                }

                if (!_reading.ContainsKey(url))
                {
                    _reading[url] = Task.Run(() => Read(url, path, stamp));
                }

                return null;
            }
        }

        private void Read(string url, string path, FileStamp stamp)
        {
            _ = Interlocked.Increment(ref _fileReads);
            byte[]? image = ReadImage(path);
            lock (_gate)
            {
                _ = _reading.Remove(url);
                if (_disposed)
                {
                    return;
                }

                Store(url, stamp, image);
            }

            if (image is not null)
            {
                Fetched?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Store(string url, FileStamp stamp, byte[]? image)
        {
            if (!_files.ContainsKey(url))
            {
                _insertion.Enqueue(url);
            }

            _files[url] = (stamp, image);
            while (_files.Count > MaxCachedFiles)
            {
                _ = _files.Remove(_insertion.Dequeue());
            }
        }

        private void OnWebFetched(object? sender, EventArgs e)
        {
            Fetched?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Identifies one version of a file, so a player that rewrites the same path for each track gets the new art.
        /// </summary>
        private readonly record struct FileStamp(long Length, DateTime LastWriteUtc);
    }
}
