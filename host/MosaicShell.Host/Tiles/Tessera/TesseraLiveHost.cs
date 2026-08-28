using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Material.Icons;
using Material.Icons.Avalonia;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.WebNowPlaying;

namespace MosaicShell.Host.Tiles.Tessera
{
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
        public bool MaterialYouVolumeGlyph { get; set; }
        public TesseraTrack? MediaScrub { get; set; }
        public TextBlock? MediaPos { get; set; }
        public TextBlock? MediaDur { get; set; }
        public Border? MediaArt { get; set; }
        public TextBlock? MediaTitle { get; set; }
        public TextBlock? MediaArtist { get; set; }
        public MaterialIcon? PlayPauseIcon { get; set; }
        public MaterialIcon? LikeIcon { get; set; }
        public MaterialIcon? DislikeIcon { get; set; }
        public TextBlock? StatusLabel { get; set; }
        /// <summary>Plainext: title uses {@code Title > Playing &lt;} and progress uses slash meter.</summary>
        public bool PlainTextMedia { get; set; }
        /// <summary>Hide percent at rest; show while dragging or wheeling (M3-style value indicator).</summary>
        public bool PercentOnAdjustOnly { get; set; }
    }

    /// <summary>Root wrapper - live pump updates Bindings directly (volume / scrub / art).</summary>
    /// <remarks>Root wrapper - live pump updates Bindings directly (volume / scrub / art).</remarks>
    public sealed class TesseraLiveHost(TesseraLiveBindings? sharedBindings = null) : ContentControl
    {
        public TesseraLiveBindings Bindings { get; } = sharedBindings ?? new TesseraLiveBindings();

        /// <summary>Module-config preview, must not sample live desktop backdrop.</summary>
        public bool IsEmbeddedPreview { get; init; }

        /// <summary>Find live host when flyout root is wrapped (scale decorator, shared backdrop parent).</summary>
        public static TesseraLiveHost? FindIn(Control? root)
        {
            if (root is TesseraLiveHost host)
            {
                return host;
            }

            if (root is Decorator { Child: Control decorated })
            {
                return FindIn(decorated);
            }

            if (root is Panel panel)
            {
                foreach (Control c in panel.Children)
                {
                    TesseraLiveHost? found = FindIn(c);
                    if (found is not null)
                    {
                        return found;
                    }
                }
                return null;
            }
            if (root is ContentControl { Content: Control content })
            {
                return FindIn(content);
            }

            if (root is Visual visual)
            {
                foreach (Visual v in visual.GetVisualChildren())
                {
                    if (v is Control c)
                    {
                        TesseraLiveHost? found = FindIn(c);
                        if (found is not null)
                        {
                            return found;
                        }
                    }
                }
            }
            return null;
        }

        public void ApplyLive(HostServices services, FlyoutRequest request)
        {
            TesseraLiveBindings b = Bindings;
            if (request.Kind.Equals("locks", StringComparison.OrdinalIgnoreCase)
                || request.Kind.Equals("flight", StringComparison.OrdinalIgnoreCase))
            {
                Dictionary<string, string> live = TesseraFlyoutRequestBuilder.RefreshStatusPayload(
                    services, request.Kind, request.Payload);
                request = request with { Payload = live };
                b.StatusLabel?.Text = TesseraStatusLabels.Format(request);

                return;
            }

            MediaSessionInfo? media = services.Media.Current;
            bool isBright = request.Kind.Equals("bright", StringComparison.OrdinalIgnoreCase);

            if (!isBright && (b.VolumeTrack is not null || b.VolumeRing is not null))
            {
                double vol = services.Audio.MasterVolume;
                bool muted = services.Audio.IsMuted;
                bool adjusting = b.VolumeTrack?.IsUserAdjusting == true || b.VolumeRing?.IsUserAdjusting == true;
                if (b.VolumeTrack is not null && !b.VolumeTrack.IsUserAdjusting)
                {
                    b.VolumeTrack.SetValueSilent(vol);
                }

                if (b.VolumeRing is not null && !b.VolumeRing.IsUserAdjusting)
                {
                    b.VolumeRing.SetValueSilent(vol);
                }

                double shown = adjusting
                    ? (b.VolumeRing?.Value ?? b.VolumeTrack?.Value ?? vol)
                    : vol;
                if (b.Percent is not null)
                {
                    int pct = VolumePercent.ToPercent(shown);
                    b.Percent.Text = TesseraVolumeLabel.Volume(muted, pct, b.SlashMeter is not null);
                    if (b.PercentOnAdjustOnly)
                    {
                        b.Percent.IsVisible = adjusting;
                    }
                }
                b.SlashMeter?.Text = TesseraChrome.SlashFill(shown);

                if (b.Glyph is not null)
                {
                    b.Glyph.Kind = muted || shown <= 0.001
                        ? MaterialIconKind.VolumeOff
                        : shown < 0.20 ? MaterialIconKind.VolumeLow
                        : shown < 0.50 ? MaterialIconKind.VolumeMedium
                        : MaterialIconKind.VolumeHigh;
                    if (b.MaterialYouVolumeGlyph)
                    {
                        double trackH = b.VolumeTrack?.Bounds.Height ?? 0;
                        TesseraMaterialYouM3.ApplyVolumeGlyphTone(b.Glyph, shown, muted, trackH);
                    }
                }
            }
            else if (isBright && b.VolumeTrack is not null)
            {
                double br = services.Brightness.IsSupported ? services.Brightness.Brightness : 0.5;
                if (!b.VolumeTrack.IsUserAdjusting)
                {
                    b.VolumeTrack.SetValueSilent(br);
                }

                if (b.Percent is not null)
                {
                    double shown = b.VolumeTrack.IsUserAdjusting ? b.VolumeTrack.Value : br;
                    b.Percent.Text = TesseraVolumeLabel.Brightness(VolumePercent.ToPercent(shown), b.PlainTextMedia);
                }
                b.SlashMeter?.Text = TesseraChrome.SlashFill(br);
            }

            if (b.MediaTitle is not null)
            {
                if (b.PlainTextMedia && media is not null)
                {
                    string title = string.IsNullOrWhiteSpace(media.Title) ? " " : media.Title;
                    string state = media.IsPlaying ? "Playing" : "Paused";
                    b.MediaTitle.Text = $"{title} > {state} <";
                }
                else
                {
                    b.MediaTitle.Text = string.IsNullOrWhiteSpace(media?.Title) ? " " : media.Title;
                }
            }
            b.MediaArtist?.Text = string.IsNullOrWhiteSpace(media?.Artist) ? " " : media.Artist;

            if (b.MediaArt is not null)
            {
                byte[]? thumb = ResolveThumbnail(media?.ThumbnailPng, media?.Title ?? b.MediaTitle?.Text);
                bool fillHost = double.IsNaN(b.MediaArt.Width) || b.MediaArt.Width <= 1.0;
                TesseraMediaPanel.ApplyArtToBorder(b.MediaArt, thumb, fillHost);
            }

            b.PlayPauseIcon?.Kind = media?.IsPlaying == true ? MaterialIconKind.Pause : MaterialIconKind.Play;

            if (b.LikeIcon is not null)
            {
                TesseraMediaPanel.ApplyLikeIcon(b.LikeIcon, media?.LikeRating);
            }

            if (b.DislikeIcon is not null)
            {
                TesseraMediaPanel.ApplyDislikeIcon(b.DislikeIcon, media?.LikeRating);
            }

            if (media is not null)
            {
                double dur = media.DurationSeconds;
                double pos = media.PositionSeconds;
                double progress = dur > 0.5 ? Math.Clamp(pos / dur, 0, 1) : 0;
                b.MediaScrub?.SetValueSilent(progress);
                b.MediaPos?.Text = b.PlainTextMedia
                        ? $"{FormatTime(pos)} {TesseraChrome.SlashFill(progress, 16)} {FormatTime(dur)}"
                        : b.MediaDur is not null
                            ? FormatTime(pos)
                            : $"{FormatTime(pos)} / {FormatTime(dur)}";
                b.MediaDur?.Text = FormatTime(dur);
            }
        }

        private static string FormatTime(double seconds)
        {
            if (seconds <= 0 || double.IsNaN(seconds))
            {
                return "0:00";
            }

            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");
        }

        public static byte[]? ResolveThumbnail(byte[]? smtcOrMerged, string? title)
        {
            return smtcOrMerged is { Length: >= 32 }
                ? smtcOrMerged
                : WebNowPlayingReduxHost.TryGetCachedCover(title, out byte[]? png) && png is { Length: >= 32 } ? png : null;
        }
    }
}
