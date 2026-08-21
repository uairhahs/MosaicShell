namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>Host hooks used by Tessera Pixel layout. Mixdeck opens the native overlay skeleton (not Rainmeter Mixdeck).</summary>
public static class TesseraHostBridge
{
    public static Func<Task>? ArmMixdeckAsync { get; set; }
    public static Action? PreviewVolumeFlyout { get; set; }
}
