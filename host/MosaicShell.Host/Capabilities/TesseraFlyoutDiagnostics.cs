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
