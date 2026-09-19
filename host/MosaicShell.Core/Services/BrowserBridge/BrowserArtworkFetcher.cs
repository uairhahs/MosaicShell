using System.Net;
using System.Net.Sockets;

namespace MosaicShell.Core.Services.BrowserBridge
{
    /// <summary>
    /// Downloads artwork for the flyout on behalf of a web page the Host does not trust: bounded in size and time,
    /// only real images, only from public addresses, no cookies or credentials, and no retry loop after a failure.
    /// Requests are asynchronous and results are read with <see cref="TryGet"/>, so the flyout never waits on the network.
    /// </summary>
    public sealed class BrowserArtworkFetcher : IDisposable
    {
        public const int MaxBytes = 2 * 1024 * 1024;
        public const int MaxCachedEntries = 16;

        /// <summary>Total bytes held; a full cache drops its least recently used images.</summary>
        public const int MaxCachedBytes = 8 * 1024 * 1024;

        /// <summary>How long a failed URL is left alone, so a broken image is not requested on every refresh.</summary>
        public static readonly TimeSpan FailureBackoff = TimeSpan.FromMinutes(1);

        private const int MaxRememberedFailures = 64;
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

        private readonly HttpClient _http;
        private readonly TimeProvider _clock;
        private readonly TimeSpan _timeout;
        private readonly Lock _gate = new();
        private readonly LinkedList<string> _recency = new();
        private readonly Dictionary<string, (LinkedListNode<string> Node, byte[] Bytes)> _cache = [];
        private readonly Dictionary<string, DateTimeOffset> _failedUntil = [];
        private readonly Dictionary<string, Task> _inFlight = [];
        private long _cachedBytes;

        /// <summary>Only tests pass arguments; the default handler refuses non-public addresses at connection time.</summary>
        public BrowserArtworkFetcher(HttpMessageHandler? handler = null, TimeProvider? clock = null, TimeSpan? timeout = null)
        {
            _http = new HttpClient(handler ?? CreateHandler(), disposeHandler: true) { Timeout = Timeout.InfiniteTimeSpan };
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("MosaicShell");
            _clock = clock ?? TimeProvider.System;
            _timeout = timeout ?? DefaultTimeout;
        }

        /// <summary>Raised after an image has been downloaded and cached.</summary>
        public event EventHandler? Fetched;

        /// <summary>The cached image for a URL, or null. Never blocks.</summary>
        public byte[]? TryGet(string url)
        {
            lock (_gate)
            {
                if (!_cache.TryGetValue(url, out (LinkedListNode<string> Node, byte[] Bytes) entry))
                {
                    return null;
                }

                _recency.Remove(entry.Node);
                _recency.AddFirst(entry.Node);
                return entry.Bytes;
            }
        }

        /// <summary>Starts a download unless the URL is refused, cached, backing off or already in flight.</summary>
        public void Request(string url)
        {
            _ = RequestAsync(url);
        }

        /// <summary>As <see cref="Request"/>, completing when the download has finished (or was not needed). Never throws.</summary>
        public Task RequestAsync(string url)
        {
            if (!BrowserArtworkPolicy.IsFetchableUrl(url))
            {
                return Task.CompletedTask;
            }

            lock (_gate)
            {
                if (_cache.ContainsKey(url)
                    || (_failedUntil.TryGetValue(url, out DateTimeOffset until) && _clock.GetUtcNow() < until))
                {
                    return Task.CompletedTask;
                }

                if (_inFlight.TryGetValue(url, out Task? running))
                {
                    return running;
                }

                Task started = Task.Run(() => FetchAsync(url));
                _inFlight[url] = started;
                return started;
            }
        }

        public void Dispose()
        {
            _http.Dispose();
        }

        /// <summary>
        /// Resolves the host and connects only to public addresses. Checking the resolved address (not just the name in
        /// the URL) is what stops a public-looking name, or a redirect, from reaching the user's own network.
        /// </summary>
        internal static async ValueTask<Stream> ConnectPublicAsync(string host, int port, CancellationToken cancellationToken)
        {
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
            IPAddress[] allowed = [.. addresses.Where(BrowserArtworkPolicy.IsPublicAddress)];
            if (allowed.Length == 0)
            {
                throw new HttpRequestException($"'{host}' does not resolve to a public address.");
            }

            Socket socket = new(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(allowed, port, cancellationToken).ConfigureAwait(false);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        private static SocketsHttpHandler CreateHandler()
        {
            return new SocketsHttpHandler
            {
                UseCookies = false,

                // A configured proxy would connect on the Host's behalf and bypass the address check.
                UseProxy = false,
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 3,
                ConnectTimeout = TimeSpan.FromSeconds(5),
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                ConnectCallback = (context, ct) => ConnectPublicAsync(context.DnsEndPoint.Host, context.DnsEndPoint.Port, ct),
            };
        }

        private static async Task<byte[]?> ReadCappedAsync(HttpContent content, CancellationToken cancellationToken)
        {
            await using Stream body = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using MemoryStream buffer = new();
            byte[] chunk = new byte[16 * 1024];
            while (true)
            {
                int read = await body.ReadAsync(chunk, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    return buffer.ToArray();
                }

                if (buffer.Length + read > MaxBytes)
                {
                    return null;
                }

                buffer.Write(chunk, 0, read);
            }
        }

        private async Task FetchAsync(string url)
        {
            byte[]? image = null;
            try
            {
                using CancellationTokenSource timeout = new(_timeout);
                using HttpRequestMessage request = new(HttpMethod.Get, url);
                using HttpResponseMessage response = await _http
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
                if (response.IsSuccessStatusCode && !(response.Content.Headers.ContentLength > MaxBytes))
                {
                    byte[]? body = await ReadCappedAsync(response.Content, timeout.Token).ConfigureAwait(false);
                    image = body is not null && BrowserArtworkPolicy.LooksLikeSupportedImage(body) ? body : null;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException)
            {
                image = null;
            }

            lock (_gate)
            {
                _ = _inFlight.Remove(url);
                if (image is null)
                {
                    RememberFailure(url);
                }
                else
                {
                    Store(url, image);
                }
            }

            if (image is not null)
            {
                Fetched?.Invoke(this, EventArgs.Empty);
            }
        }

        private void RememberFailure(string url)
        {
            DateTimeOffset now = _clock.GetUtcNow();
            if (_failedUntil.Count >= MaxRememberedFailures)
            {
                foreach (string expired in _failedUntil.Where(kv => kv.Value <= now).Select(kv => kv.Key).ToList())
                {
                    _ = _failedUntil.Remove(expired);
                }

                if (_failedUntil.Count >= MaxRememberedFailures)
                {
                    _failedUntil.Clear();
                }
            }

            _failedUntil[url] = now + FailureBackoff;
        }

        private void Store(string url, byte[] image)
        {
            _ = _failedUntil.Remove(url);
            _cache[url] = (_recency.AddFirst(url), image);
            _cachedBytes += image.Length;

            while (_recency.Last is { } oldest && (_cache.Count > MaxCachedEntries || _cachedBytes > MaxCachedBytes))
            {
                _cachedBytes -= _cache[oldest.Value].Bytes.Length;
                _ = _cache.Remove(oldest.Value);
                _recency.RemoveLast();
            }
        }
    }
}
