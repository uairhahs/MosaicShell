namespace MosaicShell.Core.Services
{
    /// <summary>
    /// Holds the current default audio endpoint and re-resolves it when it goes missing, goes stale or is marked stale.
    /// Binding once at construction breaks at log-on, when the shell starts before the default device exists or while
    /// Windows is still settling it, and again whenever the default device changes later.
    /// </summary>
    public sealed class DefaultEndpointBinding<T> : IDisposable where T : class
    {
        private readonly Func<T?> _resolve;
        private readonly Action<T>? _onBound;
        private readonly Action<T>? _onUnbound;
        private readonly object _gate = new();
        private T? _current;
        private bool _stale = true;
        private bool _disposed;

        public DefaultEndpointBinding(Func<T?> resolve, Action<T>? onBound = null, Action<T>? onUnbound = null)
        {
            _resolve = resolve;
            _onBound = onBound;
            _onUnbound = onUnbound;
        }

        /// <summary>Forces the next use to resolve the default endpoint again.</summary>
        public void MarkStale()
        {
            lock (_gate)
            {
                _stale = true;
            }
        }

        /// <summary>
        /// Runs <paramref name="operation"/> against the current endpoint. A failure rebinds and retries once; if there is
        /// still no usable endpoint the <paramref name="fallback"/> is returned rather than throwing.
        /// </summary>
        public TResult Run<TResult>(Func<T, TResult> operation, TResult fallback)
        {
            lock (_gate)
            {
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    T? endpoint = Bind();
                    if (endpoint is null)
                    {
                        return fallback;
                    }

                    try
                    {
                        return operation(endpoint);
                    }
                    catch (Exception)
                    {
                        _stale = true;
                    }
                }

                return fallback;
            }
        }

        private T? Bind()
        {
            if (_disposed)
            {
                return null;
            }

            if (!_stale && _current is not null)
            {
                return _current;
            }

            T? next = null;
            try
            {
                next = _resolve();
            }
            catch (Exception)
            {
                // No usable default endpoint yet; the next use tries again.
            }

            Unbind();
            _current = next;
            _stale = next is null;
            if (next is not null)
            {
                _onBound?.Invoke(next);
            }

            return next;
        }

        private void Unbind()
        {
            if (_current is null)
            {
                return;
            }

            T old = _current;
            _current = null;
            try
            {
                _onUnbound?.Invoke(old);
            }
            catch (Exception)
            {
                // The old endpoint may already be gone.
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                Unbind();
            }
        }
    }
}
