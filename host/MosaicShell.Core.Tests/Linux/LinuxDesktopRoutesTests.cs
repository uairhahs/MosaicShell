using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Linux;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary>
    /// KDE's shortcut, shell trigger and full-screen routes only exist on Plasma. Elsewhere the services are the inert
    /// ones, so what the Host wires matches what DesktopFeatures says the desktop can do.
    /// </summary>
    public sealed class LinuxDesktopRoutesTests
    {
        [Theory]
        [InlineData("GNOME")]
        [InlineData("Hyprland")]
        [InlineData(null)]
        public void Off_Plasma_there_are_no_KDE_routes(string? desktop)
        {
            using IHotkeyService hotkeys = HostServices.CreateLinuxHotkeys(desktop);
            using IShellFlyoutTriggerSource triggers = HostServices.CreateLinuxShellFlyoutTriggers(desktop);
            IFullscreenProbe fullscreen = HostServices.CreateLinuxFullscreenProbe(desktop);

            _ = hotkeys.Should().BeOfType<NullHotkeyService>();
            _ = triggers.Should().BeOfType<NullShellFlyoutTriggerSource>();
            _ = fullscreen.Should().BeOfType<NullFullscreenProbe>();
        }

        [Fact]
        public void On_Plasma_the_KDE_routes_are_used()
        {
            using IHotkeyService hotkeys = HostServices.CreateLinuxHotkeys("KDE");
            using IShellFlyoutTriggerSource triggers = HostServices.CreateLinuxShellFlyoutTriggers("KDE");
            IFullscreenProbe fullscreen = HostServices.CreateLinuxFullscreenProbe("KDE");

            _ = hotkeys.Should().BeOfType<KGlobalAccelHotkeyService>();
            _ = triggers.Should().BeOfType<KGlobalAccelShellFlyoutTriggers>();
            _ = fullscreen.Should().BeOfType<KWinFullscreenProbe>();
            (fullscreen as IDisposable)?.Dispose();
        }
    }
}
