namespace MosaicShell.Core.Capabilities.Platform;

/// <summary>
/// Ensures exactly one process runs <see cref="CapabilityDaemon"/> at a time.
/// Other processes use <see cref="Ipc.RemoteCapabilityHost"/> for Hub arm/disarm and
/// <see cref="Ipc.IpcFlyoutPresenter"/> for flyout UI on Host.
/// </summary>
public sealed class CapabilityDaemonCoordinator : IDisposable
{
    private const string MutexName = @"Global\MosaicShell.CapabilityDaemon.v1";
    private Mutex? _mutex;
    private bool _ownsDaemon;

    public bool OwnsDaemon => _ownsDaemon;

    /// <summary>True when this process acquired the global daemon lock.</summary>
    public bool TryAcquireOwner()
    {
        if (_ownsDaemon) return true;
        try
        {
            _mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
            if (!createdNew && !_mutex.WaitOne(0))
            {
                _mutex.Dispose();
                _mutex = null;
                return false;
            }

            _ownsDaemon = true;
            return true;
        }
        catch
        {
            _mutex?.Dispose();
            _mutex = null;
            return false;
        }
    }

    public void Dispose()
    {
        if (!_ownsDaemon)
        {
            _mutex?.Dispose();
            _mutex = null;
            return;
        }

        try { _mutex?.ReleaseMutex(); } catch { /* ignore */ }
        _mutex?.Dispose();
        _mutex = null;
        _ownsDaemon = false;
    }
}
