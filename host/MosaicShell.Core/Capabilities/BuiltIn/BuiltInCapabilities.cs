using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;
using MosaicShell.Core.Settings;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Capabilities.BuiltIn;

public abstract class HotkeyCapabilityBase : IModuleCapability
{
    private readonly HostServices _services;
    private readonly ICapabilityUiBridge _ui;
    private readonly string _hotkeyId;
    private readonly Func<string> _gesture;
    private readonly string _flyoutKind;

    protected HotkeyCapabilityBase(
        string moduleId,
        HostServices services,
        ICapabilityUiBridge ui,
        Func<string> gesture,
        string flyoutKind = "popup")
    {
        ModuleId = moduleId;
        _services = services;
        _ui = ui;
        _hotkeyId = "cap:" + moduleId;
        _gesture = gesture;
        _flyoutKind = flyoutKind;
    }

    public string ModuleId { get; }
    public bool IsArmed { get; private set; }

    public Task ArmAsync(CancellationToken cancellationToken = default)
    {
        if (IsArmed) return Task.CompletedTask;
        if (TryParseGesture(_gesture(), out var mods, out var vk))
            _services.Hotkeys.Register(_hotkeyId, mods, vk, OnHotkey);
        IsArmed = true;
        return Task.CompletedTask;
    }

    public Task DisarmAsync(CancellationToken cancellationToken = default)
    {
        if (!IsArmed) return Task.CompletedTask;
        _services.Hotkeys.Unregister(_hotkeyId);
        _ui.Flyouts.Hide(ModuleId);
        IsArmed = false;
        return Task.CompletedTask;
    }

    private void OnHotkey()
    {
        var style = StyleCatalog.DefaultFor(ModuleId);
        _ui.Flyouts.Show(new FlyoutRequest(ModuleId, _flyoutKind, style));
    }

    public void Dispose() => DisarmAsync().GetAwaiter().GetResult();

    internal static bool TryParseGesture(string gesture, out ModifierKeys mods, out int vk)
    {
        mods = ModifierKeys.None;
        vk = 0;
        if (string.IsNullOrWhiteSpace(gesture)) return false;
        var parts = gesture.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return false;
        foreach (var p in parts[..^1])
        {
            mods |= p.ToLowerInvariant() switch
            {
                "ctrl" or "control" => ModifierKeys.Control,
                "alt" => ModifierKeys.Alt,
                "shift" => ModifierKeys.Shift,
                "win" or "windows" => ModifierKeys.Win,
                _ => ModifierKeys.None
            };
        }

        var key = parts[^1];
        if (key.Length == 1)
        {
            vk = char.ToUpperInvariant(key[0]);
            return true;
        }

        vk = key.ToLowerInvariant() switch
        {
            "space" => 0x20,
            "escape" or "esc" => 0x1B,
            _ => 0
        };
        return vk != 0;
    }
}

public sealed class MixdeckCapability : HotkeyCapabilityBase
{
    public MixdeckCapability(HostServices services, ICapabilityUiBridge ui)
        : base("Mixdeck", services, ui,
            () => ModuleSettingsStore.Load("Mixdeck", () => new MixdeckSettings()).HotkeyGesture,
            "mixer")
    {
    }
}

public sealed class InlayCapability : HotkeyCapabilityBase
{
    public InlayCapability(HostServices services, ICapabilityUiBridge ui)
        : base("Inlay", services, ui,
            () => ModuleSettingsStore.Load("Inlay", () => new InlaySettings()).HotkeyGesture,
            "launcher")
    {
    }
}

public sealed class ChordCapability : HotkeyCapabilityBase
{
    public ChordCapability(HostServices services, ICapabilityUiBridge ui)
        : base("Chord", services, ui,
            () => ModuleSettingsStore.Load("Chord", () => new ChordSettings()).HotkeyGesture,
            "chord")
    {
    }
}

public sealed class SubstrateCapability : HotkeyCapabilityBase
{
    public SubstrateCapability(HostServices services, ICapabilityUiBridge ui)
        : base("Substrate", services, ui,
            () => ModuleSettingsStore.Load("Substrate", () => new SubstrateSettings()).HotkeyGesture,
            "shade")
    {
    }
}

public sealed class SlateCapability : IModuleCapability
{
    private readonly HostServices _services;
    private readonly ICapabilityUiBridge _ui;

    public SlateCapability(HostServices services, ICapabilityUiBridge ui)
    {
        _services = services;
        _ui = ui;
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
        _ui.Flyouts.Hide(ModuleId);
        IsArmed = false;
        return Task.CompletedTask;
    }

    private void OnIdle(object? s, EventArgs e)
    {
        var settings = ModuleSettingsStore.Load("Slate", () => new SlateSettings());
        _ui.Flyouts.Show(new FlyoutRequest(ModuleId, "idle", settings.Style, AutoDismissMs: 0));
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
