using System.Text.RegularExpressions;
using FluentAssertions;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Debug instrumentation must not ship. A session-tagged file logger with a hard-coded developer
    /// path once lived in the flyout motion code: it serialized JSON and appended to a file on every
    /// animation step, and on any machine without that drive it threw and swallowed the exception on
    /// every call. These scans fail the build if it, or anything of its shape, comes back.
    /// </summary>
    public class LeakedDebugInstrumentationTests
    {
        private static readonly string[] ProductionProjects =
        [
            "MosaicShell.Core",
            "MosaicShell.Host",
            "MosaicShell.Worker",
            "Mosaicist",
        ];

        private static readonly Regex DeveloperDrivePath = new(
            @"[A-Za-z]:\\{1,2}(Projects|Users)\\{1,2}",
            RegexOptions.Compiled);

        [Fact]
        public void Production_sources_contain_no_agent_debug_logger()
        {
            List<string> hits = Scan(static line =>
                line.Contains("AgentLog", StringComparison.Ordinal)
                || line.Contains("#region agent log", StringComparison.OrdinalIgnoreCase));

            _ = hits.Should().BeEmpty("debug instrumentation must not ship:\n" + string.Join('\n', hits));
        }

        [Fact]
        public void Production_sources_contain_no_hard_coded_developer_drive_paths()
        {
            List<string> hits = Scan(static line => DeveloperDrivePath.IsMatch(line));

            _ = hits.Should().BeEmpty("absolute developer paths must not be committed:\n" + string.Join('\n', hits));
        }

        private static List<string> Scan(Func<string, bool> isViolation)
        {
            string hostRoot = Path.Combine(FindRepoRoot(), "host");
            List<string> hits = [];
            foreach (string project in ProductionProjects)
            {
                string dir = Path.Combine(hostRoot, project);
                if (!Directory.Exists(dir))
                {
                    continue;
                }

                foreach (string file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
                {
                    if (IsBuildOutput(file))
                    {
                        continue;
                    }

                    string[] lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (isViolation(lines[i]))
                        {
                            hits.Add($"{Path.GetRelativePath(hostRoot, file)}:{i + 1}");
                        }
                    }
                }
            }

            return hits;
        }

        private static bool IsBuildOutput(string path)
        {
            string[] segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return segments.Contains("obj") || segments.Contains("bin");
        }

        private static string FindRepoRoot()
        {
            foreach (string start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            {
                DirectoryInfo? dir = new(start);
                while (dir is not null)
                {
                    if (Directory.Exists(Path.Combine(dir.FullName, "host", "MosaicShell.Core")))
                    {
                        return dir.FullName;
                    }

                    dir = dir.Parent;
                }
            }

            throw new DirectoryNotFoundException("Could not locate the repo root (host/MosaicShell.Core).");
        }
    }
}
