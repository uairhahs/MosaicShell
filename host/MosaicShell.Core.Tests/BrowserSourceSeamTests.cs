using System.Text.RegularExpressions;
using FluentAssertions;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Browser media reaches the rest of the app only through IBrowserMediaSource. WebNowPlaying types may be named
    /// only inside its own folder and at the one place the media stack is created; anywhere else means the seam
    /// leaked (the composite once downcast to the WebNowPlaying host, and the Host read its static cover cache).
    /// The allow-list only shrinks: it reaches "the WebNowPlaying folder is gone" when the dependency is removed.
    /// </summary>
    public class BrowserSourceSeamTests
    {
        private static readonly Regex WebNowPlayingTypes = new(
            @"\b(WebNowPlayingReduxHost|WebNowPlayingSource|WnpPlayerSnapshot|WnpState|IWebNowPlayingService)\b",
            RegexOptions.Compiled);

        private static readonly string[] Projects =
        [
            "MosaicShell.Core",
            "MosaicShell.Host",
            "MosaicShell.Worker",
            "Mosaicist",
        ];

        private static bool IsAllowed(string relativePath)
        {
            string normalized = relativePath.Replace('\\', '/');
            return normalized.StartsWith("MosaicShell.Core/Services/WebNowPlaying/", StringComparison.Ordinal)
                || normalized == "MosaicShell.Core/Services/ServiceContracts.cs";
        }

        [Fact]
        public void WebNowPlaying_types_are_named_only_behind_the_seam()
        {
            List<string> hits = [];
            foreach (string file in SourceTree.EnumerateSources(Projects))
            {
                string relative = SourceTree.RelativeToHost(file);
                if (IsAllowed(relative))
                {
                    continue;
                }

                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (WebNowPlayingTypes.IsMatch(lines[i]))
                    {
                        hits.Add($"{relative}:{i + 1}: {lines[i].Trim()}");
                    }
                }
            }

            _ = hits.Should().BeEmpty(
                "browser media must reach the app through IBrowserMediaSource, not through WebNowPlaying types:\n"
                + string.Join('\n', hits));
        }
    }
}
