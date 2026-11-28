using FluentAssertions;
using MosaicShell.Core.Install;

namespace MosaicShell.Core.Tests
{
    public class ReleaseBundleLayoutTests : IDisposable
    {
        private readonly string _root;

        public ReleaseBundleLayoutTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "ms-bundle-" + Guid.NewGuid().ToString("N"));
            _ = Directory.CreateDirectory(Path.Combine(_root, "Host"));
            _ = Directory.CreateDirectory(Path.Combine(_root, "Tiles", "Canvas"));
            File.WriteAllText(Path.Combine(_root, "Host", "MosaicShell.Host.exe"), "stub");
            File.WriteAllText(
                Path.Combine(_root, "Tiles", "Canvas", "module.native.json"),
                                     /*lang=json,strict*/
                                     """{"id":"Canvas","runtime":"avalonia"}""");
        }

        public void Dispose()
        {
            try { Directory.Delete(_root, recursive: true); } catch { /* ignore */ }
        }

        [Fact]
        public void IsBundleRoot_true_when_Host_and_Tiles_present()
        {
            _ = ReleaseBundleLayout.IsBundleRoot(_root).Should().BeTrue();
        }

        [Fact]
        public void IsBundleRoot_false_without_Host_exe()
        {
            File.Delete(Path.Combine(_root, "Host", "MosaicShell.Host.exe"));
            _ = ReleaseBundleLayout.IsBundleRoot(_root).Should().BeFalse();
        }

        [Fact]
        public void IsBundleRoot_true_for_a_linux_bundle_whose_executables_have_no_suffix()
        {
            File.Move(Path.Combine(_root, "Host", "MosaicShell.Host.exe"), Path.Combine(_root, "Host", "MosaicShell.Host"));

            _ = ReleaseBundleLayout.IsBundleRoot(_root).Should().BeTrue();
            _ = ReleaseBundleLayout.TryFindRoot(Path.Combine(_root, "Host")).Should().Be(_root);
        }

        [Fact]
        public void IsBundleRoot_false_without_Tiles()
        {
            Directory.Delete(Path.Combine(_root, "Tiles"), recursive: true);
            _ = ReleaseBundleLayout.IsBundleRoot(_root).Should().BeFalse();
        }

        [Fact]
        public void A_bundle_needs_no_Mosaicist()
        {
            // Mosaicist was removed (2026-11-26); the Host installs the default modules itself.
            _ = Directory.Exists(Path.Combine(_root, "Mosaicist")).Should().BeFalse();
            _ = ReleaseBundleLayout.IsBundleRoot(_root).Should().BeTrue();
        }

        [Fact]
        public void TryFindRoot_walks_up_from_Host_subdirectory()
        {
            string fromHost = Path.Combine(_root, "Host");
            _ = ReleaseBundleLayout.TryFindRoot(fromHost).Should().Be(_root);
        }

        [Fact]
        public void TryFindRoot_returns_null_for_unrelated_tree()
        {
            string other = Path.Combine(Path.GetTempPath(), "ms-nobundle-" + Guid.NewGuid().ToString("N"));
            _ = Directory.CreateDirectory(other);
            try
            {
                _ = ReleaseBundleLayout.TryFindRoot(other).Should().BeNull();
            }
            finally
            {
                try { Directory.Delete(other, recursive: true); } catch { /* ignore */ }
            }
        }

        [Fact]
        public void HostFolderName_and_exe_names_match_layout_spec()
        {
            _ = HostInstallLayoutSpec.HostFolder.Should().Be("Host");
            _ = HostInstallLayoutSpec.HostExeName.Should().Be("MosaicShell.Host.exe");
            _ = HostInstallLayoutSpec.TilesFolder.Should().Be("Tiles");
            _ = HostInstallLayoutSpec.SetupExeName.Should().Be("MosaicShell-Setup.exe");
        }
    }
}
