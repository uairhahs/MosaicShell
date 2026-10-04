using Avalonia;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
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
                .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true });
        }

        public override void Initialize()
        {
            RequestedThemeVariant = ThemeVariant.Dark;
            Styles.Add(new FluentTheme());
            // The compiled form of the Host's StyleInclude of MaterialIconStyles.xaml.
            Styles.Add(new Material.Icons.Avalonia.MaterialIconStyles(null));
        }
    }
}
