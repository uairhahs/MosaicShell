using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Capabilities;

public sealed class CapabilityDaemon : ICapabilityHost, IDisposable
{
    private readonly CapabilityRegistry _registry;
    private readonly HostServices _services;
    private readonly ICapabilityUiBridge _ui;
    private readonly CapabilityFlyoutPlatform _flyoutPlatform;
    private readonly MediaSessionPlatform _mediaPlatform;
    private readonly ICapabilityEventBus _events;
    private readonly Dictionary<string, IModuleCapability> _instances = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();
    private bool _disposed;

    public CapabilityDaemon(
        CapabilityRegistry registry,
        HostServices services,
        ICapabilityUiBridge ui,
        ICapabilityEventBus? events = null)
    {
        _registry = registry;
        _services = services;
        _ui = ui;
        _events = events ?? new CapabilityEventBus();
        _flyoutPlatform = new CapabilityFlyoutPlatform(ui.Flyouts);
        _mediaPlatform = new MediaSessionPlatform(services.Media);
        _mediaPlatform.Signal += OnMediaPlatformSignal;
        _services.Audio.Changed += OnVolumeChanged;
        _ui.Flyouts.TransientDismissed += OnFlyoutTransientDismissed;
    }

    /// <summary>Cross-module platform events (media track boundaries, volume, lifecycle).</summary>
    public ICapabilityEventBus Events => _events;

    /// <summary>Shared media signal layer (SMTC/WNP classification). Used by armed capabilities.</summary>
    public MediaSessionPlatform MediaPlatform => _mediaPlatform;

    public IReadOnlyList<string> ArmedModuleIds
    {
        get
        {
            lock (_gate)
                return _instances.Where(kv => kv.Value.IsArmed).Select(kv => kv.Key).ToList();
        }
    }

    public bool IsArmed(string moduleId)
    {
        lock (_gate)
            return _instances.TryGetValue(moduleId, out var c) && c.IsArmed;
    }

    /// <summary>Null when hotkey registered OK (or N/A); otherwise a user-facing error.</summary>
    public string? GetHotkeyError(string moduleId)
    {
        lock (_gate)
        {
            if (_instances.TryGetValue(moduleId, out var c) &&
                c is BuiltIn.HotkeyOverlayCapability hot)
                return hot.HotkeyRegistered ? null : hot.HotkeyError;
            return null;
        }
    }

    public async Task RestoreAsync(CancellationToken cancellationToken = default)
    {
        var state = CapabilityStore.Load();
        foreach (var id in state.Armed.ToList())
            await ArmAsync(id, persist: false, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ReArmAsync(string moduleId, CancellationToken cancellationToken = default)
    {
        await DisarmAsync(moduleId, persist: false, cancellationToken).ConfigureAwait(false);
        return await ArmAsync(moduleId, persist: true, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ArmAsync(string moduleId, bool persist = true, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!ModuleCatalog.IsInstalled(moduleId))
            return false;

        _registry.TryLoadExternal(moduleId, AppPaths.ModulesDirectory);
        if (!_registry.TryGetFactory(moduleId, out var factory) || factory is null)
            return false;

        IModuleCapability capability;
        lock (_gate)
        {
            if (_instances.TryGetValue(moduleId, out var existing) && existing.IsArmed)
                return true;

            if (_instances.TryGetValue(moduleId, out var stale))
            {
                try { stale.Dispose(); } catch { /* ignore */ }
                _instances.Remove(moduleId);
            }

            var manifest = ModuleManifest.TryLoad(moduleId) ?? ModuleManifest.CreateDefault(moduleId);
            var context = CreateContext(moduleId);
            capability = factory.Create(manifest, context);
            _instances[moduleId] = capability;
        }

        await capability.ArmAsync(cancellationToken).ConfigureAwait(false);
        _events.Publish(new CapabilityEvent(CapabilityEventKind.CapabilityArmed, moduleId));
        if (persist) Persist();
        return true;
    }

    public async Task<bool> DisarmAsync(string moduleId, bool persist = true, CancellationToken cancellationToken = default)
    {
        IModuleCapability? capability;
        lock (_gate)
        {
            if (!_instances.TryGetValue(moduleId, out capability))
                return false;
        }

        await capability.DisarmAsync(cancellationToken).ConfigureAwait(false);
        capability.Dispose();
        _events.Publish(new CapabilityEvent(CapabilityEventKind.CapabilityDisarmed, moduleId));
        lock (_gate)
            _instances.Remove(moduleId);

        _flyoutPlatform.RemoveSession(moduleId);
        _ui.Flyouts.Hide(moduleId);
        if (persist) Persist();
        return true;
    }

    public async Task DisarmAllAsync(CancellationToken cancellationToken = default)
    {
        List<string> ids;
        lock (_gate)
            ids = _instances.Keys.ToList();

        foreach (var id in ids)
            await DisarmAsync(id, persist: false, cancellationToken).ConfigureAwait(false);

        Persist();
        _ui.Flyouts.HideAll();
    }

    public void Persist()
    {
        lock (_gate)
            CapabilityStore.SaveArmed(_instances.Where(kv => kv.Value.IsArmed).Select(kv => kv.Key));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var c in _instances.Values)
        {
            try { c.Dispose(); } catch { /* ignore */ }
        }
        _instances.Clear();
        _mediaPlatform.Signal -= OnMediaPlatformSignal;
        _services.Audio.Changed -= OnVolumeChanged;
        _ui.Flyouts.TransientDismissed -= OnFlyoutTransientDismissed;
        _flyoutPlatform.Dispose();
        _mediaPlatform.Dispose();
    }

    private void OnMediaPlatformSignal(MediaSessionSignal signal) =>
        _events.Publish(CapabilityEventPublishing.FromMediaSignal(signal));

    private void OnVolumeChanged(object? sender, EventArgs e) =>
        _events.Publish(new CapabilityEvent(CapabilityEventKind.VolumeChanged));

    private void OnFlyoutTransientDismissed(string moduleId) =>
        _events.Publish(new CapabilityEvent(
            CapabilityEventKind.FlyoutTransientDismissed,
            moduleId));

    private ICapabilityContext CreateContext(string moduleId) =>
        new CapabilityContext(
            _services,
            _ui,
            _flyoutPlatform.CreateSession(moduleId),
            _mediaPlatform,
            _events);
}
