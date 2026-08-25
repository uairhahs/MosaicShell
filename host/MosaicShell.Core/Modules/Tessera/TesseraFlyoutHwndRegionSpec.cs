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
    /// Hide can zero stacked-media Opacity at a 1x1 region. Show must not: Avalonia
    /// will not tick TweenNode1 on an Opacity-0 HWND, so CompleteMotion is the first
    /// visible frame (divider and media pop). Hide starts visible, so it can dissolve.
    /// </summary>
    public const bool CollapsedShowRegionMustKeepWindowVisible = true;

    /// <summary>
    /// Win32 CreateRoundRectRgn / Host ApplyRoundRectRegion no-op below 2px. A 1x1
    /// HideChrome call leaves the rest-sized Relayout region in place, so show unhides
    /// a finished acrylic card. Collapsed media must be a renderable strip that can grow.
    /// </summary>
    public const int MinRenderableRegionPx = 2;

    public const double MinRenderableRegionDip = 2;

    public const bool CollapsedMediaRegionMustStayRenderable = true;

    /// <summary>
    /// Hide shrinks HRGN over a client that was already painted at rest. Growing HRGN
    /// as the only interpolator (layout frozen at rest) is a second clock from the
    /// volume divider. Clip and region both follow reveal progress instead.
    /// </summary>
    public const bool ShowStackedMediaMustWipeRegionOverRestLayout = false;

    /// <summary>
    /// Stacked media layout TweenNode1 must track the same p as HWND region and the
    /// volume divider. Do not freeze layout at rest while only HRGN wipes.
    /// </summary>
    public const bool ShowLayoutMustFollowRevealProgress = true;

    /// <summary>
    /// SetWindowRgn bRedraw on a growing wipe discards the rest-sized backing store.
    /// Hide may redraw; show wipe must not.
    /// </summary>
    public const bool ShowRegionWipeMustNotRedrawClient = true;

    public static bool ShouldWipeShowRegionOverRestLayout(
        bool entrance,
        TesseraStackedPanelRole? stackedRole,
        bool willRunPhase2) =>
        ShowStackedMediaMustWipeRegionOverRestLayout
        && !ShowLayoutMustFollowRevealProgress
        && entrance
        && willRunPhase2
        && stackedRole == TesseraStackedPanelRole.Media;

    public static double ResolveShowLayoutRevealProgress(bool wipeRegionOverRest) =>
        wipeRegionOverRest
            ? TesseraFlyoutRevealSpec.RestRevealProgress
            : TesseraFlyoutRevealSpec.FancyPhase2StartProgress;

    public static bool ShouldZeroWindowOpacityForCollapsedRegion(
        bool stackedMedia,
        bool hideChrome,
        bool showMotionActive)
    {
        if (!stackedMedia || !hideChrome)
            return false;
        if (CollapsedShowRegionMustKeepWindowVisible && showMotionActive)
            return false;
        return true;
    }

    /// <summary>
    /// CoreUI MediaC clip is an inner Avalonia wipe. Single-HWND SetWindowRgn must stay
    /// on the rest shell so volume/device tiles are not cropped from the origin.
    /// </summary>
    public const bool CoreUiSingleHwndMustKeepRestRegion = true;

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
            if (stackedRole == TesseraStackedPanelRole.Media)
            {
                var clipW = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaClipWidthDip(
                    TesseraCoreUiLayoutSpec.InnerRowWidthDip, p, musicVisible);
                return HorizontalMediaRegion(clipW, restH);
            }

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

    /// <summary>Never return a region Win32 will ignore. Grow from this floor, do not skip SetWindowRgn.</summary>
    public static (int WidthPx, int HeightPx, int CornerRadiusPx) ResolveRenderableRoundRectPhysical(
        double widthDip,
        double heightDip,
        double cornerRadiusDip,
        double monitorScale)
    {
        var phys = ResolveRoundRectPhysical(widthDip, heightDip, cornerRadiusDip, monitorScale);
        var w = Math.Max(phys.WidthPx, MinRenderableRegionPx);
        var h = Math.Max(phys.HeightPx, MinRenderableRegionPx);
        var cap = Math.Max(1, Math.Min(w, h) / 2);
        var r = Math.Clamp(phys.CornerRadiusPx, 1, cap);
        return (w, h, r);
    }

    private static RevealRegionDip HorizontalMediaRegion(double widthDip, double restHeightDip)
    {
        var h = restHeightDip > 1 ? restHeightDip : MinRenderableRegionDip;
        var w = widthDip < MinRenderableRegionDip ? MinRenderableRegionDip : widthDip;
        return new RevealRegionDip(w, h, HideChrome: false);
    }

    private static RevealRegionDip VerticalMediaRegion(double restWidthDip, double heightDip)
    {
        var w = restWidthDip > 1 ? restWidthDip : MinRenderableRegionDip;
        var h = heightDip < MinRenderableRegionDip ? MinRenderableRegionDip : heightDip;
        return new RevealRegionDip(w, h, HideChrome: false);
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
            return VerticalMediaRegion(restWidthDip, h);
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
            return HorizontalMediaRegion(w, h);
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
            return VerticalMediaRegion(restWidthDip, mediaClipHeightDip);
        var h = volumeHeightDip + mediaClipHeightDip;
        return new RevealRegionDip(restWidthDip, h, HideChrome: false);
    }

    private static RevealRegionDip ResolveScaledMediaRegion(
        double progress,
        bool musicVisible,
        double restWidthDip,
        double restHeightDip)
    {
        if (!musicVisible)
            return HorizontalMediaRegion(0, restHeightDip);
        return HorizontalMediaRegion(restWidthDip * progress, restHeightDip);
    }

    private static double FallbackRestWidth(string styleId, TesseraStackedPanelRole? role)
    {
        var profile = TesseraFlyoutTweenTargetCatalog.ResolveProfile(styleId);
        if (role == TesseraStackedPanelRole.Volume)
            return profile.VolumeWidthDip > 1 ? profile.VolumeWidthDip : TesseraFluentLayoutSpec.VolumeWidthDip;

        return profile.MediaWidthDip > 1 ? profile.MediaWidthDip : TesseraFluentLayoutSpec.MediaWidthDip;
    }

    private static double FallbackRestHeight(string styleId, TesseraStackedPanelRole? role)
    {
        var profile = TesseraFlyoutTweenTargetCatalog.ResolveProfile(styleId);
        if (role == TesseraStackedPanelRole.Volume)
            return profile.VolumeHeightDip > 1 ? profile.VolumeHeightDip : TesseraFluentLayoutSpec.HeightDip;

        return profile.MediaHeightDip > 1 ? profile.MediaHeightDip : TesseraFluentLayoutSpec.HeightDip;
    }
}
