namespace MosaicShell.Core.Capabilities.Ipc;

using System.IO.Pipes;
using MosaicShell.Core.Capabilities;
using MosaicShell.Core.Modules.Tessera;

/// <summary>
/// Worker-side <see cref="IFlyoutPresenter"/> that forwards flyout commands to MosaicShell Host over a named pipe.
/// </summary>
public sealed class IpcFlyoutPresenter : IFlyoutPresenter, IDisposable
{
    private readonly object _gate = new();
    private ClientConnection? _connection;
    private readonly Dictionary<string, bool> _visible = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TesseraFlyoutSessionSnapshot> _snapshots = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public event Action<string>? TransientDismissed;

    public bool IsConnected
    {
        get
        {
            lock (_gate) return _connection?.IsConnected == true;
        }
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var pipe = new NamedPipeClientStream(
            ".",
            CapabilityIpcPolicy.PipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linked.CancelAfter(CapabilityIpcPolicy.ConnectTimeoutMs);
        await pipe.ConnectAsync(linked.Token).ConfigureAwait(false);

        var conn = new ClientConnection(pipe, OnServerMessage);
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

    public void Update(FlyoutRequest request) =>
        Send(new CapabilityIpcMessage(CapabilityIpcMessageType.FlyoutUpdate, CapabilityIpcCodec.ToDto(request)));

    public void SoftRefresh(FlyoutRequest request) =>
        Send(new CapabilityIpcMessage(CapabilityIpcMessageType.FlyoutSoftRefresh, CapabilityIpcCodec.ToDto(request)));

    public void Hide(string moduleId)
    {
        SetVisible(moduleId, false);
        Send(new CapabilityIpcMessage(CapabilityIpcMessageType.FlyoutHide, ModuleId: moduleId));
    }

    public void HideAll()
    {
        lock (_gate)
        {
            foreach (var key in _visible.Keys.ToList())
                _visible[key] = false;
        }

        Send(new CapabilityIpcMessage(CapabilityIpcMessageType.FlyoutHideAll));
    }

    public bool IsVisible(string moduleId)
    {
        lock (_gate)
        {
            if (_snapshots.TryGetValue(moduleId, out var snap))
                return snap.EffectivelyShowing;
            return _visible.TryGetValue(moduleId, out var v) && v;
        }
    }

    public TesseraFlyoutSessionSnapshot GetSessionSnapshot(string moduleId)
    {
        lock (_gate)
        {
            if (_snapshots.TryGetValue(moduleId, out var snap))
                return snap;
            return new(IsVisibleUnlocked(moduleId), 0, TesseraFlyoutSessionMode.None, "", null);
        }
    }

    private bool IsVisibleUnlocked(string moduleId) =>
        _visible.TryGetValue(moduleId, out var v) && v;

    private void SetVisible(string moduleId, bool visible)
    {
        lock (_gate) _visible[moduleId] = visible;
    }

    private void Send(CapabilityIpcMessage message)
    {
        ClientConnection? conn;
        lock (_gate) conn = _connection;
        conn?.TrySend(message);
    }

    private void OnServerMessage(CapabilityIpcMessage message)
    {
        if (message.Type == CapabilityIpcMessageType.FlyoutSessionSnapshot
            && message.SessionSnapshot is { } dto)
        {
            var snap = new TesseraFlyoutSessionSnapshot(
                dto.EffectivelyShowing,
                dto.Generation,
                Enum.TryParse<TesseraFlyoutSessionMode>(dto.Mode, out var mode)
                    ? mode
                    : TesseraFlyoutSessionMode.None,
                dto.Kind,
                dto.StyleId);
            lock (_gate)
            {
                _snapshots[dto.ModuleId] = snap;
                _visible[dto.ModuleId] = dto.EffectivelyShowing;
            }

            return;
        }

        if (message.Type != CapabilityIpcMessageType.TransientDismissed
            || string.IsNullOrWhiteSpace(message.ModuleId))
            return;

        SetVisible(message.ModuleId, false);
        lock (_gate)
        {
            if (_snapshots.TryGetValue(message.ModuleId, out var prev))
                _snapshots[message.ModuleId] = prev with { EffectivelyShowing = false };
        }

        try { TransientDismissed?.Invoke(message.ModuleId); }
        catch { /* ignore */ }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        lock (_gate)
        {
            _connection?.Dispose();
            _connection = null;
        }
    }

    private sealed class ClientConnection : IDisposable
    {
        private readonly PipeStream _pipe;
        private readonly Action<CapabilityIpcMessage> _onMessage;
        private readonly object _sendGate = new();
        private readonly CancellationTokenSource _cts = new();
        private Task? _readLoop;
        private bool _disposed;

        public ClientConnection(PipeStream pipe, Action<CapabilityIpcMessage> onMessage)
        {
            _pipe = pipe;
            _onMessage = onMessage;
        }

        public bool IsConnected => !_disposed && _pipe.IsConnected;

        public void StartReadLoop() =>
            _readLoop = Task.Run(ReadLoopAsync);

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
                catch { /* soft-fail when host exits */ }
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
                    _onMessage(msg);
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
