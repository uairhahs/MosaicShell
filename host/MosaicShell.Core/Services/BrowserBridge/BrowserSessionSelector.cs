namespace MosaicShell.Core.Services.BrowserBridge
{
    /// <summary>
    /// One tab's latest report as the Host holds it. <see cref="UpdatedAt"/> is when the Host received that report;
    /// <see cref="LastHeard"/> is when the Host last heard anything (a report or a ping) on the connection it came
    /// from. A track that plays for minutes sends no reports, so liveness is a property of the connection.
    /// </summary>
    public sealed record BrowserSessionEntry(int ConnectionId, BrowserSessionReport Report, DateTimeOffset UpdatedAt, DateTimeOffset LastHeard);

    /// <summary>Chooses which of the sessions the connected browsers publish is the one the flyout shows.</summary>
    public static class BrowserSessionSelector
    {
        /// <summary>
        /// How long a connection may stay silent before its sessions are ignored. The extension pings well inside
        /// this, so it only elapses when the browser or the extension is gone without the pipe closing.
        /// </summary>
        public static readonly TimeSpan StalenessTimeout = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Ranks by, in order: audible playback, silent playback, paused, stopped; then agreement with the title
        /// Windows reports; then the newest report; then the lowest connection and tab id so the answer does not
        /// depend on enumeration order. A stopped tab with no title has nothing to show and is never chosen.
        /// </summary>
        public static BrowserSessionEntry? Select(IEnumerable<BrowserSessionEntry> entries, DateTimeOffset now, string? smtcTitle)
        {
            return entries
                .Where(e => now - e.LastHeard <= StalenessTimeout)
                .Where(e => !(e.Report.PlaybackState == BrowserPlaybackState.Stopped && string.IsNullOrWhiteSpace(e.Report.Title)))
                .OrderBy(e => Tier(e.Report))
                .ThenByDescending(e => MediaTitleNormalizer.LooselyMatch(smtcTitle, e.Report.Title))
                .ThenByDescending(e => e.UpdatedAt)
                .ThenBy(e => e.ConnectionId)
                .ThenBy(e => e.Report.TabId)
                .FirstOrDefault();
        }

        private static int Tier(BrowserSessionReport report)
        {
            return report.PlaybackState switch
            {
                BrowserPlaybackState.Playing => report.Audible ? 0 : 1,
                BrowserPlaybackState.Paused => 2,
                _ => 3,
            };
        }
    }
}
