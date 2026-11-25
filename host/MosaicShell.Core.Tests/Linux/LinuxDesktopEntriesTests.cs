using FluentAssertions;
using MosaicShell.Core.Services.Linux;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary>Finding a browser's install on Linux: its .desktop file, its executable, and a Flatpak's app id.</summary>
    public sealed class LinuxDesktopEntriesTests : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "mosaic-desktop-" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }

        private string Dir(params string[] parts)
        {
            string path = Path.Combine([_root, .. parts]);
            _ = Directory.CreateDirectory(path);
            return path;
        }

        [Fact]
        public void A_desktop_file_is_found_in_any_data_dir_or_the_users_own()
        {
            string system = Dir("usr", "share");
            File.WriteAllText(Path.Combine(Dir("usr", "share", "applications"), "microsoft-edge.desktop"), "");
            string user = Dir("home", ".local", "share");
            File.WriteAllText(Path.Combine(Dir("home", ".local", "share", "applications"), "brave-browser.desktop"), "");

            LinuxDesktopEntries entries = new(user, [system]);

            _ = entries.Exists("microsoft-edge").Should().BeTrue();
            _ = entries.Exists("brave-browser").Should().BeTrue();
            _ = entries.Exists("chromium").Should().BeFalse();
            _ = entries.Exists("../etc/passwd").Should().BeFalse("a name is a file name, never a path");
        }

        [Fact]
        public void A_flatpak_process_names_its_app_id()
        {
            string proc = Dir("proc");
            File.WriteAllText(Path.Combine(Dir("proc", "4242", "root"), ".flatpak-info"), "[Application]\nname=com.microsoft.Edge\nruntime=x\n");
            _ = Dir("proc", "13951", "root");

            _ = LinuxDesktopEntries.FlatpakAppId(4242, proc).Should().Be("com.microsoft.Edge");
            _ = LinuxDesktopEntries.FlatpakAppId(13951, proc).Should().BeNull();
            _ = LinuxDesktopEntries.FlatpakAppId(1, proc).Should().BeNull();
        }

        [Theory]
        [InlineData(null, new[] { "/usr/local/share", "/usr/share" })]
        [InlineData("", new[] { "/usr/local/share", "/usr/share" })]
        [InlineData("/a:/b::relative", new[] { "/a", "/b" })]
        public void The_data_dirs_follow_XDG(string? xdgDataDirs, string[] expected)
        {
            _ = LinuxDesktopEntries.DataDirs(xdgDataDirs).Should().Equal(expected);
        }
    }
}
