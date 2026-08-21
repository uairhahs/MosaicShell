namespace MosaicShell.Core.Capabilities;

/// <summary>Nine-point work-area anchoring used by Tessera (and other flyouts).</summary>
public static class FlyoutAnchor
{
    private static readonly HashSet<string> Known =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "TL", "TC", "TR", "CL", "CC", "CR", "BL", "BC", "BR"
        };

    /// <summary>Normalize to a known code; unknown/empty → TL (JaxCore default).</summary>
    public static string Normalize(string? position)
    {
        var p = (position ?? "").Trim().ToUpperInvariant();
        return Known.Contains(p) ? p : "TL";
    }

    /// <summary>
    /// Place a window of size (windowW×windowH) inside the work area.
    /// All arguments must be in the same unit (typically physical pixels).
    /// Result is clamped so the window stays fully on-screen when it fits.
    /// </summary>
    public static (int X, int Y) Compute(
        int workX, int workY, int workW, int workH,
        int windowW, int windowH,
        string position,
        int xPad,
        int yPad)
    {
        var p = Normalize(position);
        // Treat negative/oversized pads as 0 — stale JSON sometimes stores junk
        xPad = Math.Clamp(xPad, 0, Math.Max(0, workW / 2));
        yPad = Math.Clamp(yPad, 0, Math.Max(0, workH / 2));
        windowW = Math.Max(1, windowW);
        windowH = Math.Max(1, windowH);

        var x = p switch
        {
            "TL" or "CL" or "BL" => workX + xPad,
            "TC" or "CC" or "BC" => workX + (workW - windowW) / 2,
            _ => workX + workW - windowW - xPad // TR/CR/BR
        };
        var y = p switch
        {
            "TL" or "TC" or "TR" => workY + yPad,
            "CL" or "CC" or "CR" => workY + (workH - windowH) / 2,
            _ => workY + workH - windowH - yPad // BL/BC/BR
        };

        return ClampToWorkArea(workX, workY, workW, workH, windowW, windowH, x, y);
    }

    /// <summary>Keep the window rectangle inside the work area when possible.</summary>
    public static (int X, int Y) ClampToWorkArea(
        int workX, int workY, int workW, int workH,
        int windowW, int windowH,
        int x, int y)
    {
        if (workW <= 0 || workH <= 0) return (x, y);

        var maxX = workX + Math.Max(0, workW - windowW);
        var maxY = workY + Math.Max(0, workH - windowH);
        x = Math.Clamp(x, workX, maxX);
        y = Math.Clamp(y, workY, maxY);
        return (x, y);
    }
}
