using FluentAssertions;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Diagnostic logs must be bounded. host-size.log once reached 160 GB in a few days because a
    /// runaway resize loop appended ever-longer lines to an uncapped file. Every on-disk log goes
    /// through <see cref="DiagnosticLog"/>, which caps each file and rolls it over to one backup.
    /// </summary>
    public sealed class DiagnosticLogTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "MosaicDiagLog_" + Guid.NewGuid().ToString("N"));

        public DiagnosticLogTests()
        {
            _ = Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        [Fact]
        public void Disk_use_stays_bounded_however_much_is_written()
        {
            const long cap = 4096;
            string path = Path.Combine(_dir, "t.log");
            string line = new('x', 200);

            for (int i = 0; i < 2000; i++)
            {
                DiagnosticLog.AppendTo(path, line, cap);
            }

            long total = Directory.EnumerateFiles(_dir).Sum(static f => new FileInfo(f).Length);
            _ = total.Should().BeLessThanOrEqualTo(2 * (cap + DiagnosticLogFormat.MaxLineChars));
            _ = Directory.EnumerateFiles(_dir).Should().HaveCount(2, "one live file plus a single backup");
        }

        [Fact]
        public void Rollover_keeps_newest_lines_in_live_file_and_previous_in_backup()
        {
            const long cap = 1024;
            string path = Path.Combine(_dir, "t.log");

            for (int i = 0; i < 100; i++)
            {
                DiagnosticLog.AppendTo(path, $"line-{i:D3} " + new string('y', 40), cap);
            }

            _ = File.ReadAllText(path).Should().Contain("line-099");
            _ = File.Exists(DiagnosticLog.BackupPathFor(path)).Should().BeTrue();
            _ = File.ReadAllText(DiagnosticLog.BackupPathFor(path)).Should().NotContain("line-099");
        }

        [Fact]
        public void Oversized_messages_are_truncated()
        {
            string path = Path.Combine(_dir, "t.log");

            DiagnosticLog.AppendTo(path, new string('z', DiagnosticLog.MaxMessageChars * 50), DiagnosticLog.MaxFileBytes);

            _ = new FileInfo(path).Length.Should().BeLessThanOrEqualTo(DiagnosticLogFormat.MaxLineChars);
        }

        [Fact]
        public void A_symbolic_link_in_place_of_a_log_file_is_not_followed()
        {
            string target = Path.Combine(_dir, "target.txt");
            File.WriteAllText(target, "original");
            string link = Path.Combine(_dir, "t.log");
            try
            {
                _ = File.CreateSymbolicLink(link, target);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Creating a symbolic link needs Developer Mode or elevation; the refusal itself is
                // still covered by IsReparsePoint below.
                return;
            }

            DiagnosticLog.AppendTo(link, "redirected", 1024);

            _ = File.ReadAllText(target).Should().Be("original");
        }

        [Fact]
        public void Reparse_points_are_recognised_from_attributes()
        {
            _ = DiagnosticLog.IsReparsePoint(FileAttributes.Normal).Should().BeFalse();
            _ = DiagnosticLog.IsReparsePoint(FileAttributes.Archive | FileAttributes.ReparsePoint).Should().BeTrue();
        }

        [Fact]
        public void Every_literal_log_file_name_in_production_is_safe()
        {
            System.Text.RegularExpressions.Regex literal = new(@"DiagnosticLog\.Append\(\s*""([^""]*)""");
            List<string> bad = [];
            int seen = 0;

            foreach (string file in SourceTree.EnumerateSources(ProductionProjects))
            {
                foreach (System.Text.RegularExpressions.Match m in literal.Matches(File.ReadAllText(file)))
                {
                    seen++;
                    if (!DiagnosticLogFormat.IsSafeFileName(m.Groups[1].Value))
                    {
                        bad.Add($"{SourceTree.RelativeToHost(file)}: {m.Groups[1].Value}");
                    }
                }
            }

            _ = seen.Should().BePositive();
            _ = bad.Should().BeEmpty();
        }

        [Fact]
        public void Core_and_Host_do_not_write_to_the_console()
        {
            // Host is a windowed app: console output goes nowhere and is a synchronous write. Worker
            // and Mosaicist are console tools and are excluded on purpose.
            List<string> hits = ScanLines(["MosaicShell.Core", "MosaicShell.Host"], static line =>
                line.Contains("Console.Write", StringComparison.Ordinal)
                || line.Contains("Console.Out", StringComparison.Ordinal)
                || line.Contains("Console.Error", StringComparison.Ordinal));

            _ = hits.Should().BeEmpty("log through DiagnosticLog instead:\n" + string.Join('\n', hits));
        }

        [Fact]
        public void Flyout_visibility_queries_do_not_log()
        {
            // IsVisible runs on every routing decision; logging it buried the decisions themselves.
            List<string> hits = ScanLines(["MosaicShell.Host"], static line =>
                line.Contains("Log($\"IsVisible", StringComparison.Ordinal));

            _ = hits.Should().BeEmpty();
        }

        private static readonly string[] ProductionProjects =
            ["MosaicShell.Core", "MosaicShell.Host", "MosaicShell.Worker", "MosaicShell.BrowserRelay", "Mosaicist"];

        private static List<string> ScanLines(string[] projects, Func<string, bool> isViolation)
        {
            List<string> hits = [];
            foreach (string file in SourceTree.EnumerateSources(projects))
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

        [Fact]
        public void Backup_path_sits_beside_the_live_file()
        {
            _ = DiagnosticLog.BackupPathFor(Path.Combine("C:", "x", "flyout.log"))
                .Should().Be(Path.Combine("C:", "x", "flyout.1.log"));
        }

        [Fact]
        public void Write_failures_are_swallowed()
        {
            string bad = Path.Combine(_dir, "missing-dir", "sub", "t.log");

            Action act = () => DiagnosticLog.AppendTo(bad, "hello", 1024);

            _ = act.Should().NotThrow();
        }

        [Fact]
        public void Production_sources_append_to_files_only_through_DiagnosticLog()
        {
            string[] projects = ["MosaicShell.Core", "MosaicShell.Host", "MosaicShell.Worker", "MosaicShell.BrowserRelay", "Mosaicist"];
            string[] forbidden = ["File.AppendAllText", "File.AppendAllLines", "File.AppendText", "FileMode.Append"];
            List<string> hits = [];

            foreach (string file in SourceTree.EnumerateSources(projects))
            {
                if (Path.GetFileName(file) == "DiagnosticLog.cs")
                {
                    continue;
                }

                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (forbidden.Any(f => lines[i].Contains(f, StringComparison.Ordinal)))
                    {
                        hits.Add($"{SourceTree.RelativeToHost(file)}:{i + 1}");
                    }
                }
            }

            _ = hits.Should().BeEmpty("unbounded append-only logs fill the disk; use DiagnosticLog:\n" + string.Join('\n', hits));
        }
    }
}
