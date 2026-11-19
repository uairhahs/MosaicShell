using MosaicShell.Core.Services.Mpris;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// <see cref="IMprisBus"/> over the D-Bus session bus. It owns its connection (Avalonia keeps its own), watches
    /// NameOwnerChanged for players arriving and leaving, and maps PropertiesChanged and Seeked, which arrive from a
    /// unique name such as ":1.858", back to the player's well-known name.
    /// </summary>
    /// <param name="address">A bus address; null for the user's session bus, empty for none. Tests pass a private bus.</param>
    public sealed class DBusMprisBus(string? address = null) : IMprisBus
    {
        /// <summary>
        /// Writes a call's body. MessageWriter is a ref struct: passed by value (as Action&lt;MessageWriter&gt; would),
        /// the body lands in a copy, the real writer never advances, and the bus drops the connection over the
        /// malformed message. Hence by ref.
        /// </summary>
        private delegate void BodyWriter(ref MessageWriter writer);

        private const string DBusName = "org.freedesktop.DBus";
        private const string DBusPath = "/org/freedesktop/DBus";
        private const string PropertiesInterface = "org.freedesktop.DBus.Properties";

        // Checked in ConnectAsync, not here, so a machine without a session bus (a bare TTY, a container) still builds
        // its services and simply has no media session.
        private readonly string? _address = address ?? DBusAddress.Session;
        private readonly Lock _ownersGate = new();

        // Guards _connection, _watches and _disposed, so a Dispose during ConnectAsync still closes everything.
        private readonly Lock _lifetimeGate = new();

        // Unique name to the player names it owns. A process may own several (a browser with two windows).
        private readonly Dictionary<string, HashSet<string>> _owners = new(StringComparer.Ordinal);
        private readonly List<IDisposable> _watches = [];
        private DBusConnection? _connection;
        private bool _disposed;

        public event EventHandler<MprisPlayerSignal>? PlayerChanged;
        public event EventHandler? PlayersChanged;

        private DBusConnection Connection => _connection ?? throw new InvalidOperationException("Not connected.");

        public async Task ConnectAsync()
        {
            if (string.IsNullOrEmpty(_address))
            {
                throw new InvalidOperationException("No D-Bus session bus address (DBUS_SESSION_BUS_ADDRESS is unset).");
            }

            DBusConnection connection = new(_address);
            lock (_lifetimeGate)
            {
                if (_disposed)
                {
                    connection.Dispose();
                    throw new ObjectDisposedException(nameof(DBusMprisBus));
                }

                // Held before the handshake, not after it: a Dispose that lands mid-handshake must close it, or the
                // connection finishes connecting with nobody left to close it.
                _connection = connection;
            }

            await connection.ConnectAsync().ConfigureAwait(false);

            Keep(await connection.AddMatchAsync(
                new MatchRule
                {
                    Type = MessageType.Signal,
                    Sender = DBusName,
                    Interface = DBusName,
                    Member = "NameOwnerChanged",

                    // No Arg0Namespace: Tmds.DBus.Protocol 0.94.1 writes the key as "arg0Namespace", which
                    // dbus-daemon rejects (MatchRuleInvalid). OnNameOwnerChanged filters to player names instead.
                },
                static (m, _) =>
                {
                    Reader r = m.GetBodyReader();
                    return (Name: r.ReadString(), Old: r.ReadString(), New: r.ReadString());
                },
                n =>
                {
                    if (n.HasValue)
                    {
                        OnNameOwnerChanged(n.Value.Name, n.Value.Old, n.Value.New);
                    }
                },
                emitOnCapturedContext: false,
                ObserverFlags.None).ConfigureAwait(false));

            Keep(await connection.AddMatchAsync(
                new MatchRule
                {
                    Type = MessageType.Signal,
                    Path = MprisPolicy.ObjectPath,
                    Interface = PropertiesInterface,
                    Member = "PropertiesChanged",
                },
                static (m, _) => m.SenderAsString ?? "",
                n =>
                {
                    if (n.HasValue)
                    {
                        RaiseFor(n.Value, seeked: false);
                    }
                },
                emitOnCapturedContext: false,
                ObserverFlags.None).ConfigureAwait(false));

            Keep(await connection.AddMatchAsync(
                new MatchRule
                {
                    Type = MessageType.Signal,
                    Path = MprisPolicy.ObjectPath,
                    Interface = MprisPolicy.PlayerInterface,
                    Member = "Seeked",
                },
                static (m, _) => m.SenderAsString ?? "",
                n =>
                {
                    if (n.HasValue)
                    {
                        RaiseFor(n.Value, seeked: true);
                    }
                },
                emitOnCapturedContext: false,
                ObserverFlags.None).ConfigureAwait(false));
        }

        public async Task<IReadOnlyList<string>> ListPlayersAsync()
        {
            string[] names = await Connection.ListServicesAsync().ConfigureAwait(false);
            List<string> players = [.. names.Where(MprisPolicy.IsPlayerBusName).Order(StringComparer.Ordinal)];
            foreach (string player in players)
            {
                try
                {
                    string owner = await CallAsync(DBusName, DBusPath, DBusName, "GetNameOwner", "s",
                        (ref w) => w.WriteString(player), static (m, _) => m.GetBodyReader().ReadString()).ConfigureAwait(false);
                    SetOwner(player, owner);
                }
                catch (DBusErrorReplyException)
                {
                    // Left between ListNames and GetNameOwner.
                }
            }

            return players;
        }

        public async Task<MprisPlayerState?> ReadPlayerAsync(string busName)
        {
            Dictionary<string, VariantValue> player;
            try
            {
                player = await GetAllAsync(busName, MprisPolicy.PlayerInterface).ConfigureAwait(false);
            }
            catch (DBusErrorReplyException)
            {
                return null;
            }

            Dictionary<string, VariantValue> root;
            try
            {
                root = await GetAllAsync(busName, MprisPolicy.RootInterface).ConfigureAwait(false);
            }
            catch (DBusErrorReplyException)
            {
                root = [];
            }

            int? pid = null;
            try
            {
                uint raw = await CallAsync(DBusName, DBusPath, DBusName, "GetConnectionUnixProcessID", "s",
                    (ref w) => w.WriteString(busName), static (m, _) => m.GetBodyReader().ReadUInt32()).ConfigureAwait(false);
                pid = raw is > 0 and <= int.MaxValue ? (int)raw : null;
            }
            catch (DBusErrorReplyException)
            {
                // Some brokers refuse for sandboxed peers; mirrors are then simply not removed.
            }

            return MprisVariantDecoder.Decode(busName, root, player, pid);
        }

        public async Task<long?> ReadPositionAsync(string busName)
        {
            try
            {
                VariantValue value = await CallAsync(busName, MprisPolicy.ObjectPath, PropertiesInterface, "Get", "ss",
                    (ref w) =>
                    {
                        w.WriteString(MprisPolicy.PlayerInterface);
                        w.WriteString("Position");
                    },
                    static (m, _) => m.GetBodyReader().ReadVariantValue()).ConfigureAwait(false);
                return MprisVariantDecoder.Integer(value);
            }
            catch (DBusErrorReplyException)
            {
                return null;
            }
        }

        public Task PlayPauseAsync(string busName)
        {
            return PlayerCallAsync(busName, "PlayPause");
        }

        public Task NextAsync(string busName)
        {
            return PlayerCallAsync(busName, "Next");
        }

        public Task PreviousAsync(string busName)
        {
            return PlayerCallAsync(busName, "Previous");
        }

        public Task SetPositionAsync(string busName, string trackId, long positionMicros)
        {
            return PlayerCallAsync(busName, "SetPosition", "ox", (ref w) =>
            {
                w.WriteObjectPath(trackId);
                w.WriteInt64(positionMicros);
            });
        }

        public Task SeekByAsync(string busName, long offsetMicros)
        {
            return PlayerCallAsync(busName, "Seek", "x", (ref w) => w.WriteInt64(offsetMicros));
        }

        public Task SetShuffleAsync(string busName, bool shuffle)
        {
            return SetPropertyAsync(busName, "Shuffle", (ref w) => w.WriteVariantBool(shuffle));
        }

        public Task SetLoopStatusAsync(string busName, string loopStatus)
        {
            return SetPropertyAsync(busName, "LoopStatus", (ref w) => w.WriteVariantString(loopStatus));
        }

        public void Dispose()
        {
            IDisposable[] watches;
            DBusConnection? connection;
            lock (_lifetimeGate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                watches = [.. _watches];
                _watches.Clear();
                connection = _connection;
                _connection = null;
            }

            foreach (IDisposable watch in watches)
            {
                watch.Dispose();
            }

            connection?.Dispose();
        }

        /// <summary>Keeps a signal watch for Dispose, or closes it at once when Dispose has already run.</summary>
        private void Keep(IDisposable watch)
        {
            lock (_lifetimeGate)
            {
                if (!_disposed)
                {
                    _watches.Add(watch);
                    return;
                }
            }

            watch.Dispose();
            throw new ObjectDisposedException(nameof(DBusMprisBus));
        }

        private void OnNameOwnerChanged(string name, string oldOwner, string newOwner)
        {
            if (!MprisPolicy.IsPlayerBusName(name))
            {
                return;
            }

            lock (_ownersGate)
            {
                if (oldOwner.Length > 0 && _owners.TryGetValue(oldOwner, out HashSet<string>? old))
                {
                    _ = old.Remove(name);
                    if (old.Count == 0)
                    {
                        _ = _owners.Remove(oldOwner);
                    }
                }
            }

            if (newOwner.Length > 0)
            {
                SetOwner(name, newOwner);
            }

            PlayersChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SetOwner(string player, string owner)
        {
            lock (_ownersGate)
            {
                if (!_owners.TryGetValue(owner, out HashSet<string>? names))
                {
                    names = new HashSet<string>(StringComparer.Ordinal);
                    _owners[owner] = names;
                }

                _ = names.Add(player);
            }
        }

        private void RaiseFor(string sender, bool seeked)
        {
            string[] players;
            lock (_ownersGate)
            {
                players = _owners.TryGetValue(sender, out HashSet<string>? names) ? [.. names] : [];
            }

            foreach (string player in players)
            {
                PlayerChanged?.Invoke(this, new MprisPlayerSignal(player, seeked));
            }
        }

        private Task<Dictionary<string, VariantValue>> GetAllAsync(string busName, string iface)
        {
            return CallAsync(busName, MprisPolicy.ObjectPath, PropertiesInterface, "GetAll", "s",
                (ref w) => w.WriteString(iface),
                static (m, _) => m.GetBodyReader().ReadDictionaryOfStringToVariantValue());
        }

        private Task PlayerCallAsync(string busName, string member, string? signature = null, BodyWriter? write = null)
        {
            return CallAsync(busName, MprisPolicy.ObjectPath, MprisPolicy.PlayerInterface, member, signature, write,
                static (_, _) => true);
        }

        private Task SetPropertyAsync(string busName, string property, BodyWriter writeValue)
        {
            return CallAsync(busName, MprisPolicy.ObjectPath, PropertiesInterface, "Set", "ssv",
                (ref w) =>
                {
                    w.WriteString(MprisPolicy.PlayerInterface);
                    w.WriteString(property);
                    writeValue(ref w);
                },
                static (_, _) => true);
        }

        private Task<T> CallAsync<T>(
            string destination,
            string path,
            string iface,
            string member,
            string? signature,
            BodyWriter? write,
            MessageValueReader<T> reader)
        {
            DBusConnection connection = Connection;
            MessageBuffer message;
            MessageWriter writer = connection.GetMessageWriter();
            try
            {
                writer.WriteMethodCallHeader(destination, path, iface, member, signature);
                write?.Invoke(ref writer);
                message = writer.CreateMessage();
            }
            finally
            {
                writer.Dispose();
            }

            return connection.CallMethodAsync(message, reader);
        }
    }
}
