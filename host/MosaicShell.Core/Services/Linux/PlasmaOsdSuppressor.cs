using MosaicShell.Core.Services.Kde;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// Hides Plasma's volume and brightness pop-ups while Tessera is armed. A monitor connection sees each call that
    /// plasma-pa or PowerDevil makes to plasmashell's <c>org.kde.osdService</c>; for the ones
    /// <see cref="PlasmaOsdPolicy"/> names, a second connection calls the service's <c>hide</c> at once.
    /// </summary>
    /// <remarks>
    /// Nothing is written to the user's settings, so a crash leaves Plasma exactly as it was. Start returns at once and
    /// connects in the background; IsActive turns true once the watch is in place. Without a bus or plasmashell
    /// (GNOME), nothing happens and nothing throws.
    /// </remarks>
    /// <param name="address">A bus address; null for the user's session bus, empty for none. Tests pass a private bus.</param>
    public sealed class PlasmaOsdSuppressor(string? address = null) : INativeOsdSuppressor
    {
        private readonly string? _address = address ?? DBusAddress.Session;
        private readonly Lock _gate = new();
        private Session? _session;
        private bool _disposed;

        public bool IsActive
        {
            get
            {
                lock (_gate)
                {
                    return _session is { IsWatching: true };
                }
            }
        }

        public void Start()
        {
            Session session;
            lock (_gate)
            {
                if (_disposed || _session is not null)
                {
                    return;
                }

                session = new Session();
                _session = session;
            }

            _ = WatchAsync(session);
        }

        public void Stop()
        {
            Session? session;
            lock (_gate)
            {
                session = _session;
                _session = null;
            }

            session?.Close();
        }

        public void SuppressOnce()
        {
            Session? session;
            lock (_gate)
            {
                session = _session;
            }

            session?.Hide();
        }

        /// <summary>
        /// Each pop-up is hidden as it is shown, so a burst needs only one hide now for anything already on screen.
        /// </summary>
        public void SuppressBurst(int durationMs = 1500)
        {
            SuppressOnce();
        }

        public void Dispose()
        {
            lock (_gate)
            {
                _disposed = true;
            }

            Stop();
        }

        private async Task WatchAsync(Session session)
        {
            try
            {
                if (string.IsNullOrEmpty(_address))
                {
                    return;
                }

                DBusConnection hider = new(_address);
                if (!session.Adopt(hider))
                {
                    return;
                }

                await hider.ConnectAsync().ConfigureAwait(false);
                session.SetHider(hider);

                DBusConnection monitor = new(_address);
                if (!session.Adopt(monitor))
                {
                    return;
                }

                await monitor.ConnectAsync().ConfigureAwait(false);
                await monitor.BecomeMonitorAsync(
                    (exception, message) =>
                    {
                        using (message)
                        {
                            if (exception is null
                                && message.Message.MemberAsString is { } member
                                && PlasmaOsdPolicy.ShouldHide(member))
                            {
                                session.Hide();
                            }
                        }
                    },
                    PlasmaOsdPolicy.HiddenMembers.Select(static member => new MatchRule
                    {
                        Type = MessageType.MethodCall,
                        Interface = PlasmaOsdPolicy.Interface,
                        Member = member,
                    })).ConfigureAwait(false);
                session.MarkWatching();
            }
            catch (Exception ex)
            {
                // No bus, no plasmashell, or a closed session: Plasma's pop-ups simply stay.
                System.Diagnostics.Debug.WriteLine($"[PlasmaOsd] unavailable: {ex.Message}");
                session.Close();
            }
        }

        /// <summary>One Start-to-Stop run: owns both connections, so Stop or Dispose mid-connect closes them.</summary>
        private sealed class Session
        {
            private readonly Lock _gate = new();
            private readonly List<IDisposable> _owned = [];
            private DBusConnection? _hider;
            private bool _closed;
            private bool _watching;

            public bool IsWatching
            {
                get
                {
                    lock (_gate)
                    {
                        return _watching && !_closed;
                    }
                }
            }

            public bool Adopt(IDisposable resource)
            {
                lock (_gate)
                {
                    if (!_closed)
                    {
                        _owned.Add(resource);
                        return true;
                    }
                }

                resource.Dispose();
                return false;
            }

            public void SetHider(DBusConnection hider)
            {
                lock (_gate)
                {
                    _hider = _closed ? null : hider;
                }
            }

            public void MarkWatching()
            {
                lock (_gate)
                {
                    _watching = !_closed;
                }
            }

            /// <summary>Asks plasmashell to hide its pop-up; no reply is awaited.</summary>
            public void Hide()
            {
                DBusConnection? hider;
                lock (_gate)
                {
                    hider = _closed ? null : _hider;
                }

                if (hider is null)
                {
                    return;
                }

                try
                {
                    using MessageWriter w = hider.GetMessageWriter();
                    w.WriteMethodCallHeader(
                        PlasmaOsdPolicy.BusName,
                        PlasmaOsdPolicy.ObjectPath,
                        PlasmaOsdPolicy.Interface,
                        PlasmaOsdPolicy.HideMember,
                        signature: null,
                        flags: MessageFlags.NoReplyExpected);
                    _ = hider.TrySendMessage(w.CreateMessage());
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PlasmaOsd] hide: {ex.Message}");
                }
            }

            public void Close()
            {
                IDisposable[] owned;
                lock (_gate)
                {
                    if (_closed)
                    {
                        return;
                    }

                    _closed = true;
                    _hider = null;
                    owned = [.. _owned];
                    _owned.Clear();
                }

                foreach (IDisposable resource in owned)
                {
                    resource.Dispose();
                }
            }
        }
    }
}
