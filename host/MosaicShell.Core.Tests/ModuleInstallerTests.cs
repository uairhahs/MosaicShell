using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
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
        File.WriteAllText(Path.Combine(_repo, "Tiles", "Canvas", "README.md"), "canvas tile");
        File.WriteAllText(Path.Combine(_repo, "RunMosaicist.ps1"), "# stub");
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
        File.Exists(Path.Combine(dest, "module.json")).Should().BeTrue();
        stages.Should().Contain("local");
        stages.Should().Contain("done");
        stages.Should().NotContain("download");
    }

    [Fact]
    public async Task Install_from_rmskin_package_extracts_Skins_folder()
    {
        var package = CreateFakeRmskin("Tessera");
        var installer = new ModuleInstaller();

        await installer.InstallPackageAsync(package, "Tessera");

        var dest = Path.Combine(AppPaths.ModulesDirectory, "Tessera");
        File.Exists(Path.Combine(dest, "Main", "Main.ini")).Should().BeTrue();
        File.Exists(Path.Combine(dest, "module.json")).Should().BeTrue();
    }

    [Fact]
    public async Task Install_from_rmskin_with_Tiles_nesting_resolves_module()
    {
        var package = CreateFakeRmskinNestedTiles("Chrono");
        var installer = new ModuleInstaller();

        await installer.InstallPackageAsync(package, "Chrono");

        File.Exists(Path.Combine(AppPaths.ModulesDirectory, "Chrono", "Main.ini")).Should().BeTrue();
    }

    [Fact]
    public async Task Install_reports_progress_stages_in_order_for_package()
    {
        var package = CreateFakeRmskin("Pulse");
        var stages = new List<string>();
        var progress = new Progress<ModuleInstallProgress>(p => stages.Add(p.Stage));
        var installer = new ModuleInstaller();

        await installer.InstallPackageAsync(package, "Pulse", progress);

        stages.Should().ContainInOrder("extract", "copy", "done");
    }

    [Fact]
    public async Task Install_from_github_uses_http_client_without_script_execution()
    {
        var handler = new ScriptRecordingHandler();
        var packageBytes = await File.ReadAllBytesAsync(CreateFakeRmskin("Phono"));
        handler.Map(
            "https://api.github.com/repos/uairhahs/Phono/releases/latest",
            """
            {"assets":[{"name":"Phono.rmskin","browser_download_url":"https://example.test/Phono.rmskin"}]}
            """);
        handler.MapBinary("https://example.test/Phono.rmskin", packageBytes);

        var installer = new ModuleInstaller(new HttpClient(handler));
        // Force remote path: empty source tree
        await installer.InstallAsync("Phono", sourceTreeRoot: Path.Combine(_repo, "empty-missing"));

        ModuleCatalogIsInstalled("Phono").Should().BeTrue();
        handler.RequestedUrls.Should().Contain(u => u.Contains("api.github.com"));
        handler.RequestedUrls.Should().Contain("https://example.test/Phono.rmskin");
        handler.InvokedScripts.Should().BeEmpty();
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

    private static bool ModuleCatalogIsInstalled(string id) =>
        MosaicShell.Core.Modules.ModuleCatalog.IsInstalled(id);

    private string CreateFakeRmskin(string moduleId)
    {
        var staging = Path.Combine(_home, "pkg-" + moduleId);
        if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true);
        var skin = Path.Combine(staging, "Skins", moduleId, "Main");
        Directory.CreateDirectory(skin);
        File.WriteAllText(Path.Combine(skin, "Main.ini"), "[Rainmeter]\nUpdate=1000\n");
        File.WriteAllText(Path.Combine(staging, "RMSKIN.ini"), "[rmskin]\nAuthor=test\nVersion=1.0\n");

        var rmskin = Path.Combine(_home, $"{moduleId}.rmskin");
        if (File.Exists(rmskin)) File.Delete(rmskin);
        ZipFile.CreateFromDirectory(staging, rmskin);
        return rmskin;
    }

    private string CreateFakeRmskinNestedTiles(string moduleId)
    {
        var staging = Path.Combine(_home, "pkg-tiles-" + moduleId);
        var skin = Path.Combine(staging, "Skins", "Tiles", moduleId);
        Directory.CreateDirectory(skin);
        File.WriteAllText(Path.Combine(skin, "Main.ini"), "[Rainmeter]\n");
        File.WriteAllText(Path.Combine(staging, "RMSKIN.ini"), "[rmskin]\nAuthor=test\n");

        var rmskin = Path.Combine(_home, $"{moduleId}-tiles.rmskin");
        if (File.Exists(rmskin)) File.Delete(rmskin);
        ZipFile.CreateFromDirectory(staging, rmskin);
        return rmskin;
    }

    private sealed class ScriptRecordingHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, Func<HttpResponseMessage>> _map = new(StringComparer.OrdinalIgnoreCase);
        public List<string> RequestedUrls { get; } = [];
        public List<string> InvokedScripts { get; } = [];

        public void Map(string url, string json) =>
            _map[url] = () => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

        public void MapBinary(string url, byte[] bytes) =>
            _map[url] = () =>
            {
                var content = new ByteArrayContent(bytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
            };

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            RequestedUrls.Add(url);
            if (url.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
                InvokedScripts.Add(url);

            if (_map.TryGetValue(url, out var factory))
                return Task.FromResult(factory());

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
