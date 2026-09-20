using System.Globalization;

namespace MosaicShell.Core.Services
{
    /// <summary>
    /// First line of a diagnostic log for one process run. Log lines carry a time of day only, so
    /// without this marker runs from different days in one append-only file cannot be told apart.
    /// </summary>
    public static class RunBanner
    {
        private const int CommitChars = 8;

        /// <summary>
        /// <paramref name="informationalVersion"/> is the assembly informational version,
        /// <c>version+commit</c> (for example <c>0.0.0-dev+9ad6a110...</c>).
        /// </summary>
        public static string Format(DateTimeOffset startedAt, int processId, string? informationalVersion)
        {
            string version = "unknown";
            string commit = "unknown";
            if (!string.IsNullOrWhiteSpace(informationalVersion))
            {
                int plus = informationalVersion.IndexOf('+');
                version = plus < 0 ? informationalVersion.Trim() : informationalVersion[..plus].Trim();
                string hash = plus < 0 ? "" : informationalVersion[(plus + 1)..].Trim();
                commit = hash.Length == 0 ? "unknown" : hash[..Math.Min(CommitChars, hash.Length)];
            }

            string when = startedAt.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture);
            return $"=== run start {when} pid={processId} build={version} commit={commit} ===";
        }
    }
}
