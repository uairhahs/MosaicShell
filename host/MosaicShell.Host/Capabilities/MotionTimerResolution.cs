using System.Runtime.InteropServices;

namespace MosaicShell.Host.Capabilities
{
    /// <summary>
    /// Raises the system timer resolution to 1 ms while a motion runs, so a <c>Task.Delay</c> of a few milliseconds is a few
    /// milliseconds and not the next 15.6 ms tick. Scoped to one run; Windows keeps the finest request any process holds.
    /// </summary>
    internal sealed class MotionTimerResolution : IDisposable
    {
        private const uint PeriodMs = 1;
        private bool _active;

        private MotionTimerResolution()
        {
        }

        public static MotionTimerResolution Begin()
        {
            MotionTimerResolution scope = new();
            try
            {
                scope._active = TimeBeginPeriod(PeriodMs) == 0;
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                scope._active = false;
            }

            return scope;
        }

        public void Dispose()
        {
            if (_active)
            {
                _active = false;
                _ = TimeEndPeriod(PeriodMs);
            }
        }

        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern uint TimeBeginPeriod(uint period);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static extern uint TimeEndPeriod(uint period);
    }
}
