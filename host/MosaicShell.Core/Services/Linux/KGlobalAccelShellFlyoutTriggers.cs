using MosaicShell.Core.Services.Kde;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// Shell flyout triggers on KDE Plasma: listens on the session bus for KGlobalAccel's
    /// <c>globalShortcutPressed</c> signal and raises the flyout kind <see cref="KGlobalAccelPolicy"/> maps it to. It only
    /// observes presses; KWin, the player and PowerDevil still act on the keys.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Start returns at once and connects in the background; <see cref="IsActive"/> turns true once the signal watch is
    /// in place. Without a session bus, or on a desktop with no KGlobalAccel (GNOME), nothing is ever raised, which is
    /// the same soft failure as the Windows hook. Only signals from the owner of <c>org.kde.kglobalaccel</c> count; the
    /// bus checks the sender, so another process cannot fake a key press.
    /// </para>
    /// <para>Triggered is raised on a thread-pool thread, as the Windows hook raises it on its own thread.</para>
    /// </remarks>
    /// <param name="address">A bus address; null for the user's session bus, empty for none. Tests pass a private bus.</param>
    public sealed class KGlobalAccelShellFlyoutTriggers(string? address = null) : IShellFlyoutTriggerSource
    {
        private readonly string? _address = address ?? DBusAddress.Session;
        private readonly Lock _gate = new();
        private ListeningSession? _session;
        private bool _disposed;

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

        public event EventHandler<ShellFlyoutKind>? Triggered;

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
            }

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
        }

        public void Dispose()
        {
            lock (_gate)
            {
                _disposed = true;
            }

            Stop();
        }

        private async Task ListenAsync(ListeningSession session)
        {
            try
            {
                if (string.IsNullOrEmpty(_address))
                {
                    System.Diagnostics.Debug.WriteLine("[KGlobalAccel] no session bus address");
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
                        Sender = KGlobalAccelPolicy.BusName,
                        Interface = KGlobalAccelPolicy.ComponentInterface,
                        Member = KGlobalAccelPolicy.PressedMember,
                    },
                    static (m, _) =>
                    {
                        Reader r = m.GetBodyReader();
                        return (Component: r.ReadString(), Shortcut: r.ReadString());
                    },
                    n =>
                    {
                        if (n.HasValue && session.IsListening)
                        {
                            OnPressed(n.Value.Component, n.Value.Shortcut);
                        }
                    },
                    emitOnCapturedContext: false,
                    ObserverFlags.None).ConfigureAwait(false);
                if (session.Adopt(watch))
                {
                    session.MarkListening();
                }
            }
            catch (Exception ex)
            {
                // No bus, a refused connection or a closed session: there are simply no shell triggers.
                System.Diagnostics.Debug.WriteLine($"[KGlobalAccel] unavailable: {ex.Message}");
                session.Close();
            }
        }

        private void OnPressed(string component, string shortcut)
        {
            if (!KGlobalAccelPolicy.TryDecode(component, shortcut, out ShellFlyoutKind kind))
            {
                return;
            }

            try
            {
                Triggered?.Invoke(this, kind);
            }
            catch (Exception ex)
            {
                // A subscriber's failure must not take down the bus's dispatch thread.
                System.Diagnostics.Debug.WriteLine($"[KGlobalAccel] handler: {ex.Message}");
            }
        }
    }
}
