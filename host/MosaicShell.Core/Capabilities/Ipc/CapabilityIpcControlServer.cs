namespace MosaicShell.Core.Capabilities.Ipc;

using System.IO.Pipes;
using MosaicShell.Core.Capabilities;

/// <summary>
/// Worker-side control plane: Host Hub arm/disarm requests against the local daemon.
/// </summary>
public sealed class CapabilityIpcControlServer : IDisposable
{
    private readonly ICapabilityHost _daemon;
    private readonly CancellationTokenSource _cts = new();
    private Task? _acceptLoop;
    private ServerConnection? _client;
    private readonly object _gate = new();
    private bool _disposed;

    public CapabilityIpcControlServer(ICapabilityHost daemon) => _daemon = daemon;

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
                    CapabilityIpcControlPolicy.PipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await pipe.WaitForConnectionAsync(_cts.Token).ConfigureAwait(false);
                lock (_gate)
                {
                    _client?.Dispose();
                    _client = new ServerConnection(pipe, HandleRequest);
                }

                _client.StartReadLoop();
            }
            catch (OperationCanceledException) { break; }
            catch { /* retry */ }
        }
    }

    private CapabilityControlMessage HandleRequest(CapabilityControlMessage request)
    {
        try
        {
            return request.Type switch
            {
                CapabilityControlMessageType.Arm => ReplyBool(request, _daemon
                    .ArmAsync(request.ModuleId ?? "", request.Persist).GetAwaiter().GetResult()),
                CapabilityControlMessageType.Disarm => ReplyBool(request, _daemon
                    .DisarmAsync(request.ModuleId ?? "", request.Persist).GetAwaiter().GetResult()),
                CapabilityControlMessageType.ReArm => ReplyBool(request, _daemon
                    .ReArmAsync(request.ModuleId ?? "").GetAwaiter().GetResult()),
                CapabilityControlMessageType.IsArmed => new CapabilityControlMessage(
                    CapabilityControlMessageType.BoolResult,
                    request.RequestId,
                    BoolValue: _daemon.IsArmed(request.ModuleId ?? "")),
                CapabilityControlMessageType.GetArmedIds => new CapabilityControlMessage(
                    CapabilityControlMessageType.StringListResult,
                    request.RequestId,
                    StringList: _daemon.ArmedModuleIds.ToList()),
                CapabilityControlMessageType.GetHotkeyError => new CapabilityControlMessage(
                    CapabilityControlMessageType.StringResult,
                    request.RequestId,
                    StringValue: _daemon.GetHotkeyError(request.ModuleId ?? "")),
                CapabilityControlMessageType.Restore => RunVoid(request, () =>
                    _daemon.RestoreAsync().GetAwaiter().GetResult()),
                CapabilityControlMessageType.DisarmAll => RunVoid(request, () =>
                    _daemon.DisarmAllAsync().GetAwaiter().GetResult()),
                CapabilityControlMessageType.Persist => RunVoid(request, _daemon.Persist),
                _ => new CapabilityControlMessage(
                    CapabilityControlMessageType.BoolResult,
                    request.RequestId,
                    BoolValue: false),
            };
        }
        catch (Exception ex)
        {
            return new CapabilityControlMessage(
                CapabilityControlMessageType.StringResult,
                request.RequestId,
                StringValue: ex.Message);
        }
    }

    private static CapabilityControlMessage ReplyBool(CapabilityControlMessage request, bool ok) =>
        new(CapabilityControlMessageType.BoolResult, request.RequestId, BoolValue: ok);

    private static CapabilityControlMessage RunVoid(CapabilityControlMessage request, Action action)
    {
        action();
        return new CapabilityControlMessage(CapabilityControlMessageType.BoolResult, request.RequestId, BoolValue: true);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
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
        private readonly Func<CapabilityControlMessage, CapabilityControlMessage> _handle;
        private readonly object _sendGate = new();
        private readonly CancellationTokenSource _cts = new();
        private Task? _readLoop;
        private bool _disposed;

        public ServerConnection(
            PipeStream pipe,
            Func<CapabilityControlMessage, CapabilityControlMessage> handle)
        {
            _pipe = pipe;
            _handle = handle;
        }

        public void StartReadLoop() => _readLoop = Task.Run(ReadLoopAsync);

        private async Task ReadLoopAsync()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    var msg = await CapabilityIpcControlCodec.TryReadAsync(_pipe, _cts.Token)
                        .ConfigureAwait(false);
                    if (msg is null) break;

                    var response = _handle(msg);
                    lock (_sendGate)
                    {
                        CapabilityIpcControlCodec.WriteAsync(_pipe, response, _cts.Token)
                            .GetAwaiter().GetResult();
                    }
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
