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
            List<string> hits = [];
            foreach (string file in SourceTree.EnumerateSources(ProductionProjects))
            {
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (isViolation(lines[i]))
                    {
                        hits.Add($"{SourceTree.RelativeToHost(file)}:{i + 1}");
                    }
                }
            }

            return hits;
        }
    }
}
