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
}
