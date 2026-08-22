using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Capabilities.BuiltIn;

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
        if (IsArmed) return Task.CompletedTask;

        HotkeyRegistered = false;
        HotkeyError = null;
        var raw = _gesture() ?? "";
        var gesture = HotkeyGestureParser.EnsureRegisterable(ModuleId, raw);
        if (!string.Equals(raw.Trim(), gesture, StringComparison.OrdinalIgnoreCase))
            _persistGesture?.Invoke(gesture);

        if (!HotkeyGestureParser.TryParse(gesture, out var mods, out var vk))
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
        if (!IsArmed) return Task.CompletedTask;
        _services.Hotkeys.Unregister(_hotkeyId);
        HotkeyRegistered = false;
        HotkeyError = null;
        IsArmed = false;
        return Task.CompletedTask;
    }

    private void OnHotkey() => _ = _hostUi.OpenOverlayAsync(_overlayModuleId);

    public void Dispose() => DisarmAsync().GetAwaiter().GetResult();
}

public sealed class MixdeckCapability : HotkeyOverlayCapability
{
    public MixdeckCapability(HostServices services, ICapabilityUiBridge ui)
        : base("Mixdeck", services,
            () => ModuleSettingsStore.Load("Mixdeck", () => new MixdeckSettings()).HotkeyGesture,
            ui.HostUi,
            PersistMixdeck)
    {
    }

    private static void PersistMixdeck(string g)
    {
        var s = ModuleSettingsStore.Load("Mixdeck", () => new MixdeckSettings());
        s.HotkeyGesture = g;
        ModuleSettingsStore.Save("Mixdeck", s);
    }
}

public sealed class InlayCapability : HotkeyOverlayCapability
{
    public InlayCapability(HostServices services, ICapabilityUiBridge ui)
        : base("Inlay", services,
            () => ModuleSettingsStore.Load("Inlay", () => new InlaySettings()).HotkeyGesture,
            ui.HostUi,
            PersistInlay)
    {
    }

    private static void PersistInlay(string g)
    {
        var s = ModuleSettingsStore.Load("Inlay", () => new InlaySettings());
        s.HotkeyGesture = g;
        ModuleSettingsStore.Save("Inlay", s);
    }
}

public sealed class ChordCapability : HotkeyOverlayCapability
{
    public ChordCapability(HostServices services, ICapabilityUiBridge ui)
        : base("Chord", services,
            () => ModuleSettingsStore.Load("Chord", () => new ChordSettings()).HotkeyGesture,
            ui.HostUi,
            PersistChord)
    {
    }

    private static void PersistChord(string g)
    {
        var s = ModuleSettingsStore.Load("Chord", () => new ChordSettings());
        s.HotkeyGesture = g;
        ModuleSettingsStore.Save("Chord", s);
    }
}

public sealed class SubstrateCapability : HotkeyOverlayCapability
{
    public SubstrateCapability(HostServices services, ICapabilityUiBridge ui)
        : base("Substrate", services,
            () => ModuleSettingsStore.Load("Substrate", () => new SubstrateSettings()).HotkeyGesture,
            ui.HostUi,
            PersistSubstrate)
    {
    }

    private static void PersistSubstrate(string g)
    {
        var s = ModuleSettingsStore.Load("Substrate", () => new SubstrateSettings());
        s.HotkeyGesture = g;
        ModuleSettingsStore.Save("Substrate", s);
    }
}

public sealed class SlateCapability : IModuleCapability
{
    private readonly HostServices _services;
    private readonly IHostUiBridge _hostUi;

    public SlateCapability(HostServices services, ICapabilityUiBridge ui)
    {
        _services = services;
        _hostUi = ui.HostUi;
    }

    public string ModuleId => "Slate";
    public bool IsArmed { get; private set; }

    public Task ArmAsync(CancellationToken cancellationToken = default)
    {
        if (IsArmed) return Task.CompletedTask;
        var settings = ModuleSettingsStore.Load("Slate", () => new SlateSettings());
        _services.Idle.Threshold = TimeSpan.FromSeconds(Math.Max(30, settings.IdleSeconds));
        _services.Idle.IdleThresholdReached += OnIdle;
        _services.Idle.Start();
        IsArmed = true;
        return Task.CompletedTask;
    }

    public Task DisarmAsync(CancellationToken cancellationToken = default)
    {
        if (!IsArmed) return Task.CompletedTask;
        _services.Idle.IdleThresholdReached -= OnIdle;
        _services.Idle.Stop();
        _hostUi.CloseOverlay(ModuleId);
        IsArmed = false;
        return Task.CompletedTask;
    }

    private void OnIdle(object? s, EventArgs e)
    {
        var settings = ModuleSettingsStore.Load("Slate", () => new SlateSettings());
        if (settings.HideOnFullscreen && _services.Fullscreen.IsForegroundFullscreen)
            return;
        _ = _hostUi.OpenOverlayAsync(ModuleId);
    }

    public void Dispose() => DisarmAsync().GetAwaiter().GetResult();
}

public static class BuiltInCapabilityFactories
{
    public static void RegisterAll(CapabilityRegistry registry)
    {
        registry.Register(new DelegateFactory("Tessera", (m, s, u) => new TesseraCapability(s, u)));
        registry.Register(new DelegateFactory("Mixdeck", (m, s, u) => new MixdeckCapability(s, u)));
        registry.Register(new DelegateFactory("Inlay", (m, s, u) => new InlayCapability(s, u)));
        registry.Register(new DelegateFactory("Chord", (m, s, u) => new ChordCapability(s, u)));
        registry.Register(new DelegateFactory("Substrate", (m, s, u) => new SubstrateCapability(s, u)));
        registry.Register(new DelegateFactory("Slate", (m, s, u) => new SlateCapability(s, u)));
    }

    private sealed class DelegateFactory(
        string moduleId,
        Func<ModuleManifest, HostServices, ICapabilityUiBridge, IModuleCapability> create) : ICapabilityFactory
    {
        public string ModuleId => moduleId;
        public IModuleCapability Create(ModuleManifest manifest, HostServices services, ICapabilityUiBridge ui) =>
            create(manifest, services, ui);
    }
}
