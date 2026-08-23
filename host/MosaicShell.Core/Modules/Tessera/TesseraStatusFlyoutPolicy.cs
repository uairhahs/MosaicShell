namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// Ephemeral status flyouts (caps/airplane). Warm toggles Patch in place (dismiss timer
/// reset via TryApplyLive); cold/hidden sessions Present/revive via TesseraFlyoutRefreshPolicy.
/// </summary>
public static class TesseraStatusFlyoutPolicy
{
    public static bool IsStatusKind(string kind) =>
        kind.Equals("locks", StringComparison.OrdinalIgnoreCase)
        || kind.Equals("flight", StringComparison.OrdinalIgnoreCase);
}
