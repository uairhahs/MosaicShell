namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// Soft-frost HWND + presentability. Soft frost uses Transparent composition (4fcc41a);
/// opaque tool window was the empty-HWND recovery path and stays available if SoftFrost is flipped off.
/// Dim is the opposite (layered wash, fallback 0).
/// </summary>
public static class TesseraFlyoutWindowPolicy
{
    /// <summary>Transparent HWND + Skia frost (4fcc41a glass). Flip false only if empty-HWND returns.</summary>
    public const bool SoftFrostHwndReady = true;

    /// <summary>When SoftFrost is off, request WindowTransparencyLevel.None.</summary>
    public const bool MustRequestOpaqueToolWindow = !SoftFrostHwndReady;

    /// <summary>Transparent window brush when SoftFrost HWND is on.</summary>
    public const bool WindowBackgroundBrushIsTransparent = SoftFrostHwndReady;

    /// <summary>Never wrap flyout content in a Host debug title / style banner.</summary>
    public const bool ForbidDebugTitleChrome = true;

    /// <summary>
    /// LWA_ALPHA=255 forces an opaque layered window and kills soft frost.
    /// Only apply when SoftFrost HWND is off (opaque recovery recipe).
    /// </summary>
    public const bool MustApplyPresentableLayeredAlpha = !SoftFrostHwndReady;

    public const byte PresentableLayeredAlpha = 255;

    /// <summary>Opaque-recovery shell floor — empty-HWND regression if SoftFrost is off.</summary>
    public const byte MinPresentableFallbackAlpha = 170;

    /// <summary>
    /// Soft frost must not use a mocha composition fallback (≥170 paints a matte slab behind glass).
    /// 4fcc41a had no TransparencyBackgroundFallback fill.
    /// </summary>
    public const byte SoftFrostCompositionFallbackAlpha = 0;

    /// <summary>
    /// Soft frost needs WinUIComposition so Transparent HWND composites wallpaper
    /// (DXGI/RedirectionSurface often clears Transparent to black).
    /// Opaque recovery prefers DXGI so hint=None stays presentable.
    /// </summary>
    public const bool PreferWinUiCompositionForSoftFrost = SoftFrostHwndReady;

    /// <summary>
    /// SoftFrost Transparent HWND paints solid black for 1+ frames until composition
    /// settles (cold Show / Try now). Host must Show at Opacity 0, finish layout, then reveal.
    /// </summary>
    public static bool HideUntilCompositionReady => SoftFrostHwndReady;

    /// <summary>
    /// Posted Opacity=1 reveals must carry a generation so rapid ApplyRequest / Try now
    /// invalidates stale reveals (otherwise SoftFrost flashes stacked clear frames).
    /// </summary>
    public static bool RevealMustBeGenerationGated => HideUntilCompositionReady;

    public static IReadOnlyList<string> ResolveTransparencyHints(TesseraFlyoutMaterial material) =>
        MustRequestOpaqueToolWindow ? ["None"] : material.TransparencyHints;

    /// <summary>Opaque-window shell alpha (recovery path).</summary>
    public static byte ResolveWindowBackgroundAlpha(TesseraFlyoutMaterial material)
    {
        var alpha = material.ShellAlpha;
        return alpha < MinPresentableFallbackAlpha ? MinPresentableFallbackAlpha : alpha;
    }

    /// <summary>Avalonia TransparencyBackgroundFallback alpha — 0 when SoftFrost HWND is on.</summary>
    public static byte ResolveCompositionFallbackAlpha(TesseraFlyoutMaterial material) =>
        SoftFrostHwndReady
            ? SoftFrostCompositionFallbackAlpha
            : ResolveWindowBackgroundAlpha(material);
}
