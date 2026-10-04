using System.Runtime.InteropServices;
using MosaicShell.Core.Services;

namespace MosaicShell.Host.Capabilities
{
    /// <summary>
    /// Opts the Host out of Windows power throttling. Windows 11 may run a process whose windows are
    /// all minimized, occluded or otherwise "not visible to the user" at EcoQoS and ignore its timer
    /// resolution. The Host's own visible surfaces are no-activate tool windows (flyouts, the focus
    /// dim), so with the Hub minimized the process can be treated as background while it animates
    /// on screen. Measured 2026-10-04: with the Hub open, flyout entrances render frames about 10 ms
    /// apart and phase 2 takes about 400 ms; with the Hub minimized, 40 to 70 ms and about 1000 ms.
    /// A shell overlay has to animate on time whatever its settings window is doing.
    /// </summary>
    internal static class ProcessPowerThrottling
    {
        private const int ProcessPowerThrottlingClass = 4;
        private const uint CurrentVersion = 1;
        private const uint ExecutionSpeed = 0x1;
        private const uint IgnoreTimerResolution = 0x4;

        [StructLayout(LayoutKind.Sequential)]
        private struct PowerThrottlingState
        {
            public uint Version;
            public uint ControlMask;
            public uint StateMask;
        }

        /// <summary>Applies the opt-out and returns a one-line result for the log.</summary>
        public static string OptOut()
        {
            if (!OperatingSystem.IsWindows())
            {
                return "skipped (non-Windows)";
            }

            // StateMask 0 with a bit in ControlMask means "never throttle this", per the
            // SetProcessInformation documentation. Timer resolution control needs Windows 11; on
            // older systems fall back to execution speed alone.
            return TrySet(ExecutionSpeed | IgnoreTimerResolution)
                ? "execution speed and timer resolution: not throttled"
                : TrySet(ExecutionSpeed)
                ? "execution speed: not throttled (timer resolution control unavailable)"
                : $"failed err={Marshal.GetLastWin32Error()}";
        }

        private static bool TrySet(uint controlMask)
        {
            PowerThrottlingState state = new()
            {
                Version = CurrentVersion,
                ControlMask = controlMask,
                StateMask = 0,
            };
            return SetProcessInformation(
                GetCurrentProcess(), ProcessPowerThrottlingClass, ref state, (uint)Marshal.SizeOf<PowerThrottlingState>());
        }

        public static void OptOutAndLog()
        {
            DiagnosticLog.Append("flyout.log", DiagnosticLogLevel.Info, $"power throttling opt-out: {OptOut()}");
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetProcessInformation(
            IntPtr hProcess, int processInformationClass, ref PowerThrottlingState processInformation, uint size);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();
    }
}
