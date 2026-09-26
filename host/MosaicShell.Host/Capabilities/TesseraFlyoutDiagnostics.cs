using MosaicShell.Core;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Services;

namespace MosaicShell.Host.Capabilities
{
    /// <summary>Tessera flyout log under %LocalAppData%/MosaicShell/Cache/flyout.log, size-capped by <see cref="DiagnosticLog"/>.</summary>
    internal static class TesseraFlyoutDiagnostics
    {
        public static void Log(string message)
        {
            DiagnosticLog.Append("flyout.log", message);
            Console.WriteLine($"[Tessera flyout] {message}");
        }

        public static void LogException(string context, Exception ex)
        {
            Log($"EXCEPTION {context} {ex.GetType().Name}: {ex.Message}");
            Log(ex.StackTrace ?? "(no stack)");
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
