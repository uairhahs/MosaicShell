using System.Diagnostics;
using MosaicShell.Core.Services.Gnome;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// The Host's presence on GNOME: while it runs it owns <see cref="GnomeHubPresencePolicy.BusName"/>, so MosaicShell's
    /// Shell extension shows its icon in the top bar, and answers the icon's menu (Open, Quit). GNOME has no tray of its
    /// own; this is how a closed Hub comes back. Disposing (or a crash) gives the name up and the icon goes.
    /// </summary>
    /// <param name="showHub">Brings the Hub forward. Called on a bus thread; the Host marshals it.</param>
    /// <param name="quit">Quits MosaicShell. Called on a bus thread; the Host marshals it.</param>
    /// <param name="address">A bus address; null for the user's session bus, empty for none. Tests pass a private bus.</param>
    /// <param name="dataHome">XDG_DATA_HOME; null finds it. Tests pass a throwaway folder.</param>
    /// <param name="settings">GNOME settings; null uses gsettings. Tests pass a stand-in.</param>
    public sealed class GnomeHubPresence(
        Action showHub,
        Action quit,
        string? address = null,
        string? dataHome = null,
        IGnomeSettings? settings = null) : IDisposable
    {
        private readonly string? _address = address ?? DBusAddress.Session;
        private readonly string _dataHome = dataHome ?? Xdg.XdgBaseDirectories.CurrentDataHome();
        private readonly IGnomeSettings _settings = settings ?? new GsettingsCommand();
        private readonly Lock _gate = new();
        private DBusConnection? _connection;

        public void Start()
        {
            DBusConnection connection;
            lock (_gate)
            {
                if (_connection is not null || string.IsNullOrEmpty(_address))
                {
                    return;
                }

                connection = _connection = new DBusConnection(_address);
            }

            _ = Task.Run(() => RunAsync(connection));
        }

        public void Dispose()
        {
            DBusConnection? closing;
            lock (_gate)
            {
                closing = _connection;
                _connection = null;
            }

            // Closing the connection gives the name up, and the extension takes the icon away.
            closing?.Dispose();
        }

        private async Task RunAsync(DBusConnection connection)
        {
            GnomeShellExtensionInstall.Ensure(_dataHome, _settings);
            try
            {
                await connection.ConnectAsync().ConfigureAwait(false);
                connection.AddMethodHandler(new Handler(showHub, quit));
                await connection.RequestNameAsync(GnomeHubPresencePolicy.BusName).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is DBusConnectionException or DBusErrorReplyException or ObjectDisposedException)
            {
                // No bus, or disposed mid-connect: no icon, and the Hub still opens from the app grid.
                Debug.WriteLine($"[GnomeHub] presence: {ex.Message}");
            }
        }

        private sealed class Handler(Action showHub, Action quit) : IPathMethodHandler
        {
            public string Path => GnomeHubPresencePolicy.ObjectPath;

            public bool HandlesChildPaths => false;

            public ValueTask HandleMethodAsync(MethodContext context)
            {
                if (context.Request.InterfaceAsString != GnomeHubPresencePolicy.Interface)
                {
                    context.ReplyUnknownMethodError();
                    return ValueTask.CompletedTask;
                }

                switch (context.Request.MemberAsString)
                {
                    case GnomeHubPresencePolicy.ShowHubMethod:
                        showHub();
                        break;
                    case GnomeHubPresencePolicy.QuitMethod:
                        quit();
                        break;
                    default:
                        context.ReplyUnknownMethodError();
                        return ValueTask.CompletedTask;
                }

                using MessageWriter writer = context.CreateReplyWriter(null);
                context.Reply(writer.CreateMessage());
                return ValueTask.CompletedTask;
            }
        }
    }
}
