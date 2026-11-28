using MosaicShell.Core.Install;
using Avalonia;
using Avalonia.Threading;
using MosaicShell.Core;
using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.Services;

namespace MosaicShell.Host
{
    internal sealed class Program
    {
        /// <summary>
        /// Whether the data folder held saved settings when this process started, read before anything here writes one
        /// (detecting the native blur saves Tessera's settings at once). First run hangs on it (<see cref="FirstRunPolicy"/>).
        /// </summary>
        internal static bool SettingsSavedAtStart { get; private set; } = true;

        [STAThread]
        public static int Main(string[] args)
        {
            Core.Capabilities.Platform.HostLaunchOptions.Apply(args);
            SettingsSavedAtStart = FirstRunPolicy.AnySettingsSaved(AppPaths.ConfigDirectory);
            if (Core.Capabilities.Platform.HostLaunchOptions.InstallPackageMissingPath
                || Core.Capabilities.Platform.HostLaunchOptions.InstallPackagePath is not null)
            {
                return HostCommandLine.InstallPackage(Core.Capabilities.Platform.HostLaunchOptions.InstallPackagePath);
            }

            _ = HostServices.DetectNativeTranslucencyAsync();
            SystemAccent.StartDesktopWatch();
            DiagnosticLog.Start(
                AppPaths.CacheDirectory,
                DiagnosticLogLevels.Resolve(Environment.GetEnvironmentVariable(DiagnosticLogLevels.EnvironmentVariable), IsDebugBuild));
            HostSingleInstance? instance = HostSingleInstance.TryAcquire(AppPaths.RootDirectory);
            if (instance is null)
            {
                DeferToRunningHost();
                return 0;
            }

            instance.ActivationRequested += static (_, _) =>
                Dispatcher.UIThread.Post(static () => (Application.Current as App)?.ShowHubOnRequest());
            instance.StartListening();

            Capabilities.ProcessPowerThrottling.OptOutAndLog();
            try
            {
                _ = BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            finally
            {
                instance.Dispose();
                DiagnosticLog.Stop();
            }

            return 0;
        }

        /// <summary>
        /// Another Host already owns this data folder: a launch for the Hub brings its Hub forward, a tray-only launch
        /// (the login entry) leaves it be, and this process exits without starting a second set of capabilities.
        /// </summary>
        private static void DeferToRunningHost()
        {
            Core.Capabilities.Platform.HostLaunchMode mode = Core.Capabilities.Platform.HostLaunchOptions.Mode;
            bool shown = HostSingleInstance.ShouldSignal(mode)
                && HostSingleInstance.SignalPrimary(AppPaths.RootDirectory, TimeSpan.FromSeconds(2));
            DiagnosticLog.Append("host.log", DiagnosticLogLevel.Info, $"another Host owns {AppPaths.RootDirectory}; mode={mode} asked it to show the Hub={shown}; exiting");
            DiagnosticLog.Stop();
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
