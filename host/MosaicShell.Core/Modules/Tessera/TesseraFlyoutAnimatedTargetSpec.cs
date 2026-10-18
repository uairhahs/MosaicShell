namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// Per-meter Animated group formulas from YourFlyouts layout inc files.
    /// Each helper maps one Rainmeter Shape/meter property driven by TweenNode1 (0..1).
    /// </summary>
    public static class TesseraFlyoutAnimatedTargetSpec
    {
        /// <summary>Rainmeter uses <c>#MediaW#*#TweenNode1#+1</c> to avoid zero-width clip.</summary>
        public const double MediaClipWidthEpsilonDip = 1;

        /// <summary>Peak Host overlay paint if a frost wash is drawn; 0 disables covering white.</summary>
        public const double MediaCoverOverlayMaxAlpha = 0;

        /// <summary>
        /// Win11 VolumeB (Group=Standard) stays fully opaque. MediaB XOR fill alpha is shell frost,
        /// not the volume control tree.
        /// </summary>
        public const bool Win11VolumeControlsStayOpaque = true;

        /// <summary>Pixel.inc <c>#ColumnW#</c>. Host Material You column aliases this.</summary>
        public const double MaterialYouColumnWidthDip = 60;

        public static double ResolveFluentDividerHeightFactor(double progress, bool musicVisible)
        {
            return musicVisible ? ClampProgress(progress) : 0;
        }

        public static double ResolveFluentDividerHeightDip(double fullInnerHeight, double progress, bool musicVisible)
        {
            return fullInnerHeight * ResolveFluentDividerHeightFactor(progress, musicVisible);
        }

        public static double ResolveFluentMediaClipWidthDip(double fullMediaWidth, double progress, bool musicVisible)
        {
            return !musicVisible ? 0 : Math.Max(0, (fullMediaWidth * ClampProgress(progress)) + MediaClipWidthEpsilonDip);
        }

        public static double ResolveFluentMediaOverlayAlpha(double progress, bool musicVisible)
        {
            return ResolveMediaCoverOverlayAlpha(progress, musicVisible);
        }

        public static double ResolveMediaCoverOverlayAlpha(double progress, bool musicVisible)
        {
            return MediaCoverOverlayMaxAlpha * (musicVisible ? ClampProgress(progress) : 0);
        }

        /// <summary>
        /// Clip and HWND region are the MediaC container mask. Fading the leaf on show
        /// leaves the growing strip empty, then the card pops when TweenNode1 is high.
        /// Hide already reads as a clip dissolve; leaf alpha stays rest under the clip.
        /// </summary>
        public const bool ClippedMediaLeafMustStayOpaque = true;

        public static double ResolveMediaMaskOpacity(double progress, bool musicVisible)
        {
            return musicVisible ? ClampProgress(progress) : 0;
        }

        /// <summary>Fluent divider rest stroke opacity (Host Line.Opacity while music is visible).</summary>
        public const double FluentDividerRestOpacity = 0.55;

        /// <summary>
        /// Catalog MediaB is DividerHeight only. Scaling stroke alpha with TweenNode1
        /// hides the in-place grow on show; hide still looks like a wipe because the
        /// line is already opaque. Opacity stays rest while music is visible.
        /// </summary>
        public const bool FluentDividerOpacityMustStayRestWhileMusicVisible = true;

        public static double ResolveClippedMediaLeafOpacity(double progress, bool musicVisible)
        {
            _ = progress;
            return !musicVisible ? 0 : ClippedMediaLeafMustStayOpaque ? 1 : ClampProgress(progress);
        }

        public static double ResolveFluentMediaContentOpacity(double progress, bool musicVisible)
        {
            return ResolveClippedMediaLeafOpacity(progress, musicVisible);
        }

        public static double ResolveFluentDividerOpacity(double progress, bool musicVisible)
        {
            _ = progress;
            return !musicVisible
                ? 0
                : FluentDividerOpacityMustStayRestWhileMusicVisible
                ? FluentDividerRestOpacity
                : FluentDividerRestOpacity * ClampProgress(progress);
        }

        /// <summary>
        /// Hide wipes MediaB in place on a visible card. Show must keep that column inside
        /// the collapsed shell (height 0) so the line grows there instead of popping in
        /// when the media HWND/region first includes it.
        /// </summary>
        public const bool FluentDividerMustTweenInPlaceOnShow = true;

        public static double ResolveFluentCollapsedShellWidthDip(bool musicVisible)
        {
            return TesseraFluentLayoutSpec.VolumeWidthDip
            + (musicVisible && FluentDividerMustTweenInPlaceOnShow
                ? TesseraFluentLayoutSpec.DividerColumnWidthDip
                : 0);
        }

        /// <summary>
        /// Stacked media HWND starts at the collapsed shell. Divider column is inside
        /// volume, so media abuts volume instead of repeating the 3 DIP as a gap.
        /// </summary>
        public static double ResolveFluentStackedMediaOffsetXDip(bool musicVisible)
        {
            return ResolveFluentCollapsedShellWidthDip(musicVisible);
        }

        /// <summary>
        /// Visible Fluent shell width. At TweenNode1=0 this is volume plus the divider
        /// column (line height still 0). Rest-sized chrome at p=0 is the black media bay.
        /// </summary>
        public static double ResolveFluentVisibleShellWidthDip(double progress, bool musicVisible)
        {
            double collapsed = ResolveFluentCollapsedShellWidthDip(musicVisible);
            if (!musicVisible)
            {
                return collapsed;
            }

            double p = ClampProgress(progress);
            return p <= 0
                ? collapsed
                : TesseraFluentLayoutSpec.VolumeWidthDip
                   + TesseraStackedPlacementPolicy.FluentDividerDip
                   + ResolveFluentMediaClipWidthDip(TesseraFluentLayoutSpec.MediaWidthDip, p, true);
        }

        /// <summary>Rest measure for Fluent MediaC (YourFlyouts skin width, not TweenNode1).</summary>
        public static double ResolveFluentMediaLayoutWidthDip(double fullMediaWidth, bool musicVisible)
        {
            return !musicVisible ? 0 : Math.Max(0, fullMediaWidth + MediaClipWidthEpsilonDip);
        }

        public static double ResolveWin11LayoutHeightDip(
            double volumeHeight,
            double mediaHeight,
            bool musicVisible)
        {
            return musicVisible ? volumeHeight + mediaHeight : volumeHeight;
        }

        public static double ResolveCoreUiLayoutTrackThicknessDip(double baseTrackThickness)
        {
            return baseTrackThickness;
        }

        public static double ResolveCoreUiMediaLayoutWidthDip(double fullPanelWidth, bool musicVisible)
        {
            _ = musicVisible;
            return fullPanelWidth;
        }

        public static double ResolveCoreUiMediaClipWidthDip(
            double fullPanelWidth,
            double progress,
            bool musicVisible)
        {
            return !musicVisible ? fullPanelWidth : fullPanelWidth * ClampProgress(progress);
        }

        public static double ResolveWin11BorderHeightDip(
            double volumeHeight,
            double mediaHeight,
            double progress,
            bool musicVisible)
        {
            return !musicVisible ? volumeHeight : volumeHeight + (mediaHeight * ClampProgress(progress));
        }

        public static double ResolveWin11VolumeFillOpacityFactor(double progress, bool musicVisible)
        {
            return musicVisible ? ClampProgress(progress) : 1;
        }

        public static double ResolveWin11MediaClipHeightDip(
            double fullMediaHeight,
            double progress,
            bool musicVisible)
        {
            return !musicVisible ? 0 : fullMediaHeight * ClampProgress(progress);
        }

        public static double ResolveWin11MediaOverlayAlpha(double progress, bool musicVisible)
        {
            return ResolveMediaCoverOverlayAlpha(progress, musicVisible);
        }

        public static double ResolveGnomeContentScale(double progress, bool musicVisible)
        {
            return musicVisible ? 0.5 + (0.5 * ClampProgress(progress)) : 1;
        }

        public static double ResolveGnomeVolumeFillOpacity(double progress, bool musicVisible)
        {
            return musicVisible ? ClampProgress(progress) : 1;
        }

        public static double ResolveSquareLabelScale(double progress)
        {
            return ClampProgress(progress);
        }

        public static double ResolveGnomeOverlayAlpha(double progress, bool musicVisible)
        {
            return ResolveMediaCoverOverlayAlpha(progress, musicVisible);
        }

        public static double ResolvePlainTextFillOpacityFactor(double progress, bool musicVisible)
        {
            return musicVisible ? ClampProgress(progress) : 1;
        }

        public static double ResolveCoreUiVolumeBarScaleFactor(double progress, bool musicVisible)
        {
            return musicVisible ? ClampProgress(progress) : 1;
        }

        public static double ResolveCoreUiMediaOverlayAlpha(double progress, bool musicVisible)
        {
            return ResolveMediaCoverOverlayAlpha(progress, musicVisible);
        }

        public static double ResolveCoreUiMediaContentOpacityFactor(double progress, bool musicVisible)
        {
            return ResolveClippedMediaLeafOpacity(progress, musicVisible);
        }

        public static double ResolveModernMediaClipHeightDip(
            double fullMediaHeight,
            double progress,
            bool musicVisible)
        {
            return !musicVisible ? 0 : fullMediaHeight * ClampProgress(progress);
        }

        /// <summary>
        /// Radial ring fill-arc extent. Multiplies the ring's live value, so the arc sweeps up
        /// from nothing to the real level on entry and the ring's own Value owns it at rest.
        /// </summary>
        public static double ResolveRadialRingSweepFactor(double progress)
        {
            return ClampProgress(progress);
        }

        private static double ClampProgress(double progress)
        {
            return Math.Clamp(progress, 0, 1);
        }
    }
}
