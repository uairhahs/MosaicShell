using System.Diagnostics;
using Avalonia.Headless;
using Avalonia.Threading;

namespace MosaicShell.Host.Tests
{
    /// <summary>
    /// Drives the headless dispatcher. Runs jobs and render ticks, letting real time pass so
    /// dispatcher timers and the entrance fade can complete. Time passes inside the dispatcher's own loop, not in a
    /// sleep: since Avalonia 12.1.x, due DispatcherTimers fire only while that loop runs (a 16 ms timer ticked 3 times a
    /// second under RunJobs plus Thread.Sleep), and RequestAnimationFrame's next frame comes from such a timer.
    /// </summary>
    internal static class HeadlessPump
    {
        /// <summary>Pumps until <paramref name="done"/> holds or the timeout passes; returns the final check.</summary>
        public static bool Until(Func<bool> done, int timeoutMs = 3000)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                Dispatcher.UIThread.RunJobs();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                if (done())
                {
                    return true;
                }

                using CancellationTokenSource slice = new(TimeSpan.FromMilliseconds(10));
                Dispatcher.UIThread.MainLoop(slice.Token);
            }

            return done();
        }

        /// <summary>Pumps for a fixed time, for steps with nothing specific to wait on.</summary>
        public static void For(int ms = 300)
        {
            _ = Until(static () => false, ms);
        }
    }
}
