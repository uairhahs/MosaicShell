using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Modules;

/// <summary>User-facing how-to for armed capabilities (Hub tiles + module config).</summary>
public static class ModuleUsageGuide
{
    public static string Summary(string moduleId)
    {
        switch (moduleId.ToLowerInvariant())
        {
            case "inlay":
                return "Start-menu style launcher. Arm it, then press the hotkey to open pinned apps and a search box.";
            case "chord":
                return "Macro launcher (Keylaunch). Arm it, then press the hotkey to pick a named action or type a path.";
            case "substrate":
                return "Quick-settings shade. Arm it, then press the hotkey for mute, volume, and brightness tiles.";
            case "slate":
                return "Idle / screensaver clock. Arm it; after the idle timeout the clock overlay appears (unless fullscreen hide is on).";
            case "mixdeck":
                return "Per-app volume mixer. Arm it, then press the hotkey (or Tessera Pixel) to open the overlay.";
            case "tessera":
                return "System flyouts for volume, brightness, and media. Arm it to replace the OS OSD while Host runs in the tray.";
            default:
                return "";
        }
    }

    public static string HowToTrigger(string moduleId)
    {
        var id = moduleId.ToLowerInvariant();
        if (id == "inlay" || id == "chord" || id == "substrate" || id == "mixdeck")
        {
            var gesture = CurrentHotkey(moduleId);
            if (string.IsNullOrWhiteSpace(gesture))
                return "Arm from Tiles, then use the configured hotkey.";
            return "Arm from Tiles, then press " + gesture + ".";
        }

        if (id == "slate")
        {
            var s = ModuleSettingsStore.Load("Slate", () => new SlateSettings());
            return "Arm from Tiles, then leave the PC idle for " + Math.Max(30, s.IdleSeconds) + " seconds.";
        }

        if (id == "tessera")
            return "Arm from Tiles, then change volume / brightness / media (or use Try now in settings).";

        return "";
    }

    public static string CurrentHotkey(string moduleId)
    {
        var raw = moduleId.ToLowerInvariant() switch
        {
            "inlay" => ModuleSettingsStore.Load("Inlay", () => new InlaySettings()).HotkeyGesture,
            "chord" => ModuleSettingsStore.Load("Chord", () => new ChordSettings()).HotkeyGesture,
            "substrate" => ModuleSettingsStore.Load("Substrate", () => new SubstrateSettings()).HotkeyGesture,
            "mixdeck" => ModuleSettingsStore.Load("Mixdeck", () => new MixdeckSettings()).HotkeyGesture,
            _ => ""
        };
        if (string.IsNullOrWhiteSpace(raw)) return raw;
        return HotkeyGestureParser.EnsureRegisterable(moduleId, raw);
    }

    public static string ArmedStatus(string moduleId)
    {
        var hotkey = CurrentHotkey(moduleId);
        if (!string.IsNullOrWhiteSpace(hotkey))
            return "Armed - " + hotkey;
        if (moduleId.Equals("Slate", StringComparison.OrdinalIgnoreCase))
        {
            var s = ModuleSettingsStore.Load("Slate", () => new SlateSettings());
            return "Armed - idle " + Math.Max(30, s.IdleSeconds) + "s";
        }
        if (moduleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase))
            return "Armed - volume / media";
        return "Armed";
    }
}
