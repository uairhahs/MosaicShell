using Avalonia.Controls;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host.Tiles.Tessera;

internal static class TesseraStackedPanelFactory
{
    public static Control CreatePanel(
        string styleId,
        TesseraFlyoutViewModel vm,
        TesseraStackedPanelRole role,
        TesseraLiveBindings bindings,
        string? accentColor,
        bool embeddedPreview = false)
    {
        using var _ = TesseraStackedBuildContext.Begin(role, bindings);
        using var revealCtx = TesseraRevealBuildContext.Begin(embeddedPreview, vm.Ani);
        TesseraPalette.ApplyAccentFromSettings(accentColor);
        TesseraLiveAmbient.Current = bindings;
        if (embeddedPreview)
            TesseraGlass.EmbeddedPreviewBuild = true;
        try
        {
            var panel = TesseraStyleFactory.CreateLayoutPanel(styleId, vm);
            if (role == TesseraStackedPanelRole.Volume)
            {
                return new TesseraLiveHost(bindings)
                {
                    IsEmbeddedPreview = embeddedPreview,
                    Content = panel
                };
            }

            if (vm.ShowMediaStrip && TesseraFlyoutRevealSpec.StyleSupportsPhase2(styleId))
                panel = TesseraRevealHost.WrapMedia(vm, panel);

            return panel;
        }
        finally
        {
            TesseraLiveAmbient.Current = null;
            if (embeddedPreview)
                TesseraGlass.EmbeddedPreviewBuild = false;
        }
    }
}
