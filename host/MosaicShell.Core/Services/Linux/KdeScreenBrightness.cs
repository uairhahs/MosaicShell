using System.Diagnostics;
using MosaicShell.Core.Services.Kde;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// Screen brightness on KDE Plasma through PowerDevil's org.kde.ScreenBrightness, following the brightness keys:
    /// the value is the built-in panel's (else the first display's), and a change moves every display by the same step
    /// (AdjustBrightnessRatio), without Plasma's pop-up. Also the change source: Changed fires when the reference
    /// display's brightness moves, from the keys or anywhere else, while started.
    /// </summary>
    /// <remarks>
    /// Its own session-bus connection; the getters return the last state read and never block. Writes run off the
    /// caller's thread and accumulate, so a dragged slider sends the sum of its steps. Without PowerDevil (GNOME) it is
    /// unsupported and nothing throws.
    /// </remarks>
    public sealed class KdeScreenBrightness : IBrightnessService, IBrightnessChangeSource
    {
        private const string PropertiesInterface = "org.freedesktop.DBus.Properties";

        private readonly string? _address;
        private readonly Lock _gate = new();
        private readonly CancellationTokenSource _stop = new();
        private readonly SemaphoreSlim _writeSignal = new(0);
        private readonly SemaphoreSlim _loadGate = new(1, 1);
        private readonly List<IDisposable> _watches = [];
        private List<KdeDisplay> _displays = [];
        private DBusConnection? _connection;
        private double _pendingDelta;
        private bool _started;
        private bool _disposed;

        /// <param name="address">A bus address; null for the user's session bus, empty for none. Tests pass a private bus.</param>
        public KdeScreenBrightness(string? address = null)
        {
            _address = address ?? DBusAddress.Session;
            _ = Task.Run(() => RunAsync(_stop.Token));
        }

        public event EventHandler? Changed;

        public bool IsSupported
        {
            get
            {
                lock (_gate)
                {
                    return KdeScreenBrightnessPolicy.Reference(_displays) is not null;
                }
            }
        }

        public double Brightness
        {
            get
            {
                lock (_gate)
                {
                    return KdeScreenBrightnessPolicy.Reference(_displays) is { } reference
                        ? KdeScreenBrightnessPolicy.Ratio(reference)
                        : 0;
                }
            }

            set
            {
                lock (_gate)
                {
                    if (KdeScreenBrightnessPolicy.Reference(_displays) is not { } reference)
                    {
                        return;
                    }

                    double delta = KdeScreenBrightnessPolicy.DeltaFor(KdeScreenBrightnessPolicy.Ratio(reference), value);
                    if (Math.Abs(delta) < KdeScreenBrightnessPolicy.ChangeThreshold)
                    {
                        return;
                    }

                    // Every display moves by the same step, as PowerDevil will move them; the reference lands on the target.
                    _displays = [.. _displays.Select(d => d.DbusName == reference.DbusName
                        ? d with { Brightness = (int)Math.Round(Math.Clamp(value, 0, 1) * d.MaxBrightness) }
                        : d with { Brightness = (int)Math.Clamp(Math.Round(d.Brightness + (delta * d.MaxBrightness)), 0, d.MaxBrightness) })];
                    _pendingDelta += delta;
                }

                _ = _writeSignal.Release();
            }
        }

        public void Start()
        {
            lock (_gate)
            {
                _started = true;
            }
        }

        public void Stop()
        {
            lock (_gate)
            {
                _started = false;
            }
        }

        public void Dispose()
        {
            IDisposable[] watches;
            DBusConnection? connection;
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _started = false;
                watches = [.. _watches];
                _watches.Clear();
                connection = _connection;
                _connection = null;
            }

            _stop.Cancel();
            _ = _writeSignal.Release();
            foreach (IDisposable watch in watches)
            {
                watch.Dispose();
            }

            connection?.Dispose();
        }

        private async Task RunAsync(CancellationToken ct)
        {
            try
            {
                if (string.IsNullOrEmpty(_address))
                {
                    return;
                }

                DBusConnection connection = new(_address);
                lock (_gate)
                {
                    if (_disposed)
                    {
                        connection.Dispose();
                        return;
                    }

                    _connection = connection;
                }

                await connection.ConnectAsync().ConfigureAwait(false);
                await WatchAsync(connection, "BrightnessChanged", static (m, _) =>
                {
                    Reader r = m.GetBodyReader();
                    return (Display: r.ReadString(), Value: r.ReadInt32());
                }, n => OnBrightnessChanged(n.Display, n.Value)).ConfigureAwait(false);
                foreach (string member in new[] { "DisplayAdded", "DisplayRemoved", "BrightnessRangeChanged" })
                {
                    await WatchAsync(connection, member, static (_, _) => true, signalled =>
                    {
                        if (signalled)
                        {
                            _ = LoadAsync(connection, ct);
                        }
                    }).ConfigureAwait(false);
                }

                await LoadAsync(connection, ct).ConfigureAwait(false);
                await WriteLoopAsync(connection, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                // No bus or no PowerDevil: brightness is simply unsupported.
                Debug.WriteLine($"[KdeScreenBrightness] unavailable: {ex.Message}");
            }
        }

        private async Task WatchAsync<T>(DBusConnection connection, string member, MessageValueReader<T> reader, Action<T> handler)
        {
            IDisposable watch = await connection.AddMatchAsync(
                new MatchRule
                {
                    Type = MessageType.Signal,
                    Sender = KdeScreenBrightnessPolicy.BusName,
                    Interface = KdeScreenBrightnessPolicy.Interface,
                    Member = member,
                },
                reader,
                n =>
                {
                    if (n.HasValue)
                    {
                        handler(n.Value);
                    }
                },
                emitOnCapturedContext: false,
                ObserverFlags.None).ConfigureAwait(false);
            lock (_gate)
            {
                if (!_disposed)
                {
                    _watches.Add(watch);
                    return;
                }
            }

            watch.Dispose();
        }

        /// <summary>Reads the display list and each display; raises Changed when the reference value moved.</summary>
        private async Task LoadAsync(DBusConnection connection, CancellationToken ct)
        {
            await _loadGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                string[] names = await CallAsync(connection, KdeScreenBrightnessPolicy.ObjectPath, "Get", "ss",
                    (ref w) =>
                    {
                        w.WriteString(KdeScreenBrightnessPolicy.Interface);
                        w.WriteString("DisplaysDBusNames");
                    },
                    static (m, _) => m.GetBodyReader().ReadVariantValue().GetArray<string>()).ConfigureAwait(false);

                List<KdeDisplay> displays = [];
                foreach (string name in names)
                {
                    try
                    {
                        Dictionary<string, VariantValue> props = await CallAsync(connection,
                            KdeScreenBrightnessPolicy.DisplayPath(name), "GetAll", "s",
                            (ref w) => w.WriteString(KdeScreenBrightnessPolicy.DisplayInterface),
                            static (m, _) => m.GetBodyReader().ReadDictionaryOfStringToVariantValue()).ConfigureAwait(false);
                        displays.Add(new KdeDisplay(
                            name,
                            props.TryGetValue("IsInternal", out VariantValue internalValue) && internalValue.GetBool(),
                            props.TryGetValue("Brightness", out VariantValue b) ? b.GetInt32() : 0,
                            props.TryGetValue("MaxBrightness", out VariantValue max) ? max.GetInt32() : 0));
                    }
                    catch (DBusErrorReplyException)
                    {
                        // Removed between the list and the read.
                    }
                }

                ReplaceDisplays(displays);
            }
            catch (DBusErrorReplyException ex)
            {
                Debug.WriteLine($"[KdeScreenBrightness] load: {ex.Message}");
            }
            finally
            {
                _ = _loadGate.Release();
            }
        }

        private void ReplaceDisplays(List<KdeDisplay> displays)
        {
            bool raise;
            lock (_gate)
            {
                double? before = KdeScreenBrightnessPolicy.Reference(_displays) is { } old ? KdeScreenBrightnessPolicy.Ratio(old) : null;
                _displays = displays;
                double? after = KdeScreenBrightnessPolicy.Reference(_displays) is { } now ? KdeScreenBrightnessPolicy.Ratio(now) : null;
                raise = _started && before is not null && after is not null
                    && Math.Abs(after.Value - before.Value) >= KdeScreenBrightnessPolicy.ChangeThreshold;
            }

            if (raise)
            {
                RaiseChanged();
            }
        }

        private void OnBrightnessChanged(string display, int value)
        {
            bool raise = false;
            bool unknown;
            lock (_gate)
            {
                int index = _displays.FindIndex(d => d.DbusName == display);
                unknown = index < 0;
                if (!unknown)
                {
                    KdeDisplay before = _displays[index];
                    KdeDisplay after = before with { Brightness = value };
                    _displays[index] = after;
                    raise = _started
                        && KdeScreenBrightnessPolicy.Reference(_displays)?.DbusName == display
                        && Math.Abs(KdeScreenBrightnessPolicy.Ratio(after) - KdeScreenBrightnessPolicy.Ratio(before))
                            >= KdeScreenBrightnessPolicy.ChangeThreshold;
                }
            }

            if (unknown && _connection is { } connection)
            {
                _ = LoadAsync(connection, _stop.Token);
            }

            if (raise)
            {
                RaiseChanged();
            }
        }

        private async Task WriteLoopAsync(DBusConnection connection, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await _writeSignal.WaitAsync(ct).ConfigureAwait(false);
                double delta;
                lock (_gate)
                {
                    delta = _pendingDelta;
                    _pendingDelta = 0;
                }

                if (Math.Abs(delta) < 1e-9)
                {
                    continue;
                }

                try
                {
                    _ = await CallAsync(connection, KdeScreenBrightnessPolicy.ObjectPath, "AdjustBrightnessRatio", "du",
                        (ref w) =>
                        {
                            w.WriteDouble(delta);
                            w.WriteUInt32(KdeScreenBrightnessPolicy.SuppressIndicatorFlag);
                        },
                        static (_, _) => true,
                        KdeScreenBrightnessPolicy.Interface).ConfigureAwait(false);
                }
                catch (DBusErrorReplyException ex)
                {
                    Debug.WriteLine($"[KdeScreenBrightness] set: {ex.Message}");
                }
            }
        }

        private delegate void BodyWriter(ref MessageWriter writer);

        private static Task<T> CallAsync<T>(
            DBusConnection connection,
            string path,
            string member,
            string signature,
            BodyWriter write,
            MessageValueReader<T> reader,
            string iface = PropertiesInterface)
        {
            MessageBuffer message;
            MessageWriter writer = connection.GetMessageWriter();
            try
            {
                writer.WriteMethodCallHeader(KdeScreenBrightnessPolicy.BusName, path, iface, member, signature);
                write(ref writer);
                message = writer.CreateMessage();
            }
            finally
            {
                writer.Dispose();
            }

            return connection.CallMethodAsync(message, reader);
        }

        private void RaiseChanged()
        {
            try
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[KdeScreenBrightness] handler: {ex.Message}");
            }
        }
    }
}
