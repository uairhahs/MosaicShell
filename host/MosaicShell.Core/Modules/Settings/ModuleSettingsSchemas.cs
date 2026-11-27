using MosaicShell.Core.HostPlatform;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Modules.Settings
{
    /// <summary>
    /// The module pages the Hub draws from a schema: every built-in module except Tessera, whose page stays its own
    /// until it moves onto the same template.
    /// </summary>
    public static class ModuleSettingsSchemas
    {
        private const SettingApplyEffect Live = SettingApplyEffect.Live;
        private const SettingApplyEffect ReArm = SettingApplyEffect.ReArm;
        private const SettingApplyEffect Relaunch = SettingApplyEffect.RelaunchWidget;

        private static readonly IReadOnlyList<ModuleSettingsSchema> All =
        [
            Mixdeck(),
            Inlay(),
            Slate(),
            Chord(),
            Substrate(),
            Chrono(),
            Phono(),
            Pulse(),
            Canvas(),
        ];

        /// <summary>The modules with a schema page, in catalog order.</summary>
        public static IReadOnlyList<string> GenericModuleIds { get; } = [.. All.Select(s => s.ModuleId)];

        /// <summary>
        /// The page for a module: a built-in's own, else the one an installed manifest declares. Built-ins win on an id
        /// collision, as their factories do.
        /// </summary>
        public static bool TryGet(string moduleId, out ModuleSettingsSchema? schema)
        {
            schema = All.FirstOrDefault(s => s.ModuleId.Equals(moduleId, StringComparison.OrdinalIgnoreCase))
                ?? (ModuleCatalog.IsBundled(moduleId) || ModuleManifest.TryLoad(moduleId) is not ModuleManifest manifest
                    ? null
                    : ManifestSettingsSchema.From(manifest));
            return schema is not null;
        }

        private static ModuleSettingsSchema Mixdeck()
        {
            return ModuleSettingsSchema.For<MixdeckSettings>(ModuleIds.Mixdeck)
                .Section("Look")
                .Style(s => s.Style, (s, v) => s.Style = v, Live)
                .Choice(
                    "ColorScheme",
                    "Colours",
                    [
                        new("Default", "Match the style"),
                        new("Dark", "Dark"),
                        new("Light", "Light"),
                        new("Accent", "Accent"),
                        new("Frost", "Frost"),
                        new("Midnight", "Midnight"),
                        new("Sunset", "Sunset"),
                    ],
                    s => s.ColorScheme,
                    (s, v) => s.ColorScheme = v,
                    Live)
                .Section("Opening and closing")
                .Hotkey("HotkeyGesture", "Shortcut", s => s.HotkeyGesture, (s, v) => s.HotkeyGesture = v, "Opens the mixer from any app.")
                .Toggle("CloseOnEscape", "Close with Esc", s => s.CloseOnEscape, (s, v) => s.CloseOnEscape = v, Live)
                .Build();
        }

        private static ModuleSettingsSchema Inlay()
        {
            return ModuleSettingsSchema.For<InlaySettings>(ModuleIds.Inlay)
                .Section("Look")
                .Style(s => s.Style, (s, v) => s.Style = v, Live)
                .Section("Opening and closing")
                .Hotkey("HotkeyGesture", "Shortcut", s => s.HotkeyGesture, (s, v) => s.HotkeyGesture = v, "Opens the launcher from any app.")
                .Toggle("CloseOnEscape", "Close with Esc", s => s.CloseOnEscape, (s, v) => s.CloseOnEscape = v, Live)
                .Section("Pinned apps")
                .TextList(
                    "Pins",
                    "Pinned apps",
                    new InlaySettings().Pins,
                    s => s.Pins,
                    (s, v) => s.Pins = v,
                    Live,
                    "Shown first, before you type.")
                .Build();
        }

        private static ModuleSettingsSchema Slate()
        {
            return ModuleSettingsSchema.For<SlateSettings>(ModuleIds.Slate)
                .Section("Look")
                .Style(s => s.Style, (s, v) => s.Style = v, Live)
                .Section("When it shows")
                .WholeNumber(
                    "IdleSeconds",
                    "Show after no input for",
                    min: 30,
                    max: 3600,
                    step: 30,
                    unit: "s",
                    s => s.IdleSeconds,
                    (s, v) => s.IdleSeconds = v,
                    ReArm)
                .Toggle(
                    "HideOnFullscreen",
                    "Not over a full-screen app",
                    s => s.HideOnFullscreen,
                    (s, v) => s.HideOnFullscreen = v,
                    ReArm,
                    "Stays hidden while a video or game fills the screen.",
                    DesktopFeature.FullscreenProbe)
                .Build();
        }

        private static ModuleSettingsSchema Chord()
        {
            return ModuleSettingsSchema.For<ChordSettings>(ModuleIds.Chord)
                .Section("Look")
                .Style(s => s.Style, (s, v) => s.Style = v, Live)
                .Section("Opening and closing")
                .Hotkey("HotkeyGesture", "Shortcut", s => s.HotkeyGesture, (s, v) => s.HotkeyGesture = v, "Opens your actions from any app.")
                .Toggle("CloseOnEscape", "Close with Esc", s => s.CloseOnEscape, (s, v) => s.CloseOnEscape = v, Live)
                .Section("Actions")
                .ActionList(
                    "Actions",
                    "Actions",
                    s => s.Actions.Select(a => new SettingPair(a.Name, a.Target)),
                    (s, v) => s.Actions = [.. v.Select(p => new ChordAction { Name = p.Name, Target = p.Value })],
                    Live,
                    "Each one opens an app, a file or a link.")
                .Build();
        }

        private static ModuleSettingsSchema Substrate()
        {
            return ModuleSettingsSchema.For<SubstrateSettings>(ModuleIds.Substrate)
                .Section("Look")
                .Style(s => s.Style, (s, v) => s.Style = v, Live)
                .Section("Opening and closing")
                .Hotkey("HotkeyGesture", "Shortcut", s => s.HotkeyGesture, (s, v) => s.HotkeyGesture = v, "Opens the quick settings from any app.")
                .Toggle("CloseOnEscape", "Close with Esc", s => s.CloseOnEscape, (s, v) => s.CloseOnEscape = v, Live)
                .Section("What to show")
                .Toggle("ShowMute", "Mute button", s => s.ShowMute, (s, v) => s.ShowMute = v, Live)
                .Build();
        }

        private static ModuleSettingsSchema Chrono()
        {
            return ModuleSettingsSchema.For<ChronoSettings>(ModuleIds.Chrono)
                .Section("Look")
                .Style(s => s.Style, (s, v) => s.Style = v, Relaunch)
                .Section("Time")
                .Toggle("TwentyFourHour", "24-hour clock", s => s.TwentyFourHour, (s, v) => s.TwentyFourHour = v, Relaunch)
                .Toggle("ShowSeconds", "Show seconds", s => s.ShowSeconds, (s, v) => s.ShowSeconds = v, Relaunch)
                .Build();
        }

        private static ModuleSettingsSchema Phono()
        {
            return ModuleSettingsSchema.For<PhonoSettings>(ModuleIds.Phono)
                .Section("Look")
                .Style(s => s.Style, (s, v) => s.Style = v, Relaunch)
                .Section("What to show")
                .Toggle("ShowArtist", "Artist", s => s.ShowArtist, (s, v) => s.ShowArtist = v, Relaunch)
                .Build();
        }

        private static ModuleSettingsSchema Pulse()
        {
            return ModuleSettingsSchema.For<PulseSettings>(ModuleIds.Pulse)
                .Section("Look")
                .Style(s => s.Style, (s, v) => s.Style = v, Relaunch)
                .Choice(
                    "VisualizerType",
                    "Shape",
                    [new("Bar", "Bars"), new("Round", "Round")],
                    s => s.VisualizerType,
                    (s, v) => s.VisualizerType = v,
                    Relaunch)
                .Build();
        }

        private static ModuleSettingsSchema Canvas()
        {
            return ModuleSettingsSchema.For<CanvasSettings>(ModuleIds.Canvas)
                .Section("Look")
                .Style(s => s.Style, (s, v) => s.Style = v, Relaunch)
                .Section("What to show")
                .Toggle("ShowHost", "Computer name", s => s.ShowHost, (s, v) => s.ShowHost = v, Relaunch)
                .Toggle("ShowCpu", "CPU", s => s.ShowCpu, (s, v) => s.ShowCpu = v, Relaunch)
                .Toggle("ShowRam", "Memory", s => s.ShowRam, (s, v) => s.ShowRam = v, Relaunch)
                .Toggle("ShowDisk", "Disk", s => s.ShowDisk, (s, v) => s.ShowDisk = v, Relaunch)
                .Build();
        }
    }
}
