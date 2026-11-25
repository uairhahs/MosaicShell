using System.Diagnostics;
using MosaicShell.Core.Services.Kde;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// Whether the active window is fullscreen, on KDE Plasma: each query loads a one-shot KWin script that reports the
    /// active window to a name this probe owns, then unloads it (<see cref="KWinFullscreenPolicy"/>). Asked only when
    /// Slate's idle fires, so nothing runs in between. The session bus connection is opened on the first query and kept.
    /// </summary>
    /// <remarks>
    /// Without a bus or KWin scripting (GNOME), or when KWin does not answer within a second, it reads not fullscreen,
    /// as <see cref="NullFullscreenProbe"/> does, and nothing throws.
    /// </remarks>
    /// <param name="address">A bus address; null for the user's session bus, empty for none. Tests pass a private bus.</param>
    public sealed class KWinFullscreenProbe(string? address = null) : IFullscreenProbe, IDisposable
    {
        private static readonly TimeSpan ReportWait = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan QueryWait = TimeSpan.FromSeconds(2);

        private readonly string? _address = address ?? DBusAddress.Session;
        private readonly Lock _queryGate = new();
        private readonly Lock _gate = new();
        private DBusConnection? _connection;
        private TaskCompletionSource<string>? _pending;
        private bool _disposed;

        public bool IsForegroundFullscreen
        {
            get
            {
                if (string.IsNullOrEmpty(_address))
                {
                    return false;
                }

                lock (_queryGate)
                {
                    try
                    {
                        // Off the caller's thread, so a caller with a synchronization context cannot deadlock the reply.
                        Task<string?> query = Task.Run(QueryAsync);
                        return query.Wait(QueryWait) && KWinFullscreenPolicy.IsFullscreen(query.Result);
                    }
                    catch (AggregateException ex)
                    {
                        // No bus, no KWin scripting, or the probe name is held elsewhere.
                        Debug.WriteLine($"[Fullscreen] unavailable: {ex.InnerException?.Message}");
                        return false;
                    }
                }
            }
        }

        public void Dispose()
        {
            DBusConnection? connection;
            lock (_gate)
            {
                _disposed = true;
                connection = _connection;
                _connection = null;
            }

            connection?.Dispose();
        }

        /// <summary>The active window's facts as the script reported them, or null if it did not within the wait.</summary>
        private async Task<string?> QueryAsync()
        {
            DBusConnection connection = await ConnectAsync().ConfigureAwait(false);
            TaskCompletionSource<string> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate)
            {
                _pending = pending;
            }

            string directory = Environment.GetEnvironmentVariable(Xdg.XdgBaseDirectories.RuntimeDirVariable) is { Length: > 0 } runtime
                ? runtime
                : Path.GetTempPath();
            string file = Path.Combine(directory, $"{KWinFullscreenPolicy.ScriptName}-{Environment.ProcessId}.js");
            await File.WriteAllTextAsync(file, KWinFullscreenPolicy.Script).ConfigureAwait(false);
            try
            {
                _ = await KWinScripting.UnloadAsync(connection, KWinFullscreenPolicy.ScriptName).ConfigureAwait(false);
                if (await KWinScripting.LoadAsync(connection, file, KWinFullscreenPolicy.ScriptName).ConfigureAwait(false) < 0)
                {
                    Debug.WriteLine("[Fullscreen] KWin refused the probe script");
                    return null;
                }

                await KWinScripting.StartAsync(connection).ConfigureAwait(false);
                Task finished = await Task.WhenAny(pending.Task, Task.Delay(ReportWait)).ConfigureAwait(false);
                return finished == pending.Task ? await pending.Task.ConfigureAwait(false) : null;
            }
            finally
            {
                lock (_gate)
                {
                    _pending = null;
                }

                try
                {
                    _ = await KWinScripting.UnloadAsync(connection, KWinFullscreenPolicy.ScriptName).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is DBusErrorReplyException or DBusConnectionException or ObjectDisposedException)
                {
                    Debug.WriteLine($"[Fullscreen] unload: {ex.Message}");
                }

                try { File.Delete(file); }
                catch (IOException) { /* best effort */ }
                catch (UnauthorizedAccessException) { /* best effort */ }
            }
        }

        /// <summary>The kept connection, owning the probe name the script reports to.</summary>
        private async Task<DBusConnection> ConnectAsync()
        {
            lock (_gate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (_connection is not null)
                {
                    return _connection;
                }
            }

            DBusConnection connection = new(_address!);
            try
            {
                await connection.ConnectAsync().ConfigureAwait(false);
                connection.AddMethodHandler(new ReportHandler(this));
                await connection.RequestNameAsync(KWinFullscreenPolicy.ProbeBusName).ConfigureAwait(false);
            }
            catch
            {
                connection.Dispose();
                throw;
            }

            lock (_gate)
            {
                if (!_disposed && _connection is null)
                {
                    _connection = connection;
                    return connection;
                }
            }

            connection.Dispose();
            throw new ObjectDisposedException(nameof(KWinFullscreenProbe));
        }

        private void OnReport(string report)
        {
            lock (_gate)
            {
                _ = _pending?.TrySetResult(report);
            }
        }

        /// <summary>The object the probe script reports to.</summary>
        private sealed class ReportHandler(KWinFullscreenProbe probe) : IPathMethodHandler
        {
            public string Path => KWinFullscreenPolicy.ProbePath;
            public bool HandlesChildPaths => false;

            public ValueTask HandleMethodAsync(MethodContext context)
            {
                if (context.Request.MemberAsString != KWinFullscreenPolicy.ReportMethod)
                {
                    context.ReplyUnknownMethodError();
                    return ValueTask.CompletedTask;
                }

                probe.OnReport(context.Request.GetBodyReader().ReadString());
                using MessageWriter w = context.CreateReplyWriter(null);
                context.Reply(w.CreateMessage());
                return ValueTask.CompletedTask;
            }
        }
    }
}
