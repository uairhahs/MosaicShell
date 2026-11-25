using System.Diagnostics;
using MosaicShell.Core.Services.Xdg;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// Keeps the desktop's accent colour (<see cref="PortalAccentPolicy"/>) current on Linux: Start reads it once and a
    /// watch on the portal's SettingChanged follows each change, so a flyout built later uses the new colour, as Windows
    /// re-reads its colorization for each flyout. Nothing is read while a flyout is built.
    /// </summary>
    /// <remarks>
    /// Without a session bus or a portal, <see cref="Current"/> stays null and the palette keeps its fallback. Only
    /// signals from the portal's own name are heard; the bus checks the sender. Changed is raised on a thread-pool thread.
    /// </remarks>
    /// <param name="address">A bus address; null for the user's session bus, empty for none. Tests pass a private bus.</param>
    public sealed class PortalAccentWatcher(string? address = null) : IDisposable
    {
        private readonly string? _address = address ?? DBusAddress.Session;
        private readonly Lock _gate = new();
        private readonly List<IDisposable> _owned = [];
        private (byte R, byte G, byte B)? _current;
        private bool _started;
        private bool _listening;
        private bool _disposed;

        public event EventHandler? Changed;

        /// <summary>The accent the desktop sets, or null when it sets none or there is no portal.</summary>
        public (byte R, byte G, byte B)? Current
        {
            get
            {
                lock (_gate)
                {
                    return _current;
                }
            }
        }

        /// <summary>True once the change watch is in place.</summary>
        public bool IsListening
        {
            get
            {
                lock (_gate)
                {
                    return _listening && !_disposed;
                }
            }
        }

        public void Start()
        {
            lock (_gate)
            {
                if (_started || _disposed)
                {
                    return;
                }

                _started = true;
            }

            _ = ListenAsync();
        }

        public void Dispose()
        {
            IDisposable[] owned;
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                owned = [.. _owned];
                _owned.Clear();
            }

            // The watch before the connection that carries it.
            for (int i = owned.Length - 1; i >= 0; i--)
            {
                owned[i].Dispose();
            }
        }

        private bool Adopt(IDisposable resource)
        {
            lock (_gate)
            {
                if (!_disposed)
                {
                    _owned.Add(resource);
                    return true;
                }
            }

            resource.Dispose();
            return false;
        }

        private async Task ListenAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_address))
                {
                    return;
                }

                DBusConnection connection = new(_address);
                if (!Adopt(connection))
                {
                    return;
                }

                await connection.ConnectAsync().ConfigureAwait(false);
                IDisposable watch = await connection.AddMatchAsync(
                    new MatchRule
                    {
                        Type = MessageType.Signal,
                        Sender = PortalAccentPolicy.BusName,
                        Path = PortalAccentPolicy.ObjectPath,
                        Interface = PortalAccentPolicy.Interface,
                        Member = PortalAccentPolicy.SettingChangedMember,
                    },
                    static (m, _) =>
                    {
                        Reader r = m.GetBodyReader();
                        string ns = r.ReadString();
                        string key = r.ReadString();
                        return PortalAccentPolicy.Touches(ns, key) ? (Read: true, Value: Decode(r.ReadVariantValue())) : (false, null);
                    },
                    change =>
                    {
                        if (change.HasValue && change.Value.Read)
                        {
                            Update(change.Value.Value);
                        }
                    },
                    emitOnCapturedContext: false,
                    ObserverFlags.None).ConfigureAwait(false);
                if (!Adopt(watch))
                {
                    return;
                }

                lock (_gate)
                {
                    _listening = !_disposed;
                }

                Update(await ReadAsync(connection).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                // No bus or no portal: the palette keeps its fallback.
                Debug.WriteLine($"[PortalAccent] unavailable: {ex.Message}");
            }
        }

        /// <summary>ReadOne (portal version 2); Read, whose value is wrapped in one more variant, where it is missing.</summary>
        private static async Task<(byte R, byte G, byte B)?> ReadAsync(DBusConnection connection)
        {
            try
            {
                return await CallAsync(connection, "ReadOne").ConfigureAwait(false);
            }
            catch (DBusErrorReplyException)
            {
                return await CallAsync(connection, "Read").ConfigureAwait(false);
            }
        }

        private static Task<(byte R, byte G, byte B)?> CallAsync(DBusConnection connection, string member)
        {
            return DBusCalls.CallAsync(
                connection,
                PortalAccentPolicy.BusName,
                PortalAccentPolicy.ObjectPath,
                PortalAccentPolicy.Interface,
                member,
                "ss",
                (ref w) =>
                {
                    w.WriteString(PortalAccentPolicy.Namespace);
                    w.WriteString(PortalAccentPolicy.Key);
                },
                static (m, _) => Decode(m.GetBodyReader().ReadVariantValue()));
        }

        /// <summary>The (ddd) inside however many variants wrap it; null for anything else.</summary>
        private static (byte R, byte G, byte B)? Decode(VariantValue value)
        {
            while (value.Type == VariantValueType.Variant)
            {
                value = value.GetVariantValue();
            }

            return value.Type == VariantValueType.Struct && value.Count == 3
                ? PortalAccentPolicy.FromRgb(value.GetItem(0).GetDouble(), value.GetItem(1).GetDouble(), value.GetItem(2).GetDouble())
                : null;
        }

        private void Update((byte R, byte G, byte B)? accent)
        {
            EventHandler? changed;
            lock (_gate)
            {
                if (_disposed || _current == accent)
                {
                    return;
                }

                _current = accent;
                changed = Changed;
            }

            try
            {
                changed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // A subscriber's failure must not take down the bus's dispatch thread.
                Debug.WriteLine($"[PortalAccent] handler: {ex.Message}");
            }
        }
    }
}
