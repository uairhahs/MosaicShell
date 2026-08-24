namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// Fancy phase-2 layout reveal (TweenNode1 0..1) per YourFlyouts layout Animated meter groups.
/// </summary>
public static class TesseraFlyoutRevealSpec
{
    public const string StyleFluent = "fluent";
    public const string StyleWindows11 = "windows11";
    public const string StyleGnome = "gnome";
    public const string StylePlainText = "plaintext";
    public const string StyleCoreUi = "coreui";
    public const string StyleSquare = "square";
    public const string StyleSmouti = "smouti";

    /// <summary>
    /// YourFlyouts rest pose: TweenNode1 stays at 1 after Fancy until hide.
    /// Hub preview and non-Fancy live must start here, not at animation-start.
    /// </summary>
    public const double RestRevealProgress = 1;

    /// <summary>Ani2 TweenNode1 at Fancy phase-2 start (skin still collapsed).</summary>
    public const double FancyPhase2StartProgress = 0;

    /// <summary>Static Hub / exporter trees never run motion, so they must use rest.</summary>
    public const bool PreviewMustUseRestReveal = true;

    /// <summary>Normalized TweenNode1 (0..1) from stepped tween value 0..100.</summary>
    public static double NormalizeRevealProgress(double tweenNode1) =>
        Math.Clamp(tweenNode1, 0, 100) / 100.0;

    /// <summary>
    /// Initial TweenNode1 for a newly built reveal host.
    /// Preview and non-phase-2 live are rest; live Fancy starts at 0 through phase 1.
    /// </summary>
    public static double ResolveInitialRevealProgress(bool isPreview, bool willRunPhase2)
    {
        if (isPreview && PreviewMustUseRestReveal)
            return RestRevealProgress;
        if (!willRunPhase2)
            return RestRevealProgress;
        return FancyPhase2StartProgress;
    }

    /// <summary>Win11 media clip is live only while Fancy phase 2 is engaged or at rest.</summary>
    public static bool ResolveInitialPhase2Engaged(bool isPreview, bool willRunPhase2) =>
        ResolveInitialRevealProgress(isPreview, willRunPhase2) >= RestRevealProgress;

    /// <summary>
    /// Hide/revive pose. Fancy must start TweenNode1 at 0. Fast and fade keep rest so
    /// the next Show does not flash a collapsed media strip.
    /// </summary>
    public static double ResolveHideRevealProgress(bool willRunPhase2) =>
        willRunPhase2 ? FancyPhase2StartProgress : RestRevealProgress;

    public static bool ResolveHidePhase2Engaged(bool willRunPhase2) =>
        ResolveHideRevealProgress(willRunPhase2) >= RestRevealProgress;

    public static bool StyleSupportsPhase2(string? styleId)
    {
        var id = (styleId ?? string.Empty).ToLowerInvariant();
        return id is StyleFluent or StyleWindows11 or StyleGnome or StylePlainText
            or StyleCoreUi or StyleSquare;
    }

    /// <summary>YF Center.inc Animated fonts run on the volume card without a media strip.</summary>
    public static bool StylePhase2WithoutMediaStrip(string? styleId)
    {
        var id = (styleId ?? string.Empty).ToLowerInvariant();
        return id is StyleSquare;
    }

    /// <summary>Known binders; Host must not take the legacy MaxWidth/MaxHeight path.</summary>
    public static bool UsesDedicatedRevealBinder(string? styleId) =>
        StyleSupportsPhase2(styleId);

    public static bool StyleIsPhase2NoOp(string? styleId) =>
        (styleId ?? string.Empty).Equals(StyleSmouti, StringComparison.OrdinalIgnoreCase);

    /// <summary>Fluent.inc: media width, divider height, overlay alpha scale with TweenNode1.</summary>
    public static double ResolveMediaWidthFactor(string? styleId, double revealProgress, bool musicVisible) =>
        ResolveHorizontalReveal(styleId, revealProgress, musicVisible);

    public static double ResolveDividerScale(string? styleId, double revealProgress, bool musicVisible) =>
        ResolveHorizontalReveal(styleId, revealProgress, musicVisible);

    public static double ResolveMediaOpacity(string? styleId, double revealProgress, bool musicVisible)
    {
        if (!musicVisible)
            return 1;
        var id = (styleId ?? string.Empty).ToLowerInvariant();
        return id switch
        {
            StyleFluent => revealProgress,
            StyleWindows11 => revealProgress,
            StyleGnome => revealProgress,
            StylePlainText => revealProgress,
            StyleCoreUi => revealProgress,
            _ => 1,
        };
    }

    /// <summary>Win11.inc: shell height grows with MusicVisible * TweenNode1.</summary>
    public static double ResolveShellHeightFactor(string? styleId, double revealProgress, bool musicVisible)
    {
        if (!musicVisible)
            return 1;
        var id = (styleId ?? string.Empty).ToLowerInvariant();
        return id switch
        {
            StyleWindows11 => 1, // volume row stable; media clip uses ResolveMediaClipHeightFactor
            StyleGnome => 0.5 + 0.5 * revealProgress,
            _ => 1,
        };
    }

    /// <summary>Win11 media clip height factor (MusicVisible * TweenNode1).</summary>
    public static double ResolveMediaClipHeightFactor(string? styleId, double revealProgress, bool musicVisible)
    {
        if (!musicVisible)
            return 0;
        var id = (styleId ?? string.Empty).ToLowerInvariant();
        return id switch
        {
            StyleWindows11 => revealProgress,
            StyleCoreUi => revealProgress,
            _ => revealProgress,
        };
    }

    /// <summary>Gnome.inc scale (0.5 + 0.5 * TweenNode1).</summary>
    public static double ResolveContentScale(string? styleId, double revealProgress, bool musicVisible)
    {
        if (!musicVisible)
            return 1;
        var id = (styleId ?? string.Empty).ToLowerInvariant();
        return id switch
        {
            StyleGnome => 0.5 + 0.5 * revealProgress,
            _ => 1,
        };
    }

    /// <summary>PlainText.inc whole panel slide-in offset factor (1 - progress).</summary>
    public static double ResolvePlainTextSlideFactor(string? styleId, double revealProgress, bool musicVisible)
    {
        if (!musicVisible)
            return 0;
        if (!(styleId ?? string.Empty).Equals(StylePlainText, StringComparison.OrdinalIgnoreCase))
            return 0;
        return 1 - revealProgress;
    }

    private static double ResolveHorizontalReveal(string? styleId, double revealProgress, bool musicVisible)
    {
        if (!musicVisible)
            return 1;
        var id = (styleId ?? string.Empty).ToLowerInvariant();
        return id switch
        {
            StyleFluent => revealProgress,
            StyleCoreUi => revealProgress,
            _ => 1,
        };
    }
}
