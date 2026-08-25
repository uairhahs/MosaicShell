namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// Per-meter Animated group formulas from YourFlyouts layout inc files.
/// Each helper maps one Rainmeter Shape/meter property driven by TweenNode1 (0..1).
/// </summary>
public static class TesseraFlyoutAnimatedTargetSpec
{
    /// <summary>Rainmeter uses <c>#MediaW#*#TweenNode1#+1</c> to avoid zero-width clip.</summary>
    public const double MediaClipWidthEpsilonDip = 1;

    /// <summary>
    /// YourFlyouts MediaC <c>Fill Color 255,255,255,(255*p)</c> is a Rainmeter container mask
    /// (children become visible as alpha rises). It is not a white sheet composited over art.
    /// Host must clip/fade content; covering overlay paint alpha stays 0.
    /// </summary>
    public const bool MediaOverlayIsContainerMaskNotCover = true;

    /// <summary>Peak Host overlay paint if a frost wash is drawn; 0 disables covering white.</summary>
    public const double MediaCoverOverlayMaxAlpha = 0;

    /// <summary>
    /// Win11 VolumeB (Group=Standard) stays fully opaque. MediaB XOR fill alpha is shell frost,
    /// not the volume control tree.
    /// </summary>
    public const bool Win11VolumeControlsStayOpaque = true;

    /// <summary>
    /// YF MediaB XOR is frost-in-growing-shell, not a white sheet. Host must not paint a cover overlay.
    /// Avalonia clip and Win32 region use StrokeB (<see cref="ResolveWin11BorderHeightDip"/>).
    /// </summary>
    public const bool Win11XorFrostMustNotPaintCoverOverlay = true;

    /// <summary>YF MediaC grows as a container; Host clips in place (no leaf slide).</summary>
    public const bool CoreUiMediaMustClipOnly = true;

    /// <summary>Amber.inc MediaB X inset at TweenNode1=0 (<c>20*Scale</c>).</summary>
    public const double MeterMediaSlideRestDip = 20;

    /// <summary>Pixel.inc <c>#ColumnW#</c>. Host Material You column aliases this.</summary>
    public const double MaterialYouColumnWidthDip = 60;

    /// <summary>YF VolumeB fill alpha, not ScaleTransform. MediaB keeps scale.</summary>
    public const bool GnomeVolumeMustNotUseContentScale = true;

    public static double ResolveFluentDividerHeightFactor(double progress, bool musicVisible) =>
        musicVisible ? ClampProgress(progress) : 0;

    public static double ResolveFluentDividerHeightDip(double fullInnerHeight, double progress, bool musicVisible) =>
        fullInnerHeight * ResolveFluentDividerHeightFactor(progress, musicVisible);

    public static double ResolveFluentMediaClipWidthDip(double fullMediaWidth, double progress, bool musicVisible)
    {
        if (!musicVisible)
            return 0;
        return Math.Max(0, fullMediaWidth * ClampProgress(progress) + MediaClipWidthEpsilonDip);
    }

    public static double ResolveFluentMediaOverlayAlpha(double progress, bool musicVisible) =>
        ResolveMediaCoverOverlayAlpha(progress, musicVisible);

    public static double ResolveMediaCoverOverlayAlpha(double progress, bool musicVisible) =>
        MediaCoverOverlayMaxAlpha * (musicVisible ? ClampProgress(progress) : 0);

    /// <summary>
    /// Clip and HWND region are the MediaC container mask. Fading the leaf on show
    /// leaves the growing strip empty, then the card pops when TweenNode1 is high.
    /// Hide already reads as a clip dissolve; leaf alpha stays rest under the clip.
    /// </summary>
    public const bool ClippedMediaLeafMustStayOpaque = true;

    public static double ResolveMediaMaskOpacity(double progress, bool musicVisible) =>
        musicVisible ? ClampProgress(progress) : 0;

    /// <summary>
    /// YourFlyouts MediaC fill alpha is a container mask. Host must fade media content with
    /// TweenNode1. Leaving the leaf at 1 paints a fully rendered card behind the clip on show.
    /// </summary>
    public const bool AnimatedMediaMustDissolveWithTweenNode1 = true;

    /// <summary>YF VolumeB Group=Standard. TweenNode1 does not fade the volume column.</summary>
    public const bool FluentVolumeStaysOpaqueDuringPhase2 = true;

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
        if (!musicVisible)
            return 0;
        return ClippedMediaLeafMustStayOpaque ? 1 : ClampProgress(progress);
    }

    public static double ResolveFluentMediaContentOpacity(double progress, bool musicVisible) =>
        ResolveClippedMediaLeafOpacity(progress, musicVisible);

    public static double ResolveFluentDividerOpacity(double progress, bool musicVisible)
    {
        _ = progress;
        if (!musicVisible)
            return 0;
        return FluentDividerOpacityMustStayRestWhileMusicVisible
            ? FluentDividerRestOpacity
            : FluentDividerRestOpacity * ClampProgress(progress);
    }

    /// <summary>Fluent StrokeB static width when music visible (not Animated).</summary>
    public static double ResolveFluentShellWidthDip(
        double volumeWidth,
        double mediaWidth,
        bool musicVisible) =>
        musicVisible ? volumeWidth + mediaWidth : volumeWidth;

    /// <summary>
    /// Hide wipes MediaB in place on a visible card. Show must keep that column inside
    /// the collapsed shell (height 0) so the line grows there instead of popping in
    /// when the media HWND/region first includes it.
    /// </summary>
    public const bool FluentDividerMustTweenInPlaceOnShow = true;

    public static double ResolveFluentCollapsedShellWidthDip(bool musicVisible) =>
        TesseraFluentLayoutSpec.VolumeWidthDip
        + (musicVisible && FluentDividerMustTweenInPlaceOnShow
            ? TesseraFluentLayoutSpec.DividerColumnWidthDip
            : 0);

    /// <summary>
    /// Stacked media HWND starts at the collapsed shell. Divider column is inside
    /// volume, so media abuts volume instead of repeating the 3 DIP as a gap.
    /// </summary>
    public static double ResolveFluentStackedMediaOffsetXDip(bool musicVisible) =>
        ResolveFluentCollapsedShellWidthDip(musicVisible);

    /// <summary>
    /// Visible Fluent shell width. At TweenNode1=0 this is volume plus the divider
    /// column (line height still 0). Rest-sized chrome at p=0 is the black media bay.
    /// </summary>
    public static double ResolveFluentVisibleShellWidthDip(double progress, bool musicVisible)
    {
        var collapsed = ResolveFluentCollapsedShellWidthDip(musicVisible);
        if (!musicVisible)
            return collapsed;
        var p = ClampProgress(progress);
        if (p <= 0)
            return collapsed;
        return TesseraFluentLayoutSpec.VolumeWidthDip
               + TesseraStackedPlacementPolicy.FluentDividerDip
               + ResolveFluentMediaClipWidthDip(TesseraFluentLayoutSpec.MediaWidthDip, p, true);
    }

    /// <summary>Stacked Fluent media HWND width. 0 at p=0 so Host can hide rest-sized frost.</summary>
    public static double ResolveFluentStackedMediaRegionWidthDip(double progress, bool musicVisible)
    {
        if (!musicVisible)
            return 0;
        var p = ClampProgress(progress);
        if (p <= 0)
            return 0;
        return ResolveFluentMediaClipWidthDip(TesseraFluentLayoutSpec.MediaWidthDip, p, true);
    }

    /// <summary>Rest measure for Fluent MediaC (YourFlyouts skin width, not TweenNode1).</summary>
    public static double ResolveFluentMediaLayoutWidthDip(double fullMediaWidth, bool musicVisible)
    {
        if (!musicVisible)
            return 0;
        return Math.Max(0, fullMediaWidth + MediaClipWidthEpsilonDip);
    }

    public static double ResolveWin11LayoutHeightDip(
        double volumeHeight,
        double mediaHeight,
        bool musicVisible) =>
        musicVisible ? volumeHeight + mediaHeight : volumeHeight;

    public static double ResolveCoreUiLayoutTrackThicknessDip(double baseTrackThickness) =>
        baseTrackThickness;

    public static double ResolveCoreUiMediaLayoutWidthDip(double fullPanelWidth, bool musicVisible)
    {
        _ = musicVisible;
        return fullPanelWidth;
    }

    /// <summary>Wrapped media+transport row (inner width), never the art column alone.</summary>
    public static double ResolveCoreUiWrappedRowLayoutWidthDip(bool musicVisible) =>
        musicVisible ? TesseraCoreUiLayoutSpec.InnerRowWidthDip : 0;

    public static double ResolveCoreUiMediaClipWidthDip(
        double fullPanelWidth,
        double progress,
        bool musicVisible)
    {
        if (!musicVisible)
            return fullPanelWidth;
        return fullPanelWidth * ClampProgress(progress);
    }

    public static double ResolveCoreUiMediaSlideOffsetDip(
        double fullPanelWidth,
        double progress,
        bool musicVisible)
    {
        _ = (fullPanelWidth, progress, musicVisible);
        return 0;
    }

    public static double ResolveWin11BorderHeightDip(
        double volumeHeight,
        double mediaHeight,
        double progress,
        bool musicVisible)
    {
        if (!musicVisible)
            return volumeHeight;
        return volumeHeight + mediaHeight * ClampProgress(progress);
    }

    public static double ResolveWin11ShellClipHeightDip(
        double volumeHeight,
        double mediaHeight,
        double progress,
        bool musicVisible)
    {
        if (!musicVisible)
            return volumeHeight;
        return (volumeHeight + mediaHeight) * ClampProgress(progress);
    }

    public static double ResolveWin11VolumeFillOpacityFactor(double progress, bool musicVisible) =>
        musicVisible ? ClampProgress(progress) : 1;

    public static double ResolveWin11MediaClipHeightDip(
        double fullMediaHeight,
        double progress,
        bool musicVisible)
    {
        if (!musicVisible)
            return 0;
        return fullMediaHeight * ClampProgress(progress);
    }

    public static double ResolveWin11MediaOverlayAlpha(double progress, bool musicVisible) =>
        ResolveMediaCoverOverlayAlpha(progress, musicVisible);

    public static double ResolveGnomeContentScale(double progress, bool musicVisible) =>
        musicVisible ? 0.5 + 0.5 * ClampProgress(progress) : 1;

    public static double ResolveGnomeVolumeFillOpacity(double progress, bool musicVisible) =>
        musicVisible ? ClampProgress(progress) : 1;

    public static double ResolveSquareLabelScale(double progress) =>
        ClampProgress(progress);

    public static double ResolveGnomeOverlayAlpha(double progress, bool musicVisible) =>
        ResolveMediaCoverOverlayAlpha(progress, musicVisible);

    public static double ResolvePlainTextSlideOffsetDip(
        double panelWidthDip,
        double layoutScale,
        double progress,
        bool musicVisible)
    {
        if (!musicVisible)
            return 0;
        var inset = panelWidthDip - 25 * layoutScale;
        var p = ClampProgress(progress);
        return -inset + inset * p;
    }

    public static double ResolvePlainTextFillOpacityFactor(double progress, bool musicVisible) =>
        musicVisible ? ClampProgress(progress) : 1;

    public static double ResolveCoreUiVolumeBarScaleFactor(double progress, bool musicVisible) =>
        musicVisible ? ClampProgress(progress) : 1;

    public static double ResolveCoreUiMediaSlideWidthFactor(double progress, bool musicVisible) =>
        musicVisible ? ClampProgress(progress) : 0;

    public static double ResolveCoreUiMediaOverlayAlpha(double progress, bool musicVisible) =>
        ResolveMediaCoverOverlayAlpha(progress, musicVisible);

    public static double ResolveCoreUiMediaContentOpacityFactor(double progress, bool musicVisible) =>
        ResolveClippedMediaLeafOpacity(progress, musicVisible);

    public static double ResolveMeterMediaSlideOffsetDip(double progress, bool musicVisible)
    {
        if (!musicVisible)
            return 0;
        return MeterMediaSlideRestDip * (1 - ClampProgress(progress));
    }

    public static double ResolveCompactMediaSlideOffsetDip(
        double volumeWidthDip,
        double padDip,
        double progress,
        bool musicVisible)
    {
        if (!musicVisible)
            return 0;
        var restDelta = volumeWidthDip / 2 + padDip / 2;
        return restDelta * (ClampProgress(progress) - 1);
    }

    public static double ResolveModernMediaClipHeightDip(
        double fullMediaHeight,
        double progress,
        bool musicVisible)
    {
        if (!musicVisible)
            return 0;
        return fullMediaHeight * ClampProgress(progress);
    }

    public static double ResolveMaterialYouMediaSlideOffsetDip(double progress, bool musicVisible)
    {
        if (!musicVisible)
            return 0;
        return MaterialYouColumnWidthDip * (1 - ClampProgress(progress));
    }

    private static double ClampProgress(double progress) =>
        Math.Clamp(progress, 0, 1);
}
