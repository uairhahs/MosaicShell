using System.Collections.Concurrent;

namespace MosaicShell.Core.Services
{
    /// <summary>
    /// Cover art remembered by track title, so a flyout can show the last known cover when a source has none for
    /// the moment. Keyed by the title without its site suffix, so "Song | YouTube Music" and "Song" share an entry.
    /// </summary>
    public static class MediaArtworkCache
    {
        /// <summary>Anything shorter cannot be an image; the sources use the same floor.</summary>
        public const int MinimumImageBytes = 32;

        private static readonly ConcurrentDictionary<string, byte[]> ByTitle = new(StringComparer.OrdinalIgnoreCase);

        public static int Count => ByTitle.Count;

        public static void Store(string? title, byte[]? image)
        {
            if (string.IsNullOrWhiteSpace(title) || image is null || image.Length < MinimumImageBytes)
            {
                return;
            }

            ByTitle[Key(title)] = image;
        }

        public static bool TryGet(string? title, out byte[]? image)
        {
            image = null;
            if (string.IsNullOrWhiteSpace(title))
            {
                return false;
            }

            if (ByTitle.TryGetValue(Key(title), out byte[]? bytes) && bytes.Length >= MinimumImageBytes)
            {
                image = bytes;
                return true;
            }

            return false;
        }

        private static string Key(string title)
        {
            return (MediaTitleNormalizer.StripSiteSuffix(title) ?? title).Trim();
        }
    }
}
