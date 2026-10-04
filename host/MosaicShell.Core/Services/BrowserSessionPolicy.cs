namespace MosaicShell.Core.Services
{
    /// <summary>Whether a Windows media session belongs to a web browser or an installed web app, judged by its app id.</summary>
    public static class BrowserSessionPolicy
    {
        private static readonly string[] BrowserMarkers = ["youtube", "chrome", "msedge", "firefox", "brave"];

        /// <summary>
        /// The site an installed web app plays, from its Windows app id ("music.youtube.com-5929F88E_…!App"
        /// gives "YouTube Music"), named as <see cref="BrowserBridge.BrowserSiteNames"/> names the extension's
        /// tabs. Null for a browser or anything else whose app id names no site.
        /// </summary>
        public static string? SiteNameOfApp(string? appId)
        {
            if (string.IsNullOrWhiteSpace(appId))
            {
                return null;
            }

            int end = appId.IndexOfAny(['-', '!', '_']);
            string host = end > 0 ? appId[..end] : appId;
            return host.Contains('.', StringComparison.Ordinal)
                && !host.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                && Uri.CheckHostName(host) == UriHostNameType.Dns
                ? BrowserBridge.BrowserSiteNames.FromOrigin("https://" + host)
                : null;
        }

        public static bool LooksLikeBrowserSession(string? appId)
        {
            return !string.IsNullOrEmpty(appId)
                && BrowserMarkers.Any(marker => appId.Contains(marker, StringComparison.OrdinalIgnoreCase));
        }
    }
}
