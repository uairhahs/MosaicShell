using FluentAssertions;
using MosaicShell.Core.Services.Xdg;

namespace MosaicShell.Core.Tests
{
    /// <summary>The XDG autostart entry that starts the Host at login, as Windows' Startup folder shortcut does.</summary>
    public sealed class XdgAutostartPolicyTests
    {
        [Theory]
        [InlineData("/home/me/.cfg", "/home/me", "/home/me/.cfg/autostart/mosaicshell.desktop")]
        [InlineData(null, "/home/me", "/home/me/.config/autostart/mosaicshell.desktop")]
        [InlineData("", "/home/me/", "/home/me/.config/autostart/mosaicshell.desktop")]
        [InlineData("relative/cfg", "/home/me", "/home/me/.config/autostart/mosaicshell.desktop")]
        public void The_entry_lives_in_the_user_autostart_folder(string? xdgConfigHome, string home, string expected)
        {
            // The XDG base directory spec ignores a relative XDG_CONFIG_HOME.
            _ = XdgAutostartPolicy.EntryPath(xdgConfigHome, home).Should().Be(expected);
        }

        [Fact]
        public void A_native_host_starts_itself()
        {
            _ = XdgAutostartPolicy.ExecLine("/opt/MosaicShell/MosaicShell.Host", "/opt/MosaicShell/MosaicShell.Host.dll")
                .Should().Be("\"/opt/MosaicShell/MosaicShell.Host\"");
        }

        [Fact]
        public void A_host_run_by_dotnet_starts_dotnet_with_its_assembly()
        {
            // Under `dotnet MosaicShell.Host.dll` the process path is dotnet itself, which alone would start nothing.
            _ = XdgAutostartPolicy.ExecLine("/usr/bin/dotnet", "/opt/MosaicShell/MosaicShell.Host.dll")
                .Should().Be("\"/usr/bin/dotnet\" \"/opt/MosaicShell/MosaicShell.Host.dll\"");
        }

        [Fact]
        public void Paths_are_quoted_and_escaped_as_the_desktop_entry_spec_requires()
        {
            // Inside quotes ", `, $ and \ take a backslash; the value's own escaping then doubles each backslash.
            // A literal % is written %% so it is not read as a field code.
            _ = XdgAutostartPolicy.ExecLine("/opt/My Apps/$x`y\"z\\100%/MosaicShell.Host", null)
                .Should().Be("\"/opt/My Apps/\\\\$x\\\\`y\\\\\"z\\\\\\\\100%%/MosaicShell.Host\"");
        }

        [Fact]
        public void The_entry_is_an_application_that_runs_at_login()
        {
            string entry = XdgAutostartPolicy.EntryText("\"/opt/MosaicShell/MosaicShell.Host\"");
            _ = entry.Should().StartWith("[Desktop Entry]\n");
            _ = entry.Should().Contain("\nType=Application\n");
            _ = entry.Should().Contain("\nName=MosaicShell\n");
            _ = entry.Should().Contain("\nExec=\"/opt/MosaicShell/MosaicShell.Host\"\n");
            _ = XdgAutostartPolicy.IsEnabledEntry(entry).Should().BeTrue();
        }

        [Theory]
        [InlineData("[Desktop Entry]\nType=Application\nExec=x\nHidden=true\n", false)]
        [InlineData("[Desktop Entry]\nType=Application\nExec=x\nHidden=false\n", true)]
        [InlineData("[Desktop Entry]\nType=Application\nExec=x\nX-GNOME-Autostart-enabled=false\n", false)]
        [InlineData("[Desktop Entry]\nType=Application\nExec=x\n", true)]
        [InlineData("[Other Group]\nHidden=true\n[Desktop Entry]\nExec=x\n", true)]
        [InlineData(null, false)]
        public void An_entry_the_desktop_disabled_reads_as_off(string? entry, bool enabled)
        {
            // Plasma's and GNOME's settings switch an entry off in place rather than always deleting it.
            _ = XdgAutostartPolicy.IsEnabledEntry(entry).Should().Be(enabled);
        }
    }
}
