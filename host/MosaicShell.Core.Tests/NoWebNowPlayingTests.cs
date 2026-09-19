using System.Text.RegularExpressions;
using FluentAssertions;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// MosaicShell reads media natively: Windows' media session for title, artist and cover, and the browser's accessibility
    /// tree for YouTube Music's like and dislike. It once depended on a third-party browser extension and a local port for
    /// that, and both are gone. This fails if the dependency, or its name, comes back into source or documentation.
    /// History (git, the legacy notes on Rainmeter-era skins) is allowed to name it.
    /// </summary>
    public class NoWebNowPlayingTests
    {
        private static readonly Regex Forbidden = new(@"(?i:WebNowPlaying|\bwnp\b)|\bWnp[A-Z]", RegexOptions.Compiled);

        private static readonly string[] SourceProjects =
        [
            "MosaicShell.Core",
            "MosaicShell.Core.Tests",
            "MosaicShell.Host",
            "MosaicShell.Worker",
            "Mosaicist",
        ];

        private static IEnumerable<string> Documents()
        {
            string root = SourceTree.RepoRoot();
            foreach (string file in new[] { "README.md", Path.Combine("host", "README.md"), Path.Combine("packaging", "README.md") }
                .Select(f => Path.Combine(root, f))
                .Where(File.Exists))
            {
                yield return file;
            }

            foreach (string folder in new[] { "docs", "Tiles", Path.Combine(".github", "docs") })
            {
                string dir = Path.Combine(root, folder);
                if (!Directory.Exists(dir))
                {
                    continue;
                }

                foreach (string file in Directory.EnumerateFiles(dir, "*.md", SearchOption.AllDirectories))
                {
                    if (!file.Contains(Path.Combine("docs", "legacy") + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                    {
                        yield return file;
                    }
                }
            }
        }

        [Fact]
        public void Source_and_documentation_do_not_mention_the_removed_dependency()
        {
            List<string> hits = [];
            foreach (string file in SourceTree.EnumerateSources(SourceProjects).Concat(Documents()))
            {
                if (Path.GetFileName(file) == nameof(NoWebNowPlayingTests) + ".cs")
                {
                    continue;
                }

                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (Forbidden.IsMatch(lines[i]))
                    {
                        hits.Add($"{Path.GetRelativePath(SourceTree.RepoRoot(), file)}:{i + 1}: {lines[i].Trim()}");
                    }
                }
            }

            _ = hits.Should().BeEmpty("the third-party media extension and its local port were removed:\n" + string.Join('\n', hits));
        }

        [Fact]
        public void The_guard_finds_what_it_looks_for()
        {
            _ = Forbidden.IsMatch("uses WebNowPlaying").Should().BeTrue();
            _ = Forbidden.IsMatch("the wnp host").Should().BeTrue();
            _ = Forbidden.IsMatch("WnpPlayerSnapshot").Should().BeTrue();
            _ = Forbidden.IsMatch("browser media and windows session").Should().BeFalse();
        }
    }
}
