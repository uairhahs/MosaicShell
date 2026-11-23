using MosaicShell.Core.Services.Kde;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// Hides Plasma's volume and brightness pop-ups while Tessera is armed. A monitor connection sees each call that
    /// plasma-pa or PowerDevil makes to plasmashell's <c>org.kde.osdService</c>; for the ones
    /// <see cref="PlasmaOsdPolicy"/> names, a second connection calls the service's <c>hide</c> at once. Plasma 6.6 has
    /// no <c>hide</c>, so the same connection also loads a KWin script that hides each OSD window and asks this
    /// suppressor whether to keep it hidden (<see cref="KWinOsdScriptPolicy"/>).
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
                await LoadKWinGuardAsync(session, hider).ConfigureAwait(false);

                DBusConnection monitor = new(_address);
                if (!session.Adopt(monitor))
                {
                    return;
                }

                await monitor.ConnectAsync().ConfigureAwait(false);
                // Every osdService call, so the KWin guard knows which pop-up came last; our own hide is skipped, or the
                // watch would see it and loop.
                await monitor.BecomeMonitorAsync(
                    (exception, message) =>
                    {
                        using (message)
                        {
                            if (exception is null
                                && message.Message.MemberAsString is { } member
                                && member != PlasmaOsdPolicy.HideMember)
                            {
                                session.Record(member);
                                if (PlasmaOsdPolicy.ShouldHide(member))
                                {
                                    session.Hide();
                                }
                            }
                        }
                    },
                    [new MatchRule { Type = MessageType.MethodCall, Interface = PlasmaOsdPolicy.Interface }]).ConfigureAwait(false);
                session.MarkWatching();
            }
            catch (Exception ex)
            {
                // No bus, no plasmashell, or a closed session: Plasma's pop-ups simply stay.
                System.Diagnostics.Debug.WriteLine($"[PlasmaOsd] unavailable: {ex.Message}");
                session.Close();
            }
        }

        /// <summary>
        /// Plasma 6.6 has no osdService hide, so the pop-ups are hidden through KWin (<see cref="KWinOsdScriptPolicy"/>):
        /// own the guard name, answer the script's question, replace any copy a crashed run left loaded, then load and
        /// start the script. Without KWin scripting, the hide calls above are all there is.
        /// </summary>
        private static async Task LoadKWinGuardAsync(Session session, DBusConnection hider)
        {
            try
            {
                hider.AddMethodHandler(new GuardHandler(session));
                await hider.RequestNameAsync(KWinOsdScriptPolicy.GuardBusName).ConfigureAwait(false);

                string directory = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR") is { Length: > 0 } runtime
                    ? runtime
                    : Path.GetTempPath();
                string file = Path.Combine(directory, $"{KWinOsdScriptPolicy.ScriptName}-{Environment.ProcessId}.js");
                await File.WriteAllTextAsync(file, KWinOsdScriptPolicy.Script).ConfigureAwait(false);
                session.SetScriptFile(file);

                _ = await UnloadScriptAsync(hider).ConfigureAwait(false);
                if (await LoadScriptAsync(hider, file).ConfigureAwait(false) < 0)
                {
                    System.Diagnostics.Debug.WriteLine("[PlasmaOsd] KWin refused the guard script");
                    return;
                }

                await StartScriptsAsync(hider).ConfigureAwait(false);
                session.MarkScriptLoaded();
            }
            catch (Exception ex)
            {
                // No KWin scripting (or the guard name is taken by another Host): the hide calls still run.
                System.Diagnostics.Debug.WriteLine($"[PlasmaOsd] KWin guard unavailable: {ex.Message}");
            }
        }

        private static Task<bool> UnloadScriptAsync(DBusConnection connection)
        {
            MessageBuffer message;
            using (MessageWriter w = connection.GetMessageWriter())
            {
                w.WriteMethodCallHeader(
                    KWinOsdScriptPolicy.KWinBusName, KWinOsdScriptPolicy.ScriptingPath, KWinOsdScriptPolicy.ScriptingInterface, "unloadScript", "s");
                w.WriteString(KWinOsdScriptPolicy.ScriptName);
                message = w.CreateMessage();
            }

            return connection.CallMethodAsync(message, static (m, _) => m.GetBodyReader().ReadBool());
        }

        private static Task<int> LoadScriptAsync(DBusConnection connection, string file)
        {
            MessageBuffer message;
            using (MessageWriter w = connection.GetMessageWriter())
            {
                w.WriteMethodCallHeader(
                    KWinOsdScriptPolicy.KWinBusName, KWinOsdScriptPolicy.ScriptingPath, KWinOsdScriptPolicy.ScriptingInterface, "loadScript", "ss");
                w.WriteString(file);
                w.WriteString(KWinOsdScriptPolicy.ScriptName);
                message = w.CreateMessage();
            }

            return connection.CallMethodAsync(message, static (m, _) => m.GetBodyReader().ReadInt32());
        }

        private static Task StartScriptsAsync(DBusConnection connection)
        {
            MessageBuffer message;
            using (MessageWriter w = connection.GetMessageWriter())
            {
                w.WriteMethodCallHeader(
                    KWinOsdScriptPolicy.KWinBusName, KWinOsdScriptPolicy.ScriptingPath, KWinOsdScriptPolicy.ScriptingInterface, "start");
                message = w.CreateMessage();
            }

            return connection.CallMethodAsync(message);
        }

        /// <summary>The guard object the KWin script asks about each OSD window it hid.</summary>
        private sealed class GuardHandler(Session session) : IPathMethodHandler
        {
            public string Path => KWinOsdScriptPolicy.GuardPath;
            public bool HandlesChildPaths => false;

            public ValueTask HandleMethodAsync(MethodContext context)
            {
                if (context.Request.MemberAsString != KWinOsdScriptPolicy.GuardMethod)
                {
                    context.ReplyUnknownMethodError();
                    return ValueTask.CompletedTask;
                }

                using MessageWriter w = context.CreateReplyWriter("b");
                w.WriteBool(session.ShouldKeepHidden());
                context.Reply(w.CreateMessage());
                return ValueTask.CompletedTask;
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
            private string? _lastMember;
            private long _lastCallTicks;
            private string? _scriptFile;
            private bool _scriptLoaded;

            /// <summary>An osdService call the monitor saw (our own hide excluded).</summary>
            public void Record(string member)
            {
                lock (_gate)
                {
                    _lastMember = member;
                    _lastCallTicks = Environment.TickCount64;
                }
            }

            /// <summary>The KWin guard's answer for the OSD window it just hid.</summary>
            public bool ShouldKeepHidden()
            {
                lock (_gate)
                {
                    return !_closed
                        && KWinOsdScriptPolicy.ShouldKeepHidden(
                            _lastMember, TimeSpan.FromMilliseconds(Environment.TickCount64 - _lastCallTicks));
                }
            }

            public void SetScriptFile(string file)
            {
                lock (_gate)
                {
                    _scriptFile = file;
                }
            }

            public void MarkScriptLoaded()
            {
                lock (_gate)
                {
                    _scriptLoaded = !_closed;
                }
            }

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
                DBusConnection? hider;
                bool unload;
                string? scriptFile;
                lock (_gate)
                {
                    if (_closed)
                    {
                        return;
                    }

                    _closed = true;
                    hider = _hider;
                    unload = _scriptLoaded;
                    scriptFile = _scriptFile;
                    _hider = null;
                    owned = [.. _owned];
                    _owned.Clear();
                }

                if (unload && hider is not null)
                {
                    UnloadNoReply(hider);
                }

                foreach (IDisposable resource in owned)
                {
                    resource.Dispose();
                }

                if (scriptFile is not null)
                {
                    try { File.Delete(scriptFile); }
                    catch (IOException) { /* best effort */ }
                    catch (UnauthorizedAccessException) { /* best effort */ }
                }
            }

            /// <summary>
            /// Sent before the connection closes; no reply is awaited. If it never arrives (a crash), the script finds the
            /// guard name unowned on the next pop-up and goes inert.
            /// </summary>
            private static void UnloadNoReply(DBusConnection hider)
            {
                try
                {
                    using MessageWriter w = hider.GetMessageWriter();
                    w.WriteMethodCallHeader(
                        KWinOsdScriptPolicy.KWinBusName,
                        KWinOsdScriptPolicy.ScriptingPath,
                        KWinOsdScriptPolicy.ScriptingInterface,
                        "unloadScript",
                        "s",
                        MessageFlags.NoReplyExpected);
                    w.WriteString(KWinOsdScriptPolicy.ScriptName);
                    _ = hider.TrySendMessage(w.CreateMessage());
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PlasmaOsd] unload: {ex.Message}");
                }
            }
        }
    }
}
