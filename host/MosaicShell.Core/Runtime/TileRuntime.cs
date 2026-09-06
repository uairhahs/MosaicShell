namespace MosaicShell.Core.Runtime
{
    public sealed record TileSession(string ModuleId, DateTimeOffset StartedUtc);

    /// <summary>
    /// Platform host (Avalonia) that owns overlay windows for tiles.
    /// </summary>
    public interface ITileSurfaceHost
    {
        bool Show(string moduleId, out string? error);
        void Focus(string moduleId);
        void Close(string moduleId);
    }

    public interface ITileRuntime
    {
        IReadOnlyList<TileSession> Running { get; }
        bool IsRunning(string moduleId);
        ModuleLaunchResult Start(string moduleId);
        bool Stop(string moduleId);
        void StopAll();
        /// <summary>Drop session when the surface closed itself (user ×).</summary>
        void NotifySurfaceClosed(string moduleId);
    }

    /// <summary>
    /// In-process tile session manager. No external desktop-shell dependency.
    /// </summary>
    public sealed class TileRuntime(ITileSurfaceHost host) : ITileRuntime
    {
        private readonly ITileSurfaceHost _host = host ?? throw new ArgumentNullException(nameof(host));
        private readonly Dictionary<string, TileSession> _sessions = new(StringComparer.OrdinalIgnoreCase);
        private readonly Lock _gate = new();

        public IReadOnlyList<TileSession> Running
        {
            get
            {
                lock (_gate)
                {
                    return [.. _sessions.Values.OrderBy(s => s.StartedUtc)];
                }
            }
        }

        public bool IsRunning(string moduleId)
        {
            lock (_gate)
            {
                return _sessions.ContainsKey(moduleId);
            }
        }

        public ModuleLaunchResult Start(string moduleId)
        {
            if (!Modules.ModuleCatalog.IsInstalled(moduleId))
            {
                return new ModuleLaunchResult(
                    false,
                    ModuleLaunchBlocker.NotInstalled,
                    $"{moduleId} is not installed. Use Library to install first.");
            }

            bool already = false;
            lock (_gate)
            {
                if (_sessions.ContainsKey(moduleId))
                {
                    already = true;
                }
            }

            if (already)
            {
                _host.Focus(moduleId);
                return new ModuleLaunchResult(
                    true,
                    ModuleLaunchBlocker.None,
                    $"{moduleId} is already running.");
            }

            // Host work outside the lock so UI callbacks can Stop without deadlocking.
            if (!_host.Show(moduleId, out string? error))
            {
                return new ModuleLaunchResult(
                    false,
                    ModuleLaunchBlocker.NativeRuntimeMissing,
                    error ?? $"Failed to create overlay for {moduleId}.");
            }

            lock (_gate)
            {
                _sessions[moduleId] = new TileSession(moduleId, DateTimeOffset.UtcNow);
            }

            return new ModuleLaunchResult(
                true,
                ModuleLaunchBlocker.None,
                $"Started {moduleId}.");
        }

        public bool Stop(string moduleId)
        {
            lock (_gate)
            {
                if (!_sessions.Remove(moduleId))
                {
                    return false;
                }
            }

            _host.Close(moduleId);
            return true;
        }

        public void StopAll()
        {
            List<string> ids;
            lock (_gate)
            {
                ids = [.. _sessions.Keys];
                _sessions.Clear();
            }

            foreach (string id in ids)
            {
                _host.Close(id);
            }
        }

        public void NotifySurfaceClosed(string moduleId)
        {
            lock (_gate)
            {
                _ = _sessions.Remove(moduleId);
            }
        }
    }
}
