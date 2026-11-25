using FluentAssertions;
using MosaicShell.Core.HostPlatform;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The Hub's texts that name the operating system or describe its own pop-ups. They named Windows on every OS; now
    /// each names the system it runs on. Windows reads as before, with "the" added to the accent placeholder.
    /// </summary>
    public sealed class HostPlatformTextTests
    {
        [Theory]
        [InlineData(true, null, "Windows")]
        [InlineData(false, "KDE", "KDE Plasma")]
        [InlineData(false, "ubuntu:GNOME", "GNOME")]
        [InlineData(false, "X-Cinnamon", "Linux")]
        [InlineData(false, null, "Linux")]
        public void The_system_is_named_by_the_desktop_where_it_is_known(bool isWindows, string? desktop, string expected)
        {
            _ = HostPlatformText.NameFor(isWindows, desktop).Should().Be(expected);
        }

        [Fact]
        public void On_Windows_the_texts_name_Windows_and_describe_its_own_pop_up_hiding()
        {
            HostPlatformText windows = HostPlatformText.For(isWindows: true, xdgCurrentDesktop: null);

            _ = windows.About.Should().StartWith("MosaicShell is a native Windows desktop shell.");
            _ = windows.Autostart.Should().Be("Launch MosaicShell Host when you sign in to Windows.");
            _ = windows.AccentPlaceholder.Should().Be("#0273CD or leave blank for the Windows accent");
            _ = windows.ReservedShortcuts.Should().Be(
                "Capture a shortcut (Ctrl/Alt/Shift/Win + key). Win+S / Win+A / Win+Q are reserved by Windows and will be replaced automatically.");
            _ = windows.ReservedShortcut("Win+S").Should().Be("Win+S is reserved by Windows. Pick Ctrl+Alt+Letter instead.");
            _ = windows.NativePopups.Should().StartWith("Stock Windows volume HUD is hidden best-effort");
        }

        [Fact]
        public void On_Linux_the_texts_name_the_desktop()
        {
            HostPlatformText gnome = HostPlatformText.For(isWindows: false, xdgCurrentDesktop: "GNOME");

            _ = gnome.About.Should().StartWith("MosaicShell is a native GNOME desktop shell.");
            _ = gnome.Autostart.Should().Be("Launch MosaicShell Host when you sign in to GNOME.");
            _ = gnome.AccentPlaceholder.Should().Be("#0273CD or leave blank for the GNOME accent");
            _ = gnome.ReservedShortcut("Win+S").Should().Be("Win+S is reserved by GNOME. Pick Ctrl+Alt+Letter instead.");
            _ = gnome.ReservedShortcuts.Should().Contain("Super").And.NotContain("Windows");
        }

        [Fact]
        public void The_pop_up_note_says_how_each_desktop_hides_its_own()
        {
            _ = HostPlatformText.For(false, "KDE").NativePopups.Should().StartWith("Plasma's own volume and brightness pop-ups are hidden");
            _ = HostPlatformText.For(false, "GNOME").NativePopups
                .Should().Contain("GNOME Shell extension").And.Contain("log out and back in");
            _ = HostPlatformText.For(false, null).NativePopups.Should().Contain("not hidden");
        }

        [Fact]
        public void No_Linux_text_names_Windows()
        {
            foreach (string? desktop in new[] { "KDE", "GNOME", null })
            {
                HostPlatformText text = HostPlatformText.For(false, desktop);
                string[] all = [text.About, text.Autostart, text.AccentPlaceholder, text.ReservedShortcuts, text.ReservedShortcut("Win+S"), text.NativePopups];
                _ = all.Should().NotContain(s => s.Contains("Windows"), desktop ?? "no desktop");
            }
        }
    }
}
