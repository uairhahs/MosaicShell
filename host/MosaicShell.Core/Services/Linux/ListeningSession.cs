namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// One Start-to-Stop run of a session-bus listener. It owns the run's connection and watch, so a Stop or Dispose
    /// that lands mid-connect still closes them, and a stale run's signals are dropped even if they were already in
    /// flight (the listener checks <see cref="IsListening"/> before acting on one).
    /// </summary>
    internal class ListeningSession
    {
        private readonly Lock _gate = new();
        private readonly List<IDisposable> _owned = [];
        private bool _closed;
        private bool _listening;

        /// <summary>True from <see cref="MarkListening"/> until <see cref="Close"/>.</summary>
        public bool IsListening
        {
            get
            {
                lock (_gate)
                {
                    return _listening && !_closed;
                }
            }
        }

        /// <summary>Keeps a resource for Close, or disposes it at once when Close has already run.</summary>
        public bool Adopt(IDisposable resource)
        {
            lock (_gate)
            {
                if (!_closed)
                {
                    _owned.Add(resource);
                    return true;
                }
            }

            resource.Dispose();
            return false;
        }

        public void MarkListening()
        {
            lock (_gate)
            {
                _listening = !_closed;
            }
        }

        public void Close()
        {
            IDisposable[] owned;
            lock (_gate)
            {
                if (_closed)
                {
                    return;
                }

                _closed = true;
                owned = [.. _owned];
                _owned.Clear();
            }

            // Last adopted first: a watch before the connection that carries it.
            for (int i = owned.Length - 1; i >= 0; i--)
            {
                owned[i].Dispose();
            }
        }
    }
}
