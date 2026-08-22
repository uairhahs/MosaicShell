namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>
/// Legacy baked PNG frost — superseded by <see cref="TesseraGlass"/> Skia backdrop blur + grain.
/// Setting name retained for settings/payload compatibility.
/// </summary>
public static class TesseraBakedFrost
{
    public static void SetEnabled(bool enabled) => TesseraGlass.UseBackdropBlur = enabled;

    public static bool TryGetBrush(out Avalonia.Media.IBrush brush)
    {
        brush = Avalonia.Media.Brushes.Transparent;
        return false;
    }
}
