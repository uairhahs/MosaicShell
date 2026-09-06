using MosaicShell.Core.Capabilities.Platform;
using MosaicShell.Core.Modules;
using MosaicShell.Core.Runtime;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Capabilities
{
    public sealed class CapabilityDaemon : ICapabilityHost, IDisposable
    {
        private readonly CapabilityRegistry _registry;
        private readonly HostServices _services;
        private readonly ICapabilityUiBridge _ui;
        private readonly CapabilityFlyoutPlatform _flyoutPlatform;
        private readonly Dictionary<string, IModuleCapability> _instances = new(StringComparer.OrdinalIgnoreCase);
        private readonly Lock _gate = new();
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
            Events = events ?? new CapabilityEventBus();
            _flyoutPlatform = new CapabilityFlyoutPlatform(ui.Flyouts);
            MediaPlatform = new MediaSessionPlatform(services.Media);
            MediaPlatform.Signal += OnMediaPlatformSignal;
            _services.Audio.Changed += OnVolumeChanged;
            _ui.Flyouts.TransientDismissed += OnFlyoutTransientDismissed;
        }

        /// <summary>Cross-module platform events (media track boundaries, volume, lifecycle).</summary>
        public ICapabilityEventBus Events { get; }

        /// <summary>Shared media signal layer (SMTC/WNP classification). Used by armed capabilities.</summary>
        public MediaSessionPlatform MediaPlatform { get; }

        public IReadOnlyList<string> ArmedModuleIds
        {
            get
            {
                lock (_gate)
                {
                    return [.. _instances.Where(kv => kv.Value.IsArmed).Select(kv => kv.Key)];
                }
            }
        }

        public bool IsArmed(string moduleId)
        {
            lock (_gate)
            {
                return _instances.TryGetValue(moduleId, out IModuleCapability? c) && c.IsArmed;
            }
        }

        /// <summary>Null when hotkey registered OK (or N/A); otherwise a user-facing error.</summary>
        public string? GetHotkeyError(string moduleId)
        {
            lock (_gate)
            {
                return _instances.TryGetValue(moduleId, out IModuleCapability? c) &&
                    c is BuiltIn.HotkeyOverlayCapability hot
                    ? hot.HotkeyRegistered ? null : hot.HotkeyError
                    : null;
            }
        }

        public async Task RestoreAsync(CancellationToken cancellationToken = default)
        {
            CapabilityArmedState state = CapabilityStore.Load();
            foreach (string? id in state.Armed.ToList())
            {
                _ = await ArmAsync(id, persist: false, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<bool> ReArmAsync(string moduleId, CancellationToken cancellationToken = default)
        {
            _ = await DisarmAsync(moduleId, persist: false, cancellationToken).ConfigureAwait(false);
            return await ArmAsync(moduleId, persist: true, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> ArmAsync(string moduleId, bool persist = true, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!ModuleCatalog.IsInstalled(moduleId))
            {
                return false;
            }

            _registry.TryLoadExternal(moduleId, AppPaths.ModulesDirectory);
            if (!_registry.TryGetFactory(moduleId, out ICapabilityFactory? factory) || factory is null)
            {
                return false;
            }

            IModuleCapability capability;
            lock (_gate)
            {
                if (_instances.TryGetValue(moduleId, out IModuleCapability? existing) && existing.IsArmed)
                {
                    return true;
                }

                if (_instances.TryGetValue(moduleId, out IModuleCapability? stale))
                {
                    try { stale.Dispose(); } catch { /* ignore */ }
                    _ = _instances.Remove(moduleId);
                }

                ModuleManifest manifest = ModuleManifest.TryLoad(moduleId) ?? ModuleManifest.CreateDefault(moduleId);
                ICapabilityContext context = CreateContext(moduleId);
                capability = factory.Create(manifest, context);
                _instances[moduleId] = capability;
            }

            await capability.ArmAsync(cancellationToken).ConfigureAwait(false);
            Events.Publish(new CapabilityEvent(CapabilityEventKind.CapabilityArmed, moduleId));
            if (persist)
            {
                Persist();
            }

            return true;
        }

        public async Task<bool> DisarmAsync(string moduleId, bool persist = true, CancellationToken cancellationToken = default)
        {
            IModuleCapability? capability;
            lock (_gate)
            {
                if (!_instances.TryGetValue(moduleId, out capability))
                {
                    return false;
                }
            }

            await capability.DisarmAsync(cancellationToken).ConfigureAwait(false);
            capability.Dispose();
            Events.Publish(new CapabilityEvent(CapabilityEventKind.CapabilityDisarmed, moduleId));
            lock (_gate)
            {
                _ = _instances.Remove(moduleId);
            }

            _flyoutPlatform.RemoveSession(moduleId);
            _ui.Flyouts.Hide(moduleId);
            if (persist)
            {
                Persist();
            }

            return true;
        }

        public async Task DisarmAllAsync(CancellationToken cancellationToken = default)
        {
            List<string> ids;
            lock (_gate)
            {
                ids = [.. _instances.Keys];
            }

            foreach (string id in ids)
            {
                _ = await DisarmAsync(id, persist: false, cancellationToken).ConfigureAwait(false);
            }

            Persist();
            _ui.Flyouts.HideAll();
        }

        public void Persist()
        {
            lock (_gate)
            {
                CapabilityStore.SaveArmed(_instances.Where(kv => kv.Value.IsArmed).Select(kv => kv.Key));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (IModuleCapability c in _instances.Values)
            {
                try { c.Dispose(); } catch { /* ignore */ }
            }
            _instances.Clear();
            MediaPlatform.Signal -= OnMediaPlatformSignal;
            _services.Audio.Changed -= OnVolumeChanged;
            _ui.Flyouts.TransientDismissed -= OnFlyoutTransientDismissed;
            _flyoutPlatform.Dispose();
            MediaPlatform.Dispose();
        }

        private void OnMediaPlatformSignal(MediaSessionSignal signal)
        {
            Events.Publish(CapabilityEventPublishing.FromMediaSignal(signal));
        }

        private void OnVolumeChanged(object? sender, EventArgs e)
        {
            Events.Publish(new CapabilityEvent(CapabilityEventKind.VolumeChanged));
        }

        private void OnFlyoutTransientDismissed(string moduleId)
        {
            Events.Publish(new CapabilityEvent(
                CapabilityEventKind.FlyoutTransientDismissed,
                moduleId));
        }

        private ICapabilityContext CreateContext(string moduleId)
        {
            return new CapabilityContext(
                _services,
                _ui,
                _flyoutPlatform.CreateSession(moduleId),
                MediaPlatform,
                Events);
        }
    }
}
