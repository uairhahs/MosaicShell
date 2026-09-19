namespace MosaicShell.Core.Services.BrowserBridge
{
    /// <summary>
    /// Names the player behind a tab from its origin. <see cref="MediaLikePolicy.IsYouTubeMusic"/> and the log read
    /// the name, so "YouTube Music" must be spelled exactly as it is there.
    /// </summary>
    public static class BrowserSiteNames
    {
        public static string FromOrigin(string origin)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri))
            {
                return origin;
            }

            string host = uri.Host.ToLowerInvariant();
            return host == "music.youtube.com" ? "YouTube Music"
                : IsSiteOrSubdomain(host, "youtube.com") ? "YouTube"
                : IsSiteOrSubdomain(host, "spotify.com") ? "Spotify"
                : IsSiteOrSubdomain(host, "soundcloud.com") ? "SoundCloud"
                : IsSiteOrSubdomain(host, "bandcamp.com") ? "Bandcamp"
                : host.StartsWith("www.", StringComparison.Ordinal) ? host[4..]
                : host;
        }

        private static bool IsSiteOrSubdomain(string host, string site)
        {
            return host == site || host.EndsWith("." + site, StringComparison.Ordinal);
        }
    }
}
