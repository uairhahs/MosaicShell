using System.Diagnostics;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// How much of a core this test process burns while the test does nothing, to catch a background loop that retries
    /// without waiting. A busy loop uses all of a core in every window; leftovers from earlier tests (finishing tasks,
    /// the garbage collector) only disturb some windows, so the lowest share across several windows is the loop's own.
    /// </summary>
    internal static class IdleCpu
    {
        public static async Task<double> LowestShareAsync(int windows = 5, int windowMs = 300)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await Task.Delay(200);
            double lowest = double.MaxValue;
            using Process process = Process.GetCurrentProcess();
            for (int i = 0; i < windows; i++)
            {
                process.Refresh();
                TimeSpan before = process.TotalProcessorTime;
                long started = Stopwatch.GetTimestamp();
                await Task.Delay(windowMs);
                process.Refresh();
                double cpuMs = (process.TotalProcessorTime - before).TotalMilliseconds;
                lowest = Math.Min(lowest, cpuMs / Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            }

            return lowest;
        }
    }
}
