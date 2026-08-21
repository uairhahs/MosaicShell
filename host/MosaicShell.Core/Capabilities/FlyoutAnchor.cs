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

    public static (int X, int Y) Compute(
        int workX, int workY, int workW, int workH,
        int windowW, int windowH,
        string position,
        int xPad,
        int yPad)
    {
        var p = Normalize(position);
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
        return (x, y);
    }
}
