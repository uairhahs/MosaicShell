namespace MosaicShell.Core.Services
{
    /// <summary>
    /// Browser media sessions publish the page title, so a track arrives as "Song | YouTube Music".
    /// The site suffix is chrome, not part of the title: it must never reach a flyout, and it must
    /// not make two spellings of one title compare unequal. This is the single place that knows it.
    /// </summary>
    public static class MediaTitleNormalizer
    {
        /// <summary>Sites whose tab title carries a " | Site" suffix (measured for YouTube Music, 2026-09-19).</summary>
        private static readonly string[] SiteSuffixes = ["YouTube Music", "YouTube"];

        /// <summary>
        /// Removes one trailing " | Site" suffix. Anything else, including the bare placeholder
        /// "YouTube Music" and a suffix with nothing before it, is returned unchanged.
        /// </summary>
        public static string? StripSiteSuffix(string? title)
        {
            if (string.IsNullOrEmpty(title))
            {
                return title;
            }

            int bar = title.LastIndexOf('|');
            if (bar <= 0)
            {
                return title;
            }

            string tail = title[(bar + 1)..].Trim();
            string head = title[..bar].TrimEnd();
            return head.Length == 0 || !IsSiteName(tail) ? title : head;
        }

        /// <summary>
        /// Whether two titles name the same track once the site suffix, case and surrounding space are ignored.
        /// An empty or missing title matches nothing, not even another empty one.
        /// </summary>
        public static bool LooselyMatch(string? a, string? b)
        {
            return !string.IsNullOrWhiteSpace(a)
                && !string.IsNullOrWhiteSpace(b)
                && string.Equals(StripSiteSuffix(a)!.Trim(), StripSiteSuffix(b)!.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSiteName(string tail)
        {
            foreach (string site in SiteSuffixes)
            {
                if (string.Equals(tail, site, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
