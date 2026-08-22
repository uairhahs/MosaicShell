using Avalonia.Controls;

namespace MosaicShell.Host.Tiles.Tessera;

public static class TesseraStyleFactory
{
    public static Control Create(string styleId, TesseraFlyoutViewModel vm) =>
        Create(styleId, vm, accentColor: null);

    public static Control Create(string styleId, TesseraFlyoutViewModel vm, string? accentColor, bool embeddedPreview = false)
    {
        TesseraPalette.ApplyAccentFromSettings(accentColor);
        var host = new TesseraLiveHost { IsEmbeddedPreview = embeddedPreview };
        TesseraLiveAmbient.Current = host.Bindings;
        if (embeddedPreview)
            TesseraGlass.EmbeddedPreviewBuild = true;
        try
        {
            host.Content = styleId.ToLowerInvariant() switch
            {
                "win11" => TesseraLayouts.Win11(vm),
                "simple" => TesseraLayouts.Simple(vm),
                "pixel" => TesseraLayouts.Pixel(vm),
                "center" => TesseraLayouts.Center(vm),
                "modern" => TesseraLayouts.Modern(vm),
                "amber" => TesseraLayouts.Amber(vm),
                "gnome" => TesseraLayouts.Gnome(vm),
                "smouti" => TesseraLayouts.Smouti(vm),
                "plainext" => TesseraLayouts.Plainext(vm),
                "coreui" => TesseraLayouts.CoreUI(vm),
                _ => TesseraLayouts.Fluent(vm),
            };
        }
        finally
        {
            TesseraLiveAmbient.Current = null;
            if (embeddedPreview)
                TesseraGlass.EmbeddedPreviewBuild = false;
        }
        return host;
    }
}
