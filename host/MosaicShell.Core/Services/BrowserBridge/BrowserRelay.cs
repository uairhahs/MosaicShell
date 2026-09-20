namespace MosaicShell.Core.Services.BrowserBridge
{
    /// <summary>
    /// The native messaging host the browser starts: carries frames between the browser (standard input and output)
    /// and the Host (a named pipe). It survives the Host not running or restarting, replays the extension's hello to
    /// every new Host connection so the Host knows who is talking, and ends when the browser closes its end.
    /// It never writes anything to the browser but frames the Host sent.
    /// </summary>
    public sealed class BrowserRelay(
        Stream fromBrowser,
        Stream toBrowser,
        Func<CancellationToken, Task<Stream?>> connectHost,
        Func<int, TimeSpan>? backoff = null)
    {
        private static readonly TimeSpan FirstDelay = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(5);

        private readonly Func<int, TimeSpan> _backoff = backoff ?? DefaultBackoff;
        private readonly SemaphoreSlim _hostGate = new(1, 1);
        private readonly SemaphoreSlim _browserGate = new(1, 1);
        private Stream? _host;
        private bool _helloSent;
        private byte[]? _hello;

        /// <summary>250 ms, doubling each attempt, up to 5 seconds: quick when the Host is starting, quiet when it is not there.</summary>
        public static TimeSpan DefaultBackoff(int attempt)
        {
            return attempt >= 5 ? MaxDelay : FirstDelay * (1 << attempt);
        }

        /// <summary>Runs until the browser closes its end, sends a frame that cannot be trusted, or cancellation.</summary>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            using CancellationTokenSource stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Task hostLoop = HostLoopAsync(stop.Token);
            try
            {
                await BrowserLoopAsync(stop.Token).ConfigureAwait(false);
            }
            finally
            {
                await stop.CancelAsync().ConfigureAwait(false);
                try
                {
                    await hostLoop.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Stopping.
                }
            }
        }

        private async Task BrowserLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    byte[]? frame = await NativeMessagingFraming
                        .ReadFrameAsync(fromBrowser, BrowserProtocol.MaxMessageBytes, cancellationToken)
                        .ConfigureAwait(false);
                    if (frame is null)
                    {
                        return;
                    }

                    await ForwardToHostAsync(frame, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException or ObjectDisposedException or OperationCanceledException)
            {
                // A closed, cancelled or corrupt stream from the browser ends the relay.
            }
        }

        private async Task ForwardToHostAsync(byte[] frame, CancellationToken cancellationToken)
        {
            bool isHello = BrowserProtocol.Parse(frame).Message is BrowserHello;
            await _hostGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (isHello)
                {
                    _hello = frame;
                }

                // The Host requires the hello first and takes it once per connection.
                if (_host is null || isHello == _helloSent)
                {
                    return;
                }

                await WriteAsync(_host, frame, cancellationToken).ConfigureAwait(false);
                _helloSent |= isHello;
            }
            catch (IOException)
            {
                await DropHostLockedAsync().ConfigureAwait(false);
            }
            finally
            {
                _ = _hostGate.Release();
            }
        }

        private async Task HostLoopAsync(CancellationToken cancellationToken)
        {
            int attempt = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                Stream? host = await TryConnectAsync(cancellationToken).ConfigureAwait(false);
                if (host is null)
                {
                    await Task.Delay(_backoff(attempt++), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                attempt = 0;
                try
                {
                    await AttachAsync(host, cancellationToken).ConfigureAwait(false);
                    await PumpHostToBrowserAsync(host, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is InvalidDataException or IOException or ObjectDisposedException)
                {
                    // The Host went away or spoke nonsense; reconnect.
                }
                finally
                {
                    await _hostGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                    try
                    {
                        await DropHostLockedAsync().ConfigureAwait(false);
                    }
                    finally
                    {
                        _ = _hostGate.Release();
                    }

                    await host.DisposeAsync().ConfigureAwait(false);
                }

                await Task.Delay(_backoff(0), cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<Stream?> TryConnectAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await connectHost(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or TimeoutException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        /// <summary>Makes the connection current and replays the extension's hello, so the Host can place the connection.</summary>
        private async Task AttachAsync(Stream host, CancellationToken cancellationToken)
        {
            await _hostGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _host = host;
                _helloSent = false;
                if (_hello is not null)
                {
                    await WriteAsync(host, _hello, cancellationToken).ConfigureAwait(false);
                    _helloSent = true;
                }
            }
            finally
            {
                _ = _hostGate.Release();
            }
        }

        private async Task PumpHostToBrowserAsync(Stream host, CancellationToken cancellationToken)
        {
            while (true)
            {
                byte[]? frame = await NativeMessagingFraming
                    .ReadFrameAsync(host, NativeMessagingFraming.MaxToBrowserBytes, cancellationToken)
                    .ConfigureAwait(false);
                if (frame is null)
                {
                    return;
                }

                await _browserGate.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    // No flush: standard output is not buffered here.
                    await toBrowser.WriteAsync(NativeMessagingFraming.Encode(frame), cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    _ = _browserGate.Release();
                }
            }
        }

        private async Task DropHostLockedAsync()
        {
            Stream? host = _host;
            _host = null;
            _helloSent = false;
            if (host is not null)
            {
                await host.DisposeAsync().ConfigureAwait(false);
            }
        }

        private static async Task WriteAsync(Stream stream, byte[] payload, CancellationToken cancellationToken)
        {
            await stream.WriteAsync(NativeMessagingFraming.Encode(payload, BrowserProtocol.MaxMessageBytes), cancellationToken).ConfigureAwait(false);
        }
    }
}
