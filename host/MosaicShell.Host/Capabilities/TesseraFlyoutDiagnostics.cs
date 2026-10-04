using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;

namespace MosaicShell.Host.Capabilities
{
    /// <summary>
    /// Tessera flyout log under %LocalAppData%/MosaicShell/Cache/flyout.log. Every call only queues
    /// (see <see cref="DiagnosticLog"/>), so it is safe on the UI thread.
    /// </summary>
    internal static class TesseraFlyoutDiagnostics
    {
        private const string FileName = "flyout.log";

        public static bool IsEnabled(DiagnosticLogLevel level)
        {
            return DiagnosticLog.IsEnabled(level);
        }

        public static void Log(string message)
        {
            Log(DiagnosticLogLevel.Info, message);
        }

        public static void Log(DiagnosticLogLevel level, string message)
        {
            DiagnosticLog.Append(FileName, level, message);
        }

        /// <summary>
        /// Debug only: start a timed span for UI-thread work. Returns 0 when Debug is off, so the
        /// matching <see cref="EndSpan"/> costs nothing.
        /// </summary>
        public static long BeginSpan()
        {
            return IsEnabled(DiagnosticLogLevel.Debug) ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        }

        /// <summary>
        /// Logs <c>slow name=… ms=…</c> when the span took at least <paramref name="thresholdMs"/>.
        /// Added to attribute the UI-thread stalls seen in the motion frame trace (frames 50 to 200 ms
        /// apart during entrances that change track).
        /// </summary>
        public static void EndSpan(long started, string name, double thresholdMs = 4)
        {
            if (started == 0)
            {
                return;
            }

            double ms = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            if (ms >= thresholdMs)
            {
                Log(DiagnosticLogLevel.Debug, $"slow {name} ms={ms:0.0}");
            }
        }

        public static void LogException(string context, Exception ex)
        {
            // One entry: the stack trace's line breaks are escaped, so it cannot interleave with
            // lines from other threads.
            Log(DiagnosticLogLevel.Error, $"EXCEPTION {context} {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace ?? "(no stack)"}");
        }


        public static void LogStackedPlacement(
            string? styleId,
            TesseraStackedPanelRole role,
            double estW,
            double estH,
            double measuredW,
            double measuredH,
            double placementW,
            double placementH,
            double offsetX,
            double offsetY,
            double clientW,
            double clientH,
            int posX,
            int posY)
        {
            Log(
                $"stacked size style={styleId} role={role} " +
                $"est={estW:F0}x{estH:F0} measured={measuredW:F0}x{measuredH:F0} " +
                $"placement={placementW:F0}x{placementH:F0}@{offsetX:F0},{offsetY:F0} " +
                $"client={clientW:F0}x{clientH:F0} pos={posX},{posY}");
        }
    }
}
