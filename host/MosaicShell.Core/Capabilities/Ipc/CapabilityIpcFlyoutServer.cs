
using System.IO.Pipes;
using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Capabilities.Ipc
{
    /// <summary>
    /// Host-side named pipe server: applies Worker flyout IPC to a local <see cref="IFlyoutPresenter"/>.
    /// </summary>
    public sealed class CapabilityIpcFlyoutServer : IDisposable
    {
        private readonly IFlyoutPresenter _presenter;
        private readonly Action<Action> _runOnUiThread;
        private readonly string _pipeName;
        private readonly CancellationTokenSource _cts = new();
        private Task? _acceptLoop;
        private ServerConnection? _client;
        private readonly Lock _gate = new();
        private bool _disposed;

        /// <param name="presenter">Applies incoming flyout commands.</param>
        /// <param name="runOnUiThread">Dispatches each handled message onto the Host UI thread.</param>
        /// <param name="pipeName">Override for tests only; production callers use the default so
        /// Worker and Host agree on <see cref="CapabilityIpcPolicy.PipeName"/>.</param>
        public CapabilityIpcFlyoutServer(
            IFlyoutPresenter presenter,
            Action<Action> runOnUiThread,
            string pipeName = CapabilityIpcPolicy.PipeName)
        {
            _presenter = presenter;
            _runOnUiThread = runOnUiThread;
            _pipeName = pipeName;
            _presenter.TransientDismissed += OnPresenterTransientDismissed;
        }

        public void Start()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_acceptLoop is not null)
            {
                return;
            }

            _acceptLoop = Task.Run(AcceptLoopAsync);
        }

        private async Task AcceptLoopAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    NamedPipeServerStream pipe = new(
                        _pipeName,
                        PipeDirection.InOut,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await pipe.WaitForConnectionAsync(_cts.Token).ConfigureAwait(false);
                    lock (_gate)
                    {
                        _client?.Dispose();
                        _client = new ServerConnection(pipe, HandleClientMessage, _runOnUiThread);
                    }

                    _client.StartReadLoop();
                }
                catch (OperationCanceledException) { break; }
                catch { /* retry */ }
            }
        }

        private void HandleClientMessage(CapabilityIpcMessage message)
        {
            switch (message.Type)
            {
                case CapabilityIpcMessageType.FlyoutShow:
                    if (message.Request is not null)
                    {
                        _presenter.Show(CapabilityIpcCodec.FromDto(message.Request));
                        EchoSessionSnapshot(message.Request.ModuleId);
                    }
                    break;
                case CapabilityIpcMessageType.FlyoutUpdate:
                    if (message.Request is not null)
                    {
                        _presenter.Update(CapabilityIpcCodec.FromDto(message.Request));
                        EchoSessionSnapshot(message.Request.ModuleId);
                    }
                    break;
                case CapabilityIpcMessageType.FlyoutSoftRefresh:
                    if (message.Request is not null)
                    {
                        _presenter.SoftRefresh(CapabilityIpcCodec.FromDto(message.Request));
                        EchoSessionSnapshot(message.Request.ModuleId);
                    }
                    break;
                case CapabilityIpcMessageType.FlyoutHide:
                    if (!string.IsNullOrWhiteSpace(message.ModuleId))
                    {
                        _presenter.Hide(message.ModuleId);
                        EchoSessionSnapshot(message.ModuleId);
                    }
                    break;
                case CapabilityIpcMessageType.FlyoutHideAll:
                    _presenter.HideAll();
                    EchoSessionSnapshot("Tessera");
                    break;
                case CapabilityIpcMessageType.TransientDismissed:
                    break;
                case CapabilityIpcMessageType.FlyoutSessionSnapshot:
                    break;
                default:
                    break;
            }
        }

        private void EchoSessionSnapshot(string moduleId)
        {
            // Presenter Show/Update Post flush on the UI queue. Echo after that work so
            // EffectivelyShowing matches the applied session, not the pre-flush snapshot.
            SynchronizationContext? ctx = SynchronizationContext.Current;
            if (ctx is not null)
            {
                ctx.Post(_ => SendSessionSnapshot(moduleId), null);
                return;
            }

            SendSessionSnapshot(moduleId);
        }

        private void SendSessionSnapshot(string moduleId)
        {
            ServerConnection? client;
            lock (_gate)
            {
                client = _client;
            }

            TesseraFlyoutSessionSnapshot snap = _presenter.GetSessionSnapshot(moduleId);
            client?.TrySend(new CapabilityIpcMessage(
                CapabilityIpcMessageType.FlyoutSessionSnapshot,
                ModuleId: moduleId,
                SessionSnapshot: TesseraFlyoutIpcSnapshotDto.From(moduleId, snap)));
        }

        private void OnPresenterTransientDismissed(string moduleId)
        {
            ServerConnection? client;
            lock (_gate)
            {
                client = _client;
            }

            client?.TrySend(new CapabilityIpcMessage(
                CapabilityIpcMessageType.TransientDismissed,
                ModuleId: moduleId));
            SendSessionSnapshot(moduleId);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _presenter.TransientDismissed -= OnPresenterTransientDismissed;
            _cts.Cancel();
            lock (_gate)
            {
                _client?.Dispose();
                _client = null;
            }

            try { _ = (_acceptLoop?.Wait(TimeSpan.FromSeconds(2))); } catch { /* ignore */ }
            _cts.Dispose();
        }

        private sealed class ServerConnection(
            PipeStream pipe,
            Action<CapabilityIpcMessage> onMessage,
            Action<Action> runOnUiThread) : IDisposable
        {
            private readonly PipeStream _pipe = pipe;
            private readonly Action<CapabilityIpcMessage> _onMessage = onMessage;
            private readonly Action<Action> _runOnUiThread = runOnUiThread;
            private readonly Lock _sendGate = new();
            private readonly CancellationTokenSource _cts = new();
            private Task? _readLoop;
            private bool _disposed;

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
                    catch { /* client disconnected */ }
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

                        _runOnUiThread(() => _onMessage(msg));
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
