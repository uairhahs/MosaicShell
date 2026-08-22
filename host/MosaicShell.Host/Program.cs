using Avalonia;
using Avalonia.Win32;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host;

sealed class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect();

        // Soft frost (4fcc41a): WinUIComposition composites Transparent over wallpaper.
        // DXGI/RedirectionSurface often clears Transparent to black. Opaque recovery
        // (PreferWinUiCompositionForSoftFrost=false) should prefer DXGI first instead.
        if (TesseraFlyoutWindowPolicy.PreferWinUiCompositionForSoftFrost)
        {
            builder = builder.With(new Win32PlatformOptions
            {
                CompositionMode =
                [
                    Win32CompositionMode.WinUIComposition,
                    Win32CompositionMode.RedirectionSurface
                ]
            });
        }

        return builder.WithInterFont().LogToTrace();
    }
}
