using Avalonia.Media;
using Material.Icons;
using MosaicShell.Core.Modules;

namespace MosaicShell.Host;

internal static class HubGlyphUi
{
    public static MaterialIconKind Kind(HubGlyph glyph) =>
        Enum.TryParse(glyph.Kind, ignoreCase: false, out MaterialIconKind kind)
            ? kind
            : MaterialIconKind.Apps;

    public static IBrush Brush(HubGlyph glyph) =>
        new SolidColorBrush(Color.Parse(glyph.TintHex));

    public static IBrush StatusBrush(HubTileStatusKind kind) =>
        new SolidColorBrush(Color.Parse(HubTileStatusChromeSpec.HexFor(kind)));
}
