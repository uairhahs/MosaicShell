using System.Text.RegularExpressions;

namespace MosaicShell.Core.Install
{
    /// <summary>
    /// Name to SHA-256 map parsed from the release's <c>SHA256SUMS.txt</c>, in the format
    /// <c>sha256sum</c> emits: lowercase hex, two spaces, file name.
    /// </summary>
    public sealed partial class ReleaseAssetChecksums
    {
        /// <summary>Asset name CI publishes alongside the installer and portable zip.</summary>
        public const string FileName = "SHA256SUMS.txt";

        private readonly Dictionary<string, string> _byName;

        private ReleaseAssetChecksums(Dictionary<string, string> byName)
        {
            _byName = byName;
        }

        public int Count => _byName.Count;

        public static ReleaseAssetChecksums Parse(string? body)
        {
            Dictionary<string, string> map = new(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(body))
            {
                return new ReleaseAssetChecksums(map);
            }

            foreach (string rawLine in body.Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                {
                    continue;
                }

                Match match = LineFormat().Match(line);
                if (!match.Success)
                {
                    continue;
                }

                // '*' marks binary mode in sha256sum output; the path prefix is a CI staging artifact.
                string name = Path.GetFileName(match.Groups["name"].Value.TrimStart('*').Trim());
                if (name.Length == 0)
                {
                    continue;
                }

                map[name] = match.Groups["hash"].Value.ToLowerInvariant();
            }

            return new ReleaseAssetChecksums(map);
        }

        public string? TryGet(string? assetName)
        {
            return string.IsNullOrWhiteSpace(assetName)
                ? null
                : _byName.TryGetValue(Path.GetFileName(assetName), out string? hash) ? hash : null;
        }

        [GeneratedRegex(@"^(?<hash>[0-9a-fA-F]{64})\s+(?<name>\S.*)$")]
        private static partial Regex LineFormat();
    }
}
