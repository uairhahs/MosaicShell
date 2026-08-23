using MosaicShell.Core;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host.Capabilities;

/// <summary>Append-only Tessera flyout log under %LocalAppData%/MosaicShell/Cache/flyout.log.</summary>
internal static class TesseraFlyoutDiagnostics
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MosaicShell", "Cache", "flyout.log");

    public static void Log(string message)
    {
        try
        {
            AppPaths.EnsureLayout();
            File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch
        {
            // ignore
        }

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
