using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Install;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// <c>MosaicShell.Host --install-package &lt;folder or .zip&gt;</c> installs a third-party module and exits, for developers,
    /// scripts and CI; the Hub's Add from file does the same from a picker. It replaces Mosaicist's install-package.
    /// </summary>
    public sealed class PackageInstallCommandTests : IDisposable
    {
        public void Dispose()
        {
            HostLaunchOptions.Apply([]);
        }

        [Fact]
        public void The_flag_takes_the_path_after_it()
        {
            HostLaunchOptions.Apply(["--install-package", "/tmp/HelloTile.zip"]);

            _ = HostLaunchOptions.InstallPackagePath.Should().Be("/tmp/HelloTile.zip");
            _ = HostLaunchOptions.InstallPackageMissingPath.Should().BeFalse();
        }

        [Fact]
        public void The_flag_without_a_path_is_reported_not_ignored()
        {
            HostLaunchOptions.Apply(["--install-package"]);

            _ = HostLaunchOptions.InstallPackagePath.Should().BeNull();
            _ = HostLaunchOptions.InstallPackageMissingPath.Should().BeTrue();
        }

        [Fact]
        public void Without_the_flag_there_is_nothing_to_install()
        {
            HostLaunchOptions.Apply(["--tray-only"]);

            _ = HostLaunchOptions.InstallPackagePath.Should().BeNull();
            _ = HostLaunchOptions.InstallPackageMissingPath.Should().BeFalse();
        }

        [Theory]
        [InlineData("Widget", "Installed HelloTile. It is on the Widgets page.")]
        [InlineData("Capability", "Installed HelloTile. It is on the Modules page.")]
        [InlineData("Hybrid", "Installed HelloTile. It is on the Modules page.")]
        public void A_successful_install_says_where_to_find_it(string kind, string text)
        {
            _ = PackageInstallText.Installed("HelloTile", kind).Should().Be(text);
        }

        [Fact]
        public void A_failed_install_says_why()
        {
            _ = PackageInstallText.Failed("Package must contain module.manifest.json (at root or one level down).")
                .Should().Be("Could not install the package: Package must contain module.manifest.json (at root or one level down).");
            _ = PackageInstallText.MissingPath.Should().Be("--install-package needs the path of a folder or .zip after it.");
        }
    }
}
