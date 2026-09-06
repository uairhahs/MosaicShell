using FluentAssertions;
using MosaicShell.Core.Install;

namespace MosaicShell.Core.Tests
{
    public class ModuleInstallerTests : IDisposable
    {
        private readonly string _home;
        private readonly string _repo;

        public ModuleInstallerTests()
        {
            _home = Path.Combine(Path.GetTempPath(), "ms-install-" + Guid.NewGuid().ToString("N"));
            _repo = Path.Combine(Path.GetTempPath(), "ms-repo-" + Guid.NewGuid().ToString("N"));
            AppPaths.SetRootOverride(_home);
            AppPaths.EnsureLayout();
            _ = Directory.CreateDirectory(Path.Combine(_repo, "Tiles", "Canvas"));
            File.WriteAllText(Path.Combine(_repo, "Tiles", "Canvas", "module.native.json"),
                                     /*lang=json,strict*/
                                     """{"id":"Canvas","runtime":"avalonia","capability":false}""");
            File.WriteAllText(Path.Combine(_repo, "Tiles", "Canvas", "README.md"), "canvas tile");
            _ = Directory.CreateDirectory(Path.Combine(_repo, "host"));
            File.WriteAllText(Path.Combine(_repo, "host", "MosaicShell.sln"), "# stub");
        }

        public void Dispose()
        {
            AppPaths.ClearRootOverride();
            try { Directory.Delete(_home, recursive: true); } catch { /* ignore */ }
            try { Directory.Delete(_repo, recursive: true); } catch { /* ignore */ }
        }

        [Fact]
        public async Task Install_from_source_tree_copies_tile_and_writes_module_json()
        {
            ModuleInstaller installer = new();
            List<string> stages = [];
            Progress<ModuleInstallProgress> progress = new(p => stages.Add(p.Stage));

            await installer.InstallAsync("Canvas", progress, sourceTreeRoot: _repo);

            string dest = Path.Combine(AppPaths.ModulesDirectory, "Canvas");
            _ = Directory.Exists(dest).Should().BeTrue();
            _ = File.Exists(Path.Combine(dest, "README.md")).Should().BeTrue();
            _ = File.Exists(Path.Combine(dest, "module.native.json")).Should().BeTrue();
            _ = File.Exists(Path.Combine(dest, "module.json")).Should().BeTrue();
            string json = await File.ReadAllTextAsync(Path.Combine(dest, "module.json"));
            _ = json.Should().Contain("avalonia");
            _ = stages.Should().Contain("local");
            _ = stages.Should().Contain("done");
        }

        [Fact]
        public async Task Install_from_source_tree_accepts_native_capability_stub()
        {
            string tessera = Path.Combine(_repo, "Tiles", "Tessera");
            _ = Directory.CreateDirectory(tessera);
            File.WriteAllText(Path.Combine(tessera, "module.native.json"),
                                     /*lang=json,strict*/
                                     """{"id":"Tessera","runtime":"avalonia","capability":true}""");
            File.WriteAllText(Path.Combine(tessera, "README.md"), "native-only");

            _ = ModuleInstaller.IsNativeModuleStub(tessera).Should().BeTrue();

            ModuleInstaller installer = new();
            List<string?> details = [];
            Progress<ModuleInstallProgress> progress = new(p => details.Add(p.Detail));

            await installer.InstallAsync("Tessera", progress, sourceTreeRoot: _repo);

            string dest = Path.Combine(AppPaths.ModulesDirectory, "Tessera");
            _ = Directory.Exists(dest).Should().BeTrue();
            _ = File.Exists(Path.Combine(dest, "module.native.json")).Should().BeTrue();
            _ = File.Exists(Path.Combine(dest, "module.json")).Should().BeTrue();
            string json = await File.ReadAllTextAsync(Path.Combine(dest, "module.json"));
            _ = json.Should().Contain("avalonia");
            _ = details.Should().Contain(d => d != null && d.Contains("native", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Install_without_local_stub_fails_with_clear_message()
        {
            ModuleInstaller installer = new();
            Func<Task> act = () => installer.InstallAsync(
                "Canvas",
                sourceTreeRoot: Path.Combine(_repo, "empty-missing"));
            _ = await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Tiles/Canvas*module.native.json*");
        }

        [Fact]
        public async Task Install_rejects_non_native_folder_without_marker()
        {
            string fake = Path.Combine(_repo, "Tiles", "FakeMod");
            _ = Directory.CreateDirectory(fake);
            File.WriteAllText(Path.Combine(fake, "README.md"), "not a stub");

            ModuleInstaller installer = new();
            Func<Task> act = () => installer.InstallAsync("FakeMod", sourceTreeRoot: _repo);
            _ = await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task Reinstall_replaces_existing_module_directory()
        {
            ModuleInstaller installer = new();
            await installer.InstallAsync("Canvas", sourceTreeRoot: _repo);
            File.WriteAllText(Path.Combine(AppPaths.ModulesDirectory, "Canvas", "stale.txt"), "old");

            await installer.InstallAsync("Canvas", sourceTreeRoot: _repo);

            _ = File.Exists(Path.Combine(AppPaths.ModulesDirectory, "Canvas", "stale.txt")).Should().BeFalse();
            _ = File.Exists(Path.Combine(AppPaths.ModulesDirectory, "Canvas", "README.md")).Should().BeTrue();
        }

        [Fact]
        public async Task Install_from_release_bundle_layout_without_sln()
        {
            string bundle = Path.Combine(Path.GetTempPath(), "ms-rel-" + Guid.NewGuid().ToString("N"));
            try
            {
                _ = Directory.CreateDirectory(Path.Combine(bundle, "Host"));
                _ = Directory.CreateDirectory(Path.Combine(bundle, "Mosaicist"));
                _ = Directory.CreateDirectory(Path.Combine(bundle, "Tiles", "Canvas"));
                File.WriteAllText(Path.Combine(bundle, "Host", "MosaicShell.Host.exe"), "stub");
                File.WriteAllText(Path.Combine(bundle, "Mosaicist", "Mosaicist.exe"), "stub");
                File.WriteAllText(
                    Path.Combine(bundle, "Tiles", "Canvas", "module.native.json"),
                                         /*lang=json,strict*/
                                         """{"id":"Canvas","runtime":"avalonia"}""");
                File.WriteAllText(Path.Combine(bundle, "Tiles", "Canvas", "README.md"), "from-release");

                _ = ReleaseBundleLayout.IsBundleRoot(bundle).Should().BeTrue();
                _ = ModuleInstaller.FindRepoRoot(Path.Combine(bundle, "Host")).Should().Be(bundle);

                ModuleInstaller installer = new();
                await installer.InstallAsync("Canvas", sourceTreeRoot: bundle);

                _ = File.Exists(Path.Combine(AppPaths.ModulesDirectory, "Canvas", "README.md")).Should().BeTrue();
                _ = (await File.ReadAllTextAsync(Path.Combine(AppPaths.ModulesDirectory, "Canvas", "README.md")))
                    .Should().Be("from-release");
            }
            finally
            {
                try { Directory.Delete(bundle, recursive: true); } catch { /* ignore */ }
            }
        }
    }
}
