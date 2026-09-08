namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// DIP metrics for H3 stacked panel placement. Mirrors single-HWND Tessera layout spacing
    /// from signed reference shots under <c>.github/res/Tessera</c>.
    /// </summary>
    public static class TesseraStackedPlacementSpec
    {
        public const double MeterGapDip = 12;
        public const double MeterMediaWidthDip = 220;
        public const double MeterVolumeWidthDip = 28;
        /// <summary>Volume pill height; media card is taller per reference.</summary>
        public const double MeterVolumeHeightDip = 200;
        /// <summary>
        /// Must clear <see cref="MeterMediaMinContentHeightDip"/> so transport glyphs are not clipped.
        /// </summary>
        public const double MeterMediaHeightDip = 236;
        /// <summary>Cluster height anchor (tallest Meter panel).</summary>
        public const double MeterPanelHeightDip = MeterMediaHeightDip;
        public const float MeterVolumeCornerRadiusDip = 14f;
        public const float MeterMediaCornerRadiusDip = 16f;
        public const double MeterMediaArtDip = 120;
        public const double MeterMediaArtBottomMarginDip = 4;
        public const double MeterMediaHeaderSpacingDip = 2;
        public const double MeterMediaTitleLineDip = 20;
        public const double MeterMediaArtistLineDip = 16;
        public const double MeterMediaTransportBtnDip = 28;
        public const double MeterMediaTransportTopMarginDip = 8;
        public const double MeterMediaBodyWidthDip = 192;
        public const double MeterMediaPadHorizontalDip = 14;
        public const double MeterMediaPadTopDip = 10;
        public const double MeterMediaPadBottomDip = 14;

        /// <summary>
        /// Vertical DIP budget for stacked Meter media: pad + art + title/artist + transport.
        /// Host layout must fit inside <see cref="MeterMediaHeightDip"/>.
        /// </summary>
        public static double MeterMediaMinContentHeightDip =>
            MeterMediaPadTopDip
            + MeterMediaArtDip
            + MeterMediaArtBottomMarginDip
            + MeterMediaHeaderSpacingDip
            + MeterMediaTitleLineDip
            + MeterMediaHeaderSpacingDip
            + MeterMediaArtistLineDip
            + MeterMediaTransportTopMarginDip
            + MeterMediaTransportBtnDip
            + MeterMediaPadBottomDip;

        public const float GnomePillCornerRadiusDip = 28f;

        public const double GnomeGapDip = 10;
        public const double GnomeMediaWidthDip = 340;
        public const double GnomeVolumeWidthDip = 240;
        public const double GnomeMediaHeightDip = 64;
        public const double GnomeVolumeHeightDip = 48;

        public const double PlainTextWidthDip = 360;
        public const double PlainTextGapDip = 4;
        public const double PlainTextVolumeHeightDip = 72;
        public const double PlainTextMediaHeightDip = 96;

        public const double CompactGapDip = 10;
        public const double CompactVolumeWidthDip = 280;
        public const double CompactMediaWidthDip = 320;
        public const double CompactVolumeHeightDip = 52;
        /// <summary>SimpleRow art + title stack + heart exceeds the old 96 DIP lock.</summary>
        public const double CompactMediaHeightDip = 128;

        /// <summary>Matches single-HWND ModernFlyouts StackPanel Spacing (12).</summary>
        public const double ModernFlyoutsGapDip = 12;
        public const double ModernFlyoutsVolumeWidthDip = 320;
        public const double ModernFlyoutsMediaWidthDip = 320;
        public const double ModernFlyoutsVolumeHeightDip = 52;
        /// <summary>Matches ModernCard glass height; 120 DIP locked HWND clipped transport.</summary>
        public const double ModernFlyoutsMediaHeightDip = 190;

        public const double Win11WidthDip = 320;
        public const double Win11GapDip = 0;
        public const double Win11VolumeHeightDip = 50;
        /// <summary>Header + title/artist row, scrubber, and transport row need slightly more than
        /// 175 to avoid clipping the position/duration line under the scrubber; 195 gives headroom.</summary>
        public const double Win11MediaHeightDip = 195;
        public const double Win11PadDip = 15;
        public const double Win11CornerRadiusDip = 12;

        public const double RadialClusterWidthDip = 480;
        public const double RadialColumnGapDip = 14;
        public const double RadialVolumeWidthDip = 180;
        public const double RadialMediaWidthDip = 200;
        public const double RadialPanelHeightDip = 162;
    }
}
