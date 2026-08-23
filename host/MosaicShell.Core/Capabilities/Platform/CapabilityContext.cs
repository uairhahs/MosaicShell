namespace MosaicShell.Core.Capabilities.Platform;

using MosaicShell.Core.Services;

/// <summary>
/// Shared platform services passed to every armed capability. Modules focus on building
/// <see cref="FlyoutRequest"/> and handling module settings; Core owns flyout routing and media signals.
/// </summary>
public interface ICapabilityContext
{
    HostServices Services { get; }
    ICapabilityUiBridge Ui { get; }
    CapabilityFlyoutSession Flyouts { get; }
    MediaSessionPlatform Media { get; }
    ICapabilityEventBus Events { get; }
}

public sealed class CapabilityContext : ICapabilityContext
{
    public CapabilityContext(
        HostServices services,
        ICapabilityUiBridge ui,
        CapabilityFlyoutSession flyouts,
        MediaSessionPlatform media,
        ICapabilityEventBus events)
    {
        Services = services;
        Ui = ui;
        Flyouts = flyouts;
        Media = media;
        Events = events;
    }

    public HostServices Services { get; }
    public ICapabilityUiBridge Ui { get; }
    public CapabilityFlyoutSession Flyouts { get; }
    public MediaSessionPlatform Media { get; }
    public ICapabilityEventBus Events { get; }
}

/// <summary>
/// Owns per-module flyout sessions and wires transient dismiss to platform state.
/// Created once per <see cref="CapabilityDaemon"/>.
/// </summary>
public sealed class CapabilityFlyoutPlatform : IDisposable
{
    private readonly IFlyoutPresenter _presenter;
    private readonly Dictionary<string, CapabilityFlyoutSession> _sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();
    private bool _disposed;

    public CapabilityFlyoutPlatform(IFlyoutPresenter presenter)
    {
        _presenter = presenter;
        _presenter.TransientDismissed += OnTransientDismissed;
    }

    public CapabilityFlyoutSession CreateSession(string moduleId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        lock (_gate)
        {
            if (_sessions.TryGetValue(moduleId, out var existing))
                return existing;
            var session = new CapabilityFlyoutSession(moduleId, _presenter);
            _sessions[moduleId] = session;
            return session;
        }
    }

    public void RemoveSession(string moduleId)
    {
        lock (_gate)
        {
            if (!_sessions.Remove(moduleId, out var session)) return;
            session.Dispose();
        }
    }

    private void OnTransientDismissed(string moduleId)
    {
        lock (_gate)
        {
            if (_sessions.TryGetValue(moduleId, out var session))
                session.NotifyTransientDismissed();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _presenter.TransientDismissed -= OnTransientDismissed;
        lock (_gate)
        {
            foreach (var session in _sessions.Values)
                session.Dispose();
            _sessions.Clear();
        }
    }
}
