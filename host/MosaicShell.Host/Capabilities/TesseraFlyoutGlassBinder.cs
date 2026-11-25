using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Host.Tiles.Tessera;

namespace MosaicShell.Host.Capabilities
{
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
            bool NativeTranslucencyEligible);

        public static Binding ApplyForLiveFlyout(
            bool settingsWantBlur,
            bool nativeTranslucencyEligible = false)
        {
            bool softFrostHwnd = TesseraFlyoutWindowPolicy.SoftFrostHwndReady;
            TesseraFlyoutGlassMode mode = TesseraFlyoutGlassPolicy.ResolveLiveMode(
                softFrostHwnd,
                settingsWantBlur,
                nativeTranslucencyEligible);
            bool useEmbedded = TesseraFlyoutGlassPolicy.ShouldUseEmbeddedPreviewBuild(
                isConfigOrExportPreview: false,
                softFrostHwndReady: softFrostHwnd);
            bool useSimulatedBackdrop = TesseraFlyoutGlassPolicy.ShouldUseSimulatedBackdropBlur(
                softFrostHwnd,
                settingsWantBlur);
            bool allowGdi = TesseraFlyoutGlassPolicy.ShouldAllowGdiScreenCapture(softFrostHwnd, settingsWantBlur);

            TesseraGlass.UseBackdropBlur = useSimulatedBackdrop;
            TesseraGlass.AllowGdiScreenCapture = allowGdi;
            TesseraGlass.UseNativeTranslucencyChrome = mode == TesseraFlyoutGlassMode.NativeTranslucency;
            TesseraGlass.SuppressInnerSkiaGlass = TesseraFlyoutGlassPolicy.SuppressMeterInnerSkiaGlass(mode);

            return new Binding(mode, useEmbedded, softFrostHwnd, useSimulatedBackdrop, allowGdi, nativeTranslucencyEligible);
        }
    }
}
