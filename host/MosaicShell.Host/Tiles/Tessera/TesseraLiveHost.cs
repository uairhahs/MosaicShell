using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
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
    public TesseraTrack? MediaScrub { get; set; }
    public TextBlock? MediaPos { get; set; }
    public TextBlock? MediaDur { get; set; }
    public Border? MediaArt { get; set; }
    public TextBlock? MediaTitle { get; set; }
    public TextBlock? MediaArtist { get; set; }
    public MaterialIcon? PlayPauseIcon { get; set; }
}

/// <summary>Root wrapper - live pump updates Bindings directly (volume / scrub / art).</summary>
public sealed class TesseraLiveHost : ContentControl
{
    public TesseraLiveBindings Bindings { get; } = new();

    public void ApplyLive(HostServices services, FlyoutRequest request)
    {
        var b = Bindings;
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
                if (muted) b.Percent.Text = "Mute";
                else if (b.SlashMeter is not null)
                    b.Percent.Text = $"Speakers: {pct}%";
                else if (b.VolumeRing is not null)
                    b.Percent.Text = $"{pct}%";
                else
                    b.Percent.Text = $"{pct}";
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
                b.Percent.Text = $"{VolumePercent.ToPercent(shown)}";
            }
        }

        if (b.MediaTitle is not null)
            b.MediaTitle.Text = string.IsNullOrWhiteSpace(media?.Title) ? " " : media!.Title!;
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
            if (b.MediaPos is not null) b.MediaPos.Text = FormatTime(pos);
            if (b.MediaDur is not null) b.MediaDur.Text = FormatTime(dur);
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
