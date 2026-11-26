namespace MosaicShell.Core.Modules
{
    /// <summary>
    /// The Hub's library search: a row shows when every word of the query appears in its name, description or id,
    /// ignoring case. An empty query shows every row.
    /// </summary>
    public static class HubLibrarySearch
    {
        public const string Placeholder = "Search modules and widgets";

        public static bool Matches(string? query, string id, string name, string description)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            string haystack = $"{name}\n{description}\n{id}";
            foreach (string word in query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!haystack.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        public static string NoResultsText(string? query)
        {
            return $"Nothing matches “{query?.Trim()}”.";
        }
    }
}
