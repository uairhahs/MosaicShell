using System.Collections.Concurrent;
using System.Diagnostics;
using FluentAssertions;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The flyout path logs from the UI thread on every routing decision, so a log call must never
    /// wait on disk. <see cref="DiagnosticLogWriter"/> queues lines and writes them on a background
    /// pump; when the queue is full it drops the oldest lines and records how many it dropped.
    /// </summary>
    public sealed class DiagnosticLogWriterTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "MosaicDiagWriter_" + Guid.NewGuid().ToString("N"));

        public DiagnosticLogWriterTests()
        {
            _ = Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        [Fact]
        public void Caller_never_waits_on_a_stalled_store_and_oldest_lines_are_dropped()
        {
            GatedStore store = new();
            DiagnosticLogWriter writer = new(_dir, DiagnosticLogLevel.Debug, "run00001", 1024 * 1024, capacity: 16, store);

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                _ = writer.TryWrite("flyout.log", DiagnosticLogLevel.Info, $"msg-{i:D4}");
            }

            sw.Stop();
            _ = sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2), "a full queue drops lines instead of blocking the caller");
            _ = writer.DroppedCount.Should().BePositive();

            store.Gate.Set();
            writer.Dispose();

            string[] written = [.. store.Writes.SelectMany(static w => w.Lines)];
            _ = written.Should().Contain(static l => l.Contains("msg-0999"), "the newest line survives");
            _ = written.Should().NotContain(static l => l.Contains("msg-0500"), "lines in the middle were dropped");
            _ = written.Should().Contain(static l => l.Contains("WARN") && l.Contains("dropped"), "drops are reported in the log itself");
        }

        [Fact]
        public void Lines_are_written_in_order_with_date_run_id_and_level()
        {
            using (DiagnosticLogWriter writer = new(_dir, DiagnosticLogLevel.Debug, "abcd1234"))
            {
                _ = writer.TryWrite("flyout.log", DiagnosticLogLevel.Info, "first");
                _ = writer.TryWrite("flyout.log", DiagnosticLogLevel.Warning, "second");
                _ = writer.TryWrite("flyout.log", DiagnosticLogLevel.Error, "third");
            }

            string[] lines = File.ReadAllLines(Path.Combine(_dir, "flyout.log"));
            _ = lines.Should().HaveCount(3);
            _ = lines[0].Should().MatchRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}[+-]\d{2}:\d{2} run=abcd1234 INFO first$");
            _ = lines[1].Should().EndWith("WARN second");
            _ = lines[2].Should().EndWith("ERROR third");
        }

        [Fact]
        public void Lines_below_the_minimum_level_are_not_queued()
        {
            using (DiagnosticLogWriter writer = new(_dir, DiagnosticLogLevel.Info, "run00001"))
            {
                _ = writer.IsEnabled(DiagnosticLogLevel.Debug).Should().BeFalse();
                _ = writer.IsEnabled(DiagnosticLogLevel.Info).Should().BeTrue();
                _ = writer.TryWrite("flyout.log", DiagnosticLogLevel.Debug, "hidden").Should().BeFalse();
                _ = writer.TryWrite("flyout.log", DiagnosticLogLevel.Info, "shown").Should().BeTrue();
            }

            string text = File.ReadAllText(Path.Combine(_dir, "flyout.log"));
            _ = text.Should().Contain("shown").And.NotContain("hidden");
        }

        [Theory]
        [InlineData("..\\evil.log")]
        [InlineData("../evil.log")]
        [InlineData("sub\\evil.log")]
        [InlineData("C:\\evil.log")]
        public void Unsafe_file_names_are_refused_and_nothing_is_written_outside_the_directory(string fileName)
        {
            string inner = Path.Combine(_dir, "inner");
            using (DiagnosticLogWriter writer = new(inner, DiagnosticLogLevel.Debug, "run00001"))
            {
                _ = writer.TryWrite(fileName, DiagnosticLogLevel.Error, "payload").Should().BeFalse();
            }

            _ = File.Exists(Path.Combine(_dir, "evil.log")).Should().BeFalse();
            _ = Directory.EnumerateFiles(_dir, "*", SearchOption.AllDirectories).Should().BeEmpty();
        }

        [Fact]
        public void Each_file_name_gets_its_own_file()
        {
            using (DiagnosticLogWriter writer = new(_dir, DiagnosticLogLevel.Debug, "run00001"))
            {
                _ = writer.TryWrite("a.log", DiagnosticLogLevel.Info, "to-a");
                _ = writer.TryWrite("b.log", DiagnosticLogLevel.Info, "to-b");
            }

            _ = File.ReadAllText(Path.Combine(_dir, "a.log")).Should().Contain("to-a").And.NotContain("to-b");
            _ = File.ReadAllText(Path.Combine(_dir, "b.log")).Should().Contain("to-b").And.NotContain("to-a");
        }

        [Fact]
        public void A_message_cannot_forge_a_second_log_line()
        {
            using (DiagnosticLogWriter writer = new(_dir, DiagnosticLogLevel.Debug, "run00001"))
            {
                _ = writer.TryWrite("flyout.log", DiagnosticLogLevel.Info, "title\r\n2026-01-01T00:00:00.000+00:00 run=00000000 ERROR forged");
            }

            _ = File.ReadAllLines(Path.Combine(_dir, "flyout.log")).Should().ContainSingle();
        }

        [Fact]
        public void Writes_after_dispose_are_refused_without_throwing()
        {
            DiagnosticLogWriter writer = new(_dir, DiagnosticLogLevel.Debug, "run00001");
            writer.Dispose();

            _ = writer.TryWrite("flyout.log", DiagnosticLogLevel.Error, "late").Should().BeFalse();
            _ = writer.IsEnabled(DiagnosticLogLevel.Error).Should().BeFalse();
        }

        [Fact]
        public void A_throwing_store_does_not_stop_later_lines()
        {
            FlakyStore store = new();
            using (DiagnosticLogWriter writer = new(_dir, DiagnosticLogLevel.Debug, "run00001", 1024 * 1024, capacity: 64, store))
            {
                _ = writer.TryWrite("flyout.log", DiagnosticLogLevel.Info, "lost");
                _ = SpinWait.SpinUntil(() => store.Calls > 0, TimeSpan.FromSeconds(5));
                _ = writer.TryWrite("flyout.log", DiagnosticLogLevel.Info, "kept");
            }

            _ = store.Written.Should().Contain(static l => l.Contains("kept"));
        }

        private sealed class GatedStore : IDiagnosticLogStore
        {
            public ManualResetEventSlim Gate { get; } = new(false);

            public ConcurrentQueue<(string Path, string[] Lines)> Writes { get; } = new();

            public void Append(string path, IReadOnlyList<string> lines, long maxFileBytes)
            {
                _ = Gate.Wait(TimeSpan.FromSeconds(10));
                Writes.Enqueue((path, [.. lines]));
            }
        }

        private sealed class FlakyStore : IDiagnosticLogStore
        {
            private int _calls;

            public int Calls => Volatile.Read(ref _calls);

            public ConcurrentQueue<string> Written { get; } = new();

            public void Append(string path, IReadOnlyList<string> lines, long maxFileBytes)
            {
                if (Interlocked.Increment(ref _calls) == 1)
                {
                    throw new IOException("disk full");
                }

                foreach (string line in lines)
                {
                    Written.Enqueue(line);
                }
            }
        }
    }

    public class DiagnosticLogFormatTests
    {
        [Fact]
        public void Line_carries_iso_date_with_offset_run_id_and_level()
        {
            DateTimeOffset at = new(2026, 10, 4, 13, 5, 1, 123, TimeSpan.FromHours(1));

            string line = DiagnosticLogFormat.Line(at, "abcd1234", DiagnosticLogLevel.Warning, "hello");

            _ = line.Should().Be("2026-10-04T13:05:01.123+01:00 run=abcd1234 WARN hello");
        }

        [Theory]
        [InlineData("a\r\nb", "a\\r\\nb")]
        [InlineData("a\nb", "a\\nb")]
        [InlineData("a\u0000b", "a\\u0000b")]
        [InlineData("a\u001bb", "a\\u001Bb")]
        [InlineData("a\u0085b", "a\\u0085b")]
        [InlineData("a\u2028b", "a\\u2028b")]
        [InlineData("a\u202Eb", "a\\u202Eb")]
        [InlineData("a\u2066b", "a\\u2066b")]
        [InlineData("tab\tkept", "tab\tkept")]
        [InlineData("C:\\path\\kept", "C:\\path\\kept")]
        public void Sanitize_escapes_line_breaks_control_and_bidi_characters(string raw, string expected)
        {
            _ = DiagnosticLogFormat.Sanitize(raw).Should().Be(expected);
        }

        [Fact]
        public void Sanitize_caps_the_escaped_length()
        {
            string escaped = DiagnosticLogFormat.Sanitize(new string('\n', DiagnosticLog.MaxMessageChars));

            _ = escaped.Length.Should().BeLessThanOrEqualTo(DiagnosticLog.MaxMessageChars + DiagnosticLogFormat.TruncationMarker.Length);
            _ = escaped.Should().EndWith(DiagnosticLogFormat.TruncationMarker);
        }

        [Theory]
        [InlineData("flyout.log", true)]
        [InlineData("host-size.log", true)]
        [InlineData("lock_keys2.log", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData(".log", false)]
        [InlineData("flyout.txt", false)]
        [InlineData("flyout.log.exe", false)]
        [InlineData("..\\x.log", false)]
        [InlineData("../x.log", false)]
        [InlineData("a/b.log", false)]
        [InlineData("a\\b.log", false)]
        [InlineData("C:x.log", false)]
        [InlineData("x.log:stream", false)]
        [InlineData("con.log", false)]
        [InlineData("NUL.log", false)]
        [InlineData("com1.log", false)]
        [InlineData("lpt9.log", false)]
        [InlineData(" flyout.log", false)]
        public void Only_plain_log_file_names_are_safe(string? fileName, bool safe)
        {
            _ = DiagnosticLogFormat.IsSafeFileName(fileName).Should().Be(safe);
        }

        [Fact]
        public void Run_ids_are_short_lowercase_hex_and_distinct()
        {
            string a = DiagnosticLogFormat.NewRunId();
            string b = DiagnosticLogFormat.NewRunId();

            _ = a.Should().MatchRegex("^[0-9a-f]{8}$");
            _ = b.Should().NotBe(a);
        }
    }

    public class DiagnosticLogLevelsTests
    {
        [Theory]
        [InlineData(null, true, DiagnosticLogLevel.Debug)]
        [InlineData(null, false, DiagnosticLogLevel.Info)]
        [InlineData("", false, DiagnosticLogLevel.Info)]
        [InlineData("debug", false, DiagnosticLogLevel.Debug)]
        [InlineData("WARNING", true, DiagnosticLogLevel.Warning)]
        [InlineData("error", true, DiagnosticLogLevel.Error)]
        [InlineData("verbose", false, DiagnosticLogLevel.Info)]
        [InlineData("3", false, DiagnosticLogLevel.Info)]
        public void Configured_level_wins_and_unknown_values_fall_back_to_the_build_default(
            string? configured, bool isDebugBuild, DiagnosticLogLevel expected)
        {
            _ = DiagnosticLogLevels.Resolve(configured, isDebugBuild).Should().Be(expected);
        }
    }
}
