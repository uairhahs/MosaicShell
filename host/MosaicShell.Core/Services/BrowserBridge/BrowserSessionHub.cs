namespace MosaicShell.Core.Services.BrowserBridge
{
    /// <summary>The Host's end of one relay connection, as the hub sees it. The transport implements it.</summary>
    public interface IBrowserConnection
    {
        /// <summary>Queues an unframed JSON message for the extension. Must not block; may throw if the connection is gone.</summary>
        void Send(byte[] payload);

        /// <summary>Ends the connection. Safe to call more than once.</summary>
        void Close();
    }

    /// <summary>
    /// Holds every tab the connected browsers publish, one connection per relay. The extension is a separate process
    /// the Host does not control, so a connection that breaks the protocol, floods it or claims too many tabs is
    /// dropped, and one that goes quiet ages out of selection. No I/O happens here: the transport feeds it frames.
    /// </summary>
    public sealed class BrowserSessionHub(TimeProvider clock, int maxConnections = 8)
    {
        /// <summary>Tabs one connection may hold at once; a browser has few tabs playing media.</summary>
        public const int MaxSessionsPerConnection = 32;

        /// <summary>Malformed or excess messages tolerated on one connection before it is dropped.</summary>
        public const int MaxViolations = 10;

        /// <summary>Messages a connection may send back to back.</summary>
        public const int BurstMessages = 60;

        /// <summary>Sustained message rate; the extension sends only on change, plus a ping every few seconds.</summary>
        public const double MessagesPerSecond = 30;

        private readonly Lock _gate = new();
        private readonly Dictionary<int, ConnectionState> _connections = [];
        private readonly Dictionary<(int Connection, int Tab), Held> _sessions = [];
        private int _nextId;

        /// <summary>Raised when the set of sessions, or the content of one, changes. Not raised for pings.</summary>
        public event EventHandler? Changed;

        public int ConnectionCount
        {
            get
            {
                lock (_gate)
                {
                    return _connections.Count;
                }
            }
        }

        public IReadOnlyList<BrowserSessionEntry> Sessions
        {
            get
            {
                lock (_gate)
                {
                    return Snapshot();
                }
            }
        }

        /// <summary>Registers a connection and returns its id, or -1 when the connection cap is reached.</summary>
        public int Open(IBrowserConnection connection)
        {
            lock (_gate)
            {
                if (_connections.Count >= maxConnections)
                {
                    return -1;
                }

                int id = _nextId++;
                DateTimeOffset now = clock.GetUtcNow();
                _connections[id] = new ConnectionState(connection, now);
                return id;
            }
        }

        /// <summary>Forgets a connection and its tabs. Called by the transport when the relay goes away.</summary>
        public void Close(int connectionId)
        {
            bool changed;
            lock (_gate)
            {
                changed = RemoveLocked(connectionId);
            }

            if (changed)
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        public BrowserSessionEntry? Select(string? smtcTitle)
        {
            lock (_gate)
            {
                return BrowserSessionSelector.Select(Snapshot(), clock.GetUtcNow(), smtcTitle);
            }
        }

        /// <summary>Asks the tab's extension to put its rating in the wanted state. False when the tab or connection is gone.</summary>
        public bool SendCommand(BrowserSessionEntry target, BrowserCommandAction action)
        {
            IBrowserConnection connection;
            lock (_gate)
            {
                if (!_connections.TryGetValue(target.ConnectionId, out ConnectionState? state)
                    || !_sessions.ContainsKey((target.ConnectionId, target.Report.TabId)))
                {
                    return false;
                }

                connection = state.Connection;
            }

            try
            {
                connection.Send(BrowserProtocol.SerializeCommand(target.Report.TabId, action));
                return true;
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
            {
                Drop(target.ConnectionId);
                return false;
            }
        }

        /// <summary>Handles one unframed message from a connection.</summary>
        public void Receive(int connectionId, ReadOnlySpan<byte> frame)
        {
            // The rate limit is spent before parsing so that a flood costs the Host no more than a token check.
            bool admitted;
            bool drop = false;
            lock (_gate)
            {
                if (!_connections.TryGetValue(connectionId, out ConnectionState? state))
                {
                    return;
                }

                admitted = TakeToken(state, clock.GetUtcNow());
                if (!admitted)
                {
                    drop = Violation(state);
                }
            }

            if (!admitted)
            {
                if (drop)
                {
                    Drop(connectionId);
                }

                return;
            }

            BrowserParseResult result = BrowserProtocol.Parse(frame);
            bool changed;
            IBrowserConnection? replyTo;
            lock (_gate)
            {
                if (!_connections.TryGetValue(connectionId, out ConnectionState? state))
                {
                    return;
                }

                (changed, drop, replyTo) = Apply(connectionId, state, result, clock.GetUtcNow());
            }

            if (replyTo is not null && !TrySend(replyTo, BrowserProtocol.SerializeResync()))
            {
                drop = true;
            }

            if (drop)
            {
                Drop(connectionId);
            }
            else if (changed)
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        private (bool Changed, bool Drop, IBrowserConnection? ReplyTo) Apply(
            int connectionId, ConnectionState state, BrowserParseResult result, DateTimeOffset now)
        {
            switch (result.Error)
            {
                case BrowserParseError.None:
                    break;
                case BrowserParseError.UnknownType:
                    state.LastHeard = now;
                    return (false, false, null);
                case BrowserParseError.TooLarge:
                case BrowserParseError.UnsupportedProtocol:
                    return (false, true, null);
                default:
                    return (false, Violation(state), null);
            }

            state.LastHeard = now;
            if (result.Message is not BrowserHello && !state.HelloSeen)
            {
                return (false, true, null);
            }

            switch (result.Message)
            {
                case BrowserHello:
                    if (state.HelloSeen)
                    {
                        return (false, Violation(state), null);
                    }

                    state.HelloSeen = true;
                    return (false, false, state.Connection);
                case BrowserSessionReport report:
                    return Upsert(connectionId, state, report, now);
                case BrowserSessionRemoved removed:
                    return (Forget(connectionId, state, removed.TabId), false, null);
                default:
                    return (false, false, null);
            }
        }

        private (bool Changed, bool Drop, IBrowserConnection? ReplyTo) Upsert(
            int connectionId, ConnectionState state, BrowserSessionReport report, DateTimeOffset now)
        {
            (int, int) key = (connectionId, report.TabId);
            if (_sessions.TryGetValue(key, out Held? held))
            {
                if (SameReport(held.Report, report))
                {
                    return (false, false, null);
                }

                _sessions[key] = new Held(report, now);
                return (true, false, null);
            }

            if (state.SessionCount >= MaxSessionsPerConnection)
            {
                return (false, Violation(state), null);
            }

            _sessions[key] = new Held(report, now);
            state.SessionCount++;
            return (true, false, null);
        }

        private bool Forget(int connectionId, ConnectionState state, int tabId)
        {
            if (!_sessions.Remove((connectionId, tabId)))
            {
                return false;
            }

            state.SessionCount--;
            return true;
        }

        private static bool SameReport(BrowserSessionReport a, BrowserSessionReport b)
        {
            return a.WindowId == b.WindowId
                && a.Origin == b.Origin
                && a.Title == b.Title
                && a.Artist == b.Artist
                && a.Album == b.Album
                && a.PlaybackState == b.PlaybackState
                && a.Audible == b.Audible
                && a.Rating == b.Rating
                && a.Capabilities == b.Capabilities
                && a.Artwork.SequenceEqual(b.Artwork);
        }

        private static bool TakeToken(ConnectionState state, DateTimeOffset now)
        {
            double refilled = state.Tokens + ((now - state.TokensAt).TotalSeconds * MessagesPerSecond);
            state.Tokens = Math.Min(BurstMessages, refilled);
            state.TokensAt = now;
            if (state.Tokens < 1)
            {
                return false;
            }

            state.Tokens--;
            return true;
        }

        /// <summary>Counts a violation and reports whether the connection has now used up its allowance.</summary>
        private static bool Violation(ConnectionState state)
        {
            state.Violations++;
            return state.Violations >= MaxViolations;
        }

        private static bool TrySend(IBrowserConnection connection, byte[] payload)
        {
            try
            {
                connection.Send(payload);
                return true;
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
            {
                return false;
            }
        }

        /// <summary>Ends a connection the hub has decided against, and tells the flyout its tabs are gone.</summary>
        private void Drop(int connectionId)
        {
            IBrowserConnection? connection;
            bool changed;
            lock (_gate)
            {
                connection = _connections.TryGetValue(connectionId, out ConnectionState? state) ? state.Connection : null;
                changed = RemoveLocked(connectionId);
            }

            try
            {
                connection?.Close();
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
            {
                // Already gone; the connection is removed either way.
            }

            if (changed)
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Removes the connection and its tabs; true when at least one tab went with it.</summary>
        private bool RemoveLocked(int connectionId)
        {
            if (!_connections.Remove(connectionId))
            {
                return false;
            }

            List<(int Connection, int Tab)> owned = [.. _sessions.Keys.Where(k => k.Connection == connectionId)];
            foreach ((int Connection, int Tab) key in owned)
            {
                _ = _sessions.Remove(key);
            }

            return owned.Count > 0;
        }

        private List<BrowserSessionEntry> Snapshot()
        {
            return [.. _sessions.Select(kv => new BrowserSessionEntry(
                kv.Key.Connection, kv.Value.Report, kv.Value.UpdatedAt, _connections[kv.Key.Connection].LastHeard))];
        }

        private sealed record Held(BrowserSessionReport Report, DateTimeOffset UpdatedAt);

        private sealed class ConnectionState(IBrowserConnection connection, DateTimeOffset now)
        {
            public IBrowserConnection Connection { get; } = connection;
            public bool HelloSeen { get; set; }
            public int Violations { get; set; }
            public int SessionCount { get; set; }
            public double Tokens { get; set; } = BurstMessages;
            public DateTimeOffset TokensAt { get; set; } = now;
            public DateTimeOffset LastHeard { get; set; } = now;
        }
    }
}
