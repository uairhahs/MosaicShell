using FluentAssertions;
using MosaicShell.Core;
using MosaicShell.Core.Install;

namespace MosaicShell.Core.Tests;

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
        Directory.CreateDirectory(Path.Combine(_repo, "Tiles", "Canvas"));
        File.WriteAllText(Path.Combine(_repo, "Tiles", "Canvas", "module.native.json"),
            """{"id":"Canvas","runtime":"avalonia","capability":false}""");
        File.WriteAllText(Path.Combine(_repo, "Tiles", "Canvas", "README.md"), "canvas tile");
        Directory.CreateDirectory(Path.Combine(_repo, "host"));
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
        var installer = new ModuleInstaller();
        var stages = new List<string>();
        var progress = new Progress<ModuleInstallProgress>(p => stages.Add(p.Stage));

        await installer.InstallAsync("Canvas", progress, sourceTreeRoot: _repo);

        var dest = Path.Combine(AppPaths.ModulesDirectory, "Canvas");
        Directory.Exists(dest).Should().BeTrue();
        File.Exists(Path.Combine(dest, "README.md")).Should().BeTrue();
        File.Exists(Path.Combine(dest, "module.native.json")).Should().BeTrue();
        File.Exists(Path.Combine(dest, "module.json")).Should().BeTrue();
        var json = await File.ReadAllTextAsync(Path.Combine(dest, "module.json"));
        json.Should().Contain("avalonia");
        stages.Should().Contain("local");
        stages.Should().Contain("done");
    }

    [Fact]
    public async Task Install_from_source_tree_accepts_native_capability_stub()
    {
        var tessera = Path.Combine(_repo, "Tiles", "Tessera");
        Directory.CreateDirectory(tessera);
        File.WriteAllText(Path.Combine(tessera, "module.native.json"),
            """{"id":"Tessera","runtime":"avalonia","capability":true}""");
        File.WriteAllText(Path.Combine(tessera, "README.md"), "native-only");

        ModuleInstaller.IsNativeModuleStub(tessera).Should().BeTrue();

        var installer = new ModuleInstaller();
        var details = new List<string?>();
        var progress = new Progress<ModuleInstallProgress>(p => details.Add(p.Detail));

        await installer.InstallAsync("Tessera", progress, sourceTreeRoot: _repo);

        var dest = Path.Combine(AppPaths.ModulesDirectory, "Tessera");
        Directory.Exists(dest).Should().BeTrue();
        File.Exists(Path.Combine(dest, "module.native.json")).Should().BeTrue();
        File.Exists(Path.Combine(dest, "module.json")).Should().BeTrue();
        var json = await File.ReadAllTextAsync(Path.Combine(dest, "module.json"));
        json.Should().Contain("avalonia");
        details.Should().Contain(d => d != null && d.Contains("native", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Install_without_local_stub_fails_with_clear_message()
    {
        var installer = new ModuleInstaller();
        var act = () => installer.InstallAsync(
            "Canvas",
            sourceTreeRoot: Path.Combine(_repo, "empty-missing"));
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Tiles/Canvas*module.native.json*");
    }

    [Fact]
    public async Task Install_rejects_non_native_folder_without_marker()
    {
        var fake = Path.Combine(_repo, "Tiles", "FakeMod");
        Directory.CreateDirectory(fake);
        File.WriteAllText(Path.Combine(fake, "README.md"), "not a stub");

        var installer = new ModuleInstaller();
        var act = () => installer.InstallAsync("FakeMod", sourceTreeRoot: _repo);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Reinstall_replaces_existing_module_directory()
    {
        var installer = new ModuleInstaller();
        await installer.InstallAsync("Canvas", sourceTreeRoot: _repo);
        File.WriteAllText(Path.Combine(AppPaths.ModulesDirectory, "Canvas", "stale.txt"), "old");

        await installer.InstallAsync("Canvas", sourceTreeRoot: _repo);

        File.Exists(Path.Combine(AppPaths.ModulesDirectory, "Canvas", "stale.txt")).Should().BeFalse();
        File.Exists(Path.Combine(AppPaths.ModulesDirectory, "Canvas", "README.md")).Should().BeTrue();
    }
}
