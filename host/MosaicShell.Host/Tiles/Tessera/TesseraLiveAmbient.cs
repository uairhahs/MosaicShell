using Avalonia.Controls;
using MosaicShell.Core.Services;

namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>Ambient bindings while a flyout tree is being built (thread-static).</summary>
internal static class TesseraLiveAmbient
{
    [ThreadStatic] private static TesseraLiveBindings? _current;
    public static TesseraLiveBindings? Current
    {
        get => _current;
        set => _current = value;
    }

    public static void RegisterVolume(
        TesseraTrack track,
        TextBlock? percent,
        Material.Icons.Avalonia.MaterialIcon? glyph,
        bool pixelVolumeGlyph = false,
        bool percentOnAdjustOnly = false)
    {
        if (_current is null) return;
        _current.VolumeTrack = track;
        _current.Percent = percent;
        _current.Glyph = glyph;
        _current.PixelVolumeGlyph = pixelVolumeGlyph;
        _current.PercentOnAdjustOnly = percentOnAdjustOnly;
    }

    public static void RegisterRing(TesseraRingVolume ring)
    {
        if (_current is null) return;
        _current.VolumeRing = ring;
        _current.Percent = ring.PercentLabel;
    }

    public static void RegisterSlash(TextBlock slash) 
    {
        if (_current is null) return;
        _current.SlashMeter = slash;
    }

    public static void RegisterMedia(
        Border art,
        TextBlock title,
        TextBlock artist,
        TesseraTrack? scrub,
        TextBlock? pos,
        TextBlock? dur,
        Material.Icons.Avalonia.MaterialIcon? play)
    {
        if (_current is null) return;
        _current.MediaArt = art;
        _current.MediaTitle = title;
        _current.MediaArtist = artist;
        _current.MediaScrub = scrub;
        _current.MediaPos = pos;
        _current.MediaDur = dur;
        _current.PlayPauseIcon = play;
    }

    public static void RegisterPlainextMedia(TextBlock titleState, TextBlock artist, TextBlock progressLine)
    {
        if (_current is null) return;
        _current.PlainextMedia = true;
        _current.MediaTitle = titleState;
        _current.MediaArtist = artist;
        _current.MediaPos = progressLine;
    }

    public static void RegisterStatus(TextBlock label)
    {
        if (_current is null) return;
        _current.StatusLabel = label;
    }
}
