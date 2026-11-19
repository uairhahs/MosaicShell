using System.Diagnostics;
using MosaicShell.Core.Services.Mpris;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary>A fact that needs a real dbus-daemon. Reported as skipped (not dropped) where there is none.</summary>
    public sealed class LinuxBusFactAttribute : FactAttribute
    {
        public LinuxBusFactAttribute()
        {
            if (!OperatingSystem.IsLinux())
            {
                Skip = "Linux only.";
            }
            else if (PrivateDBus.DaemonPath() is null)
            {
                Skip = "dbus-daemon is not installed.";
            }
        }
    }

    /// <summary>
    /// A throwaway session bus for one test, so tests never see (or disturb) the user's real players.
    /// </summary>
    internal sealed class PrivateDBus : IDisposable
    {
        private readonly Process _daemon;

        private PrivateDBus(Process daemon, string address)
        {
            _daemon = daemon;
            Address = address;
        }

        public string Address { get; }

        public static string? DaemonPath()
        {
            return new[] { "/usr/bin/dbus-daemon", "/bin/dbus-daemon", "/run/current-system/sw/bin/dbus-daemon" }
                .FirstOrDefault(File.Exists);
        }

        public static PrivateDBus Start()
        {
            ProcessStartInfo info = new(DaemonPath()!, "--session --nofork --nopidfile --print-address=1")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            Process daemon = Process.Start(info) ?? throw new InvalidOperationException("dbus-daemon did not start.");
            string? address = daemon.StandardOutput.ReadLine();
            if (string.IsNullOrWhiteSpace(address))
            {
                daemon.Kill();
                throw new InvalidOperationException("dbus-daemon printed no address: " + daemon.StandardError.ReadToEnd());
            }

            return new PrivateDBus(daemon, address.Trim());
        }

        public void Dispose()
        {
            try
            {
                _daemon.Kill();
                _ = _daemon.WaitForExit(2000);
            }
            catch (InvalidOperationException)
            {
                // Already gone.
            }

            _daemon.Dispose();
        }
    }

    /// <summary>
    /// A minimal MPRIS player exported on a bus: answers Get, GetAll and Set, records Player method calls with their
    /// arguments, and emits PropertiesChanged and Seeked on demand, as a real player would.
    /// </summary>
    internal sealed class FakeMprisPlayer(string suffix) : IPathMethodHandler, IAsyncDisposable
    {
        private readonly Lock _gate = new();
        private DBusConnection? _connection;

        public string BusName { get; } = MprisPolicy.BusNamePrefix + suffix;
        public string Path => MprisPolicy.ObjectPath;
        public bool HandlesChildPaths => false;

        public Dictionary<string, VariantValue> Root { get; } = new()
        {
            ["Identity"] = VariantValue.String("Fake Player"),
            ["DesktopEntry"] = VariantValue.String("fake-player"),
        };

        public Dictionary<string, VariantValue> Player { get; } = new()
        {
            ["PlaybackStatus"] = VariantValue.String("Playing"),
            ["Position"] = VariantValue.Int64(10_000_000),
            ["CanSeek"] = VariantValue.Bool(true),
            ["Shuffle"] = VariantValue.Bool(false),
            ["LoopStatus"] = VariantValue.String("None"),
        };

        public List<string> Calls { get; } = [];

        public void SetMetadata(string title, string artist, long lengthMicros, string trackId = "/fake/track/1")
        {
            Player["Metadata"] = new Dict<string, VariantValue>(new Dictionary<string, VariantValue>
            {
                ["mpris:trackid"] = VariantValue.ObjectPath(new ObjectPath(trackId)),
                ["mpris:length"] = VariantValue.Int64(lengthMicros),
                ["xesam:title"] = VariantValue.String(title),
                ["xesam:artist"] = VariantValue.Array(new[] { artist }),
            }).AsVariantValue();
        }

        public async Task PublishAsync(string address)
        {
            DBusConnection connection = new(address);
            await connection.ConnectAsync();
            connection.AddMethodHandler(this);
            await connection.RequestNameAsync(BusName);
            _connection = connection;
        }

        /// <summary>Signals that the given Player properties changed, with their new values.</summary>
        public void EmitPropertiesChanged(params string[] properties)
        {
            DBusConnection connection = _connection!;
            using MessageWriter w = connection.GetMessageWriter();
            w.WriteSignalHeader(null, Path, "org.freedesktop.DBus.Properties", "PropertiesChanged", "sa{sv}as");
            w.WriteString(MprisPolicy.PlayerInterface);
            ArrayStart dict = w.WriteDictionaryStart();
            foreach (string name in properties)
            {
                w.WriteDictionaryEntryStart();
                w.WriteString(name);
                w.WriteVariant(Player[name]);
            }

            w.WriteDictionaryEnd(dict);
            w.WriteArray(Array.Empty<string>());
            _ = connection.TrySendMessage(w.CreateMessage());
        }

        public void EmitSeeked(long positionMicros)
        {
            Player["Position"] = VariantValue.Int64(positionMicros);
            DBusConnection connection = _connection!;
            using MessageWriter w = connection.GetMessageWriter();
            w.WriteSignalHeader(null, Path, MprisPolicy.PlayerInterface, "Seeked", "x");
            w.WriteInt64(positionMicros);
            _ = connection.TrySendMessage(w.CreateMessage());
        }

        public ValueTask HandleMethodAsync(MethodContext context)
        {
            Message request = context.Request;
            string iface = request.InterfaceAsString ?? "";
            string member = request.MemberAsString ?? "";
            Reader body = request.GetBodyReader();

            if (iface == "org.freedesktop.DBus.Properties")
            {
                string target = body.ReadString();
                Dictionary<string, VariantValue> map = target == MprisPolicy.RootInterface ? Root : Player;
                switch (member)
                {
                    case "GetAll":
                        {
                            using MessageWriter w = context.CreateReplyWriter("a{sv}");
                            ArrayStart dict = w.WriteDictionaryStart();
                            foreach (KeyValuePair<string, VariantValue> entry in map)
                            {
                                w.WriteDictionaryEntryStart();
                                w.WriteString(entry.Key);
                                w.WriteVariant(entry.Value);
                            }

                            w.WriteDictionaryEnd(dict);
                            context.Reply(w.CreateMessage());
                            return ValueTask.CompletedTask;
                        }

                    case "Get":
                        {
                            string name = body.ReadString();
                            if (!map.TryGetValue(name, out VariantValue value))
                            {
                                context.ReplyError("org.freedesktop.DBus.Error.UnknownProperty", name);
                                return ValueTask.CompletedTask;
                            }

                            using MessageWriter w = context.CreateReplyWriter("v");
                            w.WriteVariant(value);
                            context.Reply(w.CreateMessage());
                            return ValueTask.CompletedTask;
                        }

                    case "Set":
                        {
                            string name = body.ReadString();
                            VariantValue value = body.ReadVariantValue();
                            lock (_gate)
                            {
                                Calls.Add($"Set {name} {value.ToString(includeTypeSuffix: false)}");
                            }

                            map[name] = value;
                            using MessageWriter w = context.CreateReplyWriter(null);
                            context.Reply(w.CreateMessage());
                            return ValueTask.CompletedTask;
                        }
                }
            }

            if (iface == MprisPolicy.PlayerInterface)
            {
                string call = member switch
                {
                    "SetPosition" => $"SetPosition {body.ReadObjectPathAsString()} {body.ReadInt64()}",
                    "Seek" => $"Seek {body.ReadInt64()}",
                    _ => member,
                };
                lock (_gate)
                {
                    Calls.Add(call);
                }

                using MessageWriter w = context.CreateReplyWriter(null);
                context.Reply(w.CreateMessage());
                return ValueTask.CompletedTask;
            }

            context.ReplyUnknownMethodError();
            return ValueTask.CompletedTask;
        }

        public string[] SnapshotCalls()
        {
            lock (_gate)
            {
                return [.. Calls];
            }
        }

        public async Task UnpublishAsync()
        {
            if (_connection is not null)
            {
                _ = await _connection.ReleaseNameAsync(BusName);
            }
        }

        public ValueTask DisposeAsync()
        {
            _connection?.Dispose();
            _connection = null;
            return ValueTask.CompletedTask;
        }
    }
}
