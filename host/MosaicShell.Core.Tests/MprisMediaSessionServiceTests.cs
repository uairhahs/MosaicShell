using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Mpris;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The MPRIS service must raise the same events at the same moments as the Windows SMTC service: Changed for a
    /// new moment (track, artist, app, play state, skip), ProgressChanged for position and late art, and no
    /// stop-then-start blip while a player re-attaches.
    /// </summary>
    public class MprisMediaSessionServiceTests
    {
        private const string Spotify = "org.mpris.MediaPlayer2.spotify";
        private const string Vlc = "org.mpris.MediaPlayer2.vlc";

        private readonly FakeMprisBus _bus = new();
        private readonly FakeArtSource _art = new();
        private readonly ManualTimeProvider _clock = new();
        private int _changed;
        private int _progress;

        private static MprisPlayerState Track(string bus, string title, MprisPlaybackStatus status = MprisPlaybackStatus.Playing)
        {
            return new MprisPlayerState(bus)
            {
                PlaybackStatus = status,
                TrackId = "/track/" + title.Replace(' ', '_'),
                Title = title,
                Artists = ["Artist"],
                LengthMicros = 200_000_000,
                PositionMicros = 10_000_000,
                CanSeek = true,
                LoopStatus = "None",
                Shuffle = false,
            };
        }

        private MprisMediaSessionService Start()
        {
            MprisMediaSessionService service = new(_bus, _art, _clock);
            service.Changed += (_, _) => _changed++;
            service.ProgressChanged += (_, _) => _progress++;
            return service;
        }

        [Fact]
        public void A_playing_player_becomes_the_session()
        {
            _bus.Add(Track(Spotify, "One"));

            using MprisMediaSessionService service = Start();

            _ = service.Current!.Title.Should().Be("One");
            _ = service.Current.AppId.Should().Be("spotify");
            _ = service.Current.IsPlaying.Should().BeTrue();
            _ = service.Current.PositionSeconds.Should().Be(10);
            _ = service.Current.DurationSeconds.Should().Be(200);
        }

        [Fact]
        public void No_bus_means_no_session_and_no_exception()
        {
            _bus.FailConnect = true;

            using MprisMediaSessionService service = Start();

            _ = service.Current.Should().BeNull();
        }

        [Fact]
        public void A_new_title_raises_changed()
        {
            _bus.Add(Track(Spotify, "One"));
            using MprisMediaSessionService service = Start();
            _changed = 0;

            _bus.Update(Track(Spotify, "Two"));

            _ = service.Current!.Title.Should().Be("Two");
            _ = _changed.Should().Be(1);
        }

        [Fact]
        public void Pausing_raises_changed()
        {
            _bus.Add(Track(Spotify, "One"));
            using MprisMediaSessionService service = Start();
            _changed = 0;

            _bus.Update(Track(Spotify, "One", MprisPlaybackStatus.Paused));

            _ = service.Current!.IsPlaying.Should().BeFalse();
            _ = _changed.Should().Be(1);
        }

        [Fact]
        public void A_seek_raises_progress_not_changed()
        {
            _bus.Add(Track(Spotify, "One"));
            using MprisMediaSessionService service = Start();
            _changed = 0;

            _bus.Update(Track(Spotify, "One") with { PositionMicros = 60_000_000 }, seeked: true);

            _ = service.Current!.PositionSeconds.Should().Be(60);
            _ = _changed.Should().Be(0);
            _ = _progress.Should().Be(1);
        }

        [Fact]
        public void Art_arriving_after_the_track_patches_in_with_progress()
        {
            _bus.Add(Track(Spotify, "One") with { ArtUrl = "https://i.scdn.co/image/abc" });
            using MprisMediaSessionService service = Start();
            _ = service.Current!.ThumbnailPng.Should().BeNull();
            _changed = 0;

            byte[] art = [9, 9, 9];
            _art.Arrive("https://i.scdn.co/image/abc", art);

            _ = service.Current!.ThumbnailPng.Should().Equal(art);
            _ = _changed.Should().Be(0);
            _ = _progress.Should().Be(1);
        }

        [Fact]
        public void Art_that_is_ready_comes_with_the_track()
        {
            _art.Ready["file:///tmp/a.jpg"] = [7];
            _bus.Add(Track(Spotify, "One") with { ArtUrl = "file:///tmp/a.jpg" });

            using MprisMediaSessionService service = Start();

            _ = service.Current!.ThumbnailPng.Should().Equal(7);
        }

        [Fact]
        public void A_player_that_leaves_and_returns_within_the_grace_window_never_blanks()
        {
            _bus.Add(Track(Spotify, "One"));
            using MprisMediaSessionService service = Start();
            _changed = 0;

            _bus.Remove(Spotify);
            _clock.Advance(TimeSpan.FromMilliseconds(MediaSessionChangePolicy.NullSessionGraceMs / 2));
            _bus.Add(Track(Spotify, "Two"));
            _clock.Advance(TimeSpan.FromMilliseconds(MediaSessionChangePolicy.NullSessionGraceMs));

            _ = service.Current!.Title.Should().Be("Two");
            _ = _changed.Should().Be(1, "only the new track, never a null in between");
        }

        [Fact]
        public void A_player_that_stays_gone_clears_the_session_after_the_grace_window()
        {
            _bus.Add(Track(Spotify, "One"));
            using MprisMediaSessionService service = Start();
            _changed = 0;

            _bus.Remove(Spotify);
            _ = service.Current.Should().NotBeNull("still inside the grace window");

            _clock.Advance(TimeSpan.FromMilliseconds(MediaSessionChangePolicy.NullSessionGraceMs));

            _ = service.Current.Should().BeNull();
            _ = _changed.Should().Be(1);
        }

        [Fact]
        public void The_player_that_starts_playing_takes_over()
        {
            _bus.Add(Track(Spotify, "One"));
            _bus.Add(Track(Vlc, "Film", MprisPlaybackStatus.Paused));
            using MprisMediaSessionService service = Start();
            _ = service.Current!.AppId.Should().Be("spotify");

            _clock.Advance(TimeSpan.FromSeconds(1));
            _bus.Update(Track(Vlc, "Film"));

            _ = service.Current!.AppId.Should().Be("vlc");
        }

        [Fact]
        public void A_browser_mirror_is_shown_instead_of_the_browser_player()
        {
            _bus.Add(Track("org.mpris.MediaPlayer2.edge.instance66176", "Song | YouTube Music") with { Artists = [""], OwnerPid = 66176 });
            _bus.Add(Track("org.mpris.MediaPlayer2.plasma-browser-integration", "Song") with { DesktopEntry = "microsoft-edge", MirroredPid = 66176 });

            using MprisMediaSessionService service = Start();

            _ = service.Current!.Title.Should().Be("Song");
            _ = service.Current.AppId.Should().Be("microsoft-edge");
        }

        [Fact]
        public void The_timeline_poll_moves_the_position_with_progress_only()
        {
            _bus.Add(Track(Spotify, "One"));
            using MprisMediaSessionService service = Start();
            _changed = 0;

            _bus.Positions[Spotify] = 11_000_000;
            _clock.Advance(TimeSpan.FromMilliseconds(MediaSessionChangePolicy.TimelinePollMs));

            _ = service.Current!.PositionSeconds.Should().Be(11);
            _ = _progress.Should().Be(1);
            _ = _changed.Should().Be(0);
        }

        [Fact]
        public void A_lagging_position_does_not_pull_the_playing_scrubber_back()
        {
            _bus.Add(Track(Spotify, "One"));
            using MprisMediaSessionService service = Start();
            _bus.Positions[Spotify] = 12_000_000;
            _clock.Advance(TimeSpan.FromMilliseconds(MediaSessionChangePolicy.TimelinePollMs));

            _bus.Positions[Spotify] = 11_000_000;
            _clock.Advance(TimeSpan.FromMilliseconds(MediaSessionChangePolicy.TimelinePollMs));

            _ = service.Current!.PositionSeconds.Should().Be(12);
        }

        [Fact]
        public void A_position_falling_back_to_the_start_reads_as_a_new_track()
        {
            _bus.Add(Track(Spotify, "One") with { PositionMicros = 90_000_000 });
            using MprisMediaSessionService service = Start();
            _changed = 0;

            _bus.Positions[Spotify] = 500_000;
            _clock.Advance(TimeSpan.FromMilliseconds(MediaSessionChangePolicy.TimelinePollMs));

            _ = service.Current!.PositionSeconds.Should().Be(0.5);
            _ = _changed.Should().Be(1);
        }

        [Fact]
        public void Pump_timeline_without_a_session_does_nothing()
        {
            using MprisMediaSessionService service = Start();

            service.PumpTimeline();

            _ = _bus.PositionReads.Should().Be(0);
        }

        [Fact]
        public async Task Transport_commands_go_to_the_shown_player()
        {
            _bus.Add(Track(Spotify, "One"));
            using MprisMediaSessionService service = Start();

            await service.PlayPauseAsync();
            await service.NextAsync();
            await service.PreviousAsync();

            _ = _bus.Calls.Should().Equal($"PlayPause {Spotify}", $"Next {Spotify}", $"Previous {Spotify}");
        }

        [Fact]
        public async Task Seek_sets_the_absolute_position_of_the_current_track()
        {
            _bus.Add(Track(Spotify, "One"));
            using MprisMediaSessionService service = Start();

            await service.SeekAsync(42.5);

            _ = _bus.Calls.Should().Equal($"SetPosition {Spotify} /track/One 42500000");
        }

        [Fact]
        public async Task Seek_without_a_track_id_moves_relative_to_the_position()
        {
            _bus.Add(Track(Spotify, "One") with { TrackId = MprisPolicy.NoTrackId });
            using MprisMediaSessionService service = Start();

            await service.SeekAsync(40);

            _ = _bus.Calls.Should().Equal($"SeekBy {Spotify} 30000000");
        }

        [Fact]
        public async Task Shuffle_and_repeat_toggle_from_the_player_state()
        {
            _bus.Add(Track(Spotify, "One") with { Shuffle = true, LoopStatus = "Playlist" });
            using MprisMediaSessionService service = Start();

            await service.ToggleShuffleAsync();
            await service.ToggleRepeatAsync();

            _ = _bus.Calls.Should().Equal($"SetShuffle {Spotify} False", $"SetLoopStatus {Spotify} Track");
        }

        [Fact]
        public async Task Commands_without_a_session_are_ignored()
        {
            using MprisMediaSessionService service = Start();

            await service.PlayPauseAsync();
            await service.SeekAsync(5);

            _ = _bus.Calls.Should().BeEmpty();
        }

        [Fact]
        public async Task A_player_failing_mid_command_does_not_throw()
        {
            _bus.Add(Track(Spotify, "One"));
            using MprisMediaSessionService service = Start();
            _bus.FailCommands = true;

            Func<Task> act = service.PlayPauseAsync;

            _ = await act.Should().NotThrowAsync();
        }

        [Fact]
        public void Dispose_stops_the_poll_and_releases_the_bus()
        {
            _bus.Add(Track(Spotify, "One"));
            MprisMediaSessionService service = Start();

            service.Dispose();
            _clock.Advance(TimeSpan.FromSeconds(5));

            _ = _bus.PositionReads.Should().Be(0);
            _ = _bus.Disposed.Should().BeTrue();
            _ = _art.Disposed.Should().BeTrue();
        }

        private sealed class FakeMprisBus : IMprisBus
        {
            private readonly Dictionary<string, MprisPlayerState> _players = [];

            public Dictionary<string, long> Positions { get; } = [];
            public List<string> Calls { get; } = [];
            public bool FailConnect { get; set; }
            public bool FailCommands { get; set; }
            public int PositionReads { get; private set; }
            public bool Disposed { get; private set; }

            public event EventHandler<MprisPlayerSignal>? PlayerChanged;
            public event EventHandler? PlayersChanged;

            public void Add(MprisPlayerState player)
            {
                _players[player.BusName] = player;
                Positions[player.BusName] = player.PositionMicros;
                PlayersChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Update(MprisPlayerState player, bool seeked = false)
            {
                _players[player.BusName] = player;
                Positions[player.BusName] = player.PositionMicros;
                PlayerChanged?.Invoke(this, new MprisPlayerSignal(player.BusName, seeked));
            }

            public void Remove(string busName)
            {
                _ = _players.Remove(busName);
                PlayersChanged?.Invoke(this, EventArgs.Empty);
            }

            public Task ConnectAsync()
            {
                return FailConnect ? Task.FromException(new InvalidOperationException("no session bus")) : Task.CompletedTask;
            }

            public Task<IReadOnlyList<string>> ListPlayersAsync()
            {
                return Task.FromResult<IReadOnlyList<string>>([.. _players.Keys]);
            }

            public Task<MprisPlayerState?> ReadPlayerAsync(string busName)
            {
                return Task.FromResult(_players.TryGetValue(busName, out MprisPlayerState? p)
                    ? p with { PositionMicros = Positions[busName] }
                    : null);
            }

            public Task<long?> ReadPositionAsync(string busName)
            {
                PositionReads++;
                return Task.FromResult(Positions.TryGetValue(busName, out long p) ? p : (long?)null);
            }

            public Task PlayPauseAsync(string busName) { return Record($"PlayPause {busName}"); }
            public Task NextAsync(string busName) { return Record($"Next {busName}"); }
            public Task PreviousAsync(string busName) { return Record($"Previous {busName}"); }
            public Task SetPositionAsync(string busName, string trackId, long positionMicros) { return Record($"SetPosition {busName} {trackId} {positionMicros}"); }
            public Task SeekByAsync(string busName, long offsetMicros) { return Record($"SeekBy {busName} {offsetMicros}"); }
            public Task SetShuffleAsync(string busName, bool shuffle) { return Record($"SetShuffle {busName} {shuffle}"); }
            public Task SetLoopStatusAsync(string busName, string loopStatus) { return Record($"SetLoopStatus {busName} {loopStatus}"); }

            public void Dispose()
            {
                Disposed = true;
            }

            private Task Record(string call)
            {
                if (FailCommands)
                {
                    return Task.FromException(new InvalidOperationException("player went away"));
                }

                Calls.Add(call);
                return Task.CompletedTask;
            }
        }

        private sealed class FakeArtSource : IMprisArtSource
        {
            public Dictionary<string, byte[]> Ready { get; } = [];
            public bool Disposed { get; private set; }

            public event EventHandler? Fetched;

            public byte[]? TryGet(string url)
            {
                return Ready.TryGetValue(url, out byte[]? bytes) ? bytes : null;
            }

            public void Arrive(string url, byte[] bytes)
            {
                Ready[url] = bytes;
                Fetched?.Invoke(this, EventArgs.Empty);
            }

            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}
