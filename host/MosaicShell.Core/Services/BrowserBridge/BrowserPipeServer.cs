using System.IO.Pipes;
using System.Security.Principal;
using System.Threading.Channels;

namespace MosaicShell.Core.Services.BrowserBridge
{
    /// <summary>
    /// Accepts the relay (one per browser) on a named pipe that only the current user can open, and feeds what it
    /// sends to a <see cref="BrowserSessionHub"/>. The relay process is the browser's native messaging host; this is
    /// the Host's end of the pipe between them. A relay that disappears, floods or lies costs the Host nothing more
    /// than its own connection.
    /// </summary>
    public sealed class BrowserPipeServer(string pipeName, BrowserSessionHub hub, int maxInstances) : IDisposable
    {
        /// <summary>Messages queued for one relay; when full the oldest is dropped, so a stalled relay cannot grow memory.</summary>
        private const int OutboundQueueLength = 16;

        private readonly CancellationTokenSource _stop = new();
        private readonly Lock _gate = new();
        private readonly HashSet<PipeConnection> _connections = [];
        private Task? _acceptLoop;
        private bool _disposed;

        /// <summary>The pipe name for this user. Pipe names are machine-wide, so a second user's Host must not collide.</summary>
        public static string PipeNameForCurrentUser()
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            return "MosaicShell.BrowserMedia." + (identity.User?.Value ?? Environment.UserName);
        }

        public void Start()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _acceptLoop ??= Task.Run(AcceptLoopAsync);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _stop.Cancel();
            PipeConnection[] open;
            lock (_gate)
            {
                open = [.. _connections];
            }

            foreach (PipeConnection connection in open)
            {
                connection.Close();
            }

            try
            {
                _ = _acceptLoop?.Wait(TimeSpan.FromSeconds(2));
            }
            catch (AggregateException)
            {
                // The loop ends by cancellation.
            }

            _stop.Dispose();
        }

        private async Task AcceptLoopAsync()
        {
            while (!_stop.IsCancellationRequested)
            {
                NamedPipeServerStream? pipe = null;
                try
                {
                    // CurrentUserOnly restricts the pipe to the account that runs the Host.
                    pipe = new NamedPipeServerStream(
                        pipeName,
                        PipeDirection.InOut,
                        maxInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                    await pipe.WaitForConnectionAsync(_stop.Token).ConfigureAwait(false);
                    PipeConnection connection = new(pipe);
                    pipe = null;
                    _ = Task.Run(() => RunAsync(connection));
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (IOException)
                {
                    // Could not create or accept an instance (name in use, too many instances); try again shortly.
                    await DelayAsync().ConfigureAwait(false);
                }
                finally
                {
                    pipe?.Dispose();
                }
            }
        }

        private async Task DelayAsync()
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), _stop.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Stopping.
            }
        }

        private async Task RunAsync(PipeConnection connection)
        {
            int id = hub.Open(connection);
            if (id < 0)
            {
                connection.Close();
                return;
            }

            lock (_gate)
            {
                _ = _connections.Add(connection);
            }

            Task writer = connection.RunWriterAsync();
            try
            {
                await connection.RunReaderAsync(frame => hub.Receive(id, frame)).ConfigureAwait(false);
            }
            finally
            {
                connection.Close();
                hub.Close(id);
                lock (_gate)
                {
                    _ = _connections.Remove(connection);
                }

                await writer.ConfigureAwait(false);
            }
        }

        private sealed class PipeConnection(NamedPipeServerStream pipe) : IBrowserConnection
        {
            private readonly Channel<byte[]> _outbound = Channel.CreateBounded<byte[]>(
                new BoundedChannelOptions(OutboundQueueLength)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true,
                });
            private readonly CancellationTokenSource _closed = new();
            private int _isClosed;

            public void Send(byte[] payload)
            {
                ObjectDisposedException.ThrowIf(_isClosed != 0, this);
                _ = _outbound.Writer.TryWrite(payload);
            }

            public void Close()
            {
                if (Interlocked.Exchange(ref _isClosed, 1) != 0)
                {
                    return;
                }

                _closed.Cancel();
                _ = _outbound.Writer.TryComplete();
                pipe.Dispose();
            }

            public async Task RunReaderAsync(Action<byte[]> onFrame)
            {
                try
                {
                    while (true)
                    {
                        byte[]? frame = await NativeMessagingFraming
                            .ReadFrameAsync(pipe, BrowserProtocol.MaxMessageBytes, _closed.Token)
                            .ConfigureAwait(false);
                        if (frame is null)
                        {
                            return;
                        }

                        onFrame(frame);
                    }
                }
                catch (Exception ex) when (ex is InvalidDataException or IOException or ObjectDisposedException or OperationCanceledException)
                {
                    // A broken, oversized or closed stream all end the connection the same way.
                }
            }

            public async Task RunWriterAsync()
            {
                try
                {
                    await foreach (byte[] payload in _outbound.Reader.ReadAllAsync(_closed.Token).ConfigureAwait(false))
                    {
                        await pipe.WriteAsync(NativeMessagingFraming.Encode(payload), _closed.Token).ConfigureAwait(false);
                    }
                }
                catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException or ArgumentOutOfRangeException)
                {
                    Close();
                }
            }
        }
    }
}
