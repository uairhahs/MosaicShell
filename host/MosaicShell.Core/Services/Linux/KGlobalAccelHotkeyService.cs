using MosaicShell.Core.Services.Kde;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// Global hotkeys on KDE Plasma through KGlobalAccel (<see cref="KGlobalAccelHotkeyPolicy"/>): Register binds the key
    /// to an action of the <c>mosaicshell</c> component and fails, as RegisterHotKey does, when another component holds
    /// it. Presses arrive as that component's <c>globalShortcutPressed</c> signal; only the real KGlobalAccel's count.
    /// </summary>
    /// <remarks>
    /// Register blocks for the two bus calls (at most <see cref="CallTimeout"/>), since callers read the result at once.
    /// Unregister and Dispose remove the action, so the key is free again after the Host closes. Callbacks run on a
    /// thread-pool thread, as Windows runs them on its hotkey thread. Without a bus or KGlobalAccel nothing binds.
    /// </remarks>
    /// <param name="address">A bus address; null for the user's session bus, empty for none. Tests pass a private bus.</param>
    public sealed class KGlobalAccelHotkeyService(string? address = null) : IHotkeyService
    {
        /// <summary>By ref: MessageWriter is a ref struct, and a copy's writes never reach the message.</summary>
        private delegate void Body(ref MessageWriter writer);

        private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(3);

        private readonly string? _address = address ?? DBusAddress.Session;
        private readonly Lock _gate = new();
        private readonly Dictionary<string, Action> _callbacks = new(StringComparer.OrdinalIgnoreCase);
        private Task<DBusConnection?>? _connecting;
        private bool _disposed;

        public bool Register(string id, ModifierKeys modifiers, int virtualKey, Action callback)
        {
            Unregister(id);
            if (!KGlobalAccelHotkeyPolicy.TryQtKey(modifiers, virtualKey, out int key))
            {
                return false;
            }

            string[] action = KGlobalAccelHotkeyPolicy.ActionId(id);
            int[] keys = [key];
            bool took = Run(async connection =>
            {
                await CallAsync(connection, "doRegister", "as", (ref w) => w.WriteArray(action)).ConfigureAwait(false);
                int[] assigned = await connection.CallMethodAsync(
                    Message(connection, "setShortcut", "asaiu", (ref w) =>
                    {
                        w.WriteArray(action);
                        w.WriteArray(keys);
                        w.WriteUInt32(KGlobalAccelHotkeyPolicy.SetFlags);
                    }),
                    static (m, _) => m.GetBodyReader().ReadArrayOfInt32()).ConfigureAwait(false);
                if (KGlobalAccelHotkeyPolicy.Took(key, assigned))
                {
                    return true;
                }

                // Taken by another component: leave no empty action behind in System Settings.
                await UnregisterAsync(connection, id).ConfigureAwait(false);
                return false;
            });
            if (took)
            {
                lock (_gate)
                {
                    _callbacks[id] = callback;
                }
            }

            return took;
        }

        public void Unregister(string id)
        {
            lock (_gate)
            {
                if (!_callbacks.Remove(id))
                {
                    return;
                }
            }

            _ = Run(async connection =>
            {
                await UnregisterAsync(connection, id).ConfigureAwait(false);
                return true;
            });
        }

        public void Dispose()
        {
            string[] ids;
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                ids = [.. _callbacks.Keys];
            }

            foreach (string id in ids)
            {
                Unregister(id);
            }

            Task<DBusConnection?>? connecting;
            lock (_gate)
            {
                _disposed = true;
                connecting = _connecting;
                _connecting = null;
            }

            if (connecting is { IsCompletedSuccessfully: true, Result: { } connection })
            {
                connection.Dispose();
            }
        }

        private bool Run(Func<DBusConnection, Task<bool>> work)
        {
            try
            {
                Task<bool> task = Task.Run(async () =>
                    await ConnectionAsync().ConfigureAwait(false) is { } connection && await work(connection).ConfigureAwait(false));
                return task.Wait(CallTimeout) && task.Result;
            }
            catch (Exception ex)
            {
                // No KGlobalAccel (GNOME), a refused call or a closed bus: the hotkey simply does not bind.
                System.Diagnostics.Debug.WriteLine($"[KGlobalAccel] hotkey call failed: {ex.Message}");
                return false;
            }
        }

        private Task<DBusConnection?> ConnectionAsync()
        {
            lock (_gate)
            {
                return _disposed || string.IsNullOrEmpty(_address)
                    ? Task.FromResult<DBusConnection?>(null)
                    : _connecting ??= ConnectAsync(_address);
            }
        }

        private async Task<DBusConnection?> ConnectAsync(string address)
        {
            DBusConnection connection = new(address);
            try
            {
                await connection.ConnectAsync().ConfigureAwait(false);
                _ = await connection.AddMatchAsync(
                    new MatchRule
                    {
                        Type = MessageType.Signal,
                        Sender = KGlobalAccelPolicy.BusName,
                        Path = KGlobalAccelHotkeyPolicy.ComponentPath,
                        Interface = KGlobalAccelPolicy.ComponentInterface,
                        Member = KGlobalAccelPolicy.PressedMember,
                    },
                    static (m, _) =>
                    {
                        Reader r = m.GetBodyReader();
                        _ = r.ReadString();
                        return r.ReadString();
                    },
                    n =>
                    {
                        if (n.HasValue)
                        {
                            OnPressed(n.Value);
                        }
                    },
                    emitOnCapturedContext: false,
                    ObserverFlags.None).ConfigureAwait(false);
                return connection;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[KGlobalAccel] hotkeys unavailable: {ex.Message}");
                connection.Dispose();
                lock (_gate)
                {
                    // Let a later Register try again.
                    _connecting = null;
                }

                return null;
            }
        }

        private void OnPressed(string id)
        {
            Action? callback;
            lock (_gate)
            {
                _ = _callbacks.TryGetValue(id, out callback);
            }

            try
            {
                callback?.Invoke();
            }
            catch (Exception ex)
            {
                // A callback's failure must not take down the bus's dispatch thread.
                System.Diagnostics.Debug.WriteLine($"[KGlobalAccel] hotkey handler: {ex.Message}");
            }
        }

        private static Task UnregisterAsync(DBusConnection connection, string id)
        {
            return connection.CallMethodAsync(
                Message(connection, "unregister", "ss", (ref w) =>
                {
                    w.WriteString(KGlobalAccelHotkeyPolicy.ComponentUnique);
                    w.WriteString(id);
                }),
                static (m, _) => m.GetBodyReader().ReadBool());
        }

        private static Task CallAsync(DBusConnection connection, string member, string signature, Body body)
        {
            return connection.CallMethodAsync(Message(connection, member, signature, body));
        }

        private static MessageBuffer Message(DBusConnection connection, string member, string signature, Body body)
        {
            MessageWriter w = connection.GetMessageWriter();
            try
            {
                w.WriteMethodCallHeader(KGlobalAccelPolicy.BusName, KGlobalAccelHotkeyPolicy.Path, KGlobalAccelHotkeyPolicy.Interface, member, signature);
                body(ref w);
                return w.CreateMessage();
            }
            finally
            {
                w.Dispose();
            }
        }
    }
}
