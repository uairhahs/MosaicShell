using Avalonia;
using MosaicShell.Core;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.Services;

namespace MosaicShell.Host
{
    internal sealed class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            Core.Capabilities.Platform.HostLaunchOptions.Apply(args);
            DiagnosticLog.Start(
                AppPaths.CacheDirectory,
                DiagnosticLogLevels.Resolve(Environment.GetEnvironmentVariable(DiagnosticLogLevels.EnvironmentVariable), IsDebugBuild));
            try
            {
                _ = BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            finally
            {
                DiagnosticLog.Stop();
            }

            return 0;
        }

#if DEBUG
        private const bool IsDebugBuild = true;
#else
        private const bool IsDebugBuild = false;
#endif

        public static AppBuilder BuildAvaloniaApp()
        {
            AppBuilder builder = AppBuilder.Configure<App>()
                .UsePlatformDetect();

            // Neutral host-platform hints (SoftFrost maps into PreferWinUiComposition).
            // Do not import Tessera module types from Program.
            if (Win32HostCompositionPolicy.PreferWinUiComposition
                || Win32HostCompositionPolicy.PreferAngleEglRendering)
            {
                builder = builder.With(new Win32PlatformOptions
                {
                    CompositionMode =
                    [
                        Win32CompositionMode.WinUIComposition,
                        Win32CompositionMode.RedirectionSurface
                    ],
                    RenderingMode = [.. Win32HostCompositionPolicy.RenderingModeHints
                        .Select(static hint => hint switch
                        {
                            "AngleEgl" => Win32RenderingMode.AngleEgl,
                            "Software" => Win32RenderingMode.Software,
                            _ => throw new InvalidOperationException($"Unknown rendering hint: {hint}")
                        })],
                    WinUICompositionBackdropCornerRadius =
                        Win32HostCompositionPolicy.WinUiCompositionBackdropCornerRadius
                });
            }

            return builder.WithInterFont().LogToTrace();
        }
    }
}
