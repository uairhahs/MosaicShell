namespace MosaicShell.Core.Capabilities.Ipc;

using System.IO.Pipes;
using MosaicShell.Core.Capabilities;

/// <summary>
/// Host-side named pipe server: applies Worker flyout IPC to a local <see cref="IFlyoutPresenter"/>.
/// </summary>
public sealed class CapabilityIpcFlyoutServer : IDisposable
{
    private readonly IFlyoutPresenter _presenter;
    private readonly Action<Action> _runOnUiThread;
    private readonly CancellationTokenSource _cts = new();
    private Task? _acceptLoop;
    private ServerConnection? _client;
    private readonly object _gate = new();
    private bool _disposed;

    public CapabilityIpcFlyoutServer(IFlyoutPresenter presenter, Action<Action> runOnUiThread)
    {
        _presenter = presenter;
        _runOnUiThread = runOnUiThread;
        _presenter.TransientDismissed += OnPresenterTransientDismissed;
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_acceptLoop is not null) return;
        _acceptLoop = Task.Run(AcceptLoopAsync);
    }

    private async Task AcceptLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                var pipe = new NamedPipeServerStream(
                    CapabilityIpcPolicy.PipeName,
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
                    _presenter.Show(CapabilityIpcCodec.FromDto(message.Request));
                break;
            case CapabilityIpcMessageType.FlyoutUpdate:
                if (message.Request is not null)
                    _presenter.Update(CapabilityIpcCodec.FromDto(message.Request));
                break;
            case CapabilityIpcMessageType.FlyoutSoftRefresh:
                if (message.Request is not null)
                    _presenter.SoftRefresh(CapabilityIpcCodec.FromDto(message.Request));
                break;
            case CapabilityIpcMessageType.FlyoutHide:
                if (!string.IsNullOrWhiteSpace(message.ModuleId))
                    _presenter.Hide(message.ModuleId);
                break;
            case CapabilityIpcMessageType.FlyoutHideAll:
                _presenter.HideAll();
                break;
        }
    }

    private void OnPresenterTransientDismissed(string moduleId)
    {
        ServerConnection? client;
        lock (_gate) client = _client;
        client?.TrySend(new CapabilityIpcMessage(
            CapabilityIpcMessageType.TransientDismissed,
            ModuleId: moduleId));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _presenter.TransientDismissed -= OnPresenterTransientDismissed;
        _cts.Cancel();
        lock (_gate)
        {
            _client?.Dispose();
            _client = null;
        }

        try { _acceptLoop?.Wait(TimeSpan.FromSeconds(2)); } catch { /* ignore */ }
        _cts.Dispose();
    }

    private sealed class ServerConnection : IDisposable
    {
        private readonly PipeStream _pipe;
        private readonly Action<CapabilityIpcMessage> _onMessage;
        private readonly Action<Action> _runOnUiThread;
        private readonly object _sendGate = new();
        private readonly CancellationTokenSource _cts = new();
        private Task? _readLoop;
        private bool _disposed;

        public ServerConnection(
            PipeStream pipe,
            Action<CapabilityIpcMessage> onMessage,
            Action<Action> runOnUiThread)
        {
            _pipe = pipe;
            _onMessage = onMessage;
            _runOnUiThread = runOnUiThread;
        }

        public void StartReadLoop() => _readLoop = Task.Run(ReadLoopAsync);

        public void TrySend(CapabilityIpcMessage message)
        {
            if (_disposed) return;
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
                    var msg = await CapabilityIpcCodec.TryReadMessageAsync(_pipe, _cts.Token)
                        .ConfigureAwait(false);
                    if (msg is null) break;
                    _runOnUiThread(() => _onMessage(msg));
                }
            }
            catch (OperationCanceledException) { /* shutdown */ }
            catch { /* pipe closed */ }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _cts.Cancel();
            try { _pipe.Dispose(); } catch { /* ignore */ }
            try { _readLoop?.Wait(TimeSpan.FromSeconds(2)); } catch { /* ignore */ }
            _cts.Dispose();
        }
    }
}
