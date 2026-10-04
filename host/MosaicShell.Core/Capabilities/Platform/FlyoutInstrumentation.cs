using System.Globalization;

namespace MosaicShell.Core.Capabilities.Platform
{
    /// <summary>
    /// Measured phase 2 pacing. Phase 2 waits a fixed interval between steps with no correction for
    /// time already spent (audit F05), so the planned duration (steps times interval) and the real
    /// one can differ; this summarises one run from the elapsed time at each applied step.
    /// </summary>
    public static class FlyoutPhaseTiming
    {
        public readonly record struct Summary(
            int StepsRun,
            int StepsPlanned,
            double PlannedMs,
            double TotalMs,
            double MeanGapMs,
            double MaxGapMs,
            bool Completed);

        /// <param name="stepElapsedMs">Elapsed time when each step was applied, first step first.</param>
        /// <param name="plannedSteps">Step count; a full run applies <c>plannedSteps + 1</c> values (0 to N).</param>
        /// <param name="intervalMs">Planned wait between consecutive steps.</param>
        public static Summary Summarize(IReadOnlyList<double> stepElapsedMs, int plannedSteps, int intervalMs)
        {
            int planned = Math.Max(0, plannedSteps) + 1;
            double plannedMs = Math.Max(0, plannedSteps) * (double)intervalMs;
            if (stepElapsedMs.Count == 0)
            {
                return new Summary(0, planned, plannedMs, 0, 0, 0, Completed: false);
            }

            double maxGap = 0;
            for (int i = 1; i < stepElapsedMs.Count; i++)
            {
                maxGap = Math.Max(maxGap, stepElapsedMs[i] - stepElapsedMs[i - 1]);
            }

            double total = stepElapsedMs[^1] - stepElapsedMs[0];
            double meanGap = stepElapsedMs.Count > 1 ? total / (stepElapsedMs.Count - 1) : 0;
            return new Summary(stepElapsedMs.Count, planned, plannedMs, total, meanGap, maxGap, stepElapsedMs.Count >= planned);
        }

        public static string Format(string? styleId, bool entrance, Summary s)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"phase2 style={styleId} entrance={entrance} steps={s.StepsRun}/{s.StepsPlanned} " +
                $"planned={s.PlannedMs:0}ms actual={s.TotalMs:0}ms gapMean={s.MeanGapMs:0.0}ms gapMax={s.MaxGapMs:0.0}ms " +
                $"completed={s.Completed}");
        }
    }

    /// <summary>
    /// Orders HWND region writes for one window. Some writes apply synchronously and some are
    /// posted, so an older posted write can land after a newer synchronous one and leave a stale
    /// region on screen (audit H2). Each write takes a sequence number when requested; applying a
    /// number lower than the last applied one is reported as stale. Thread-safe.
    /// </summary>
    public sealed class FlyoutRegionWriteTracker
    {
        private readonly Lock _gate = new();
        private long _next;
        private long _lastApplied;

        public long LastApplied
        {
            get
            {
                lock (_gate)
                {
                    return _lastApplied;
                }
            }
        }

        public int StaleCount { get; private set; }

        public long Request()
        {
            return Interlocked.Increment(ref _next);
        }

        /// <summary>Records that write <paramref name="seq"/> reached the window; true when it was stale.</summary>
        public bool Applied(long seq)
        {
            lock (_gate)
            {
                if (seq < _lastApplied)
                {
                    StaleCount++;
                    return true;
                }

                _lastApplied = seq;
                return false;
            }
        }
    }
}
