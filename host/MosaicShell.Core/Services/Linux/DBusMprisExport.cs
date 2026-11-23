using System.Text;
using MosaicShell.Core.Services.Mpris;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Services.Linux
{
    /// <summary>
    /// Publishes <see cref="MprisExportPolicy.BusName"/> on the session bus: answers the MPRIS root and Player
    /// interfaces from the last <see cref="MprisExportedState"/>, announces changes with PropertiesChanged and Seeked, and
    /// hands transport calls to <see cref="IMprisExportCommands"/>. Owns the name only while there is something to
    /// publish, so clients drop the player when there is nothing.
    /// </summary>
    public sealed class DBusMprisExport(string? address, IMprisExportCommands commands, TimeProvider? clock = null)
        : IPathMethodHandler, IMprisExport
    {
        private const string PropertiesInterface = "org.freedesktop.DBus.Properties";

        private static readonly ReadOnlyMemory<byte>[] IntrospectXml =
        [
            Encoding.UTF8.GetBytes("""
                <interface name="org.mpris.MediaPlayer2">
                  <method name="Raise"/>
                  <method name="Quit"/>
                  <property name="CanQuit" type="b" access="read"/>
                  <property name="CanRaise" type="b" access="read"/>
                  <property name="HasTrackList" type="b" access="read"/>
                  <property name="Identity" type="s" access="read"/>
                  <property name="DesktopEntry" type="s" access="read"/>
                  <property name="SupportedUriSchemes" type="as" access="read"/>
                  <property name="SupportedMimeTypes" type="as" access="read"/>
                </interface>
                """),
            Encoding.UTF8.GetBytes("""
                <interface name="org.mpris.MediaPlayer2.Player">
                  <method name="Next"/>
                  <method name="Previous"/>
                  <method name="Pause"/>
                  <method name="PlayPause"/>
                  <method name="Stop"/>
                  <method name="Play"/>
                  <method name="Seek"><arg name="Offset" type="x" direction="in"/></method>
                  <method name="SetPosition"><arg name="TrackId" type="o" direction="in"/><arg name="Position" type="x" direction="in"/></method>
                  <method name="OpenUri"><arg name="Uri" type="s" direction="in"/></method>
                  <signal name="Seeked"><arg name="Position" type="x"/></signal>
                  <property name="PlaybackStatus" type="s" access="read"/>
                  <property name="Rate" type="d" access="readwrite"/>
                  <property name="Metadata" type="a{sv}" access="read"/>
                  <property name="Volume" type="d" access="readwrite"/>
                  <property name="Position" type="x" access="read"/>
                  <property name="MinimumRate" type="d" access="read"/>
                  <property name="MaximumRate" type="d" access="read"/>
                  <property name="CanGoNext" type="b" access="read"/>
                  <property name="CanGoPrevious" type="b" access="read"/>
                  <property name="CanPlay" type="b" access="read"/>
                  <property name="CanPause" type="b" access="read"/>
                  <property name="CanSeek" type="b" access="read"/>
                  <property name="CanControl" type="b" access="read"/>
                </interface>
                """),
        ];

        private readonly TimeProvider _clock = clock ?? TimeProvider.System;
        private readonly SemaphoreSlim _publish = new(1, 1);
        private readonly Lock _gate = new();
        private DBusConnection? _connection;
        private bool _ownsName;
        private bool _disposed;
        private MprisExportedState? _state;
        private long _reportedAt;

        public string Path => MprisExportPolicy.ObjectPath;

        public bool HandlesChildPaths => false;

        /// <summary>Publishes <paramref name="next"/>, or gives up the name when it is null.</summary>
        public async Task PublishAsync(MprisExportedState? next)
        {
            await _publish.WaitAsync().ConfigureAwait(false);
            try
            {
                DBusConnection? connection = await ConnectAsync().ConfigureAwait(false);
                if (connection is null)
                {
                    return;
                }

                MprisExportedState? previous;
                double elapsed;
                lock (_gate)
                {
                    previous = _state;
                    elapsed = _clock.GetElapsedTime(_reportedAt).TotalSeconds;
                    _state = next;
                    _reportedAt = _clock.GetTimestamp();
                }

                if (next is null)
                {
                    if (_ownsName)
                    {
                        _ownsName = false;
                        _ = await connection.ReleaseNameAsync(MprisExportPolicy.BusName).ConfigureAwait(false);
                    }

                    return;
                }

                if (!_ownsName)
                {
                    // A newly owned name is read whole (GetAll), so nothing needs announcing.
                    _ownsName = await connection.TryRequestNameAsync(MprisExportPolicy.BusName, RequestNameOptions.None)
                        .ConfigureAwait(false);
                    return;
                }

                if (previous is null)
                {
                    return;
                }

                IReadOnlyList<string> changed = MprisExportPolicy.ChangedPlayerProperties(previous, next);
                if (changed.Count > 0)
                {
                    Dictionary<string, VariantValue> player = PlayerProperties(next, next.PositionMicros);
                    EmitPropertiesChanged(connection, MprisPolicy.PlayerInterface, changed.ToDictionary(n => n, n => player[n]));
                }

                if (previous.Identity != next.Identity || previous.DesktopEntry != next.DesktopEntry)
                {
                    EmitPropertiesChanged(connection, MprisPolicy.RootInterface, RootProperties(next));
                }

                if (changed.Contains("Metadata")
                    || MprisExportPolicy.IsSeek(previous.PositionMicros, elapsed, previous.PlaybackStatus == "Playing", next.PositionMicros))
                {
                    EmitSeeked(connection, next.PositionMicros);
                }
            }
            catch (Exception ex) when (ex is DBusConnectionException or DBusErrorReplyException or ObjectDisposedException or InvalidOperationException)
            {
                // No session bus, or it went away: other MPRIS clients simply see no player.
            }
            finally
            {
                _ = _publish.Release();
            }
        }

        public void Dispose()
        {
            DBusConnection? connection;
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                connection = _connection;
                _connection = null;
            }

            connection?.Dispose();
        }

        private async Task<DBusConnection?> ConnectAsync()
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return null;
                }

                if (_connection is not null)
                {
                    return _connection;
                }
            }

            string? target = address ?? DBusAddress.Session;
            if (string.IsNullOrEmpty(target))
            {
                return null;
            }

            DBusConnection connection = new(target);
            await connection.ConnectAsync().ConfigureAwait(false);
            connection.AddMethodHandler(this);
            lock (_gate)
            {
                if (_disposed)
                {
                    connection.Dispose();
                    return null;
                }

                _connection = connection;
            }

            return connection;
        }

        public ValueTask HandleMethodAsync(MethodContext context)
        {
            MprisExportedState? state;
            long position;
            lock (_gate)
            {
                state = _state;
                position = state is null ? 0 : MprisExportPolicy.PositionAt(state, _clock.GetElapsedTime(_reportedAt).TotalSeconds);
            }

            Message request = context.Request;
            string iface = request.InterfaceAsString ?? "";
            string member = request.MemberAsString ?? "";

            if (context.IsDBusIntrospectRequest)
            {
                context.ReplyIntrospectXml(IntrospectXml, []);
                return ValueTask.CompletedTask;
            }

            if (iface == PropertiesInterface)
            {
                Reader body = request.GetBodyReader();
                string target = body.ReadString();
                Dictionary<string, VariantValue> map = target == MprisPolicy.RootInterface
                    ? RootProperties(state)
                    : target == MprisPolicy.PlayerInterface ? PlayerProperties(state, position) : [];
                switch (member)
                {
                    case "GetAll":
                        {
                            using MessageWriter w = context.CreateReplyWriter("a{sv}");
                            w.WriteDictionary(map);
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
                            // Rate and Volume are fixed; MPRIS lets a player ignore a write it cannot honour.
                            using MessageWriter w = context.CreateReplyWriter(null);
                            context.Reply(w.CreateMessage());
                            return ValueTask.CompletedTask;
                        }
                }

                context.ReplyUnknownMethodError();
                return ValueTask.CompletedTask;
            }

            if (iface == MprisPolicy.RootInterface && member is "Raise" or "Quit")
            {
                ReplyEmpty(context);
                return ValueTask.CompletedTask;
            }

            if (iface == MprisPolicy.PlayerInterface)
            {
                Func<Task>? run = null;
                switch (member)
                {
                    case "PlayPause":
                        run = commands.PlayPauseAsync;
                        break;
                    case "Play":
                        run = commands.PlayAsync;
                        break;
                    case "Pause":
                    case "Stop":
                        run = commands.PauseAsync;
                        break;
                    case "Next":
                        run = commands.NextAsync;
                        break;
                    case "Previous":
                        run = commands.PreviousAsync;
                        break;
                    case "Seek":
                        {
                            long offset = request.GetBodyReader().ReadInt64();
                            double to = Math.Max(0, position + offset) / 1_000_000.0;
                            run = () => commands.SeekToAsync(to);
                            break;
                        }

                    case "SetPosition":
                        {
                            Reader body = request.GetBodyReader();
                            string trackId = body.ReadObjectPathAsString();
                            long at = body.ReadInt64();
                            // MPRIS: a SetPosition for another track, or outside the track, is ignored.
                            if (state is not null && trackId == state.Track.TrackId && at >= 0
                                && (state.Track.LengthMicros is not long length || at <= length))
                            {
                                run = () => commands.SeekToAsync(at / 1_000_000.0);
                            }

                            break;
                        }

                    case "OpenUri":
                        break;
                    default:
                        context.ReplyUnknownMethodError();
                        return ValueTask.CompletedTask;
                }

                ReplyEmpty(context);
                if (run is not null)
                {
                    _ = RunAsync(run);
                }

                return ValueTask.CompletedTask;
            }

            context.ReplyUnknownMethodError();
            return ValueTask.CompletedTask;
        }

        private static async Task RunAsync(Func<Task> command)
        {
            try
            {
                await command().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MPRIS export] command failed: {ex.Message}");
            }
        }

        private static void ReplyEmpty(MethodContext context)
        {
            if (context.NoReplyExpected)
            {
                return;
            }

            using MessageWriter w = context.CreateReplyWriter(null);
            context.Reply(w.CreateMessage());
        }

        private static Dictionary<string, VariantValue> RootProperties(MprisExportedState? state)
        {
            Dictionary<string, VariantValue> map = new()
            {
                ["CanQuit"] = VariantValue.Bool(false),
                ["CanRaise"] = VariantValue.Bool(false),
                ["HasTrackList"] = VariantValue.Bool(false),
                ["Identity"] = VariantValue.String(state?.Identity ?? "MosaicShell"),
                ["SupportedUriSchemes"] = VariantValue.Array(Array.Empty<string>()),
                ["SupportedMimeTypes"] = VariantValue.Array(Array.Empty<string>()),
            };
            if (state?.DesktopEntry is { Length: > 0 } entry)
            {
                map["DesktopEntry"] = VariantValue.String(entry);
            }

            return map;
        }

        private static Dictionary<string, VariantValue> PlayerProperties(MprisExportedState? state, long position)
        {
            bool active = state is not null;
            bool canSeek = state?.Track.LengthMicros is > 0;
            return new Dictionary<string, VariantValue>
            {
                ["PlaybackStatus"] = VariantValue.String(state?.PlaybackStatus ?? "Stopped"),
                ["Rate"] = VariantValue.Double(1.0),
                ["Volume"] = VariantValue.Double(1.0),
                ["Metadata"] = Metadata(state?.Track),
                ["Position"] = VariantValue.Int64(position),
                ["MinimumRate"] = VariantValue.Double(1.0),
                ["MaximumRate"] = VariantValue.Double(1.0),
                ["CanGoNext"] = VariantValue.Bool(active),
                ["CanGoPrevious"] = VariantValue.Bool(active),
                ["CanPlay"] = VariantValue.Bool(active),
                ["CanPause"] = VariantValue.Bool(active),
                ["CanSeek"] = VariantValue.Bool(canSeek),
                ["CanControl"] = VariantValue.Bool(true),
            };
        }

        private static VariantValue Metadata(MprisExportedTrack? track)
        {
            Dictionary<string, VariantValue> map = new()
            {
                ["mpris:trackid"] = VariantValue.ObjectPath(new ObjectPath(track?.TrackId ?? MprisPolicy.NoTrackId)),
            };
            if (track is not null)
            {
                map["xesam:title"] = VariantValue.String(track.Title);
                map["xesam:artist"] = VariantValue.Array(track.Artists.ToArray());
                if (track.Album is { } album)
                {
                    map["xesam:album"] = VariantValue.String(album);
                }

                if (track.LengthMicros is long length)
                {
                    map["mpris:length"] = VariantValue.Int64(length);
                }

                if (track.ArtUrl is { } art)
                {
                    map["mpris:artUrl"] = VariantValue.String(art);
                }
            }

            return new Dict<string, VariantValue>(map).AsVariantValue();
        }

        private static void EmitPropertiesChanged(DBusConnection connection, string iface, Dictionary<string, VariantValue> changed)
        {
            using MessageWriter w = connection.GetMessageWriter();
            w.WriteSignalHeader(null, MprisExportPolicy.ObjectPath, PropertiesInterface, "PropertiesChanged", "sa{sv}as");
            w.WriteString(iface);
            w.WriteDictionary(changed);
            w.WriteArray(Array.Empty<string>());
            _ = connection.TrySendMessage(w.CreateMessage());
        }

        private static void EmitSeeked(DBusConnection connection, long positionMicros)
        {
            using MessageWriter w = connection.GetMessageWriter();
            w.WriteSignalHeader(null, MprisExportPolicy.ObjectPath, MprisPolicy.PlayerInterface, "Seeked", "x");
            w.WriteInt64(positionMicros);
            _ = connection.TrySendMessage(w.CreateMessage());
        }
    }
}
