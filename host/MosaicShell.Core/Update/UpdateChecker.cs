using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MosaicShell.Core.Update;

public sealed record UpdateCheckResult(bool UpdateAvailable, string? LatestVersion, string? CurrentVersion, string? ReleaseUrl);

public static class UpdateChecker
{
    public static async Task<UpdateCheckResult> CheckGitHubAsync(
        HttpClient http,
        string owner = "uairhahs",
        string repo = "MosaicShell",
        string? currentVersion = null,
        CancellationToken ct = default)
    {
        currentVersion ??= HostBuildVersion.ReadCurrent();
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}/releases/latest");
            req.Headers.Accept.ParseAdd("application/vnd.github+json");
            if (!http.DefaultRequestHeaders.UserAgent.Any())
                http.DefaultRequestHeaders.UserAgent.ParseAdd($"MosaicShell-Host/{currentVersion}");
            using var res = await http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
                return new UpdateCheckResult(false, null, currentVersion, null);

            var release = await res.Content.ReadFromJsonAsync<GhRelease>(cancellationToken: ct);
            var latest = NormalizeTag(release?.TagName);
            if (string.IsNullOrWhiteSpace(latest))
                return new UpdateCheckResult(false, null, currentVersion, release?.HtmlUrl);

            var available = HostBuildVersionPolicy.IsNewer(latest, currentVersion);
            return new UpdateCheckResult(available, latest, currentVersion, release?.HtmlUrl);
        }
        catch
        {
            return new UpdateCheckResult(false, null, currentVersion, null);
        }
    }

    private static string? NormalizeTag(string? tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            return null;

        var text = tagName.Trim().TrimStart('v', 'V');
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private sealed class GhRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }
    }
}
