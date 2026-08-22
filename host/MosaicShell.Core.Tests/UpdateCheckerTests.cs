using FluentAssertions;
using MosaicShell.Core.Update;
using System.Net;
using System.Text;

namespace MosaicShell.Core.Tests;

public class UpdateCheckerTests
{
    [Fact]
    public async Task Check_parses_newer_tag()
    {
        var handler = new StubHandler("""{"tag_name":"v9.9.9","html_url":"https://example.test/r"}""");
        using var http = new HttpClient(handler);
        var result = await UpdateChecker.CheckGitHubAsync(http, currentVersion: "0.1.0");
        result.UpdateAvailable.Should().BeTrue();
        result.LatestVersion.Should().Be("9.9.9");
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
