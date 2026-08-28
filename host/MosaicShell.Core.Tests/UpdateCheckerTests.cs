using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;
using FluentAssertions;
using MosaicShell.Core.Install;
using MosaicShell.Core.Update;

namespace MosaicShell.Core.Tests
{
    public class UpdateCheckerTests
    {
        [Fact]
        public async Task Check_reports_newer_date_build_tag()
        {
            StubHandler handler = new(/*lang=json,strict*/ """{"tag_name":"2026.9.1-b2","html_url":"https://example.test/r","assets":[]}""");
            using HttpClient http = new(handler);
            UpdateCheckResult result = await UpdateChecker.CheckGitHubAsync(http, currentVersion: "2026.8.21-b13");
            _ = result.UpdateAvailable.Should().BeTrue();
            _ = result.LatestVersion.Should().Be("2026.9.1-b2");
            _ = result.CurrentVersion.Should().Be("2026.8.21-b13");
        }

        [Fact]
        public async Task Check_is_up_to_date_when_build_numbers_match()
        {
            StubHandler handler = new(/*lang=json,strict*/ """{"tag_name":"2026.8.21-b13","html_url":"https://example.test/r","assets":[]}""");
            using HttpClient http = new(handler);
            UpdateCheckResult result = await UpdateChecker.CheckGitHubAsync(http, currentVersion: "2026.8.21-b13");
            _ = result.UpdateAvailable.Should().BeFalse();
            _ = result.LatestVersion.Should().Be("2026.8.21-b13");
        }

        [Fact]
        public async Task Check_uses_stamped_assembly_version_when_current_not_passed()
        {
            StubHandler handler = new(/*lang=json,strict*/ """{"tag_name":"2026.8.21-b13","html_url":"https://example.test/r","assets":[]}""");
            using HttpClient http = new(handler);
            Assembly assembly = Assembly.GetExecutingAssembly();
            UpdateCheckResult result = await UpdateChecker.CheckGitHubAsync(http, currentVersion: HostBuildVersion.ReadFromAssembly(assembly));
            _ = result.CurrentVersion.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Check_resolves_setup_asset_url()
        {
            string json = /*lang=json,strict*/ """
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
            StubHandler handler = new(json);
            using HttpClient http = new(handler);
            UpdateCheckResult result = await UpdateChecker.CheckGitHubAsync(http, currentVersion: "2026.8.21-b13");
            _ = result.SetupFileName.Should().Be(HostInstallLayoutSpec.SetupExeName);
            _ = result.SetupDownloadUrl.Should().Be("https://example.test/MosaicShell-Setup.exe");
        }

        [Fact]
        public void SelectSetupAsset_prefers_stable_alias_over_versioned()
        {
            List<UpdateChecker.GhAsset> assets =
            [
                new() { Name = "MosaicShell-Setup-2026.9.1-b2.exe", BrowserDownloadUrl = "https://example.test/v.exe" },
                new() { Name = "MosaicShell-Setup.exe", BrowserDownloadUrl = "https://example.test/s.exe" },
            ];
            UpdateChecker.GhAsset? pick = UpdateChecker.SelectSetupAsset(assets);
            _ = pick!.Name.Should().Be("MosaicShell-Setup.exe");
        }

        [Fact]
        public void HostUpdateApplier_builds_silent_start_info()
        {
            string path = Path.Combine(Path.GetTempPath(), "ms-setup-" + Guid.NewGuid().ToString("N") + ".exe");
            File.WriteAllBytes(path, [0x4D, 0x5A]); // MZ stub so File.Exists passes
            try
            {
                ProcessStartInfo psi = HostUpdateApplier.CreateSetupStartInfo(path);
                _ = psi.FileName.Should().Be(Path.GetFullPath(path));
                _ = psi.Arguments.Should().Contain("/SILENT");
                _ = psi.Arguments.Should().Contain("/CLOSEAPPLICATIONS");
            }
            finally
            {
                try { File.Delete(path); } catch { /* ignore */ }
            }
        }

        private sealed class StubHandler(string json) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            }
        }
    }
}
