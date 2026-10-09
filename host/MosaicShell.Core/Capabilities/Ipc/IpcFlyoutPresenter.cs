
using System.IO.Pipes;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Capabilities.Ipc
{
    /// <summary>
    /// Worker-side <see cref="IFlyoutPresenter"/> that forwards flyout commands to MosaicShell Host over a named pipe.
    /// </summary>
    public sealed class IpcFlyoutPresenter : IFlyoutPresenter, IDisposable
    {
        private readonly Lock _gate = new();
        private ClientConnection? _connection;
        private readonly Dictionary<string, bool> _visible = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, TesseraFlyoutSessionSnapshot> _snapshots = new(StringComparer.OrdinalIgnoreCase);
        private bool _disposed;

        public event Action<string>? TransientDismissed;

        public bool IsConnected
        {
            get
            {
                lock (_gate)
                {
                    return _connection?.IsConnected == true;
                }
            }
        }

        /// <summary>
        /// Keeps trying <see cref="ConnectAsync"/> until Host's flyout pipe accepts, so the Worker no longer has to
        /// start within one attempt of Host.
        /// </summary>
        /// <param name="window">How long to keep trying before giving up with a <see cref="TimeoutException"/>.</param>
        /// <param name="cancellationToken">Stops waiting; throws <see cref="OperationCanceledException"/>.</param>
        /// <param name="pipeName">Override for tests only.</param>
        /// <param name="attemptTimeoutMs">How long one attempt waits for the pipe. Tests shorten it.</param>
        /// <param name="retryDelayMs">Pause between attempts. Tests shorten it.</param>
        public async Task ConnectWithRetryAsync(
            TimeSpan window,
            CancellationToken cancellationToken = default,
            string pipeName = CapabilityIpcPolicy.PipeName,
            int attemptTimeoutMs = CapabilityIpcPolicy.ConnectTimeoutMs,
            int retryDelayMs = CapabilityIpcPolicy.ConnectRetryDelayMs)
        {
            long deadline = Environment.TickCount64 + (long)window.TotalMilliseconds;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await ConnectAsync(cancellationToken, pipeName, attemptTimeoutMs).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex) when (ex is OperationCanceledException or TimeoutException or IOException
                                           && !cancellationToken.IsCancellationRequested)
                {
                    if (Environment.TickCount64 >= deadline)
                    {
                        throw new TimeoutException(
                            $"Host's flyout pipe did not appear within {window.TotalSeconds:0} s.", ex);
                    }
                }

                await Task.Delay(retryDelayMs, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <param name="cancellationToken">Cancels the connect attempt.</param>
        /// <param name="pipeName">Override for tests only; production callers use the default so
        /// Worker and Host agree on <see cref="CapabilityIpcPolicy.PipeName"/>.</param>
        /// <param name="attemptTimeoutMs">How long this attempt waits for the pipe.</param>
        public async Task ConnectAsync(
            CancellationToken cancellationToken = default,
            string pipeName = CapabilityIpcPolicy.PipeName,
            int attemptTimeoutMs = CapabilityIpcPolicy.ConnectTimeoutMs)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            NamedPipeClientStream pipe = new(
                ".",
                pipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);

            try
            {
                using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                linked.CancelAfter(attemptTimeoutMs);
                await pipe.ConnectAsync(linked.Token).ConfigureAwait(false);
            }
            catch
            {
                pipe.Dispose();
                throw;
            }

            ClientConnection conn = new(pipe, OnServerMessage);
            lock (_gate)
            {
                _connection?.Dispose();
                _connection = conn;
            }

            conn.StartReadLoop();
        }

        public void Show(FlyoutRequest request)
        {
            SetVisible(request.ModuleId, true);
            Send(new CapabilityIpcMessage(CapabilityIpcMessageType.FlyoutShow, CapabilityIpcCodec.ToDto(request)));
        }

        public void Update(FlyoutRequest request)
        {
            Send(new CapabilityIpcMessage(CapabilityIpcMessageType.FlyoutUpdate, CapabilityIpcCodec.ToDto(request)));
        }

        public void SoftRefresh(FlyoutRequest request)
        {
            Send(new CapabilityIpcMessage(CapabilityIpcMessageType.FlyoutSoftRefresh, CapabilityIpcCodec.ToDto(request)));
        }

        public void Hide(string moduleId)
        {
            SetVisible(moduleId, false);
            Send(new CapabilityIpcMessage(CapabilityIpcMessageType.FlyoutHide, ModuleId: moduleId));
        }

        public void HideAll()
        {
            lock (_gate)
            {
                foreach (string? key in _visible.Keys.ToList())
                {
                    _visible[key] = false;
                }
            }

            Send(new CapabilityIpcMessage(CapabilityIpcMessageType.FlyoutHideAll));
        }

        public bool IsVisible(string moduleId)
        {
            lock (_gate)
            {
                return _snapshots.TryGetValue(moduleId, out TesseraFlyoutSessionSnapshot snap) ? snap.EffectivelyShowing : _visible.TryGetValue(moduleId, out bool v) && v;
            }
        }

        public TesseraFlyoutSessionSnapshot GetSessionSnapshot(string moduleId)
        {
            lock (_gate)
            {
                return _snapshots.TryGetValue(moduleId, out TesseraFlyoutSessionSnapshot snap)
                    ? snap
                    : new(IsVisibleUnlocked(moduleId), 0, TesseraFlyoutSessionMode.None, "", null, IsVisibleUnlocked(moduleId) ? TesseraFlyoutPhase.Shown : TesseraFlyoutPhase.Hidden);
            }
        }

        private bool IsVisibleUnlocked(string moduleId)
        {
            return _visible.TryGetValue(moduleId, out bool v) && v;
        }

        private void SetVisible(string moduleId, bool visible)
        {
            lock (_gate)
            {
                _visible[moduleId] = visible;
            }
        }

        private void Send(CapabilityIpcMessage message)
        {
            ClientConnection? conn;
            lock (_gate)
            {
                conn = _connection;
            }

            conn?.TrySend(message);
        }

        private void OnServerMessage(CapabilityIpcMessage message)
        {
            if (message.Type == CapabilityIpcMessageType.FlyoutSessionSnapshot
                && message.SessionSnapshot is { } dto)
            {
                TesseraFlyoutSessionSnapshot snap = new(
                    dto.EffectivelyShowing,
                    dto.Generation,
                    Enum.TryParse<TesseraFlyoutSessionMode>(dto.Mode, out TesseraFlyoutSessionMode mode)
                        ? mode
                        : TesseraFlyoutSessionMode.None,
                    dto.Kind,
                    dto.StyleId,
                    Enum.TryParse<TesseraFlyoutPhase>(dto.Phase, out TesseraFlyoutPhase phase)
                        ? phase
                        : dto.EffectivelyShowing ? TesseraFlyoutPhase.Shown : TesseraFlyoutPhase.Hidden);
                lock (_gate)
                {
                    _snapshots[dto.ModuleId] = snap;
                    _visible[dto.ModuleId] = dto.EffectivelyShowing;
                }

                return;
            }

            if (message.Type != CapabilityIpcMessageType.TransientDismissed
                || string.IsNullOrWhiteSpace(message.ModuleId))
            {
                return;
            }

            SetVisible(message.ModuleId, false);
            lock (_gate)
            {
                if (_snapshots.TryGetValue(message.ModuleId, out TesseraFlyoutSessionSnapshot prev))
                {
                    _snapshots[message.ModuleId] = prev with { EffectivelyShowing = false };
                }
            }

            try { TransientDismissed?.Invoke(message.ModuleId); }
            catch { /* ignore */ }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            lock (_gate)
            {
                _connection?.Dispose();
                _connection = null;
            }
        }

        private sealed class ClientConnection(PipeStream pipe, Action<CapabilityIpcMessage> onMessage) : IDisposable
        {
            private readonly PipeStream _pipe = pipe;
            private readonly Action<CapabilityIpcMessage> _onMessage = onMessage;
            private readonly Lock _sendGate = new();
            private readonly CancellationTokenSource _cts = new();
            private Task? _readLoop;
            private bool _disposed;

            public bool IsConnected => !_disposed && _pipe.IsConnected;

            public void StartReadLoop()
            {
                _readLoop = Task.Run(ReadLoopAsync);
            }

            public void TrySend(CapabilityIpcMessage message)
            {
                if (_disposed)
                {
                    return;
                }

                lock (_sendGate)
                {
                    try
                    {
                        CapabilityIpcCodec.WriteMessageAsync(_pipe, message, _cts.Token)
                            .GetAwaiter().GetResult();
                    }
                    catch { /* soft-fail when host exits */ }
                }
            }

            private async Task ReadLoopAsync()
            {
                try
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        CapabilityIpcMessage? msg = await CapabilityIpcCodec.TryReadMessageAsync(_pipe, _cts.Token)
                            .ConfigureAwait(false);
                        if (msg is null)
                        {
                            break;
                        }

                        _onMessage(msg);
                    }
                }
                catch (OperationCanceledException) { /* shutdown */ }
                catch { /* pipe closed */ }
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _cts.Cancel();
                try { _pipe.Dispose(); } catch { /* ignore */ }
                try { _ = (_readLoop?.Wait(TimeSpan.FromSeconds(2))); } catch { /* ignore */ }
                _cts.Dispose();
            }
        }
    }
}
