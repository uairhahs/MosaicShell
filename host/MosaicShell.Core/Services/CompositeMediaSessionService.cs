using MosaicShell.Core.Services.WebNowPlaying;

namespace MosaicShell.Core.Services;

/// <summary>
/// Merges Windows SMTC with WebNowPlaying. SMTC drives transport when present;
/// WNP supplies album art for browser players (YouTube Music) where SMTC Thumbnail is null.
/// </summary>
public sealed class CompositeMediaSessionService : IMediaSessionService
{
    private readonly IMediaSessionService _smtc;
    private readonly IWebNowPlayingService _wnp;
    private MediaSessionInfo? _current;

    public CompositeMediaSessionService(IMediaSessionService smtc, IWebNowPlayingService wnp)
    {
        _smtc = smtc;
        _wnp = wnp;
        _smtc.Changed += OnSourceChanged;
        _smtc.ProgressChanged += OnSmtcProgress;
        _wnp.Changed += OnSourceChanged;
        Rebuild(raiseProgress: false);
    }

    public MediaSessionInfo? Current => _current;
    public event EventHandler? Changed;
    public event EventHandler? ProgressChanged;

    public void PumpTimeline()
    {
        _smtc.PumpTimeline();
        // WNP position updates arrive via Changed; still refresh merge in case only SMTC moved
        Rebuild(raiseProgress: true);
    }

    public Task PlayPauseAsync() => _smtc.PlayPauseAsync();
    public Task NextAsync() => _smtc.NextAsync();
    public Task PreviousAsync() => _smtc.PreviousAsync();
    public Task SeekAsync(double positionSeconds) => _smtc.SeekAsync(positionSeconds);
    public async Task ToggleShuffleAsync()
    {
        await _smtc.ToggleShuffleAsync();
        if (_wnp is WebNowPlaying.WebNowPlayingReduxHost host)
            await host.TryToggleShuffleAsync();
    }
    public async Task ToggleRepeatAsync()
    {
        await _smtc.ToggleRepeatAsync();
        if (_wnp is WebNowPlaying.WebNowPlayingReduxHost host)
            await host.TryToggleRepeatAsync();
    }
    public async Task ToggleLikeAsync()
    {
        await _smtc.ToggleLikeAsync();
        if (_wnp is WebNowPlaying.WebNowPlayingReduxHost host)
            await host.TryToggleLikeAsync();
    }

    public void Dispose()
    {
        _smtc.Changed -= OnSourceChanged;
        _smtc.ProgressChanged -= OnSmtcProgress;
        _wnp.Changed -= OnSourceChanged;
        _smtc.Dispose();
        _wnp.Dispose();
    }

    private void OnSourceChanged(object? sender, EventArgs e) => Rebuild(raiseProgress: false);

    private void OnSmtcProgress(object? sender, EventArgs e) => Rebuild(raiseProgress: true);

    private void Rebuild(bool raiseProgress)
    {
        var next = Merge(_smtc.Current, _wnp.Active);
        var prev = _current;
        _current = next;

        if (prev is null && next is null) return;

        if (raiseProgress)
        {
            if (prev is not null && next is not null
                && string.Equals(prev.Title, next.Title, StringComparison.Ordinal)
                && string.Equals(prev.Artist, next.Artist, StringComparison.Ordinal)
                && ThumbEqual(prev.ThumbnailPng, next.ThumbnailPng)
                && prev.IsPlaying == next.IsPlaying
                && (Math.Abs(prev.PositionSeconds - next.PositionSeconds) >= 0.05
                    || Math.Abs(prev.DurationSeconds - next.DurationSeconds) >= 0.5))
            {
                ProgressChanged?.Invoke(this, EventArgs.Empty);
                return;
            }
        }

        if (!SessionEqual(prev, next))
            Changed?.Invoke(this, EventArgs.Empty);
        else if (raiseProgress && prev is not null && next is not null
                 && Math.Abs(prev.PositionSeconds - next.PositionSeconds) >= 0.05)
            ProgressChanged?.Invoke(this, EventArgs.Empty);
    }

    internal static MediaSessionInfo? Merge(MediaSessionInfo? smtc, WnpPlayerSnapshot? wnp)
    {
        if (smtc is null && wnp is null) return null;

        if (smtc is null)
        {
            return new MediaSessionInfo(
                Title: NullIfEmpty(wnp!.Title),
                Artist: NullIfEmpty(wnp.Artist),
                AppId: NullIfEmpty(wnp.Name) ?? "WebNowPlaying",
                IsPlaying: wnp.IsPlaying,
                ThumbnailPng: wnp.CoverPng,
                PositionSeconds: wnp.PositionSeconds,
                DurationSeconds: wnp.DurationSeconds);
        }

        // Prefer any WNP cover when SMTC has none (YTM PWA / browser)
        var thumb = smtc.ThumbnailPng;
        if ((thumb is null || thumb.Length < 32) && wnp?.CoverPng is { Length: > 32 })
            thumb = wnp.CoverPng;
        if ((thumb is null || thumb.Length < 32)
            && WebNowPlaying.WebNowPlayingReduxHost.TryGetCachedCover(smtc.Title, out var cached)
            && cached is { Length: > 32 })
            thumb = cached;
        if ((thumb is null || thumb.Length < 32)
            && wnp is not null
            && WebNowPlaying.WebNowPlayingReduxHost.TryGetCachedCover(wnp.Title, out var cached2)
            && cached2 is { Length: > 32 })
            thumb = cached2;

        // Always take WNP metadata for browser-looking sessions; also when titles match
        var title = smtc.Title;
        var artist = smtc.Artist;
        if (wnp is not null && !string.IsNullOrWhiteSpace(wnp.Title)
            && (LooksLikeBrowserSession(smtc.AppId)
                || TitlesLooselyMatch(smtc.Title, wnp.Title)))
        {
            title = wnp.Title;
            if (!string.IsNullOrWhiteSpace(wnp.Artist))
                artist = wnp.Artist;
            if ((thumb is null || thumb.Length < 32) && wnp.CoverPng is { Length: > 32 })
                thumb = wnp.CoverPng;
        }

        // Prefer WNP timeline when SMTC duration is missing / sticky
        var pos = smtc.PositionSeconds;
        var dur = smtc.DurationSeconds;
        if (wnp is not null && wnp.DurationSeconds > 0
            && (dur <= 0.5 || LooksLikeBrowserSession(smtc.AppId)))
        {
            pos = wnp.PositionSeconds;
            dur = wnp.DurationSeconds;
        }

        return smtc with
        {
            Title = title,
            Artist = artist,
            ThumbnailPng = thumb,
            PositionSeconds = pos,
            DurationSeconds = dur,
        };
    }

    private static bool LooksLikeBrowserSession(string? appId)
    {
        if (string.IsNullOrEmpty(appId)) return false;
        return appId.Contains("youtube", StringComparison.OrdinalIgnoreCase)
               || appId.Contains("chrome", StringComparison.OrdinalIgnoreCase)
               || appId.Contains("msedge", StringComparison.OrdinalIgnoreCase)
               || appId.Contains("firefox", StringComparison.OrdinalIgnoreCase)
               || appId.Contains("music.youtube", StringComparison.OrdinalIgnoreCase)
               || appId.Contains("brave", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TitlesLooselyMatch(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
        static string Norm(string s)
        {
            var i = s.IndexOf('|');
            if (i > 0) s = s[..i];
            return s.Trim();
        }
        return string.Equals(Norm(a), Norm(b), StringComparison.OrdinalIgnoreCase);
    }

    private static string? NullIfEmpty(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;

    private static bool ThumbEqual(byte[]? a, byte[]? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a.Length != b.Length) return false;
        return a.AsSpan().SequenceEqual(b);
    }

    private static bool SessionEqual(MediaSessionInfo? a, MediaSessionInfo? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return string.Equals(a.Title, b.Title, StringComparison.Ordinal)
               && string.Equals(a.Artist, b.Artist, StringComparison.Ordinal)
               && string.Equals(a.AppId, b.AppId, StringComparison.Ordinal)
               && a.IsPlaying == b.IsPlaying
               && ThumbEqual(a.ThumbnailPng, b.ThumbnailPng)
               && Math.Abs(a.DurationSeconds - b.DurationSeconds) < 0.5;
    }
}
