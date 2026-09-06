using Avalonia.Controls;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host.Tiles.Tessera
{
    /// <summary>Ambient context while building one stacked acrylic panel (H3).</summary>
    internal static class TesseraStackedBuildContext
    {
        [field: ThreadStatic]
        public static TesseraStackedPanelRole? Role { get; private set; }
        public static bool IsActive => Role is not null;
        [field: ThreadStatic]
        public static TesseraLiveBindings? Bindings { get; private set; }

        public static IDisposable Begin(TesseraStackedPanelRole role, TesseraLiveBindings bindings)
        {
            Role = role;
            Bindings = bindings;
            return new Scope();
        }

        public static Control? TryCreatePanel(
            TesseraFlyoutViewModel vm,
            Func<TesseraFlyoutViewModel, Control> buildVolume,
            Func<TesseraFlyoutViewModel, Control> buildMedia,
            Func<Control, Control>? wrapVolume = null,
            Func<Control, Control>? wrapMedia = null)
        {
            if (Role is not { } role)
            {
                return null;
            }

            Control panel = role == TesseraStackedPanelRole.Volume
                ? buildVolume(vm)
                : buildMedia(vm);
            Func<Control, Control>? wrap = role == TesseraStackedPanelRole.Volume ? wrapVolume : wrapMedia;
            return wrap is not null ? wrap(panel) : panel;
        }

        private sealed class Scope : IDisposable
        {
            public void Dispose()
            {
                Role = null;
                Bindings = null;
            }
        }
    }
}
