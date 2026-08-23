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
        bool AllowGdiScreenCapture);

    public static Binding ApplyForLiveFlyout(bool settingsWantBlur)
    {
        var softFrostHwnd = TesseraFlyoutWindowPolicy.SoftFrostHwndReady;
        var mode = TesseraFlyoutGlassPolicy.ResolveLiveMode(softFrostHwnd, settingsWantBlur);
        var useEmbedded = TesseraFlyoutGlassPolicy.ShouldUseEmbeddedPreviewBuild(
            isConfigOrExportPreview: false,
            softFrostHwndReady: softFrostHwnd);
        var useBackdrop = TesseraFlyoutGlassPolicy.ShouldEnableBackdropBlur(softFrostHwnd, settingsWantBlur);
        var allowGdi = TesseraFlyoutGlassPolicy.ShouldAllowGdiScreenCapture(softFrostHwnd, settingsWantBlur);

        TesseraGlass.UseBackdropBlur = useBackdrop;
        TesseraGlass.AllowGdiScreenCapture = allowGdi;

        return new Binding(mode, useEmbedded, softFrostHwnd, useBackdrop, allowGdi);
    }
}
