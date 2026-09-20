namespace MosaicShell.Core.Services
{
    /// <summary>Whether a Windows media session belongs to a web browser or an installed web app, judged by its app id.</summary>
    public static class BrowserSessionPolicy
    {
        private static readonly string[] BrowserMarkers = ["youtube", "chrome", "msedge", "firefox", "brave"];

        public static bool LooksLikeBrowserSession(string? appId)
        {
            return !string.IsNullOrEmpty(appId)
                && BrowserMarkers.Any(marker => appId.Contains(marker, StringComparison.OrdinalIgnoreCase));
        }
    }
}
