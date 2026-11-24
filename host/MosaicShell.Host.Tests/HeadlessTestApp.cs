using Avalonia;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Themes.Fluent2;
using MosaicShell.Host.Tests;

[assembly: AvaloniaTestApplication(typeof(HeadlessTestApp))]

namespace MosaicShell.Host.Tests
{
    /// <summary>
    /// The Host's styles without its startup work: no tray, no Hub window, no capability daemon.
    /// Flyouts get the same theme and icon styles they get in the real app.
    /// </summary>
    public sealed class HeadlessTestApp : Application
    {
        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<HeadlessTestApp>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions
                {
                    UseHeadlessDrawing = true,
                    // Since Avalonia 12.1.x (12.1.6 and 12.2.0 both), the UI-thread render timer delivers two
                    // RequestAnimationFrame callbacks and then stops, so flyout motion (frame-driven) never completed
                    // and the baseline replays timed out. The off-thread timer keeps delivering them.
                    ShouldRenderOnUIThread = false,
                });
        }

        public override void Initialize()
        {
            RequestedThemeVariant = ThemeVariant.Dark;
            Styles.Add(new Fluent2Theme());
            // The compiled form of the Host's StyleInclude of MaterialIconStyles.xaml.
            Styles.Add(new Material.Icons.Avalonia.MaterialIconStyles(null));
        }
    }
}
