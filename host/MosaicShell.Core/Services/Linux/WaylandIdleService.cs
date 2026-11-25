using System.Diagnostics;
using System.Net.Sockets;
using MosaicShell.Core.Services.Kde;
using MosaicShell.Core.Services.Wayland;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// Idle on Linux from the compositor's <c>ext_idle_notifier_v1</c> (<see cref="ExtIdleNotifyPolicy"/>) on the Host's own
    /// Wayland connection: the Host is an XWayland client, which sees no input to native Wayland windows. As the Windows
    /// service, IdleThresholdReached is raised once when the threshold is reached and again only after input in between,
    /// on the reading thread. Between Start and Stop only; nothing is sampled.
    /// </summary>
    /// <remarks>
    /// Without a Wayland socket, or on a compositor without the protocol (GNOME), it stays inactive and never raises.
    /// <see cref="IdleTime"/> is known only past the threshold (the protocol reports crossings, not a running count), so
    /// it reads zero before then.
    /// </remarks>
    /// <param name="socketPath">The compositor socket; null finds it from WAYLAND_DISPLAY and XDG_RUNTIME_DIR. Tests pass their own.</param>
    public sealed class WaylandIdleService(string? socketPath = null) : IIdleService
    {
        private readonly string? _socketPath = socketPath ?? KWinKeystatePolicy.SocketPath(
            Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"),
            Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR"));

        private readonly Lock _gate = new();
        private Session? _session;
        private TimeSpan _threshold = TimeSpan.FromMinutes(5);
        private bool _fired;
        private long? _idledAt;

        public event EventHandler? IdleThresholdReached;

        public bool IsActive
        {
            get
            {
                lock (_gate)
                {
                    return _session is { IsBound: true };
                }
            }
        }

        public TimeSpan IdleTime
        {
            get
            {
                lock (_gate)
                {
                    return _idledAt is long at ? _threshold + Stopwatch.GetElapsedTime(at) : TimeSpan.Zero;
                }
            }
        }

        public TimeSpan Threshold
        {
            get
            {
                lock (_gate)
                {
                    return _threshold;
                }
            }
            set
            {
                lock (_gate)
                {
                    if (_threshold == value)
                    {
                        return;
                    }

                    _threshold = value;
                    _session?.Renotify(value);
                }
            }
        }

        public void Start()
        {
            lock (_gate)
            {
                _fired = false;
                _idledAt = null;
                if (_session is not null)
                {
                    return;
                }

                Session session = new();
                _session = session;
                _ = Task.Run(() => RunAsync(session));
            }
        }

        public void Stop()
        {
            Session? closing;
            lock (_gate)
            {
                closing = _session;
                _session = null;
                _fired = false;
                _idledAt = null;
            }

            closing?.Close();
        }

        public void Dispose()
        {
            Stop();
        }

        private async Task RunAsync(Session session)
        {
            try
            {
                if (_socketPath is null)
                {
                    return;
                }

                Socket socket = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                if (!session.Adopt(socket))
                {
                    return;
                }

                await socket.ConnectAsync(new UnixDomainSocketEndPoint(_socketPath), session.Token).ConfigureAwait(false);
                session.Send(KWinKeystatePolicy.Hello());
                byte[] buffer = new byte[4096];
                int filled = 0;
                while (true)
                {
                    if (filled == buffer.Length)
                    {
                        Array.Resize(ref buffer, buffer.Length * 2);
                    }

                    int read = await socket.ReceiveAsync(buffer.AsMemory(filled), SocketFlags.None, session.Token).ConfigureAwait(false);
                    if (read == 0)
                    {
                        return;
                    }

                    filled += read;
                    int offset = 0;
                    List<WaylandMessage> messages = [];
                    while (KWinKeystatePolicy.TryRead(buffer.AsSpan(offset, filled - offset), out WaylandMessage? message, out int used))
                    {
                        offset += used;
                        messages.Add(message!);
                    }

                    Buffer.BlockCopy(buffer, offset, buffer, 0, filled - offset);
                    filled -= offset;
                    foreach (WaylandMessage message in messages)
                    {
                        if (!Handle(session, message))
                        {
                            return;
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is SocketException or IOException or OperationCanceledException or ObjectDisposedException or InvalidDataException)
            {
                // No compositor, a closed connection or a Stop: idle simply never fires.
                Debug.WriteLine($"[Idle] {ex.Message}");
            }
            finally
            {
                session.Close();
            }
        }

        /// <summary>Acts on one event; false ends the connection.</summary>
        private bool Handle(Session session, WaylandMessage message)
        {
            switch (message.ObjectId, message.Opcode)
            {
                case (KWinKeystatePolicy.RegistryId, KWinKeystatePolicy.RegistryGlobal):
                    session.Offer(KWinKeystatePolicy.ReadGlobal(message.Args));
                    return true;

                case (KWinKeystatePolicy.SyncCallbackId, KWinKeystatePolicy.CallbackDone):
                    // Every global has been listed: without a seat and the notifier there is nothing to wait for.
                    return session.Bind(Threshold);

                case (KWinKeystatePolicy.DisplayId, KWinKeystatePolicy.DisplayError):
                    Debug.WriteLine("[Idle] the compositor reported a protocol error");
                    return false;

                case (_, ExtIdleNotifyPolicy.NotificationIdled) when session.IsCurrent(message.ObjectId):
                    OnIdled(session);
                    return true;

                case (_, ExtIdleNotifyPolicy.NotificationResumed) when session.IsCurrent(message.ObjectId):
                    OnResumed(session);
                    return true;

                default:
                    return true;
            }
        }

        private void OnIdled(Session session)
        {
            EventHandler? reached;
            lock (_gate)
            {
                if (_session != session || _fired)
                {
                    return;
                }

                _fired = true;
                _idledAt = Stopwatch.GetTimestamp();
                reached = IdleThresholdReached;
            }

            try
            {
                reached?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // A subscriber's failure must not end the connection.
                Debug.WriteLine($"[Idle] handler: {ex.Message}");
            }
        }

        private void OnResumed(Session session)
        {
            lock (_gate)
            {
                if (_session == session)
                {
                    _fired = false;
                    _idledAt = null;
                }
            }
        }

        /// <summary>One connection: its socket, the globals it was offered, and the notification now in force.</summary>
        private sealed class Session
        {
            private readonly Lock _gate = new();
            private readonly CancellationTokenSource _cancel = new();
            private Socket? _socket;
            private bool _closed;
            private uint? _seatName;
            private (uint Name, uint Version)? _notifier;
            private uint _notifierVersion;
            private uint _current;
            private uint _nextId = ExtIdleNotifyPolicy.FirstNotificationId;

            public CancellationToken Token => _cancel.Token;

            public bool IsBound
            {
                get
                {
                    lock (_gate)
                    {
                        return _current != 0 && !_closed;
                    }
                }
            }

            public bool Adopt(Socket socket)
            {
                lock (_gate)
                {
                    if (!_closed)
                    {
                        _socket = socket;
                        return true;
                    }
                }

                socket.Dispose();
                return false;
            }

            public void Offer((uint Name, string Interface, uint Version) global)
            {
                lock (_gate)
                {
                    if (global.Interface == ExtIdleNotifyPolicy.SeatInterface)
                    {
                        // The first seat, as clients without seat handling take it.
                        _seatName ??= global.Name;
                    }
                    else if (global.Interface == ExtIdleNotifyPolicy.NotifierInterface)
                    {
                        _notifier = (global.Name, global.Version);
                    }
                }
            }

            /// <summary>Binds the seat and notifier and asks for the first notification; false when either is missing.</summary>
            public bool Bind(TimeSpan threshold)
            {
                lock (_gate)
                {
                    if (_current != 0)
                    {
                        return true;
                    }

                    if (_seatName is not uint seat || _notifier is not { } notifier)
                    {
                        return false;
                    }

                    _notifierVersion = ExtIdleNotifyPolicy.BindVersion(notifier.Version);
                    SendLocked(ExtIdleNotifyPolicy.Bind(seat, notifier.Name, _notifierVersion));
                    NotifyLocked(threshold);
                    return true;
                }
            }

            /// <summary>Replaces the notification with one at the new threshold, once bound.</summary>
            public void Renotify(TimeSpan threshold)
            {
                lock (_gate)
                {
                    if (_current == 0 || _closed)
                    {
                        return;
                    }

                    try
                    {
                        SendLocked(ExtIdleNotifyPolicy.Destroy(_current));
                        NotifyLocked(threshold);
                    }
                    catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
                    {
                        // The connection is going; the caller setting a threshold must not see it.
                        Debug.WriteLine($"[Idle] renotify: {ex.Message}");
                    }
                }
            }

            public bool IsCurrent(uint objectId)
            {
                lock (_gate)
                {
                    return objectId == _current && _current != 0;
                }
            }

            public void Send(byte[] bytes)
            {
                lock (_gate)
                {
                    SendLocked(bytes);
                }
            }

            public void Close()
            {
                Socket? socket;
                lock (_gate)
                {
                    if (_closed)
                    {
                        return;
                    }

                    _closed = true;
                    socket = _socket;
                    _socket = null;
                }

                _cancel.Cancel();
                socket?.Dispose();
                _cancel.Dispose();
            }

            private void NotifyLocked(TimeSpan threshold)
            {
                _current = _nextId++;
                SendLocked(ExtIdleNotifyPolicy.Notify(_current, _notifierVersion, threshold));
            }

            private void SendLocked(byte[] bytes)
            {
                if (_closed || _socket is null)
                {
                    throw new ObjectDisposedException(nameof(Session));
                }

                _ = _socket.Send(bytes);
            }
        }
    }
}
