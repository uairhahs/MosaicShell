namespace MosaicShell.Core.Capabilities.Platform
{
    /// <summary>Cross-module capability platform events (media, volume, lifecycle).</summary>
    public enum CapabilityEventKind
    {
        MediaTrackBoundary,
        MediaSessionChanged,
        MediaProgress,
        VolumeChanged,
        FlyoutTransientDismissed,
        CapabilityArmed,
        CapabilityDisarmed,
    }

    public sealed record CapabilityEvent(
        CapabilityEventKind Kind,
        string? SourceModuleId = null,
        object? Payload = null);

    public interface ICapabilityEventBus
    {
        void Publish(CapabilityEvent evt);

        /// <summary>Subscribe to one kind; dispose to unsubscribe.</summary>
        IDisposable Subscribe(CapabilityEventKind kind, Action<CapabilityEvent> handler);

        /// <summary>Subscribe to all kinds; dispose to unsubscribe.</summary>
        IDisposable SubscribeAll(Action<CapabilityEvent> handler);
    }

    public sealed class CapabilityEventBus : ICapabilityEventBus
    {
        private readonly Lock _gate = new();
        private readonly Dictionary<CapabilityEventKind, List<Action<CapabilityEvent>>> _byKind = [];
        private readonly List<Action<CapabilityEvent>> _all = [];

        public void Publish(CapabilityEvent evt)
        {
            Action<CapabilityEvent>[] kindHandlers;
            Action<CapabilityEvent>[] allHandlers;
            lock (_gate)
            {
                kindHandlers = _byKind.TryGetValue(evt.Kind, out List<Action<CapabilityEvent>>? list)
                    ? [.. list]
                    : [];
                allHandlers = [.. _all];
            }

            foreach (Action<CapabilityEvent> handler in kindHandlers)
            {
                try { handler(evt); }
                catch { /* soft-fail */ }
            }

            foreach (Action<CapabilityEvent> handler in allHandlers)
            {
                try { handler(evt); }
                catch { /* soft-fail */ }
            }
        }

        public IDisposable Subscribe(CapabilityEventKind kind, Action<CapabilityEvent> handler)
        {
            lock (_gate)
            {
                if (!_byKind.TryGetValue(kind, out List<Action<CapabilityEvent>>? list))
                {
                    list = [];
                    _byKind[kind] = list;
                }

                list.Add(handler);
            }

            return new Subscription(() => Unsubscribe(kind, handler));
        }

        public IDisposable SubscribeAll(Action<CapabilityEvent> handler)
        {
            lock (_gate)
            {
                _all.Add(handler);
            }

            return new Subscription(() =>
            {
                lock (_gate)
                {
                    _ = _all.Remove(handler);
                }
            });
        }

        private void Unsubscribe(CapabilityEventKind kind, Action<CapabilityEvent> handler)
        {
            lock (_gate)
            {
                if (_byKind.TryGetValue(kind, out List<Action<CapabilityEvent>>? list))
                {
                    _ = list.Remove(handler);
                }
            }
        }

        private sealed class Subscription(Action dispose) : IDisposable
        {
            private int _disposed;
            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                {
                    return;
                }

                dispose();
            }
        }
    }

    /// <summary>Maps platform media signals to bus events.</summary>
    public static class CapabilityEventPublishing
    {
        public static CapabilityEvent FromMediaSignal(MediaSessionSignal signal)
        {
            return signal.Kind switch
            {
                MediaSessionSignalKind.Progress => new CapabilityEvent(
                    CapabilityEventKind.MediaProgress,
                    Payload: signal),
                _ when signal.IsTrackBoundary => new CapabilityEvent(
                    CapabilityEventKind.MediaTrackBoundary,
                    Payload: signal),
                _ => new CapabilityEvent(
                                CapabilityEventKind.MediaSessionChanged,
                                Payload: signal),
            };
        }
    }
}
