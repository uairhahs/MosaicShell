using System.Diagnostics;
using System.Text;
using MosaicShell.Core.Services.Kde;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// Airplane mode on KDE Plasma from Plasma's own setting (<see cref="PlasmaAirplanePolicy"/>): Start reads
    /// <c>AirplaneModeEnabled</c> from plasma-nm's file without raising, then a session-bus watch on KConfig's
    /// ConfigChanged re-reads it whenever a change names the key, and Changed is raised when the value flips. The radios
    /// only say whether there is anything to switch off (<see cref="IsSupported"/>), so a desktop with no radio shows no
    /// flight flyout, as on Windows; their own changes are not toggles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The signal has no fixed sender (KConfig sends it from whichever process wrote the file), so any sender is heard;
    /// a forged signal can only cause a re-read of the file, never a state the file does not hold.
    /// </para>
    /// <para>
    /// Without a session bus the state is read once at Start and never changes, and nothing throws. Changed is raised on
    /// a thread-pool thread.
    /// </para>
    /// </remarks>
    /// <param name="radios">Says whether the machine has a radio; started and stopped with this service.</param>
    /// <param name="configPath">The plasma-nm file; null for the user's. Tests pass a throwaway one.</param>
    /// <param name="address">A bus address; null for the user's session bus, empty for none. Tests pass a private bus.</param>
    public sealed class PlasmaAirplaneModeService(IAirplaneModeService radios, string? configPath = null, string? address = null) : IAirplaneModeService
    {
        private readonly string _configPath = configPath ?? PlasmaAirplanePolicy.ConfigPath(
            Environment.GetEnvironmentVariable(Xdg.XdgBaseDirectories.ConfigHomeVariable),
            Xdg.XdgBaseDirectories.CurrentHome());

        private readonly string? _address = address ?? DBusAddress.Session;
        private readonly Lock _gate = new();
        private ListeningSession? _session;
        private bool _enabled;
        private bool _disposed;

        public event EventHandler? Changed;

        public bool IsSupported => radios.IsSupported;

        public bool IsEnabled
        {
            get
            {
                lock (_gate)
                {
                    return _enabled;
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
                    return _session is { IsListening: true };
                }
            }
        }

        public void Start()
        {
            ListeningSession session;
            lock (_gate)
            {
                if (_disposed || _session is not null)
                {
                    return;
                }

                session = new ListeningSession();
                _session = session;
                _enabled = ReadState();
            }

            radios.Start();
            _ = ListenAsync(session);
        }

        public void Stop()
        {
            ListeningSession? session;
            lock (_gate)
            {
                session = _session;
                _session = null;
            }

            session?.Close();
            radios.Stop();
        }

        public void Dispose()
        {
            lock (_gate)
            {
                _disposed = true;
            }

            Stop();
            radios.Dispose();
        }

        private bool ReadState()
        {
            try
            {
                return PlasmaAirplanePolicy.ReadEnabled(File.Exists(_configPath) ? File.ReadAllText(_configPath) : null);
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"[PlasmaAirplane] read: {ex.Message}");
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"[PlasmaAirplane] read: {ex.Message}");
                return false;
            }
        }

        private async Task ListenAsync(ListeningSession session)
        {
            try
            {
                if (string.IsNullOrEmpty(_address))
                {
                    Debug.WriteLine("[PlasmaAirplane] no session bus address");
                    return;
                }

                DBusConnection connection = new(_address);
                if (!session.Adopt(connection))
                {
                    return;
                }

                await connection.ConnectAsync().ConfigureAwait(false);
                IDisposable watch = await connection.AddMatchAsync(
                    new MatchRule
                    {
                        Type = MessageType.Signal,
                        Path = PlasmaAirplanePolicy.NotifyPath,
                        Interface = PlasmaAirplanePolicy.NotifyInterface,
                        Member = PlasmaAirplanePolicy.NotifyMember,
                    },
                    static (m, _) => NamesAirplaneKey(m.GetBodyReader()),
                    touched =>
                    {
                        if (touched.HasValue && touched.Value && session.IsListening)
                        {
                            OnConfigChanged(session);
                        }
                    },
                    emitOnCapturedContext: false,
                    ObserverFlags.None).ConfigureAwait(false);
                if (!session.Adopt(watch))
                {
                    return;
                }

                session.MarkListening();

                // A toggle between Start's read and the watch would otherwise be missed; take it as current state.
                bool now = ReadState();
                lock (_gate)
                {
                    if (_session == session)
                    {
                        _enabled = now;
                    }
                }
            }
            catch (Exception ex)
            {
                // No bus, a refused connection or a closed session: the state read at Start simply stays.
                Debug.WriteLine($"[PlasmaAirplane] unavailable: {ex.Message}");
                session.Close();
            }
        }

        /// <summary>Reads ConfigChanged's a{saay} body (group to written keys) and says whether it names the airplane key.</summary>
        private static bool NamesAirplaneKey(Reader r)
        {
            bool touched = false;
            ArrayEnd groups = r.ReadDictionaryStart();
            while (r.HasNext(groups))
            {
                string group = r.ReadString();
                List<string> keys = [];
                ArrayEnd names = r.ReadArrayStart(DBusType.Array);
                while (r.HasNext(names))
                {
                    keys.Add(Encoding.UTF8.GetString(r.ReadArrayOfByte()));
                }

                touched |= PlasmaAirplanePolicy.Touches(group, keys);
            }

            return touched;
        }

        private void OnConfigChanged(ListeningSession session)
        {
            bool now = ReadState();
            EventHandler? changed;
            lock (_gate)
            {
                if (_session != session || now == _enabled)
                {
                    return;
                }

                _enabled = now;
                changed = Changed;
            }

            try
            {
                changed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // A subscriber's failure must not take down the bus's dispatch thread.
                Debug.WriteLine($"[PlasmaAirplane] handler: {ex.Message}");
            }
        }
    }
}
