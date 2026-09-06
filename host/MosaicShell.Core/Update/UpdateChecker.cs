using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MosaicShell.Core.Install;

namespace MosaicShell.Core.Update
{
    public sealed record UpdateCheckResult(
        bool UpdateAvailable,
        string? LatestVersion,
        string? CurrentVersion,
        string? ReleaseUrl,
        string? SetupDownloadUrl = null,
        string? SetupFileName = null);

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
                using HttpRequestMessage req = new(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}/releases/latest");
                req.Headers.Accept.ParseAdd("application/vnd.github+json");
                if (!http.DefaultRequestHeaders.UserAgent.Any())
                {
                    http.DefaultRequestHeaders.UserAgent.ParseAdd($"MosaicShell-Host/{currentVersion}");
                }

                using HttpResponseMessage res = await http.SendAsync(req, ct);
                if (!res.IsSuccessStatusCode)
                {
                    return new UpdateCheckResult(false, null, currentVersion, null);
                }

                GhRelease? release = await res.Content.ReadFromJsonAsync<GhRelease>(cancellationToken: ct);
                string? latest = NormalizeTag(release?.TagName);
                if (string.IsNullOrWhiteSpace(latest))
                {
                    return new UpdateCheckResult(false, null, currentVersion, release?.HtmlUrl);
                }

                GhAsset? setup = SelectSetupAsset(release?.Assets);
                bool available = HostBuildVersionPolicy.IsNewer(latest, currentVersion);
                return new UpdateCheckResult(
                    available,
                    latest,
                    currentVersion,
                    release?.HtmlUrl,
                    setup?.BrowserDownloadUrl,
                    setup?.Name);
            }
            catch
            {
                return new UpdateCheckResult(false, null, currentVersion, null);
            }
        }

        /// <summary>
        /// Downloads the Setup asset for an update check result into the update cache.
        /// </summary>
        public static async Task<string> DownloadSetupAsync(
            HttpClient http,
            UpdateCheckResult check,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(check.SetupDownloadUrl))
            {
                throw new InvalidOperationException("No Setup.exe asset URL on the latest release.");
            }

            string name = string.IsNullOrWhiteSpace(check.SetupFileName)
                ? HostInstallLayoutSpec.SetupExeName
                : check.SetupFileName;

            ReleaseDownloader dl = new(http);
            return await dl.DownloadAsync(
                new ReleaseAsset
                {
                    Url = check.SetupDownloadUrl,
                    FileName = name,
                },
                HostUpdatePolicy.CacheDirectory,
                ct);
        }

        /// <summary>Picks MosaicShell-Setup.exe or MosaicShell-Setup-*.exe from release assets.</summary>
        public static GhAsset? SelectSetupAsset(IReadOnlyList<GhAsset>? assets)
        {
            if (assets is null || assets.Count == 0)
            {
                return null;
            }

            GhAsset? exact = null;
            GhAsset? versioned = null;
            foreach (GhAsset asset in assets)
            {
                string name = asset.Name ?? "";
                if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (name.Equals(HostInstallLayoutSpec.SetupExeName, StringComparison.OrdinalIgnoreCase))
                {
                    exact = asset;
                    break;
                }

                if (name.StartsWith("MosaicShell-Setup-", StringComparison.OrdinalIgnoreCase)
                    && versioned is null)
                {
                    versioned = asset;
                }
            }

            return exact ?? versioned;
        }

        private static string? NormalizeTag(string? tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return null;
            }

            string text = tagName.Trim().TrimStart('v', 'V');
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        private sealed class GhRelease
        {
            [JsonPropertyName("tag_name")]
            public string? TagName { get; set; }

            [JsonPropertyName("html_url")]
            public string? HtmlUrl { get; set; }

            [JsonPropertyName("assets")]
            public List<GhAsset>? Assets { get; set; }
        }

        public sealed class GhAsset
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("browser_download_url")]
            public string? BrowserDownloadUrl { get; set; }
        }
    }
}
