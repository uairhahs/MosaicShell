using Avalonia.Controls;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Host.Tiles.Tessera;

/// <summary>Ambient context while building one stacked acrylic panel (H3).</summary>
internal static class TesseraStackedBuildContext
{
    [ThreadStatic] private static TesseraStackedPanelRole? _role;
    [ThreadStatic] private static TesseraLiveBindings? _bindings;

    public static TesseraStackedPanelRole? Role => _role;
    public static bool IsActive => _role is not null;
    public static TesseraLiveBindings? Bindings => _bindings;

    public static IDisposable Begin(TesseraStackedPanelRole role, TesseraLiveBindings bindings)
    {
        _role = role;
        _bindings = bindings;
        return new Scope();
    }

    public static Control? TryCreatePanel(
        TesseraFlyoutViewModel vm,
        Func<TesseraFlyoutViewModel, Control> buildVolume,
        Func<TesseraFlyoutViewModel, Control> buildMedia,
        Func<Control, Control>? wrapVolume = null,
        Func<Control, Control>? wrapMedia = null)
    {
        if (_role is not { } role)
            return null;

        var panel = role == TesseraStackedPanelRole.Volume
            ? buildVolume(vm)
            : buildMedia(vm);
        var wrap = role == TesseraStackedPanelRole.Volume ? wrapVolume : wrapMedia;
        return wrap is not null ? wrap(panel) : panel;
    }

    private sealed class Scope : IDisposable
    {
        public void Dispose()
        {
            _role = null;
            _bindings = null;
        }
    }
}
