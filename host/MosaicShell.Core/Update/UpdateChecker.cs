using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MosaicShell.Core.Update;

public sealed record UpdateCheckResult(bool UpdateAvailable, string? LatestVersion, string? CurrentVersion, string? ReleaseUrl);

public static class UpdateChecker
{
    public const string DefaultVersion = "0.1.0-native";

    public static async Task<UpdateCheckResult> CheckGitHubAsync(
        HttpClient http,
        string owner = "uairhahs",
        string repo = "MosaicShell",
        string? currentVersion = null,
        CancellationToken ct = default)
    {
        currentVersion ??= DefaultVersion;
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}/releases/latest");
            req.Headers.Accept.ParseAdd("application/vnd.github+json");
            if (!http.DefaultRequestHeaders.UserAgent.Any())
                http.DefaultRequestHeaders.UserAgent.ParseAdd("MosaicShell-Host/0.1");
            using var res = await http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
                return new UpdateCheckResult(false, null, currentVersion, null);

            var release = await res.Content.ReadFromJsonAsync<GhRelease>(cancellationToken: ct);
            var latest = release?.TagName?.TrimStart('v', 'V');
            if (string.IsNullOrWhiteSpace(latest))
                return new UpdateCheckResult(false, null, currentVersion, release?.HtmlUrl);

            var available = !string.Equals(latest, currentVersion.TrimStart('v', 'V'), StringComparison.OrdinalIgnoreCase);
            return new UpdateCheckResult(available, latest, currentVersion, release?.HtmlUrl);
        }
        catch
        {
            return new UpdateCheckResult(false, null, currentVersion, null);
        }
    }

    private sealed class GhRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }
    }
}
