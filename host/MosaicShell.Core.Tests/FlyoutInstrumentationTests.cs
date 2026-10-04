using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// A2 instrumentation. Phase 2 is paced by a fixed delay per step with no elapsed-time
    /// correction (audit F05), so its real duration must be measured before it is fixed. Region
    /// writes are partly posted and partly synchronous, so an older posted write can land after a
    /// newer one (audit H2); the tracker makes that visible in the log.
    /// </summary>
    public class FlyoutPhaseTimingTests
    {
        [Fact]
        public void Summary_compares_measured_duration_with_the_planned_one()
        {
            // 4 steps at 16 ms between them: planned 64 ms. The delays ran long.
            FlyoutPhaseTiming.Summary s = FlyoutPhaseTiming.Summarize([0, 20, 45, 70, 100], plannedSteps: 4, intervalMs: 16);

            _ = s.StepsRun.Should().Be(5);
            _ = s.PlannedMs.Should().Be(64);
            _ = s.TotalMs.Should().Be(100);
            _ = s.MaxGapMs.Should().Be(30);
            _ = s.MeanGapMs.Should().Be(25);
            _ = s.Completed.Should().BeTrue();
        }

        [Fact]
        public void A_cancelled_run_is_reported_as_incomplete()
        {
            FlyoutPhaseTiming.Summary s = FlyoutPhaseTiming.Summarize([0, 18], plannedSteps: 4, intervalMs: 16);

            _ = s.Completed.Should().BeFalse();
            _ = s.StepsRun.Should().Be(2);
        }

        [Fact]
        public void An_empty_run_has_zero_durations()
        {
            FlyoutPhaseTiming.Summary s = FlyoutPhaseTiming.Summarize([], plannedSteps: 4, intervalMs: 16);

            _ = s.StepsRun.Should().Be(0);
            _ = s.TotalMs.Should().Be(0);
            _ = s.MaxGapMs.Should().Be(0);
            _ = s.MeanGapMs.Should().Be(0);
        }

        [Fact]
        public void Line_carries_every_number_needed_to_judge_pacing()
        {
            FlyoutPhaseTiming.Summary s = FlyoutPhaseTiming.Summarize([0, 20, 45, 70, 100], plannedSteps: 4, intervalMs: 16);

            string line = FlyoutPhaseTiming.Format("Fluent", entrance: true, s);

            _ = line.Should().Be("phase2 style=Fluent entrance=True steps=5/5 planned=64ms actual=100ms gapMean=25.0ms gapMax=30.0ms completed=True");
        }
    }

    public class FlyoutRegionWriteTrackerTests
    {
        [Fact]
        public void Writes_applied_in_request_order_are_not_stale()
        {
            FlyoutRegionWriteTracker t = new();
            long a = t.Request();
            long b = t.Request();

            _ = t.TryApply(a).Should().BeTrue();
            _ = t.TryApply(b).Should().BeTrue();
            _ = t.StaleCount.Should().Be(0);
        }

        [Fact]
        public void An_older_write_arriving_after_a_newer_one_is_refused()
        {
            // Modern Flyouts cold show, 2026-10-04: posted full-card regions requested before the
            // reveal's synchronous collapsed region landed after it, so the media card flipped
            // collapsed, full, collapsed, full before phase 2 (the reported "jiggle").
            FlyoutRegionWriteTracker t = new();
            long posted = t.Request();
            long sync = t.Request();

            _ = t.TryApply(sync).Should().BeTrue();
            _ = t.TryApply(posted).Should().BeFalse("an older region must never replace a newer one");
            _ = t.StaleCount.Should().Be(1);
            _ = t.LastApplied.Should().Be(sync, "a refused write does not move the high-water mark");
        }

        [Fact]
        public void A_write_requested_after_the_last_applied_one_is_still_accepted()
        {
            FlyoutRegionWriteTracker t = new();
            long first = t.Request();
            _ = t.TryApply(first).Should().BeTrue();

            long later = t.Request();

            _ = t.TryApply(later).Should().BeTrue();
        }

        [Fact]
        public void Sequence_numbers_increase_across_threads()
        {
            FlyoutRegionWriteTracker t = new();
            long[] seqs = new long[1000];

            _ = Parallel.For(0, seqs.Length, i => seqs[i] = t.Request());

            _ = seqs.Should().OnlyHaveUniqueItems();
            _ = seqs.Max().Should().Be(1000);
        }
    }
}
