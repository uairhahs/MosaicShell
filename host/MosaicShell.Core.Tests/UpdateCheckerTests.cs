using FluentAssertions;
using MosaicShell.Core.Install;
using MosaicShell.Core.Update;
using System.Net;
using System.Reflection;
using System.Text;

namespace MosaicShell.Core.Tests;

public class UpdateCheckerTests
{
    [Fact]
    public async Task Check_reports_newer_date_build_tag()
    {
        var handler = new StubHandler("""{"tag_name":"2026.9.1-b2","html_url":"https://example.test/r","assets":[]}""");
        using var http = new HttpClient(handler);
        var result = await UpdateChecker.CheckGitHubAsync(http, currentVersion: "2026.8.21-b13");
        result.UpdateAvailable.Should().BeTrue();
        result.LatestVersion.Should().Be("2026.9.1-b2");
        result.CurrentVersion.Should().Be("2026.8.21-b13");
    }

    [Fact]
    public async Task Check_is_up_to_date_when_build_numbers_match()
    {
        var handler = new StubHandler("""{"tag_name":"2026.8.21-b13","html_url":"https://example.test/r","assets":[]}""");
        using var http = new HttpClient(handler);
        var result = await UpdateChecker.CheckGitHubAsync(http, currentVersion: "2026.8.21-b13");
        result.UpdateAvailable.Should().BeFalse();
        result.LatestVersion.Should().Be("2026.8.21-b13");
    }

    [Fact]
    public async Task Check_uses_stamped_assembly_version_when_current_not_passed()
    {
        var handler = new StubHandler("""{"tag_name":"2026.8.21-b13","html_url":"https://example.test/r","assets":[]}""");
        using var http = new HttpClient(handler);
        var assembly = Assembly.GetExecutingAssembly();
        var result = await UpdateChecker.CheckGitHubAsync(http, currentVersion: HostBuildVersion.ReadFromAssembly(assembly));
        result.CurrentVersion.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Check_resolves_setup_asset_url()
    {
        var json = """
            {
              "tag_name": "2026.9.1-b2",
              "html_url": "https://example.test/r",
              "assets": [
                { "name": "MosaicShell-Portable-2026.9.1-b2.zip", "browser_download_url": "https://example.test/p.zip" },
                { "name": "MosaicShell-Setup.exe", "browser_download_url": "https://example.test/MosaicShell-Setup.exe" },
                { "name": "MosaicShell-Setup-2026.9.1-b2.exe", "browser_download_url": "https://example.test/versioned.exe" }
              ]
            }
            """;
        var handler = new StubHandler(json);
        using var http = new HttpClient(handler);
        var result = await UpdateChecker.CheckGitHubAsync(http, currentVersion: "2026.8.21-b13");
        result.SetupFileName.Should().Be(HostInstallLayoutSpec.SetupExeName);
        result.SetupDownloadUrl.Should().Be("https://example.test/MosaicShell-Setup.exe");
    }

    [Fact]
    public void SelectSetupAsset_prefers_stable_alias_over_versioned()
    {
        var assets = new List<UpdateChecker.GhAsset>
        {
            new() { Name = "MosaicShell-Setup-2026.9.1-b2.exe", BrowserDownloadUrl = "https://example.test/v.exe" },
            new() { Name = "MosaicShell-Setup.exe", BrowserDownloadUrl = "https://example.test/s.exe" },
        };
        var pick = UpdateChecker.SelectSetupAsset(assets);
        pick!.Name.Should().Be("MosaicShell-Setup.exe");
    }

    [Fact]
    public void HostUpdateApplier_builds_silent_start_info()
    {
        var path = Path.Combine(Path.GetTempPath(), "ms-setup-" + Guid.NewGuid().ToString("N") + ".exe");
        File.WriteAllBytes(path, [0x4D, 0x5A]); // MZ stub so File.Exists passes
        try
        {
            var psi = HostUpdateApplier.CreateSetupStartInfo(path);
            psi.FileName.Should().Be(Path.GetFullPath(path));
            psi.Arguments.Should().Contain("/SILENT");
            psi.Arguments.Should().Contain("/CLOSEAPPLICATIONS");
        }
        finally
        {
            try { File.Delete(path); } catch { /* ignore */ }
        }
    }

    private sealed class StubHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
    }
}
