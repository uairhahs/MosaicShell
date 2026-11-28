using System.Globalization;
using MosaicShell.Core.Capabilities.BuiltIn;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Modules
{
    /// <summary>
    /// What a module is (its page's subtitle) and how to use it once it is on (the page and the turn-on toast). In the
    /// words the Hub shows: a module is on or off; nothing is armed, and there are no tiles.
    /// </summary>
    public static class ModuleUsageGuide
    {
        public static string Summary(string moduleId)
        {
            switch (moduleId.ToLowerInvariant())
            {
                case "inlay":
                    return "A start-menu launcher with your pinned apps and a search box, opened with a shortcut.";
                case "chord":
                    return "A macro launcher: pick a named action or type a path, opened with a shortcut.";
                case "substrate":
                    return "A quick-settings shade with mute, volume and brightness, opened with a shortcut.";
                case "slate":
                    return "A clock that covers the screen when you step away. It can stay hidden while an app is full screen.";
                case "mixdeck":
                    return "A volume mixer with a slider for each app, opened with a shortcut.";
                case "tessera":
                    return "Volume, brightness and media pop-ups that replace the system's own while MosaicShell runs.";
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

        /// <summary>
        /// The how-to line for this desktop: where a shortcut module's shortcut cannot work, the line says so instead
        /// of naming a key.
        /// </summary>
        public static string HowToTrigger(string moduleId, HostPlatform.DesktopFeatures features)
        {
            return moduleId.ToLowerInvariant() is "inlay" or "chord" or "substrate" or "mixdeck"
                && features.MissingText(HostPlatform.DesktopFeature.GlobalHotkeys) is string missing
                ? missing
                : HowToTrigger(moduleId);
        }

        public static string HowToTrigger(string moduleId)
        {
            string id = moduleId.ToLowerInvariant();
            if (Opens(id) is string opens)
            {
                string gesture = CurrentHotkey(moduleId);
                return string.IsNullOrWhiteSpace(gesture)
                    ? $"Set a shortcut below to open {opens}."
                    : $"Press {gesture} to open {opens}.";
            }

            if (id == "slate")
            {
                return $"Shows after {IdleText()} without input.";
            }

            if (id == "tessera")
            {
                return "Its pop-up shows when you change the volume or brightness, or press Caps Lock or a media key.";
            }

            // A third-party module says how it is used in its manifest; without that, nothing is better than a guess.
            ModuleManifest? manifest = ModuleManifest.TryLoad(moduleId);
            return !string.IsNullOrWhiteSpace(manifest?.HowToTrigger) ? manifest.HowToTrigger : "";
        }

        /// <summary>What a shortcut module's shortcut opens ("the mixer"), or null for a module without one.</summary>
        public static string? Opens(string moduleId)
        {
            return moduleId.ToLowerInvariant() switch
            {
                "mixdeck" => "the mixer",
                "inlay" => "the launcher",
                "chord" => "your actions",
                "substrate" => "the quick settings",
                _ => null,
            };
        }

        /// <summary>Slate's idle time as the Hub says it: "45 s", "10 min"; never under 30 seconds.</summary>
        public static string IdleText()
        {
            int seconds = Math.Max(30, ModuleSettingsStore.Load("Slate", () => new SlateSettings()).IdleSeconds);
            return seconds % 60 == 0 && seconds >= 120
                ? (seconds / 60).ToString(CultureInfo.InvariantCulture) + " min"
                : seconds.ToString(CultureInfo.InvariantCulture) + " s";
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

    }
}
