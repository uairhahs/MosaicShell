using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Host.Tiles.Tessera;

namespace MosaicShell.Host.Capabilities;

/// <summary>
/// Applies <see cref="TesseraFlyoutGlassPolicy"/> to Host <see cref="TesseraGlass"/> statics
/// so <see cref="AvaloniaFlyoutPresenter"/> BuildContent stays thin.
/// </summary>
internal static class TesseraFlyoutGlassBinder
{
    public readonly record struct Binding(
        TesseraFlyoutGlassMode Mode,
        bool UseEmbeddedPreview,
        bool SoftFrostHwndReady,
        bool UseBackdropBlur,
        bool AllowGdiScreenCapture,
        bool OsAcrylicEligible);

    public static Binding ApplyForLiveFlyout(
        bool settingsWantBlur,
        bool osAcrylicEligible = false)
    {
        var softFrostHwnd = TesseraFlyoutWindowPolicy.SoftFrostHwndReady;
        var mode = TesseraFlyoutGlassPolicy.ResolveLiveMode(
            softFrostHwnd,
            settingsWantBlur,
            osAcrylicEligible);
        var useEmbedded = TesseraFlyoutGlassPolicy.ShouldUseEmbeddedPreviewBuild(
            isConfigOrExportPreview: false,
            softFrostHwndReady: softFrostHwnd);
        var useSimulatedBackdrop = TesseraFlyoutGlassPolicy.ShouldUseSimulatedBackdropBlur(
            softFrostHwnd,
            settingsWantBlur);
        var allowGdi = TesseraFlyoutGlassPolicy.ShouldAllowGdiScreenCapture(softFrostHwnd, settingsWantBlur);

        TesseraGlass.UseBackdropBlur = useSimulatedBackdrop;
        TesseraGlass.AllowGdiScreenCapture = allowGdi;
        TesseraGlass.UseOsAcrylicChrome = mode == TesseraFlyoutGlassMode.OsAcrylic;
        TesseraGlass.SuppressInnerSkiaGlass = TesseraFlyoutGlassPolicy.SuppressMeterInnerSkiaGlass(mode);

        return new Binding(mode, useEmbedded, softFrostHwnd, useSimulatedBackdrop, allowGdi, osAcrylicEligible);
    }
}
