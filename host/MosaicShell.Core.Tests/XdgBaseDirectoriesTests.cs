using FluentAssertions;
using MosaicShell.Core.Services.Xdg;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// XDG_CONFIG_HOME and XDG_DATA_HOME as the base directory spec reads them: an absolute value, else the home
    /// folder's default. The spec ignores a relative value. One rule for every Linux route that keeps a file per user
    /// (browser manifests, autostart, desktop entries, plasma-nm, the GNOME extension).
    /// </summary>
    public sealed class XdgBaseDirectoriesTests
    {
        [Theory]
        [InlineData("/custom/config", "/home/u", "/custom/config")]
        [InlineData("/x/cfg/", "/home/u", "/x/cfg")]
        [InlineData(null, "/home/u", "/home/u/.config")]
        [InlineData("", "/home/u/", "/home/u/.config")]
        [InlineData("relative/config", "/home/u", "/home/u/.config")]
        public void The_config_home_is_an_absolute_XDG_CONFIG_HOME_or_dot_config(string? xdg, string home, string expected)
        {
            _ = XdgBaseDirectories.ConfigHome(xdg, home).Should().Be(expected);
        }

        [Theory]
        [InlineData("/data", "/home/u", "/data")]
        [InlineData("/data/", "/home/u", "/data")]
        [InlineData(null, "/home/u", "/home/u/.local/share")]
        [InlineData("relative", "/home/u/", "/home/u/.local/share")]
        public void The_data_home_is_an_absolute_XDG_DATA_HOME_or_dot_local_share(string? xdg, string home, string expected)
        {
            _ = XdgBaseDirectories.DataHome(xdg, home).Should().Be(expected);
        }
    }
}
