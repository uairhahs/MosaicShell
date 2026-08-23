using Avalonia.Media;
using Material.Icons.Avalonia;

namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>Per-style shell/muted colors; accent comes from <see cref="TesseraPalette"/> (settings or system).</summary>
internal static class TesseraStylePalette
{
    public static class MaterialYou
    {
        public static Color Shell => Color.FromRgb(27, 27, 30);
        public static Color Secondary => Color.FromRgb(164, 171, 192);
        public static Color TrackInactive => Color.FromRgb(55, 55, 62);
        public static IBrush ShellBrush => new SolidColorBrush(Shell);
        public static IBrush AccentBrush => TesseraPalette.AccentBrush;
        public static IBrush SecondaryBrush => new SolidColorBrush(Secondary);
        public static IBrush TrackInactiveBrush => new SolidColorBrush(TrackInactive);
        public static IBrush OnAccentBrush => TesseraPalette.OnAccentBrush;
    }

    public static class CoreUi
    {
        public static Color Shell => Color.FromRgb(12, 12, 12);
        public static Color Muted => Color.FromRgb(150, 150, 150);
        public static Color Tile => Color.FromRgb(22, 22, 22);
        public static Color TileHover => Color.FromRgb(38, 38, 38);
        public static IBrush ShellBrush => new SolidColorBrush(Shell);
        public static IBrush AccentBrush => TesseraPalette.AccentBrush;
        public static IBrush TileBrush => new SolidColorBrush(Tile);
        public static IBrush TileHoverBrush => new SolidColorBrush(TileHover);
        public static IBrush IconHoverBrush => new SolidColorBrush(Color.FromArgb(56, 255, 255, 255));
        public static IBrush ArtDimBrush => new SolidColorBrush(Color.FromArgb(150, 12, 12, 12));
    }

    public static class Windows11
    {
        public static Color Shell => Color.FromArgb(185, 34, 34, 34);
        public static IBrush ShellBrush => new SolidColorBrush(Shell);
        public static IBrush AccentBrush => TesseraPalette.AccentBrush;
    }

    public static class Radial
    {
        public static Color Shell => Color.FromArgb(210, 24, 32, 48);
        public static Color AccentHi => TesseraPalette.LightenAccent(0.18);
        public static Color Bright => TesseraPalette.LightenAccent(0.42);
        public static IBrush AccentBrush => TesseraPalette.AccentBrush;
        public static IBrush AccentHiBrush => new SolidColorBrush(AccentHi);
        public static IBrush BrightBrush => new SolidColorBrush(Bright);
        public static IBrush ShellBrush => new SolidColorBrush(Shell);
    }
}

/// <summary>M3 Expressive helpers for Pixel flyout.</summary>
internal static class TesseraMaterialYouM3
{
    public static void ApplyVolumeGlyphTone(MaterialIcon glyph, double volume, bool muted, double trackHeight = 0)
    {
        if (muted || volume <= 0.001)
        {
            glyph.Foreground = TesseraStylePalette.MaterialYou.SecondaryBrush;
            return;
        }

        var iconCenterFromBottom =
            TesseraStyleMetrics.MaterialYouTrackIconBottom + TesseraStyleMetrics.MaterialYouTrackIconSize / 2.0;
        var trackH = trackHeight > 1 ? trackHeight : 160.0;
        var fillH = trackH * volume;
        glyph.Foreground = fillH >= iconCenterFromBottom - 2
            ? TesseraStylePalette.MaterialYou.OnAccentBrush
            : TesseraStylePalette.MaterialYou.SecondaryBrush;
    }

    public static bool IsMutedIcon(MaterialIcon icon) =>
        icon.Foreground is SolidColorBrush b && b.Color == TesseraStylePalette.MaterialYou.Secondary;
}

internal static class TesseraStyleMetrics
{
    public const double MaterialYouColumnW = 60;
    public const double MaterialYouHeight = 384;
    public const double MaterialYouColH = 154;
    public const double MaterialYouGap = 10;
    public const double MaterialYouPad = 10;
    /// <summary>M3 icon button: 24dp icon, 40dp touch target (compact column).</summary>
    public const double MaterialYouIconSize = 24;
    public const double MaterialYouHitTarget = 40;
    public const double MaterialYouPlayW = 40;
    public const double MaterialYouPlayH = 48;
    /// <summary>M3 expressive inset track icon (m/l/xl scale).</summary>
    public const double MaterialYouTrackIconSize = 24;
    public const double MaterialYouTrackIconBottom = 10;

    public const double CoreUiWidth = 400;
    /// <summary>Corner device tile, square, matches volume row height (ref proportions).</summary>
    public const double CoreUiVolumeH = 58;
    public const double CoreUiMediaH = 150;
    public const double CoreUiPad = 15;
    public const double CoreUiGap = 6;
    public const double CoreUiDevice = 58;
    public const double CoreUiTransportW = 58;

    /// <summary>Balanced flyout, between ref width and shell cap.</summary>
    public const double RadialWidth = 480;
    public const double RadialMinHeight = 162;
    public const double RadialMaxHeight = 176;
    public const double RadialPad = 14;
    public const double RadialRing = 100;
    public const double RadialColumnGap = 14;
}
