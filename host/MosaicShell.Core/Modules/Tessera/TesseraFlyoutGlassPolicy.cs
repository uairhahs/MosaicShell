namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// Live flyout glass stages vs config/export preview.
/// Skia custom-draw on opaque HWND paints as black slabs, use presentable Border shells
/// until <see cref="TesseraFlyoutWindowPolicy.SoftFrostHwndReady"/>.
/// </summary>
public enum TesseraFlyoutGlassMode
{
    /// <summary>
    /// Solid Border shells, config/export preview, and live flyouts while SoftFrost HWND is not ready.
    /// </summary>
    EmbeddedSimple,

    /// <summary>WinUI AcrylicBlur on Transparent HWND (H1 trial, single-shell only).</summary>
    OsAcrylic,

    /// <summary>Skia glass chrome (Transparent SoftFrost HWND, no live backdrop).</summary>
    SkiaFallback,

    /// <summary>Skia glass + live desktop backdrop (requires SoftFrost Transparent HWND).</summary>
    SkiaBackdrop,
}

public static class TesseraFlyoutGlassPolicy
{
    public const bool PreviewMayUseEmbeddedSimple = true;

    /// <summary>
    /// Live path must not force Skia glass while the window is still opaque, that regression
    /// paints black boxes. Presentable shells until SoftFrost HWND is proven.
    /// </summary>
    public const bool PreferPresentableShellUntilSoftFrostHwnd = true;

    /// <summary>Skia glass on opaque HWND is not presentable (black slabs).</summary>
    public const bool SkiaGlassAllowedWithoutTransparentHwnd = false;

    /// <summary>
    /// GDI BitBlt / shared-surface sampling of "what's behind" self-captures the flyout
    /// (opaque black) and overwrites frost. Soft frost uses Transparent HWND + Skia fake glass
    /// (tint/noise/edge) instead, same practical approach as most Avalonia glass UIs.
    /// Shared-backdrop Host wrap stays as a dormant scaffold for future extensibility;
    /// do not flip this without measured non-blanking pixel usability (size ≠ content).
    /// </summary>
    public const bool ForbidLiveBackdropPixelSampling = true;

    /// <summary>
    /// Glass chrome must measure 0×0 (like 4fcc41a). Claiming available size makes Stretch
    /// tracks/grids expand to the window cell and Y-stretches Amber/CoreUI/Fluent/Win11.
    /// </summary>
    public const bool GlassBackgroundClaimsAvailableSize = false;

    public static bool ShouldUseEmbeddedPreviewBuild(bool isConfigOrExportPreview) =>
        ShouldUseEmbeddedPreviewBuild(isConfigOrExportPreview, TesseraFlyoutWindowPolicy.SoftFrostHwndReady);

    public static bool ShouldUseEmbeddedPreviewBuild(bool isConfigOrExportPreview, bool softFrostHwndReady)
    {
        if (isConfigOrExportPreview && PreviewMayUseEmbeddedSimple)
            return true;

        if (!softFrostHwndReady && PreferPresentableShellUntilSoftFrostHwnd)
            return true;

        return false;
    }

    public static bool ShouldEnableBackdropBlur(bool softFrostHwndReady, bool settingsWantBlur) =>
        softFrostHwndReady
        && settingsWantBlur
        && !ForbidLiveBackdropPixelSampling;

    public static bool ShouldAllowGdiScreenCapture(bool softFrostHwndReady, bool settingsWantBlur) =>
        ShouldEnableBackdropBlur(softFrostHwndReady, settingsWantBlur)
        && !ForbidLiveBackdropPixelSampling;

    public static TesseraFlyoutGlassMode ResolveLiveMode(bool softFrostHwndReady, bool useBackdropBlur) =>
        ResolveLiveMode(softFrostHwndReady, useBackdropBlur, osAcrylicEligible: false);

    public static TesseraFlyoutGlassMode ResolveLiveMode(
        bool softFrostHwndReady,
        bool useBackdropBlur,
        bool osAcrylicEligible)
    {
        if (!softFrostHwndReady && PreferPresentableShellUntilSoftFrostHwnd)
            return TesseraFlyoutGlassMode.EmbeddedSimple;

        if (osAcrylicEligible)
            return TesseraFlyoutGlassMode.OsAcrylic;

        if (ShouldEnableBackdropBlur(softFrostHwndReady, useBackdropBlur))
            return TesseraFlyoutGlassMode.SkiaBackdrop;

        return TesseraFlyoutGlassMode.SkiaFallback;
    }

    /// <summary>
    /// Meter (Amber) wraps the pill and media card in inner <c>TesseraChrome.Glass</c> panels.
    /// On live SoftFrost/OsAcrylic flyouts that stacks Skia layers on the outer shell and
    /// produces GPU shimmer/artifacting until the compositor dies.
    /// </summary>
    public static bool SuppressMeterInnerSkiaGlass(TesseraFlyoutGlassMode mode) =>
        mode is TesseraFlyoutGlassMode.SkiaFallback
            or TesseraFlyoutGlassMode.OsAcrylic
            or TesseraFlyoutGlassMode.SkiaBackdrop;
}
