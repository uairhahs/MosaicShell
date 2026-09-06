using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Capabilities.BuiltIn
{
    /// <summary>Armed hotkey opens Host overlay via <see cref="IHostUiBridge"/>.</summary>
    public class HotkeyOverlayCapability : IModuleCapability
    {
        private readonly HostServices _services;
        private readonly string _hotkeyId;
        private readonly string _overlayModuleId;
        private readonly Func<string> _gesture;
        private readonly Action<string>? _persistGesture;
        private readonly IHostUiBridge _hostUi;

        public HotkeyOverlayCapability(
            string moduleId,
            ICapabilityContext context,
            Func<string> gesture,
            Action<string>? persistGesture = null)
        {
            ModuleId = moduleId;
            _overlayModuleId = moduleId;
            _services = context.Services;
            _hotkeyId = "cap:" + moduleId;
            _gesture = gesture;
            _hostUi = context.Ui.HostUi;
            _persistGesture = persistGesture;
        }

        /// <summary>Legacy ctor for tests that pass services/ui directly.</summary>
        public HotkeyOverlayCapability(
            string moduleId,
            HostServices services,
            Func<string> gesture,
            IHostUiBridge hostUi,
            Action<string>? persistGesture = null)
        {
            ModuleId = moduleId;
            _overlayModuleId = moduleId;
            _services = services;
            _hotkeyId = "cap:" + moduleId;
            _gesture = gesture;
            _hostUi = hostUi;
            _persistGesture = persistGesture;
        }

        public string ModuleId { get; }
        public bool IsArmed { get; private set; }
        public bool HotkeyRegistered { get; private set; }
        public string? HotkeyError { get; private set; }

        public Task ArmAsync(CancellationToken cancellationToken = default)
        {
            if (IsArmed)
            {
                return Task.CompletedTask;
            }

            HotkeyRegistered = false;
            HotkeyError = null;
            string raw = _gesture() ?? "";
            string gesture = HotkeyGestureParser.EnsureRegisterable(ModuleId, raw);
            if (!string.Equals(raw.Trim(), gesture, StringComparison.OrdinalIgnoreCase))
            {
                _persistGesture?.Invoke(gesture);
            }

            if (!HotkeyGestureParser.TryParse(gesture, out ModifierKeys mods, out int vk))
            {
                HotkeyError = $"Could not parse hotkey '{raw}'.";
                IsArmed = true;
                return Task.CompletedTask;
            }

            if (!_services.Hotkeys.Register(_hotkeyId, mods, vk, OnHotkey))
            {
                HotkeyError =
                    $"Could not register {gesture} (in use by Windows or another app). Try Ctrl+Alt+Letter.";
                IsArmed = true;
                return Task.CompletedTask;
            }

            HotkeyRegistered = true;
            IsArmed = true;
            return Task.CompletedTask;
        }

        public Task DisarmAsync(CancellationToken cancellationToken = default)
        {
            if (!IsArmed)
            {
                return Task.CompletedTask;
            }

            _services.Hotkeys.Unregister(_hotkeyId);
            HotkeyRegistered = false;
            HotkeyError = null;
            IsArmed = false;
            return Task.CompletedTask;
        }

        private void OnHotkey()
        {
            _ = _hostUi.OpenOverlayAsync(_overlayModuleId);
        }

        public void Dispose()
        {
            DisarmAsync().GetAwaiter().GetResult();
        }
    }

    public sealed class MixdeckCapability(ICapabilityContext context) : HotkeyOverlayCapability("Mixdeck", context,
            () => ModuleSettingsStore.Load("Mixdeck", () => new MixdeckSettings()).HotkeyGesture,
            PersistMixdeck)
    {
        private static void PersistMixdeck(string g)
        {
            MixdeckSettings s = ModuleSettingsStore.Load("Mixdeck", () => new MixdeckSettings());
            s.HotkeyGesture = g;
            ModuleSettingsStore.Save("Mixdeck", s);
        }
    }

    public sealed class InlayCapability(ICapabilityContext context) : HotkeyOverlayCapability("Inlay", context,
            () => ModuleSettingsStore.Load("Inlay", () => new InlaySettings()).HotkeyGesture,
            PersistInlay)
    {
        private static void PersistInlay(string g)
        {
            InlaySettings s = ModuleSettingsStore.Load("Inlay", () => new InlaySettings());
            s.HotkeyGesture = g;
            ModuleSettingsStore.Save("Inlay", s);
        }
    }

    public sealed class ChordCapability(ICapabilityContext context) : HotkeyOverlayCapability("Chord", context,
            () => ModuleSettingsStore.Load("Chord", () => new ChordSettings()).HotkeyGesture,
            PersistChord)
    {
        private static void PersistChord(string g)
        {
            ChordSettings s = ModuleSettingsStore.Load("Chord", () => new ChordSettings());
            s.HotkeyGesture = g;
            ModuleSettingsStore.Save("Chord", s);
        }
    }

    public sealed class SubstrateCapability(ICapabilityContext context) : HotkeyOverlayCapability("Substrate", context,
            () => ModuleSettingsStore.Load("Substrate", () => new SubstrateSettings()).HotkeyGesture,
            PersistSubstrate)
    {
        private static void PersistSubstrate(string g)
        {
            SubstrateSettings s = ModuleSettingsStore.Load("Substrate", () => new SubstrateSettings());
            s.HotkeyGesture = g;
            ModuleSettingsStore.Save("Substrate", s);
        }
    }

    public sealed class SlateCapability(ICapabilityContext context) : IModuleCapability
    {
        private readonly HostServices _services = context.Services;
        private readonly IHostUiBridge _hostUi = context.Ui.HostUi;

        public string ModuleId => "Slate";
        public bool IsArmed { get; private set; }

        public Task ArmAsync(CancellationToken cancellationToken = default)
        {
            if (IsArmed)
            {
                return Task.CompletedTask;
            }

            SlateSettings settings = ModuleSettingsStore.Load("Slate", () => new SlateSettings());
            _services.Idle.Threshold = TimeSpan.FromSeconds(Math.Max(30, settings.IdleSeconds));
            _services.Idle.IdleThresholdReached += OnIdle;
            _services.Idle.Start();
            IsArmed = true;
            return Task.CompletedTask;
        }

        public Task DisarmAsync(CancellationToken cancellationToken = default)
        {
            if (!IsArmed)
            {
                return Task.CompletedTask;
            }

            _services.Idle.IdleThresholdReached -= OnIdle;
            _services.Idle.Stop();
            _hostUi.CloseOverlay(ModuleId);
            IsArmed = false;
            return Task.CompletedTask;
        }

        private void OnIdle(object? s, EventArgs e)
        {
            SlateSettings settings = ModuleSettingsStore.Load("Slate", () => new SlateSettings());
            if (settings.HideOnFullscreen && _services.Fullscreen.IsForegroundFullscreen)
            {
                return;
            }

            _ = _hostUi.OpenOverlayAsync(ModuleId);
        }

        public void Dispose()
        {
            DisarmAsync().GetAwaiter().GetResult();
        }
    }

    public static class BuiltInCapabilityFactories
    {
        public static void RegisterAll(CapabilityRegistry registry)
        {
            registry.Register(new DelegateFactory("Tessera", (m, c) => new TesseraCapability(c)));
            registry.Register(new DelegateFactory("Mixdeck", (m, c) => new MixdeckCapability(c)));
            registry.Register(new DelegateFactory("Inlay", (m, c) => new InlayCapability(c)));
            registry.Register(new DelegateFactory("Chord", (m, c) => new ChordCapability(c)));
            registry.Register(new DelegateFactory("Substrate", (m, c) => new SubstrateCapability(c)));
            registry.Register(new DelegateFactory("Slate", (m, c) => new SlateCapability(c)));
        }

        private sealed class DelegateFactory(
            string moduleId,
            Func<ModuleManifest, ICapabilityContext, IModuleCapability> create) : ICapabilityFactory
        {
            public string ModuleId => moduleId;
            public IModuleCapability Create(ModuleManifest manifest, ICapabilityContext context)
            {
                return create(manifest, context);
            }
        }
    }
}
