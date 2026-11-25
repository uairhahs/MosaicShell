using System.Diagnostics;
using MosaicShell.Core.Services.Gnome;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// Idle on GNOME from Mutter's idle monitor (<see cref="MutterIdlePolicy"/>). Start asks for an idle watch at the
    /// threshold; when it fires, IdleThresholdReached is raised and a user-active watch is added, and only once that
    /// fires (input) can the next idle stretch raise again: once per stretch, as the Windows and Wayland services do.
    /// Nothing is sampled; Mutter pushes both.
    /// </summary>
    /// <remarks>
    /// Without a session bus, or without Mutter (any other desktop), it stays inactive and never raises.
    /// <see cref="IdleTime"/> is known only past the threshold, so it reads zero before then. Raised on a thread-pool
    /// thread. Only WatchFired from Mutter's own name is heard; the bus checks the sender.
    /// </remarks>
    /// <param name="address">A bus address; null for the user's session bus, empty for none. Tests pass a private bus.</param>
    public sealed class MutterIdleService(string? address = null) : IIdleService
    {
        private readonly string? _address = address ?? DBusAddress.Session;
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
                    return _session is { IsListening: true };
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
                Session? session;
                lock (_gate)
                {
                    if (_threshold == value)
                    {
                        return;
                    }

                    _threshold = value;
                    session = _session;
                }

                if (session is { IsListening: true })
                {
                    _ = ReplaceIdleWatchAsync(session, value);
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
                _ = ListenAsync(session);
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

        private async Task ListenAsync(Session session)
        {
            try
            {
                if (string.IsNullOrEmpty(_address))
                {
                    Debug.WriteLine("[MutterIdle] no session bus address");
                    return;
                }

                DBusConnection connection = new(_address);
                if (!session.Adopt(connection))
                {
                    return;
                }

                await connection.ConnectAsync().ConfigureAwait(false);
                session.Connection = connection;
                IDisposable watch = await connection.AddMatchAsync(
                    new MatchRule
                    {
                        Type = MessageType.Signal,
                        Sender = MutterIdlePolicy.BusName,
                        Path = MutterIdlePolicy.ObjectPath,
                        Interface = MutterIdlePolicy.Interface,
                        Member = MutterIdlePolicy.WatchFiredMember,
                    },
                    static (m, _) => m.GetBodyReader().ReadUInt32(),
                    id =>
                    {
                        if (id.HasValue && session.IsListening)
                        {
                            OnWatchFired(session, id.Value);
                        }
                    },
                    emitOnCapturedContext: false,
                    ObserverFlags.None).ConfigureAwait(false);
                if (!session.Adopt(watch))
                {
                    return;
                }

                TimeSpan threshold;
                lock (_gate)
                {
                    threshold = _threshold;
                }

                session.IdleWatchId = await AddIdleWatchAsync(connection, threshold).ConfigureAwait(false);
                session.MarkListening();
            }
            catch (Exception ex)
            {
                // No bus, no Mutter or a closed session: there is simply no idle signal.
                Debug.WriteLine($"[MutterIdle] unavailable: {ex.Message}");
                session.Close();
            }
        }

        private void OnWatchFired(Session session, uint id)
        {
            if (id == session.IdleWatchId)
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

                _ = WaitForInputAsync(session);
                try
                {
                    reached?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    // A subscriber's failure must not take down the bus's dispatch thread.
                    Debug.WriteLine($"[MutterIdle] handler: {ex.Message}");
                }

                return;
            }

            if (id != 0 && id == session.ActiveWatchId)
            {
                session.ActiveWatchId = 0;
                lock (_gate)
                {
                    if (_session == session)
                    {
                        _fired = false;
                        _idledAt = null;
                    }
                }
            }
        }

        private static async Task WaitForInputAsync(Session session)
        {
            try
            {
                if (session.Connection is DBusConnection connection)
                {
                    session.ActiveWatchId = await CallForIdAsync(connection, MutterIdlePolicy.AddUserActiveWatchMember, null, null).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MutterIdle] active watch: {ex.Message}");
            }
        }

        private static async Task ReplaceIdleWatchAsync(Session session, TimeSpan threshold)
        {
            try
            {
                if (session.Connection is not DBusConnection connection)
                {
                    return;
                }

                uint old = session.IdleWatchId;
                session.IdleWatchId = await AddIdleWatchAsync(connection, threshold).ConfigureAwait(false);
                if (old != 0)
                {
                    await RemoveWatchAsync(connection, old).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MutterIdle] new threshold: {ex.Message}");
            }
        }

        private static Task<uint> AddIdleWatchAsync(DBusConnection connection, TimeSpan threshold)
        {
            ulong interval = MutterIdlePolicy.WatchIntervalMs(threshold);
            return CallForIdAsync(connection, MutterIdlePolicy.AddIdleWatchMember, "t", (ref w) => w.WriteUInt64(interval));
        }

        private static Task RemoveWatchAsync(DBusConnection connection, uint id)
        {
            return DBusCalls.CallAsync(
                connection, MutterIdlePolicy.BusName, MutterIdlePolicy.ObjectPath, MutterIdlePolicy.Interface,
                MutterIdlePolicy.RemoveWatchMember, "u", (ref w) => w.WriteUInt32(id));
        }

        private static Task<uint> CallForIdAsync(DBusConnection connection, string member, string? signature, DBusBodyWriter? write)
        {
            return DBusCalls.CallAsync(
                connection, MutterIdlePolicy.BusName, MutterIdlePolicy.ObjectPath, MutterIdlePolicy.Interface,
                member, signature, write, static (m, _) => m.GetBodyReader().ReadUInt32());
        }

        /// <summary>A <see cref="ListeningSession"/> that also keeps its connection and Mutter's watch ids.</summary>
        private sealed class Session : ListeningSession
        {
            public DBusConnection? Connection { get; set; }

            public uint IdleWatchId { get; set; }

            public uint ActiveWatchId { get; set; }
        }
    }
}
