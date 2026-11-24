using System.Net.Sockets;
using MosaicShell.Core.Services.Kde;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// Caps, Num and Scroll Lock on KDE Plasma, pushed by KWin through <c>org_kde_kwin_keystate</c>
    /// (<see cref="KWinKeystatePolicy"/>) on the Host's own Wayland connection, so nothing is sampled while idle. Like
    /// the Windows service, it runs while started or subscribed, the first state of each key is taken without raising
    /// Changed, and Changed is raised on each edge on the reading thread (Tessera marshals it to the UI).
    /// </summary>
    /// <remarks>
    /// Without a Wayland socket, or on a compositor that does not offer the protocol (GNOME), it stays inactive and every
    /// key reads off, as <see cref="NullLockKeysService"/> does.
    /// </remarks>
    /// <param name="socketPath">The compositor socket; null finds it from WAYLAND_DISPLAY and XDG_RUNTIME_DIR. Tests pass their own.</param>
    public sealed class KWinKeystateLockKeys(string? socketPath = null) : ILockKeysService
    {
        private readonly string? _socketPath = socketPath ?? KWinKeystatePolicy.SocketPath(
            Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"),
            Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR"));

        private readonly Lock _gate = new();
        private EventHandler<LockKeyState>? _changed;
        private Session? _session;
        private int _startRef;
        private bool _caps, _num, _scroll;

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

        public LockKeyState Caps => Read(LockKeyKind.CapsLock);
        public LockKeyState Num => Read(LockKeyKind.NumLock);
        public LockKeyState Scroll => Read(LockKeyKind.ScrollLock);

        public event EventHandler<LockKeyState>? Changed
        {
            add
            {
                lock (_gate)
                {
                    _changed += value;
                    EnsureSessionLocked();
                }
            }
            remove
            {
                Session? closing;
                lock (_gate)
                {
                    _changed -= value;
                    closing = ReleaseSessionLocked();
                }

                closing?.Close();
            }
        }

        public void Start()
        {
            lock (_gate)
            {
                _startRef++;
                EnsureSessionLocked();
            }
        }

        public void Stop()
        {
            Session? closing;
            lock (_gate)
            {
                if (_startRef <= 0)
                {
                    return;
                }

                _startRef--;
                closing = ReleaseSessionLocked();
            }

            closing?.Close();
        }

        public void Dispose()
        {
            Session? closing;
            lock (_gate)
            {
                _startRef = 0;
                _changed = null;
                closing = ReleaseSessionLocked();
            }

            closing?.Close();
        }

        private LockKeyState Read(LockKeyKind kind)
        {
            lock (_gate)
            {
                return new LockKeyState(kind, kind switch
                {
                    LockKeyKind.CapsLock => _caps,
                    LockKeyKind.NumLock => _num,
                    _ => _scroll,
                });
            }
        }

        private void EnsureSessionLocked()
        {
            if (_session is not null || (_startRef == 0 && _changed is null))
            {
                return;
            }

            Session session = new();
            _session = session;
            _ = Task.Run(() => RunAsync(session));
        }

        private Session? ReleaseSessionLocked()
        {
            if (_startRef > 0 || _changed is not null || _session is not { } session)
            {
                return null;
            }

            _session = null;
            return session;
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
                _ = await socket.SendAsync(KWinKeystatePolicy.Hello(), SocketFlags.None, session.Token).ConfigureAwait(false);
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
                        if (!await HandleAsync(session, socket, message).ConfigureAwait(false))
                        {
                            return;
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is SocketException or IOException or OperationCanceledException or ObjectDisposedException or InvalidDataException)
            {
                // No compositor, a closed connection or a Stop: lock keys simply read off.
                System.Diagnostics.Debug.WriteLine($"[Keystate] {ex.Message}");
            }
            finally
            {
                session.Close();
            }
        }

        /// <summary>Acts on one event; false ends the connection.</summary>
        private async Task<bool> HandleAsync(Session session, Socket socket, WaylandMessage message)
        {
            switch (message.ObjectId, message.Opcode)
            {
                case (KWinKeystatePolicy.RegistryId, KWinKeystatePolicy.RegistryGlobal) when !session.IsBound:
                    (uint name, string iface, _) = KWinKeystatePolicy.ReadGlobal(message.Args);
                    if (iface == KWinKeystatePolicy.Interface)
                    {
                        _ = await socket.SendAsync(KWinKeystatePolicy.BindAndFetch(name), SocketFlags.None, session.Token).ConfigureAwait(false);
                        session.MarkBound();
                    }

                    return true;

                case (KWinKeystatePolicy.SyncCallbackId, KWinKeystatePolicy.CallbackDone):
                    // Every global has been listed: without keystate there is nothing to wait for.
                    return session.IsBound;

                case (KWinKeystatePolicy.KeystateId, KWinKeystatePolicy.KeystateStateChanged):
                    if (KWinKeystatePolicy.ReadStateChanged(message.Args) is { } state)
                    {
                        OnState(session, state);
                    }

                    return true;

                case (KWinKeystatePolicy.DisplayId, KWinKeystatePolicy.DisplayError):
                    System.Diagnostics.Debug.WriteLine("[Keystate] the compositor reported a protocol error");
                    return false;

                default:
                    return true;
            }
        }

        private void OnState(Session session, LockKeyState state)
        {
            EventHandler<LockKeyState>? changed;
            lock (_gate)
            {
                if (_session != session)
                {
                    return;
                }

                ref bool field = ref Field(state.Key);
                bool edge = !session.Prime(state.Key) && field != state.IsOn;
                field = state.IsOn;
                changed = edge ? _changed : null;
            }

            try
            {
                changed?.Invoke(this, state);
            }
            catch (Exception ex)
            {
                // A subscriber's failure must not end the connection.
                System.Diagnostics.Debug.WriteLine($"[Keystate] handler: {ex.Message}");
            }
        }

        private ref bool Field(LockKeyKind key)
        {
            return ref key == LockKeyKind.CapsLock ? ref _caps : ref key == LockKeyKind.NumLock ? ref _num : ref _scroll;
        }

        /// <summary>One connection: its socket, its cancellation, and which keys have had their first state.</summary>
        private sealed class Session
        {
            private readonly Lock _gate = new();
            private readonly CancellationTokenSource _cancel = new();
            private readonly HashSet<LockKeyKind> _primed = [];
            private Socket? _socket;
            private bool _closed;
            private bool _bound;

            public CancellationToken Token => _cancel.Token;

            public bool IsBound
            {
                get
                {
                    lock (_gate)
                    {
                        return _bound && !_closed;
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

            public void MarkBound()
            {
                lock (_gate)
                {
                    _bound = true;
                }
            }

            /// <summary>True the first time a key is seen on this connection.</summary>
            public bool Prime(LockKeyKind key)
            {
                lock (_gate)
                {
                    return _primed.Add(key);
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
        }
    }
}
