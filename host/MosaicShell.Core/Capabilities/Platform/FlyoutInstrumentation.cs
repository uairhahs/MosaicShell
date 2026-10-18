using System.Globalization;

namespace MosaicShell.Core.Capabilities.Platform
{
    /// <summary>
    /// Measured phase 2 pacing. Phase 2 follows rendered frames against elapsed time, so the planned and the measured
    /// duration should agree; this summarises one run from the elapsed time at each applied frame, which shows a stall.
    /// </summary>
    public static class FlyoutPhaseTiming
    {
        public readonly record struct Summary(
            int FramesRun,
            double PlannedMs,
            double TotalMs,
            double MeanGapMs,
            double MaxGapMs,
            bool Completed);

        /// <param name="frameElapsedMs">Elapsed time when each frame was applied, first frame first.</param>
        /// <param name="plannedMs">The duration the run was meant to take; a run is complete once a frame lands at or after it.</param>
        public static Summary SummarizeFrames(IReadOnlyList<double> frameElapsedMs, double plannedMs)
        {
            if (frameElapsedMs.Count == 0)
            {
                return new Summary(0, plannedMs, 0, 0, 0, Completed: false);
            }

            double maxGap = 0;
            for (int i = 1; i < frameElapsedMs.Count; i++)
            {
                maxGap = Math.Max(maxGap, frameElapsedMs[i] - frameElapsedMs[i - 1]);
            }

            double total = frameElapsedMs[^1] - frameElapsedMs[0];
            double meanGap = frameElapsedMs.Count > 1 ? total / (frameElapsedMs.Count - 1) : 0;
            return new Summary(frameElapsedMs.Count, plannedMs, total, meanGap, maxGap, frameElapsedMs[^1] >= plannedMs);
        }

        public static string Format(string? styleId, bool entrance, Summary s)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"phase2 style={styleId} entrance={entrance} frames={s.FramesRun} " +
                $"planned={s.PlannedMs:0}ms actual={s.TotalMs:0}ms gapMean={s.MeanGapMs:0.0}ms gapMax={s.MaxGapMs:0.0}ms " +
                $"completed={s.Completed}");
        }
    }

    /// <summary>
    /// Orders HWND region writes for one window. Some writes apply synchronously and some are
    /// posted, so an older posted write can arrive after a newer synchronous one (audit H2). Each
    /// write takes a sequence number when requested; <see cref="TryApply"/> refuses a number lower
    /// than the last applied one, so the caller drops it instead of putting an old region back on
    /// screen (that made the Modern Flyouts media card flicker on a cold show). Thread-safe.
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

        /// <summary>
        /// Call just before applying write <paramref name="seq"/>. True: apply it (it becomes the
        /// newest applied). False: a newer write already reached the window, so drop this one.
        /// </summary>
        public bool TryApply(long seq)
        {
            lock (_gate)
            {
                if (seq < _lastApplied)
                {
                    StaleCount++;
                    return false;
                }

                _lastApplied = seq;
                return true;
            }
        }
    }
}
