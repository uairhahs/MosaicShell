using Avalonia;
using Avalonia.Win32;
using MosaicShell.Core.HostPlatform;

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

        // Neutral host-platform hint (SoftFrost maps into PreferWinUiComposition).
        // Do not import Tessera module types from Program.
        if (Win32HostCompositionPolicy.PreferWinUiComposition)
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
