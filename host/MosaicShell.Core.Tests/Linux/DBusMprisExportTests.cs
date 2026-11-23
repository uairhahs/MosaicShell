using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Linux;
using MosaicShell.Core.Services.Mpris;
using Tmds.DBus.Protocol;

namespace MosaicShell.Core.Tests.Linux
{
    /// <summary>
    /// <see cref="DBusMprisExport"/> on a real dbus-daemon, read back through the Host's own MPRIS reader, so the
    /// published player is checked with the same decoding every other player gets.
    /// </summary>
    public sealed class DBusMprisExportTests : IAsyncLifetime
    {
        private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

        private PrivateDBus? _dbus;
        private RecordingCommands _commands = new();
        private DBusMprisExport? _export;
        private DBusMprisBus? _reader;
        private DBusConnection? _client;

        public async Task InitializeAsync()
        {
            if (new LinuxBusFactAttribute().Skip is not null)
            {
                return;
            }

            _dbus = PrivateDBus.Start();
            _commands = new RecordingCommands();
            _export = new DBusMprisExport(_dbus.Address, _commands);
            _reader = new DBusMprisBus(_dbus.Address);
            await _reader.ConnectAsync();
            _client = new DBusConnection(_dbus.Address);
            await _client.ConnectAsync();
        }

        public Task DisposeAsync()
        {
            _client?.Dispose();
            _reader?.Dispose();
            _export?.Dispose();
            _dbus?.Dispose();
            return Task.CompletedTask;
        }

        private static MprisExportedState State(string title = "Vision", BrowserPlaybackState playback = BrowserPlaybackState.Playing, double position = 12)
        {
            return MprisExportPolicy.From(
                new BrowserPlayerSnapshot
                {
                    Name = "YouTube Music",
                    Title = title,
                    Artist = "Pìjus",
                    Album = "Vision - Single",
                    State = playback,
                    PositionSeconds = position,
                    DurationSeconds = 200,
                },
                "file:///cache/cover-0123456789abcdef.png",
                "microsoft-edge")!;
        }

        private async Task<bool> HasOwnerAsync()
        {
            MessageBuffer message;
            using (MessageWriter w = _client!.GetMessageWriter())
            {
                w.WriteMethodCallHeader("org.freedesktop.DBus", "/org/freedesktop/DBus", "org.freedesktop.DBus", "NameHasOwner", "s");
                w.WriteString(MprisExportPolicy.BusName);
                message = w.CreateMessage();
            }

            return await _client.CallMethodAsync(message, static (m, _) => m.GetBodyReader().ReadBool());
        }

        private async Task CallAsync(string member, string? signature = null, Action<MessageWriterBox>? body = null)
        {
            MessageBuffer message;
            using (MessageWriter w = _client!.GetMessageWriter())
            {
                w.WriteMethodCallHeader(MprisExportPolicy.BusName, MprisExportPolicy.ObjectPath, MprisPolicy.PlayerInterface, member, signature);
                MessageWriterBox box = new();
                body?.Invoke(box);
                foreach (object arg in box.Args)
                {
                    switch (arg)
                    {
                        case long x:
                            w.WriteInt64(x);
                            break;
                        case ObjectPath o:
                            w.WriteObjectPath(o);
                            break;
                    }
                }

                message = w.CreateMessage();
            }

            await _client.CallMethodAsync(message);
        }

        private static async Task<bool> EventuallyAsync(Func<bool> condition)
        {
            DateTime until = DateTime.UtcNow + Patience;
            while (DateTime.UtcNow < until)
            {
                if (condition())
                {
                    return true;
                }

                await Task.Delay(20);
            }

            return condition();
        }

        [LinuxBusFact]
        public async Task A_published_report_reads_back_as_an_ordinary_MPRIS_player()
        {
            await _export!.PublishAsync(State());

            MprisPlayerState? player = await _reader!.ReadPlayerAsync(MprisExportPolicy.BusName);

            _ = player.Should().NotBeNull();
            _ = player!.Identity.Should().Be("YouTube Music");
            _ = player.DesktopEntry.Should().Be("microsoft-edge");
            _ = player.PlaybackStatus.Should().Be(MprisPlaybackStatus.Playing);
            _ = player.Title.Should().Be("Vision");
            _ = player.Artists.Should().Equal("Pìjus");
            _ = player.ArtUrl.Should().Be("file:///cache/cover-0123456789abcdef.png");
            _ = player.LengthMicros.Should().Be(200_000_000);
            _ = player.TrackId.Should().Be(MprisExportPolicy.TrackId("Vision", "Pìjus"));
            _ = player.CanSeek.Should().BeTrue();
            _ = player.PositionMicros.Should().BeInRange(12_000_000, 14_000_000);
        }

        [LinuxBusFact]
        public async Task Publishing_nothing_gives_up_the_name_so_clients_drop_the_player()
        {
            await _export!.PublishAsync(State());
            _ = (await HasOwnerAsync()).Should().BeTrue();

            await _export.PublishAsync(null);

            _ = (await HasOwnerAsync()).Should().BeFalse();
        }

        [LinuxBusFact]
        public async Task Transport_calls_reach_the_commands()
        {
            await _export!.PublishAsync(State(position: 12));

            await CallAsync("PlayPause");
            await CallAsync("Play");
            await CallAsync("Pause");
            await CallAsync("Next");
            await CallAsync("Previous");
            await CallAsync("SetPosition", "ox", b => b.Args.AddRange([new ObjectPath(MprisExportPolicy.TrackId("Vision", "Pìjus")), 30_000_000L]));
            await CallAsync("SetPosition", "ox", b => b.Args.AddRange([new ObjectPath("/org/mosaicshell/track/tstale"), 99_000_000L]));

            _ = (await EventuallyAsync(() => _commands.Calls.Count >= 6)).Should().BeTrue();
            _ = _commands.Calls.Should().Equal("PlayPause", "Play", "Pause", "Next", "Previous", "SeekTo 30");
        }

        [LinuxBusFact]
        public async Task Seek_moves_by_the_offset_from_the_current_position()
        {
            await _export!.PublishAsync(State(playback: BrowserPlaybackState.Paused, position: 12));

            await CallAsync("Seek", "x", b => b.Args.Add(5_000_000L));

            _ = (await EventuallyAsync(() => _commands.Calls.Count >= 1)).Should().BeTrue();
            _ = _commands.Calls.Should().Equal("SeekTo 17");
        }

        [LinuxBusFact]
        public async Task A_new_track_is_announced_with_PropertiesChanged()
        {
            await _export!.PublishAsync(State("Vision"));
            List<string> changed = [];
            using IDisposable watch = await _client!.AddMatchAsync(
                new MatchRule { Type = MessageType.Signal, Path = MprisExportPolicy.ObjectPath, Interface = "org.freedesktop.DBus.Properties", Member = "PropertiesChanged" },
                static (m, _) =>
                {
                    Reader r = m.GetBodyReader();
                    _ = r.ReadString();
                    List<string> names = [];
                    ArrayEnd end = r.ReadDictionaryStart();
                    while (r.HasNext(end))
                    {
                        names.Add(r.ReadString());
                        _ = r.ReadVariantValue();
                    }

                    return names;
                },
                n =>
                {
                    if (n.HasValue)
                    {
                        lock (changed)
                        {
                            changed.AddRange(n.Value);
                        }
                    }
                },
                false,
                ObserverFlags.None);

            await _export.PublishAsync(State("Rush"));

            _ = (await EventuallyAsync(() => { lock (changed) { return changed.Contains("Metadata"); } })).Should().BeTrue();
            MprisPlayerState? player = await _reader!.ReadPlayerAsync(MprisExportPolicy.BusName);
            _ = player!.Title.Should().Be("Rush");
        }

        private sealed class MessageWriterBox
        {
            public List<object> Args { get; } = [];
        }

        private sealed class RecordingCommands : IMprisExportCommands
        {
            private readonly List<string> _calls = [];

            public IReadOnlyList<string> Calls
            {
                get
                {
                    lock (_calls)
                    {
                        return [.. _calls];
                    }
                }
            }

            private Task Record(string call)
            {
                lock (_calls)
                {
                    _calls.Add(call);
                }

                return Task.CompletedTask;
            }

            public Task PlayPauseAsync()
            {
                return Record("PlayPause");
            }

            public Task PlayAsync()
            {
                return Record("Play");
            }

            public Task PauseAsync()
            {
                return Record("Pause");
            }

            public Task NextAsync()
            {
                return Record("Next");
            }

            public Task PreviousAsync()
            {
                return Record("Previous");
            }

            public Task SeekToAsync(double positionSeconds)
            {
                return Record($"SeekTo {positionSeconds:0.###}");
            }
        }
    }
}
