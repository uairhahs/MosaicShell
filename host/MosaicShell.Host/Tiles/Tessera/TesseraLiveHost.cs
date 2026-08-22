using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material.Icons;
using Material.Icons.Avalonia;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.WebNowPlaying;

namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>
/// Holds strong refs to dynamic Tessera controls so live updates do not depend on visual-tree Name search.
/// </summary>
public sealed class TesseraLiveBindings
{
    public TesseraTrack? VolumeTrack { get; set; }
    public TesseraRingVolume? VolumeRing { get; set; }
    public TextBlock? Percent { get; set; }
    public TextBlock? SlashMeter { get; set; }
    public MaterialIcon? Glyph { get; set; }
    /// <summary>Pixel M3: two-tone inset volume icon (on-primary / on-secondary-container).</summary>
    public bool PixelVolumeGlyph { get; set; }
    public TesseraTrack? MediaScrub { get; set; }
    public TextBlock? MediaPos { get; set; }
    public TextBlock? MediaDur { get; set; }
    public Border? MediaArt { get; set; }
    public TextBlock? MediaTitle { get; set; }
    public TextBlock? MediaArtist { get; set; }
    public MaterialIcon? PlayPauseIcon { get; set; }
    public TextBlock? StatusLabel { get; set; }
    /// <summary>Plainext: title uses {@code Title > Playing &lt;} and progress uses slash meter.</summary>
    public bool PlainextMedia { get; set; }
    /// <summary>Hide percent at rest; show while dragging or wheeling (M3-style value indicator).</summary>
    public bool PercentOnAdjustOnly { get; set; }
}

/// <summary>Root wrapper - live pump updates Bindings directly (volume / scrub / art).</summary>
public sealed class TesseraLiveHost : ContentControl
{
    public TesseraLiveBindings Bindings { get; } = new();

    /// <summary>Find live host when flyout root is wrapped (e.g. LayoutTransformControl scale).</summary>
    public static TesseraLiveHost? FindIn(Control? root)
    {
        if (root is TesseraLiveHost host) return host;
        if (root is LayoutTransformControl { Child: Control child }) return FindIn(child);
        if (root is ContentControl { Content: Control content }) return FindIn(content);
        if (root is Visual visual)
        {
            foreach (var v in visual.GetVisualChildren())
            {
                if (v is Control c)
                {
                    var found = FindIn(c);
                    if (found is not null) return found;
                }
            }
        }
        return null;
    }

    public void ApplyLive(HostServices services, FlyoutRequest request)
    {
        var b = Bindings;
        if (request.Kind.Equals("locks", StringComparison.OrdinalIgnoreCase)
            || request.Kind.Equals("flight", StringComparison.OrdinalIgnoreCase))
        {
            var live = TesseraFlyoutRequestBuilder.RefreshStatusPayload(
                services, request.Kind, request.Payload);
            request = request with { Payload = live };
            if (b.StatusLabel is not null)
                b.StatusLabel.Text = TesseraStatusLabels.Format(request);
            return;
        }

        var media = services.Media.Current;
        var isBright = request.Kind.Equals("bright", StringComparison.OrdinalIgnoreCase);

        if (!isBright && (b.VolumeTrack is not null || b.VolumeRing is not null))
        {
            var vol = services.Audio.MasterVolume;
            var muted = services.Audio.IsMuted;
            var adjusting = b.VolumeTrack?.IsUserAdjusting == true || b.VolumeRing?.IsUserAdjusting == true;
            if (b.VolumeTrack is not null && !b.VolumeTrack.IsUserAdjusting)
                b.VolumeTrack.SetValueSilent(vol);
            if (b.VolumeRing is not null && !b.VolumeRing.IsUserAdjusting)
                b.VolumeRing.SetValueSilent(vol);

            var shown = adjusting
                ? (b.VolumeRing?.Value ?? b.VolumeTrack?.Value ?? vol)
                : vol;
            if (b.Percent is not null)
            {
                var pct = VolumePercent.ToPercent(shown);
                b.Percent.Text = TesseraVolumeLabel.Volume(muted, pct, b.SlashMeter is not null);
                if (b.PercentOnAdjustOnly)
                    b.Percent.IsVisible = adjusting;
            }
            if (b.SlashMeter is not null)
                b.SlashMeter.Text = TesseraChrome.SlashFill(shown);
            if (b.Glyph is not null)
            {
                b.Glyph.Kind = muted || shown <= 0.001
                    ? MaterialIconKind.VolumeOff
                    : shown < 0.20 ? MaterialIconKind.VolumeLow
                    : shown < 0.50 ? MaterialIconKind.VolumeMedium
                    : MaterialIconKind.VolumeHigh;
                if (b.PixelVolumeGlyph)
                {
                    var trackH = b.VolumeTrack?.Bounds.Height ?? 0;
                    TesseraPixelM3.ApplyVolumeGlyphTone(b.Glyph, shown, muted, trackH);
                }
            }
        }
        else if (isBright && b.VolumeTrack is not null)
        {
            var br = services.Brightness.IsSupported ? services.Brightness.Brightness : 0.5;
            if (!b.VolumeTrack.IsUserAdjusting)
                b.VolumeTrack.SetValueSilent(br);
            if (b.Percent is not null)
            {
                var shown = b.VolumeTrack.IsUserAdjusting ? b.VolumeTrack.Value : br;
                b.Percent.Text = TesseraVolumeLabel.Brightness(VolumePercent.ToPercent(shown), b.PlainextMedia);
            }
            if (b.SlashMeter is not null)
                b.SlashMeter.Text = TesseraChrome.SlashFill(br);
        }

        if (b.MediaTitle is not null)
        {
            if (b.PlainextMedia && media is not null)
            {
                var title = string.IsNullOrWhiteSpace(media.Title) ? " " : media.Title!;
                var state = media.IsPlaying ? "Playing" : "Paused";
                b.MediaTitle.Text = $"{title} > {state} <";
            }
            else
                b.MediaTitle.Text = string.IsNullOrWhiteSpace(media?.Title) ? " " : media!.Title!;
        }
        if (b.MediaArtist is not null)
            b.MediaArtist.Text = string.IsNullOrWhiteSpace(media?.Artist) ? " " : media!.Artist!;

        if (b.MediaArt is not null)
        {
            var thumb = TesseraLiveHost.ResolveThumbnail(media?.ThumbnailPng, media?.Title ?? b.MediaTitle?.Text);
            var fillHost = double.IsNaN(b.MediaArt.Width) || b.MediaArt.Width <= 1.0;
            TesseraMediaPanel.ApplyArtToBorder(b.MediaArt, thumb, fillHost);
        }

        if (b.PlayPauseIcon is not null)
            b.PlayPauseIcon.Kind = media?.IsPlaying == true ? MaterialIconKind.Pause : MaterialIconKind.Play;

        if (media is not null)
        {
            var dur = media.DurationSeconds;
            var pos = media.PositionSeconds;
            var progress = dur > 0.5 ? Math.Clamp(pos / dur, 0, 1) : 0;
            b.MediaScrub?.SetValueSilent(progress);
            if (b.MediaPos is not null)
            {
                if (b.PlainextMedia)
                    b.MediaPos.Text =
                        $"{FormatTime(pos)} {TesseraChrome.SlashFill(progress, 16)} {FormatTime(dur)}";
                else
                    b.MediaPos.Text = b.MediaDur is not null
                        ? FormatTime(pos)
                        : $"{FormatTime(pos)} / {FormatTime(dur)}";
            }
            if (b.MediaDur is not null)
                b.MediaDur.Text = FormatTime(dur);
        }
    }

    private static string FormatTime(double seconds)
    {
        if (seconds <= 0 || double.IsNaN(seconds)) return "0:00";
        var t = TimeSpan.FromSeconds(seconds);
        return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");
    }

    public static byte[]? ResolveThumbnail(byte[]? smtcOrMerged, string? title)
    {
        if (smtcOrMerged is { Length: >= 32 }) return smtcOrMerged;
        if (WebNowPlayingReduxHost.TryGetCachedCover(title, out var png) && png is { Length: >= 32 })
            return png;
        return null;
    }
}
