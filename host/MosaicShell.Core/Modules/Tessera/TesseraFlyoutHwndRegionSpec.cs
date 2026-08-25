using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// Win32 region for Fancy media chrome without SetWindowPos.
/// Physical conversion only; Host calls CreateRoundRectRgn / SetWindowRgn.
/// </summary>
public static class TesseraFlyoutHwndRegionSpec
{
    /// <summary>Animate HRGN in place. Do not SizeToContent / SetWindowPos per tick.</summary>
    public const bool Phase2MustAnimateRegionHeightInPlace = true;

    /// <summary>
    /// Rest-sized SoftFrost/acrylic at TweenNode1=0 is the black media bay on show.
    /// Host must clip HWND chrome to the visible reveal, matching the hide wipe.
    /// </summary>
    public const bool Phase2MustClipMediaChromeToRevealProgress = true;

    /// <summary>
    /// Do not skip SetWindowRgn when collapsed. Skipping leaves the rest-sized region
    /// from Relayout (black hole next to volume).
    /// </summary>
    public const bool CollapsedRevealRegionMustHideRestChrome = true;

    /// <summary>
    /// Rest pose must use the signed placement client, never a live Bounds sampled under
    /// an active clip. Feeding the current region back as rest freezes a partial card.
    /// </summary>
    public const bool RestRevealMustUseSignedClientRegion = true;

    /// <summary>
    /// Host must take Max(host progresses), not FirstOrDefault. A newly attached host
    /// at 0 must not collapse an already-open card.
    /// </summary>
    public const bool RevealRegionProgressMustUseMaxHost = true;

    /// <summary>GDI CreateRoundRectRgn right/bottom are exclusive; Host adds this padding.</summary>
    public const int RegionRectInclusivePaddingPx = 1;

    public readonly record struct RevealRegionDip(double WidthDip, double HeightDip, bool HideChrome);

    public static bool IsRestRevealProgress(double progress) =>
        progress >= TesseraFlyoutRevealSpec.RestRevealProgress - 0.001;

    public static bool ShouldForceRestRevealRegion(
        bool motionAnimating,
        bool phase2Animating,
        bool sessionShowing) =>
        TesseraFlyoutAnimationPolicy.IdleShowingSessionMustSnapRevealToRest
        && sessionShowing
        && !motionAnimating
        && !phase2Animating;

    /// <summary>
    /// HWND region progress. Idle showing sessions are rest; in-flight motion uses the
    /// max host so a stale 0 cannot shrink the clip.
    /// </summary>
    public static double ResolveRegionProgress(
        IReadOnlyList<double> hostProgresses,
        bool motionAnimating,
        bool phase2Animating,
        bool sessionShowing)
    {
        if (ShouldForceRestRevealRegion(motionAnimating, phase2Animating, sessionShowing))
            return TesseraFlyoutRevealSpec.RestRevealProgress;

        var max = TesseraFlyoutRevealSpec.FancyPhase2StartProgress;
        var any = false;
        if (hostProgresses is not null)
        {
            foreach (var p in hostProgresses)
            {
                any = true;
                max = Math.Max(max, Math.Clamp(p, 0, 1));
            }
        }

        return any ? max : TesseraFlyoutRevealSpec.FancyPhase2StartProgress;
    }

    /// <summary>Signed placement wins over a clipped live Bounds sample.</summary>
    public static double ResolveRestExtentDip(double signedRestDip, double liveBoundsDip)
    {
        if (signedRestDip > 1)
            return signedRestDip;
        return liveBoundsDip > 1 ? liveBoundsDip : Math.Max(0, signedRestDip);
    }

    public static bool StyleNeedsStrokeBRegion(string? styleId, bool musicVisible)
    {
        if (!musicVisible)
            return false;
        return StyleIds.Normalize(styleId)
            .Equals(StyleIds.Windows11, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Win11 StrokeB plus Fluent/Modern/CoreUI clips and stacked media dissolve styles.</summary>
    public static bool StyleNeedsRevealRegion(
        string? styleId,
        bool musicVisible,
        TesseraStackedPanelRole? stackedRole)
    {
        if (StyleNeedsStrokeBRegion(styleId, musicVisible))
            return true;
        if (!musicVisible)
            return false;
        if (stackedRole == TesseraStackedPanelRole.Volume
            && !TesseraFlyoutTweenTargetCatalog.HasChannel(styleId, TesseraTweenChannel.VolumeFillOpacity)
            && !TesseraFlyoutTweenTargetCatalog.StylePhase2WithoutMediaStrip(styleId))
            return false;

        var id = StyleIds.Normalize(styleId);
        return id.Equals(StyleIds.Fluent, StringComparison.OrdinalIgnoreCase)
               || id.Equals(StyleIds.ModernFlyouts, StringComparison.OrdinalIgnoreCase)
               || id.Equals(StyleIds.CoreUI, StringComparison.OrdinalIgnoreCase)
               || id.Equals(StyleIds.Gnome, StringComparison.OrdinalIgnoreCase)
               || id.Equals(StyleIds.Meter, StringComparison.OrdinalIgnoreCase)
               || id.Equals(StyleIds.Compact, StringComparison.OrdinalIgnoreCase)
               || id.Equals(StyleIds.PlainText, StringComparison.OrdinalIgnoreCase)
               || id.Equals(StyleIds.MaterialYou, StringComparison.OrdinalIgnoreCase)
               || id.Equals(StyleIds.Windows11, StringComparison.OrdinalIgnoreCase);
    }

    public static double ResolveRegionHeightDip(
        double progress,
        bool phase2Engaged,
        bool musicVisible)
    {
        var volume = TesseraStackedPlacementSpec.Win11VolumeHeightDip;
        var media = TesseraStackedPlacementSpec.Win11MediaHeightDip;
        if (!musicVisible)
            return volume;
        if (!phase2Engaged)
            return volume;
        return TesseraFlyoutAnimatedTargetSpec.ResolveWin11BorderHeightDip(
            volume, media, progress, musicVisible);
    }

    public static RevealRegionDip ResolveRevealRegionDip(
        string? styleId,
        TesseraStackedPanelRole? stackedRole,
        double progress,
        bool phase2Engaged,
        bool musicVisible,
        double restWidthDip,
        double restHeightDip)
    {
        var id = StyleIds.Normalize(styleId);
        var restW = restWidthDip > 1 ? restWidthDip : FallbackRestWidth(id, stackedRole);
        var restH = restHeightDip > 1 ? restHeightDip : FallbackRestHeight(id, stackedRole);
        var p = Math.Clamp(progress, 0, 1);

        if (id.Equals(StyleIds.Windows11, StringComparison.OrdinalIgnoreCase))
            return ResolveWin11Region(stackedRole, p, phase2Engaged, musicVisible, restW);

        if (id.Equals(StyleIds.Fluent, StringComparison.OrdinalIgnoreCase))
            return ResolveFluentRegion(stackedRole, p, musicVisible, restW, restH);

        if (id.Equals(StyleIds.ModernFlyouts, StringComparison.OrdinalIgnoreCase))
            return ResolveClipHeightRegion(
                stackedRole,
                TesseraFlyoutAnimatedTargetSpec.ResolveModernMediaClipHeightDip(
                    TesseraStackedPlacementSpec.ModernFlyoutsMediaHeightDip, p, musicVisible),
                restW,
                restH,
                TesseraStackedPlacementSpec.ModernFlyoutsVolumeHeightDip);

        if (id.Equals(StyleIds.CoreUI, StringComparison.OrdinalIgnoreCase))
        {
            var clipW = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaClipWidthDip(
                TesseraCoreUiLayoutSpec.InnerRowWidthDip, p, musicVisible);
            if (stackedRole == TesseraStackedPanelRole.Media || stackedRole is null)
                return new RevealRegionDip(clipW, restH, clipW < 2);
            return new RevealRegionDip(restW, restH, HideChrome: false);
        }

        if (stackedRole == TesseraStackedPanelRole.Volume)
            return new RevealRegionDip(restW, restH, HideChrome: false);

        if (stackedRole == TesseraStackedPanelRole.Media || stackedRole is null)
            return ResolveScaledMediaRegion(p, musicVisible, restW, restH);

        return new RevealRegionDip(restW, restH, HideChrome: false);
    }

    public static (int WidthPx, int HeightPx, int CornerRadiusPx) ResolveRoundRectPhysical(
        double widthDip,
        double heightDip,
        double cornerRadiusDip,
        double monitorScale)
    {
        var scale = monitorScale > 0.1 ? monitorScale : 1.0;
        var widthPx = Math.Max(1, (int)Math.Ceiling(Math.Max(0, widthDip) * scale));
        var heightPx = Math.Max(1, (int)Math.Ceiling(Math.Max(0, heightDip) * scale));
        var radiusPx = Math.Max(1, (int)Math.Round(Math.Max(0, cornerRadiusDip) * scale));
        var cap = Math.Max(1, Math.Min(widthPx, heightPx) / 2);
        radiusPx = Math.Clamp(radiusPx, 1, cap);
        return (widthPx, heightPx, radiusPx);
    }

    private static RevealRegionDip ResolveWin11Region(
        TesseraStackedPanelRole? role,
        double progress,
        bool phase2Engaged,
        bool musicVisible,
        double restWidthDip)
    {
        var volume = TesseraStackedPlacementSpec.Win11VolumeHeightDip;
        var media = TesseraStackedPlacementSpec.Win11MediaHeightDip;
        if (role == TesseraStackedPanelRole.Volume)
            return new RevealRegionDip(restWidthDip, volume, HideChrome: false);
        if (role == TesseraStackedPanelRole.Media)
        {
            var h = TesseraFlyoutAnimatedTargetSpec.ResolveWin11MediaClipHeightDip(
                media, progress, musicVisible);
            return new RevealRegionDip(restWidthDip, h, h < 2);
        }

        var shellH = ResolveRegionHeightDip(progress, phase2Engaged, musicVisible);
        return new RevealRegionDip(restWidthDip, shellH, HideChrome: false);
    }

    private static RevealRegionDip ResolveFluentRegion(
        TesseraStackedPanelRole? role,
        double progress,
        bool musicVisible,
        double restWidthDip,
        double restHeightDip)
    {
        var h = restHeightDip > 1 ? restHeightDip : TesseraFluentLayoutSpec.HeightDip;
        if (role == TesseraStackedPanelRole.Volume)
            return new RevealRegionDip(
                restWidthDip > 1 ? restWidthDip : TesseraFluentLayoutSpec.VolumeWidthDip,
                h,
                HideChrome: false);
        if (role == TesseraStackedPanelRole.Media)
        {
            var mediaRest = restWidthDip > 1 ? restWidthDip : TesseraFluentLayoutSpec.MediaWidthDip;
            var w = mediaRest * progress;
            return new RevealRegionDip(w, h, w < 2);
        }

        var collapsed = TesseraFlyoutAnimatedTargetSpec.ResolveFluentVisibleShellWidthDip(0, musicVisible);
        var rest = restWidthDip > 1
            ? restWidthDip
            : TesseraFlyoutAnimatedTargetSpec.ResolveFluentVisibleShellWidthDip(1, musicVisible);
        var shellW = collapsed + (rest - collapsed) * progress;
        return new RevealRegionDip(shellW, h, HideChrome: false);
    }

    private static RevealRegionDip ResolveClipHeightRegion(
        TesseraStackedPanelRole? role,
        double mediaClipHeightDip,
        double restWidthDip,
        double restHeightDip,
        double volumeHeightDip)
    {
        if (role == TesseraStackedPanelRole.Volume)
            return new RevealRegionDip(restWidthDip, volumeHeightDip, HideChrome: false);
        if (role == TesseraStackedPanelRole.Media)
            return new RevealRegionDip(restWidthDip, mediaClipHeightDip, mediaClipHeightDip < 2);
        var h = volumeHeightDip + mediaClipHeightDip;
        return new RevealRegionDip(restWidthDip, h, HideChrome: false);
    }

    private static RevealRegionDip ResolveScaledMediaRegion(
        double progress,
        bool musicVisible,
        double restWidthDip,
        double restHeightDip)
    {
        if (!musicVisible || progress <= 0)
            return new RevealRegionDip(0, 0, HideChrome: true);
        return new RevealRegionDip(
            restWidthDip * progress,
            restHeightDip * progress,
            HideChrome: progress < 0.02);
    }

    private static double FallbackRestWidth(string styleId, TesseraStackedPanelRole? role)
    {
        if (role == TesseraStackedPanelRole.Volume)
        {
            return styleId.Equals(StyleIds.Fluent, StringComparison.OrdinalIgnoreCase)
                ? TesseraFluentLayoutSpec.VolumeWidthDip
                : 72;
        }

        TesseraFlyoutTweenTargetCatalog.TryResolveMediaRestSizeDip(styleId, out var w, out _);
        return w > 1 ? w : TesseraFluentLayoutSpec.MediaWidthDip;
    }

    private static double FallbackRestHeight(string styleId, TesseraStackedPanelRole? role)
    {
        if (role == TesseraStackedPanelRole.Volume)
            return TesseraFluentLayoutSpec.HeightDip;
        TesseraFlyoutTweenTargetCatalog.TryResolveMediaRestSizeDip(styleId, out _, out var h);
        return h > 1 ? h : TesseraFluentLayoutSpec.HeightDip;
    }
}
