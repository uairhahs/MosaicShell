using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace MosaicShell.Core.Services.WebNowPlaying;

/// <summary>
/// WebNowPlaying adapter (WNPLIB rev 3). Player ids from the extension are often
/// larger than Int32 (chrome port timestamps). Binary covers use JS <c>setUint32</c>
/// truncation - we match covers with <c>(uint)id</c>, not Int32 parse.
/// </summary>
public sealed class WebNowPlayingReduxHost : IWebNowPlayingService
{
    public const int DefaultPort = 5468;
    public const string AdapterVersion = "3.0.0";

    private static readonly HttpClient Http = CreateHttp();
    private static readonly ConcurrentDictionary<string, byte[]> CoversByTitle = new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<long, MutablePlayer> _players = new();
    private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();
    private readonly ConcurrentDictionary<uint, byte[]> _pendingCovers = new(); // JS uint32 key
    private readonly ConcurrentQueue<string> _trace = new();
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private int _clients;
    private WnpPlayerSnapshot? _active;
    private string _diagDir = "";

    public WebNowPlayingReduxHost(int port = DefaultPort) => ListenPort = port;

    public int ListenPort { get; }
    public int ConnectedClients => Volatile.Read(ref _clients);
    public WnpPlayerSnapshot? Active => _active;
    public bool IsListening => _listener?.IsListening == true;
    public event EventHandler? Changed;

    public static bool TryGetCachedCover(string? title, out byte[]? png)
    {
        png = null;
        if (string.IsNullOrWhiteSpace(title)) return false;
        if (CoversByTitle.TryGetValue(NormTitle(title), out var bytes) && bytes.Length > 32)
        {
            png = bytes;
            return true;
        }
        return false;
    }

    public void Start()
    {
        if (_listener is not null) return;
        _diagDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MosaicShell", "wnp");
        try { Directory.CreateDirectory(_diagDir); } catch { /* ignore */ }

        _cts = new CancellationTokenSource();
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{ListenPort}/");
        try { _listener.Start(); }
        catch (Exception ex)
        {
            Trace($"listen FAILED on {ListenPort}: {ex.Message}");
            WriteStatusFile();
            _listener = null;
            return;
        }

        _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
        Trace($"listening ws://127.0.0.1:{ListenPort}/ (CLI adapter)");
        WriteStatusFile();
    }

    public async Task WaitUntilListeningAsync(TimeSpan? timeout = null)
    {
        var until = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(3));
        while (DateTime.UtcNow < until)
        {
            if (IsListening) return;
            await Task.Delay(20);
        }
        throw new TimeoutException($"WNP host did not listen on {ListenPort}");
    }

    public void Dispose()
    {
        try { _cts?.Cancel(); } catch { /* ignore */ }
        try { _listener?.Stop(); _listener?.Close(); } catch { /* ignore */ }
        _listener = null;
        foreach (var s in _sockets.Values)
        {
            try { s.Abort(); } catch { /* ignore */ }
        }
        _sockets.Clear();
        _players.Clear();
        _active = null;
    }

    private static HttpClient CreateHttp()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
        http.DefaultRequestHeaders.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
        http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "image/avif,image/webp,image/apng,image/*,*/*;q=0.8");
        return http;
    }

    /// <summary>Same truncation as JS <c>DataView.setUint32(0, id, true)</c>.</summary>
    internal static uint ToJsUint32(long id) => unchecked((uint)id);

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        var listener = _listener;
        if (listener is null) return;
        while (!ct.IsCancellationRequested && listener.IsListening)
        {
            try
            {
                var ctx = await listener.GetContextAsync().WaitAsync(ct);
                if (ctx.Request.IsWebSocketRequest)
                {
                    _ = Task.Run(() => HandleClientAsync(ctx, ct), ct);
                    continue;
                }
                await HandleHttpAsync(ctx);
            }
            catch (OperationCanceledException) { break; }
            catch (HttpListenerException) { break; }
            catch (Exception ex) { Trace($"accept: {ex.Message}"); }
        }
    }

    private async Task HandleHttpAsync(HttpListenerContext ctx)
    {
        try
        {
            var path = ctx.Request.Url?.AbsolutePath ?? "";
            if (path.Equals("/wnp/status", StringComparison.OrdinalIgnoreCase)
                || path.Equals("/status", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = Encoding.UTF8.GetBytes(BuildStatusJson());
                ctx.Response.ContentType = "application/json; charset=utf-8";
                ctx.Response.ContentLength64 = bytes.Length;
                ctx.Response.StatusCode = 200;
                await ctx.Response.OutputStream.WriteAsync(bytes);
            }
            else if (path.Equals("/wnp/cover", StringComparison.OrdinalIgnoreCase))
            {
                var cover = _active?.CoverPng;
                if (cover is null || cover.Length < 32) ctx.Response.StatusCode = 404;
                else
                {
                    ctx.Response.ContentType = LooksLikePng(cover) ? "image/png" : "application/octet-stream";
                    ctx.Response.ContentLength64 = cover.Length;
                    ctx.Response.StatusCode = 200;
                    await ctx.Response.OutputStream.WriteAsync(cover);
                }
            }
            else ctx.Response.StatusCode = 404;
        }
        catch { /* ignore */ }
        finally
        {
            try { ctx.Response.Close(); } catch { /* ignore */ }
        }
    }

    private async Task HandleClientAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        WebSocket? ws = null;
        var clientId = Guid.NewGuid().ToString("N");
        try
        {
            var wsCtx = await ctx.AcceptWebSocketAsync(null);
            ws = wsCtx.WebSocket;
            _sockets[clientId] = ws;
            Interlocked.Increment(ref _clients);
            Trace($"client +{clientId[..8]} clients={_clients}");

            var hello = Encoding.UTF8.GetBytes($"ADAPTER_VERSION {AdapterVersion};WNPLIB_REVISION 3");
            await ws.SendAsync(hello, WebSocketMessageType.Text, true, ct);

            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var (type, payload) = await ReceiveFullAsync(ws, ct);
                if (type == WebSocketMessageType.Close) break;
                if (type == WebSocketMessageType.Binary) OnBinary(payload);
                else if (type == WebSocketMessageType.Text) OnText(Encoding.UTF8.GetString(payload));
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Trace($"client err: {ex.Message}"); }
        finally
        {
            _sockets.TryRemove(clientId, out _);
            Interlocked.Decrement(ref _clients);
            try { ws?.Dispose(); } catch { }
            Trace($"client -{clientId[..8]} clients={_clients}");
            Publish();
        }
    }

    private static async Task<(WebSocketMessageType Type, byte[] Payload)> ReceiveFullAsync(
        WebSocket ws, CancellationToken ct)
    {
        var buffer = new byte[256 * 1024];
        using var ms = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await ws.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close)
                return (WebSocketMessageType.Close, Array.Empty<byte>());
            ms.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);
        return (result.MessageType, ms.ToArray());
    }

    private void OnBinary(byte[] payload)
    {
        if (payload.Length < 4) return;
        var trunc = BitConverter.ToUInt32(payload, 0);
        var img = new byte[payload.Length - 4];
        Buffer.BlockCopy(payload, 4, img, 0, img.Length);
        if (img.Length < 32) return;

        Trace($"binary cover uint32={trunc} len={img.Length} magic={Magic(img)}");
        SaveCoverFile(trunc, img);

        var player = FindByUint32(trunc);
        if (player is not null)
        {
            player.CoverPng = img;
            CacheCover(player.Title, img);
            Publish();
            return;
        }

        _pendingCovers[trunc] = img;
    }

    private void OnText(string message)
    {
        if (message.Length > 200) Trace($"text {message.Length}b: {message[..200]}…");
        else Trace($"text: {message}");

        var sp = message.IndexOf(' ');
        if (sp <= 0) return;
        if (!int.TryParse(message.AsSpan(0, sp), out var msgType)) return;
        var rest = message[(sp + 1)..];

        switch (msgType)
        {
            case 0:
            case 1:
            {
                var sp2 = rest.IndexOf(' ');
                if (sp2 <= 0) return;
                if (!long.TryParse(rest.AsSpan(0, sp2), out var portId)) return;
                UpsertPlayer(portId, rest[(sp2 + 1)..], isAdd: msgType == 0);
                break;
            }
            case 2:
            {
                if (!long.TryParse(rest, out var portId)) return;
                if (_players.TryRemove(portId, out _)) Publish();
                break;
            }
        }
    }

    private void UpsertPlayer(long portId, string fieldBlob, bool isAdd)
    {
        var tokens = SplitFields(fieldBlob);
        var player = _players.GetOrAdd(portId, id => new MutablePlayer { PortId = id });
        ApplyFields(player, tokens);

        var trunc = ToJsUint32(portId);
        if (_pendingCovers.TryRemove(trunc, out var pending))
        {
            player.CoverPng = pending;
            CacheCover(player.Title, pending);
            Trace($"attached pending cover uint32={trunc} to id={portId}");
        }

        if ((player.CoverPng is null || player.CoverPng.Length < 32 || !LooksLikeImage(player.CoverPng))
            && !string.IsNullOrWhiteSpace(player.CoverSrc))
            _ = TryFetchCoverSrcAsync(player);

        Trace($"{(isAdd ? "ADD" : "UPD")} id={portId} u32={trunc} '{player.Title}' cover={(player.CoverPng?.Length ?? 0)} src={(player.CoverSrc.Length > 0)}");
        Publish();
    }

    private MutablePlayer? FindByUint32(uint trunc)
    {
        foreach (var kv in _players)
        {
            if (ToJsUint32(kv.Key) == trunc)
                return kv.Value;
        }
        return null;
    }

    private async Task TryFetchCoverSrcAsync(MutablePlayer player)
    {
        var src = player.CoverSrc;
        if (string.IsNullOrWhiteSpace(src)) return;
        try
        {
            var bytes = await DownloadCoverAsync(src);
            if (bytes is null || bytes.Length < 32) return;
            if (!_players.TryGetValue(player.PortId, out var live)) return;
            if (live.CoverPng is { Length: > 32 } && LooksLikeImage(live.CoverPng)) return;
            live.CoverPng = bytes;
            CacheCover(live.Title, bytes);
            SaveCoverFile(ToJsUint32(live.PortId), bytes);
            Trace($"fetched cover-src {bytes.Length}b for '{live.Title}'");
            Publish();
        }
        catch (Exception ex) { Trace($"cover-src fail: {ex.Message}"); }
    }

    private static async Task<byte[]?> DownloadCoverAsync(string src)
    {
        if (src.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            var path = new Uri(src).LocalPath;
            return File.Exists(path) ? await File.ReadAllBytesAsync(path) : null;
        }
        if (src.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || src.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var url in CoverUrlCandidates(src))
            {
                try
                {
                    var bytes = await Http.GetByteArrayAsync(url);
                    if (bytes.Length >= 32 && LooksLikeImage(bytes)) return bytes;
                }
                catch { }
            }
            return null;
        }
        return File.Exists(src) ? await File.ReadAllBytesAsync(src) : null;
    }

    private static IEnumerable<string> CoverUrlCandidates(string src)
    {
        yield return src;
        if (src.Contains("googleusercontent", StringComparison.OrdinalIgnoreCase)
            && !src.Contains("=w", StringComparison.OrdinalIgnoreCase))
        {
            yield return src + "=w544-h544-l90-rj";
            yield return src + "=s512-c";
        }
    }

    private void Publish()
    {
        _active = PickActive();
        WriteStatusFile();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private WnpPlayerSnapshot? PickActive()
    {
        var list = _players.Values.Select(p => p.ToSnapshot()).ToList();
        if (list.Count == 0) return null;
        var withMedia = list
            .Where(p => !string.IsNullOrEmpty(p.Title) || p.CoverPng is { Length: > 0 })
            .ToList();
        if (withMedia.Count == 0) return list.FirstOrDefault();

        return withMedia
            .Where(p => p.IsPlaying)
            .OrderByDescending(p => p.CoverPng?.Length ?? 0)
            .ThenByDescending(p => p.ActiveAt)
            .FirstOrDefault()
            ?? withMedia
                .OrderByDescending(p => p.CoverPng?.Length ?? 0)
                .ThenByDescending(p => p.ActiveAt)
                .FirstOrDefault();
    }

    private void CacheCover(string? title, byte[] img)
    {
        if (string.IsNullOrWhiteSpace(title) || img.Length < 32) return;
        CoversByTitle[NormTitle(title)] = img;
    }

    private void SaveCoverFile(uint id, byte[] img)
    {
        if (string.IsNullOrEmpty(_diagDir)) return;
        try
        {
            var ext = LooksLikePng(img) ? "png" : LooksLikeJpeg(img) ? "jpg" : "bin";
            File.WriteAllBytes(Path.Combine(_diagDir, $"cover-{id}.{ext}"), img);
        }
        catch { }
    }

    private void Trace(string line)
    {
        var msg = $"{DateTime.Now:HH:mm:ss.fff} {line}";
        System.Diagnostics.Debug.WriteLine("[WNP] " + line);
        _trace.Enqueue(msg);
        while (_trace.Count > 80) _trace.TryDequeue(out _);
        if (string.IsNullOrEmpty(_diagDir)) return;
        try { File.AppendAllText(Path.Combine(_diagDir, "trace.log"), msg + Environment.NewLine); }
        catch { }
    }

    private void WriteStatusFile()
    {
        if (string.IsNullOrEmpty(_diagDir)) return;
        try { File.WriteAllText(Path.Combine(_diagDir, "status.json"), BuildStatusJson()); }
        catch { }
    }

    private string BuildStatusJson()
    {
        var players = _players.Values.Select(p => new
        {
            p.PortId,
            u32 = ToJsUint32(p.PortId),
            p.Name,
            p.Title,
            p.Artist,
            state = p.State.ToString(),
            coverBytes = p.CoverPng?.Length ?? 0,
            coverSrc = string.IsNullOrEmpty(p.CoverSrc) ? null : p.CoverSrc[..Math.Min(80, p.CoverSrc.Length)],
            coverMagic = p.CoverPng is null ? null : Magic(p.CoverPng),
        }).ToList();

        return JsonSerializer.Serialize(new
        {
            listenPort = ListenPort,
            listening = IsListening,
            clients = ConnectedClients,
            activeTitle = _active?.Title,
            activeCoverBytes = _active?.CoverPng?.Length ?? 0,
            cachedTitles = CoversByTitle.Count,
            players,
            trace = _trace.Reverse().Take(25).ToArray(),
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    private int _eventSeq;
    private bool _likedOptimistic;

    public async Task TryToggleShuffleAsync()
    {
        var p = ActivePlayer();
        if (p is null) return;
        var next = p.Shuffle ? 0 : 1;
        p.Shuffle = next != 0;
        await SendEventAsync(p.PortId, eventType: 7 /* SET_SHUFFLE */, data: next);
    }

    public async Task TryToggleRepeatAsync()
    {
        var p = ActivePlayer();
        if (p is null) return;
        // WNP_REPEAT_NONE=1, ALL=2, ONE=4 - cycle NONE → ALL → ONE → NONE
        var next = p.Repeat switch
        {
            2 => 4,
            4 => 1,
            _ => 2
        };
        p.Repeat = next;
        await SendEventAsync(p.PortId, eventType: 6 /* SET_REPEAT */, data: next);
    }

    public async Task TryToggleLikeAsync()
    {
        var p = ActivePlayer();
        if (p is null) return;
        var liked = p.Rating >= 5;
        var next = liked ? 0 : 5;
        p.Rating = next;
        _likedOptimistic = next >= 5;
        await SendEventAsync(p.PortId, eventType: 5 /* SET_RATING */, data: next);
    }

    private MutablePlayer? ActivePlayer()
    {
        var snap = Active;
        if (snap is null) return null;
        return _players.TryGetValue(snap.PortId, out var p) ? p : null;
    }

    private async Task SendEventAsync(long portId, int eventType, int data)
    {
        var eventId = Interlocked.Increment(ref _eventSeq) & 0x1FF;
        // WNPLIB web: "{portId} {eventId} {event} {data}"
        var msg = $"{portId} {eventId} {eventType} {data}";
        var bytes = Encoding.UTF8.GetBytes(msg);
        Trace($"event → {msg}");
        foreach (var kv in _sockets)
        {
            var ws = kv.Value;
            if (ws.State != WebSocketState.Open) continue;
            try
            {
                await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex) { Trace($"event send fail: {ex.Message}"); }
        }
    }

    internal static List<string> SplitFields(string blob)
    {
        var list = new List<string>(26);
        var sb = new StringBuilder();
        for (var i = 0; i < blob.Length; i++)
        {
            var c = blob[i];
            if (c == '\\' && i + 1 < blob.Length && blob[i + 1] == '|')
            {
                sb.Append('|');
                i++;
                continue;
            }
            if (c == '|')
            {
                list.Add(NormalizeToken(sb.ToString()));
                sb.Clear();
                continue;
            }
            sb.Append(c);
        }
        if (sb.Length > 0) list.Add(NormalizeToken(sb.ToString()));
        return list;
    }

    private static string NormalizeToken(string t) =>
        t.Length == 1 && t[0] == '\u0001' ? "" : t;

    private static void ApplyFields(MutablePlayer p, IReadOnlyList<string> t)
    {
        Set(t, 1, v => p.Name = v);
        Set(t, 2, v => p.Title = v);
        Set(t, 3, v => p.Artist = v);
        Set(t, 4, v => p.Album = v);
        Set(t, 5, v => p.CoverSrc = v);
        SetInt(t, 6, v => p.State = (WnpState)v);
        SetInt(t, 7, v => p.PositionSeconds = v);
        SetInt(t, 8, v => p.DurationSeconds = v);
        SetInt(t, 9, v => p.Volume = v);
        SetInt(t, 10, v => p.Rating = v);
        SetInt(t, 11, v => p.Repeat = v);
        SetInt(t, 12, v => p.Shuffle = v != 0);
        SetUlong(t, 25, v => p.ActiveAt = v);
    }

    private static void Set(IReadOnlyList<string> t, int i, Action<string> apply)
    {
        if (i < t.Count && t[i].Length > 0) apply(t[i]);
    }

    private static void SetInt(IReadOnlyList<string> t, int i, Action<int> apply)
    {
        if (i >= t.Count || t[i].Length == 0) return;
        if (double.TryParse(t[i], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var d))
            apply((int)d);
    }

    private static void SetUlong(IReadOnlyList<string> t, int i, Action<ulong> apply)
    {
        if (i < t.Count && t[i].Length > 0 && ulong.TryParse(t[i], out var v)) apply(v);
    }

    private static string NormTitle(string title)
    {
        var i = title.IndexOf('|');
        if (i > 0) title = title[..i];
        return title.Trim();
    }

    private static bool LooksLikeImage(byte[] b) => LooksLikePng(b) || LooksLikeJpeg(b) || LooksLikeWebp(b);
    private static bool LooksLikePng(byte[] b) => b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47;
    private static bool LooksLikeJpeg(byte[] b) => b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF;
    private static bool LooksLikeWebp(byte[] b) =>
        b.Length > 12 && b[0] == (byte)'R' && b[1] == (byte)'I' && b[2] == (byte)'F' && b[3] == (byte)'F';

    private static string Magic(byte[] b)
    {
        if (LooksLikePng(b)) return "png";
        if (LooksLikeJpeg(b)) return "jpg";
        if (LooksLikeWebp(b)) return "webp";
        return BitConverter.ToString(b, 0, Math.Min(4, b.Length));
    }

    private sealed class MutablePlayer
    {
        public long PortId { get; init; }
        public string Name { get; set; } = "";
        public string Title { get; set; } = "";
        public string Artist { get; set; } = "";
        public string Album { get; set; } = "";
        public string CoverSrc { get; set; } = "";
        public WnpState State { get; set; } = WnpState.Stopped;
        public int PositionSeconds { get; set; }
        public int DurationSeconds { get; set; }
        public int Volume { get; set; } = 100;
        public int Rating { get; set; }
        public int Repeat { get; set; } = 1;
        public bool Shuffle { get; set; }
        public ulong ActiveAt { get; set; }
        public byte[]? CoverPng { get; set; }

        public WnpPlayerSnapshot ToSnapshot() => new()
        {
            PortId = PortId,
            Name = Name,
            Title = Title,
            Artist = Artist,
            Album = Album,
            CoverSrc = CoverSrc,
            State = State,
            PositionSeconds = PositionSeconds,
            DurationSeconds = DurationSeconds,
            Volume = Volume,
            ActiveAt = ActiveAt,
            CoverPng = CoverPng,
            Rating = Rating,
            Repeat = Repeat,
            Shuffle = Shuffle
        };
    }
}
