namespace MosaicShell.Core.Capabilities;

/// <summary>Status chip text for locks / flight flyouts.</summary>
public static class TesseraStatusLabels
{
    public static string Format(FlyoutRequest request)
    {
        var on = request.Payload?.GetValueOrDefault("on") == "1";
        if (request.Kind.Equals("flight", StringComparison.OrdinalIgnoreCase))
            return on ? "Airplane mode On" : "Airplane mode Off";
        var lockName = request.Payload?.GetValueOrDefault("lock") ?? "CapsLock";
        return on ? $"{lockName} On" : $"{lockName} Off";
    }
}
