using Avalonia.Controls;

namespace MosaicShell.Host.Tiles.Tessera
{
    /// <summary>Ambient bindings while a flyout tree is being built (thread-static).</summary>
    internal static class TesseraLiveAmbient
    {
        [field: ThreadStatic]
        public static TesseraLiveBindings? Current { get; set; }

        public static void RegisterVolume(
            TesseraTrack track,
            TextBlock? percent,
            Material.Icons.Avalonia.MaterialIcon? glyph,
            bool pixelVolumeGlyph = false,
            bool percentOnAdjustOnly = false)
        {
            if (Current is null)
            {
                return;
            }

            Current.VolumeRing = null;
            Current.VolumeTrack = track;
            Current.Percent = percent;
            Current.Glyph = glyph;
            Current.MaterialYouVolumeGlyph = pixelVolumeGlyph;
            Current.PercentOnAdjustOnly = percentOnAdjustOnly;
        }

        public static void RegisterRing(TesseraRingVolume ring)
        {
            if (Current is null)
            {
                return;
            }

            Current.VolumeTrack = null;
            Current.VolumeRing = ring;
            Current.Percent = ring.PercentLabel;
        }

        public static void RegisterSlash(TextBlock slash)
        {
            if (Current is null)
            {
                return;
            }

            Current.SlashMeter = slash;
        }

        public static void RegisterMedia(
            Border art,
            TextBlock title,
            TextBlock artist,
            TesseraTrack? scrub,
            TextBlock? pos,
            TextBlock? dur,
            Material.Icons.Avalonia.MaterialIcon? play,
            Material.Icons.Avalonia.MaterialIcon? like = null,
            Material.Icons.Avalonia.MaterialIcon? dislike = null)
        {
            if (Current is null)
            {
                return;
            }

            Current.MediaArt = art;
            Current.MediaTitle = title;
            Current.MediaArtist = artist;
            Current.MediaScrub = scrub;
            Current.MediaPos = pos;
            Current.MediaDur = dur;
            Current.PlayPauseIcon = play;
            Current.LikeIcon = like;
            Current.DislikeIcon = dislike;
        }

        public static void RegisterPlainTextMedia(TextBlock titleState, TextBlock artist, TextBlock progressLine)
        {
            if (Current is null)
            {
                return;
            }

            Current.PlainTextMedia = true;
            Current.MediaTitle = titleState;
            Current.MediaArtist = artist;
            Current.MediaPos = progressLine;
        }

        public static void RegisterStatus(TextBlock label)
        {
            if (Current is null)
            {
                return;
            }

            Current.StatusLabel = label;
        }
    }
}
