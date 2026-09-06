using Avalonia.Controls;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host.Tiles.Tessera
{
    internal static class TesseraStackedPanelFactory
    {
        public static Control CreatePanel(
            string styleId,
            TesseraFlyoutViewModel vm,
            TesseraStackedPanelRole role,
            TesseraLiveBindings bindings,
            string? accentColor,
            bool embeddedPreview = false,
            bool sessionAlreadyShowing = false)
        {
            using IDisposable _ = TesseraStackedBuildContext.Begin(role, bindings);
            using IDisposable revealCtx = TesseraRevealBuildContext.Begin(embeddedPreview, vm.Ani, sessionAlreadyShowing);
            TesseraPalette.ApplyAccentFromSettings(accentColor);
            TesseraLiveAmbient.Current = bindings;
            if (embeddedPreview)
            {
                TesseraGlass.EmbeddedPreviewBuild = true;
            }

            try
            {
                Control panel = TesseraStyleFactory.CreateLayoutPanel(styleId, vm);
                return role == TesseraStackedPanelRole.Volume
                    ? new TesseraLiveHost(bindings)
                    {
                        IsEmbeddedPreview = embeddedPreview,
                        Content = panel
                    }
                    : TesseraRevealHostFactory.FromCatalog(vm, role, panel);
            }
            finally
            {
                TesseraLiveAmbient.Current = null;
                if (embeddedPreview)
                {
                    TesseraGlass.EmbeddedPreviewBuild = false;
                }
            }
        }
    }
}
