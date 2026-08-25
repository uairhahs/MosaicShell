using Avalonia.Controls;
using MosaicShell.Core.Styles;

namespace MosaicShell.Host.Tiles.Tessera;

public static class TesseraStyleFactory
{
    public static Control Create(string styleId, TesseraFlyoutViewModel vm) =>
        Create(styleId, vm, accentColor: null);

    public static Control Create(
        string styleId,
        TesseraFlyoutViewModel vm,
        string? accentColor,
        bool embeddedPreview = false,
        bool sessionAlreadyShowing = false)
    {
        TesseraPalette.ApplyAccentFromSettings(accentColor);
        var host = new TesseraLiveHost { IsEmbeddedPreview = embeddedPreview };
        TesseraLiveAmbient.Current = host.Bindings;
        if (embeddedPreview)
            TesseraGlass.EmbeddedPreviewBuild = true;
        using var revealCtx = TesseraRevealBuildContext.Begin(embeddedPreview, vm.Ani, sessionAlreadyShowing);
        try
        {
            host.Content = CreateLayoutPanel(styleId, vm);
        }
        finally
        {
            TesseraLiveAmbient.Current = null;
            if (embeddedPreview)
                TesseraGlass.EmbeddedPreviewBuild = false;
        }
        return host;
    }

    internal static Control CreateLayoutPanel(string styleId, TesseraFlyoutViewModel vm) =>
        StyleIds.Normalize(styleId) switch
        {
            StyleIds.Windows11 => TesseraLayouts.Windows11(vm),
            StyleIds.Compact => TesseraLayouts.Compact(vm),
            StyleIds.MaterialYou => TesseraLayouts.MaterialYou(vm),
            StyleIds.Square => TesseraLayouts.Square(vm),
            StyleIds.ModernFlyouts => TesseraLayouts.ModernFlyouts(vm),
            StyleIds.Meter => TesseraLayouts.Meter(vm),
            StyleIds.Gnome => TesseraLayouts.Gnome(vm),
            StyleIds.Radial => TesseraLayouts.Radial(vm),
            StyleIds.PlainText => TesseraLayouts.PlainText(vm),
            StyleIds.CoreUI => TesseraLayouts.CoreUI(vm),
            _ => TesseraLayouts.Fluent(vm),
        };
}
