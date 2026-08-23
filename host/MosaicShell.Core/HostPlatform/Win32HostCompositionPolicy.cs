using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.HostPlatform;

/// <summary>
/// Process-wide Win32 Avalonia composition hints. App bootstrap (<c>Program</c>) reads only this,
/// not Tessera SoftFrost types, so tiles/main window aren't conceptually owned by flyout SoftFrost.
/// SoftFrost maps into <see cref="PreferWinUiComposition"/> via
/// <see cref="TesseraFlyoutWindowPolicy.PreferWinUiCompositionForSoftFrost"/>.
/// </summary>
public static class Win32HostCompositionPolicy
{
    /// <summary>
    /// Prefer WinUIComposition so Transparent HWNDs composite over wallpaper
    /// (DXGI/RedirectionSurface often clears Transparent to black).
    /// </summary>
    public static bool PreferWinUiComposition =>
        TesseraFlyoutWindowPolicy.PreferWinUiCompositionForSoftFrost;

    /// <summary>Pin AngleEgl ahead of Software (H0); required for backdrop corner radius.</summary>
    public const bool PreferAngleEglRendering = true;

    /// <summary>String hints for Host bootstrap; mirrors Avalonia Win32RenderingMode order.</summary>
    public static IReadOnlyList<string> RenderingModeHints =>
        HostLaunchOptions.TesseraForceSoftwareRender
            ? ["Software"]
            : ["AngleEgl", "Software"];

    /// <summary>OsAcrylic needs AngleEgl; Software-only forces frost fallback (H2 row).</summary>
    public static bool OsAcrylicRenderingAvailable =>
        !HostLaunchOptions.TesseraForceSoftwareRender;

    /// <summary>Runtime opt-in from launch flag or persisted Tessera hub setting.</summary>
    public static bool OsAcrylicTrialRequested =>
        TesseraOsAcrylicTrialPolicy.Available
        && TesseraOsAcrylicTrialPolicy.IsTrialRequested()
        && OsAcrylicRenderingAvailable;

    /// <summary>
    /// Process-wide rounded acrylic brushes. Null while trial is off so alpha keeps Skia frost.
    /// </summary>
    public static float? WinUiCompositionBackdropCornerRadius =>
        OsAcrylicTrialRequested ? TesseraOsAcrylicTrialPolicy.SpikeCornerRadius : null;
}
