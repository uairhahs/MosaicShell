using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Modules;

/// <summary>Overlay chrome settings shared by capability overlays.</summary>
public static class ModuleOverlaySettings
{
    public static bool CloseOnEscape(string moduleId) => moduleId switch
    {
        "Mixdeck" => MosaicShell.Core.Runtime.ModuleSettingsStore.Load("Mixdeck", () => new MixdeckSettings()).CloseOnEscape,
        "Inlay" => MosaicShell.Core.Runtime.ModuleSettingsStore.Load("Inlay", () => new InlaySettings()).CloseOnEscape,
        "Chord" => MosaicShell.Core.Runtime.ModuleSettingsStore.Load("Chord", () => new ChordSettings()).CloseOnEscape,
        "Substrate" => MosaicShell.Core.Runtime.ModuleSettingsStore.Load("Substrate", () => new SubstrateSettings()).CloseOnEscape,
        _ => true
    };
}

/// <summary>Contract for widget tile overlay context menu (Host must honor).</summary>
public static class TileOverlayChromeSpec
{
    public static readonly string[] RequiredContextMenuHeaders =
    [
        "Configure in Host",
        "Align",
        "Change Z layer",
        "Refresh",
        "Unload"
    ];
}
