namespace MosaicShell.Core.Capabilities.Ipc;

using System.Collections.Concurrent;
using System.IO.Pipes;
using MosaicShell.Core.Capabilities;

/// <summary>
/// Host-side proxy when Worker owns the in-process <see cref="CapabilityDaemon"/>.
/// Hub arm/disarm toggles still work for the full mosaic of armed modules.
/// </summary>
public sealed class RemoteCapabilityHost : ICapabilityHost
{
    private readonly object _sendGate = new();
    private ClientConnection? _connection;
    private int _nextId;
    private bool _disposed;

    public bool IsConnected
    {
        get
        {
            lock (_sendGate) return _connection?.IsConnected == true;
        }
    }

    public IReadOnlyList<string> ArmedModuleIds =>
        SendRequest(new CapabilityControlMessage(CapabilityControlMessageType.GetArmedIds, NextId()))
            .StringList?.ToList() ?? [];

    public bool TryConnect(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        try
        {
            var pipe = new NamedPipeClientStream(
                ".",
                CapabilityIpcControlPolicy.PipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked.CancelAfter(CapabilityIpcControlPolicy.ConnectTimeoutMs);
            pipe.ConnectAsync(linked.Token).GetAwaiter().GetResult();

            lock (_sendGate)
            {
                _connection?.Dispose();
                _connection = new ClientConnection(pipe);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsArmed(string moduleId) =>
        SendRequest(new CapabilityControlMessage(
            CapabilityControlMessageType.IsArmed,
            NextId(),
            ModuleId: moduleId)).BoolValue;

    public Task<bool> ArmAsync(string moduleId, bool persist = true, CancellationToken cancellationToken = default) =>
        Task.FromResult(SendRequest(new CapabilityControlMessage(
            CapabilityControlMessageType.Arm,
            NextId(),
            ModuleId: moduleId,
            Persist: persist)).BoolValue);

    public Task<bool> DisarmAsync(string moduleId, bool persist = true, CancellationToken cancellationToken = default) =>
        Task.FromResult(SendRequest(new CapabilityControlMessage(
            CapabilityControlMessageType.Disarm,
            NextId(),
            ModuleId: moduleId,
            Persist: persist)).BoolValue);

    public Task<bool> ReArmAsync(string moduleId, CancellationToken cancellationToken = default) =>
        Task.FromResult(SendRequest(new CapabilityControlMessage(
            CapabilityControlMessageType.ReArm,
            NextId(),
            ModuleId: moduleId)).BoolValue);

    public string? GetHotkeyError(string moduleId) =>
        SendRequest(new CapabilityControlMessage(
            CapabilityControlMessageType.GetHotkeyError,
            NextId(),
            ModuleId: moduleId)).StringValue;

    public Task RestoreAsync(CancellationToken cancellationToken = default)
    {
        SendRequest(new CapabilityControlMessage(CapabilityControlMessageType.Restore, NextId()));
        return Task.CompletedTask;
    }

    public Task DisarmAllAsync(CancellationToken cancellationToken = default)
    {
        SendRequest(new CapabilityControlMessage(CapabilityControlMessageType.DisarmAll, NextId()));
        return Task.CompletedTask;
    }

    public void Persist() =>
        SendRequest(new CapabilityControlMessage(CapabilityControlMessageType.Persist, NextId()));

    private int NextId() => Interlocked.Increment(ref _nextId);

    private CapabilityControlMessage SendRequest(CapabilityControlMessage request)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ClientConnection? conn;
        lock (_sendGate) conn = _connection;
        if (conn is null || !conn.IsConnected)
            throw new InvalidOperationException("Not connected to capability control IPC.");

        return conn.SendRequest(request);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        lock (_sendGate)
        {
            _connection?.Dispose();
            _connection = null;
        }
    }

    private sealed class ClientConnection : IDisposable
    {
        private readonly PipeStream _pipe;
        private readonly object _gate = new();
        private readonly ConcurrentDictionary<int, CapabilityControlMessage> _responses = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _readLoop;
        private bool _disposed;

        public ClientConnection(PipeStream pipe)
        {
            _pipe = pipe;
            _readLoop = Task.Run(ReadLoopAsync);
        }

        public bool IsConnected => !_disposed && _pipe.IsConnected;

        public CapabilityControlMessage SendRequest(CapabilityControlMessage request)
        {
            lock (_gate)
            {
                CapabilityIpcControlCodec.WriteAsync(_pipe, request, _cts.Token)
                    .GetAwaiter().GetResult();

                var deadline = Environment.TickCount64 + CapabilityIpcControlPolicy.ConnectTimeoutMs;
                while (Environment.TickCount64 < deadline)
                {
                    if (_responses.TryRemove(request.RequestId, out var response))
                        return response;
                    Thread.Sleep(5);
                }

                throw new TimeoutException("Capability control IPC response timed out.");
            }
        }

        private async Task ReadLoopAsync()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    var msg = await CapabilityIpcControlCodec.TryReadAsync(_pipe, _cts.Token)
                        .ConfigureAwait(false);
                    if (msg is null) break;
                    if (msg.RequestId > 0)
                        _responses[msg.RequestId] = msg;
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
            try { _readLoop.Wait(TimeSpan.FromSeconds(2)); } catch { /* ignore */ }
            _cts.Dispose();
        }
    }
}
