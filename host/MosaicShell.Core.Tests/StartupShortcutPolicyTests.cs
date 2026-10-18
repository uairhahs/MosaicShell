using FluentAssertions;
using MosaicShell.Core.Install;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The .url files the installer writes for the Start menu and logon. Only the path-taking members are tested; the
    /// Start-menu and Startup wrappers write into the real user profile.
    /// </summary>
    public class StartupShortcutPolicyTests : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "MosaicShortcut_" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            try { Directory.Delete(_root, true); } catch { /* ignore */ }
        }

        [Fact]
        public void A_shortcut_points_at_the_exe_with_forward_slashes_and_uses_it_as_its_icon()
        {
            string exe = Path.Combine(_root, "app", "MosaicShell.Host.exe");
            string normalized = Path.GetFullPath(exe).Replace('\\', '/');

            string text = StartupShortcutPolicy.FormatUrlShortcut(exe);

            text.Should().Be($"[InternetShortcut]\r\nURL=file:///{normalized}\r\nIconIndex=0\r\nIconFile={normalized}\r\n");
        }

        [Theory]
        [InlineData("--tray-only", " --tray-only")]
        [InlineData("  --tray-only  ", " --tray-only")]
        public void Arguments_follow_the_exe_trimmed(string arguments, string expectedSuffix)
        {
            string exe = Path.Combine(_root, "Host.exe");
            string normalized = Path.GetFullPath(exe).Replace('\\', '/');

            StartupShortcutPolicy.FormatUrlShortcut(exe, arguments)
                .Should().Contain($"URL=file:///{normalized}{expectedSuffix}\r\n")
                .And.Contain($"IconFile={normalized}\r\n", "the icon is the exe, never the arguments");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_arguments_add_nothing(string? arguments)
        {
            string exe = Path.Combine(_root, "Host.exe");

            StartupShortcutPolicy.FormatUrlShortcut(exe, arguments)
                .Should().Contain($"URL=file:///{Path.GetFullPath(exe).Replace('\\', '/')}\r\n");
        }

        [Fact]
        public void Writing_a_host_shortcut_creates_missing_folders()
        {
            string shortcut = Path.Combine(_root, "a", "b", "Host.url");

            StartupShortcutPolicy.WriteHostShortcut(shortcut, Path.Combine(_root, "Host.exe"));

            File.Exists(shortcut).Should().BeTrue();
            File.ReadAllText(shortcut).Should().StartWith("[InternetShortcut]");
        }

        [Fact]
        public void A_tray_only_shortcut_carries_the_tray_only_argument_and_a_normal_one_does_not()
        {
            string trayOnly = Path.Combine(_root, "tray.url");
            string normal = Path.Combine(_root, "normal.url");

            StartupShortcutPolicy.WriteHostShortcut(trayOnly, Path.Combine(_root, "Host.exe"), trayOnly: true);
            StartupShortcutPolicy.WriteHostShortcut(normal, Path.Combine(_root, "Host.exe"), trayOnly: false);

            File.ReadAllText(trayOnly).Should().Contain(HostInstallPolicy.TrayOnlyArg);
            File.ReadAllText(normal).Should().NotContain(HostInstallPolicy.TrayOnlyArg);
        }

        [Fact]
        public void Writing_over_an_existing_shortcut_replaces_it()
        {
            string shortcut = Path.Combine(_root, "Host.url");
            StartupShortcutPolicy.WriteHostShortcut(shortcut, Path.Combine(_root, "old", "Host.exe"));

            StartupShortcutPolicy.WriteHostShortcut(shortcut, Path.Combine(_root, "new", "Host.exe"));

            File.ReadAllText(shortcut).Should().Contain("/new/Host.exe").And.NotContain("/old/");
        }
    }
}
