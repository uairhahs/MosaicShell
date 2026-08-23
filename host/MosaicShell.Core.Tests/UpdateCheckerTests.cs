using FluentAssertions;
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
        var handler = new StubHandler("""{"tag_name":"2026.9.1-b2","html_url":"https://example.test/r"}""");
        using var http = new HttpClient(handler);
        var result = await UpdateChecker.CheckGitHubAsync(http, currentVersion: "2026.8.21-b13");
        result.UpdateAvailable.Should().BeTrue();
        result.LatestVersion.Should().Be("2026.9.1-b2");
        result.CurrentVersion.Should().Be("2026.8.21-b13");
    }

    [Fact]
    public async Task Check_is_up_to_date_when_build_numbers_match()
    {
        var handler = new StubHandler("""{"tag_name":"2026.8.21-b13","html_url":"https://example.test/r"}""");
        using var http = new HttpClient(handler);
        var result = await UpdateChecker.CheckGitHubAsync(http, currentVersion: "2026.8.21-b13");
        result.UpdateAvailable.Should().BeFalse();
        result.LatestVersion.Should().Be("2026.8.21-b13");
    }

    [Fact]
    public async Task Check_uses_stamped_assembly_version_when_current_not_passed()
    {
        var handler = new StubHandler("""{"tag_name":"2026.8.21-b13","html_url":"https://example.test/r"}""");
        using var http = new HttpClient(handler);
        var assembly = Assembly.GetExecutingAssembly();
        var result = await UpdateChecker.CheckGitHubAsync(http, currentVersion: HostBuildVersion.ReadFromAssembly(assembly));
        result.CurrentVersion.Should().NotBeNullOrWhiteSpace();
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
