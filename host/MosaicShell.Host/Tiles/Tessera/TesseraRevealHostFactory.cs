using Avalonia.Controls;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>
/// Catalog-driven reveal wrap. Layout formulas stay in TesseraLayouts; Host picks
/// rest sizes and wrap kind from <see cref="TesseraFlyoutTweenTargetCatalog"/>.
/// </summary>
internal static class TesseraRevealHostFactory
{
    public static Control FromCatalog(
        TesseraFlyoutViewModel vm,
        TesseraStackedPanelRole role,
        Control panel)
    {
        if (role != TesseraStackedPanelRole.Media || panel is TesseraRevealHost)
            return panel;
        if (!vm.ShowMediaStrip || !TesseraFlyoutRevealSpec.StyleSupportsPhase2(vm.StyleId))
            return panel;

        return WrapMediaFromCatalog(vm, panel);
    }

    public static TesseraRevealHost WrapMediaFromCatalog(TesseraFlyoutViewModel vm, Control media)
    {
        var profile = TesseraFlyoutTweenTargetCatalog.ResolveProfile(vm.StyleId);
        return TesseraRevealHost.WrapMedia(
            vm,
            media,
            fullMediaWidth: profile.MediaWidthDip,
            fullMediaHeight: profile.MediaHeightDip);
    }
}
