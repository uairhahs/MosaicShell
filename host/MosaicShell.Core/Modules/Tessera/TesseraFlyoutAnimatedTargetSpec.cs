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

    /// <summary>YF MediaC grows as a container; Host clips in place (no leaf slide or fade).</summary>
    public const bool CoreUiMediaMustClipOnly = true;

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

    public static double ResolveMediaMaskOpacity(double progress, bool musicVisible) =>
        musicVisible ? ClampProgress(progress) : 0;

    /// <summary>Fluent StrokeB static width when music visible (not Animated).</summary>
    public static double ResolveFluentShellWidthDip(
        double volumeWidth,
        double mediaWidth,
        bool musicVisible) =>
        musicVisible ? volumeWidth + mediaWidth : volumeWidth;

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

    public static double ResolveCoreUiMediaContentOpacityFactor(double progress, bool musicVisible)
    {
        _ = (progress, musicVisible);
        return 1;
    }

    private static double ClampProgress(double progress) =>
        Math.Clamp(progress, 0, 1);
}
