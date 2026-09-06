using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Modules
{
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
                    return "Per-app volume mixer. Arm it, then press the hotkey (or Tessera Material You) to open the overlay.";
                case "tessera":
                    return "System flyouts for volume, brightness, and media. Arm it to replace the OS OSD while Host runs in the tray.";
                default:
                    {
                        ModuleManifest? manifest = ModuleManifest.TryLoad(moduleId);
                        return !string.IsNullOrWhiteSpace(manifest?.UsageSummary)
                            ? manifest.UsageSummary
                            : ModuleCatalog.TryGet(moduleId, out ModuleInfo? info) && info is not null
                            && !string.IsNullOrWhiteSpace(info.Description)
                            ? info.Description
                            : "";
                    }
            }
        }

        public static string HowToTrigger(string moduleId)
        {
            string id = moduleId.ToLowerInvariant();
            if (id is "inlay" or "chord" or "substrate" or "mixdeck")
            {
                string gesture = CurrentHotkey(moduleId);
                return string.IsNullOrWhiteSpace(gesture)
                    ? "Arm from Tiles, then use the configured hotkey."
                    : "Arm from Tiles, then press " + gesture + ".";
            }

            if (id == "slate")
            {
                SlateSettings s = ModuleSettingsStore.Load("Slate", () => new SlateSettings());
                return "Arm from Tiles, then leave the PC idle for " + Math.Max(30, s.IdleSeconds) + " seconds.";
            }

            if (id == "tessera")
            {
                return "Arm from Tiles, then change volume, brightness, Caps Lock, or media (track skip / media keys), or use Try now in settings.";
            }

            ModuleManifest? manifest = ModuleManifest.TryLoad(moduleId);
            return !string.IsNullOrWhiteSpace(manifest?.HowToTrigger)
                ? manifest.HowToTrigger
                : ModuleCatalog.IsCapability(moduleId)
                ? "Arm from Tiles, then use the module's configured trigger."
                : "";
        }

        public static string CurrentHotkey(string moduleId)
        {
            string raw = moduleId.ToLowerInvariant() switch
            {
                "inlay" => ModuleSettingsStore.Load("Inlay", () => new InlaySettings()).HotkeyGesture,
                "chord" => ModuleSettingsStore.Load("Chord", () => new ChordSettings()).HotkeyGesture,
                "substrate" => ModuleSettingsStore.Load("Substrate", () => new SubstrateSettings()).HotkeyGesture,
                "mixdeck" => ModuleSettingsStore.Load("Mixdeck", () => new MixdeckSettings()).HotkeyGesture,
                _ => ""
            };
            return string.IsNullOrWhiteSpace(raw) ? raw : HotkeyGestureParser.EnsureRegisterable(moduleId, raw);
        }

        public static string ArmedStatus(string moduleId)
        {
            string hotkey = CurrentHotkey(moduleId);
            if (!string.IsNullOrWhiteSpace(hotkey))
            {
                return "Armed - " + hotkey;
            }

            if (moduleId.Equals("Slate", StringComparison.OrdinalIgnoreCase))
            {
                SlateSettings s = ModuleSettingsStore.Load("Slate", () => new SlateSettings());
                return "Armed - idle " + Math.Max(30, s.IdleSeconds) + "s";
            }
            return moduleId.Equals("Tessera", StringComparison.OrdinalIgnoreCase) ? "Armed - volume / media" : "Armed";
        }
    }
}
