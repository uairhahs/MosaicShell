using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.Mpris;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// <see cref="GroutMprisBridge"/>: Grout's report becomes the Host's published MPRIS player, with the cover in a
    /// file that stays for the whole track, and the player's transport goes back through the Host's media service.
    /// </summary>
    public sealed class GroutMprisBridgeTests : IDisposable
    {
        private static readonly byte[] CoverA = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 0xA];
        private static readonly byte[] CoverB = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 0xB];
        private static readonly byte[] CoverC = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 0xC];

        private readonly string _cache = Path.Combine(Path.GetTempPath(), "mosaic-mpris-art-" + Guid.NewGuid().ToString("N"));
        private readonly FakeBrowser _browser = new();
        private readonly FakeMedia _media = new();
        private readonly Dictionary<string, FakeExport> _exports = [];

        private FakeExport Export => _exports[MprisExportPolicy.BusName];

        private IMprisExport Create(string busName, IMprisExportCommands commands)
        {
            lock (_exports)
            {
                FakeExport export = new(commands);
                _exports[busName] = export;
                return export;
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(_cache))
            {
                Directory.Delete(_cache, recursive: true);
            }
        }

        private GroutMprisBridge Bridge()
        {
            return new GroutMprisBridge(_browser, _media, Create, _cache);
        }

        private static BrowserPlayerSnapshot Report(string title = "Vision", byte[]? cover = null, BrowserPlaybackState state = BrowserPlaybackState.Playing)
        {
            return new BrowserPlayerSnapshot { Name = "YouTube Music", Title = title, Artist = "Pìjus", State = state, DurationSeconds = 200, CoverPng = cover ?? CoverA };
        }

        [Fact]
        public async Task A_report_is_published_with_its_cover_in_a_file_that_stays()
        {
            _media.Current = new MediaSessionInfo("Vision", "Pìjus", "microsoft-edge", true);
            using GroutMprisBridge bridge = Bridge();

            _browser.Report(Report());
            await bridge.SettledAsync();

            MprisExportedState state = Export.Last!;
            _ = state.Track.Title.Should().Be("Vision");
            _ = state.DesktopEntry.Should().Be("microsoft-edge");
            string file = Path.Combine(_cache, MprisExportPolicy.ArtFileName(CoverA));
            _ = File.ReadAllBytes(file).Should().Equal(CoverA);
            _ = state.Track.ArtUrl.Should().Be(new Uri(file).AbsoluteUri);
        }

        [Fact]
        public async Task The_published_length_comes_from_the_media_session()
        {
            _media.Current = new MediaSessionInfo("Vision", "Pìjus", "microsoft-edge", true, PositionSeconds: 30, DurationSeconds: 157);
            using GroutMprisBridge bridge = Bridge();

            _browser.Report(new BrowserPlayerSnapshot { Name = "YouTube Music", Title = "Vision", Artist = "Pìjus", State = BrowserPlaybackState.Playing });
            await bridge.SettledAsync();

            _ = Export.Last!.Track.LengthMicros.Should().Be(157_000_000L);
            _ = Export.Last.PositionMicros.Should().Be(30_000_000L);
        }

        [Fact]
        public async Task Progress_from_the_media_session_is_published_so_a_jump_can_be_seen()
        {
            _media.Current = new MediaSessionInfo("Vision", "Pìjus", "microsoft-edge", true, PositionSeconds: 30, DurationSeconds: 157);
            using GroutMprisBridge bridge = Bridge();
            _browser.Report(Report());
            await bridge.SettledAsync();

            _media.Current = _media.Current with { PositionSeconds = 120 };
            _media.Raise();
            await bridge.SettledAsync();

            _ = Export.Last!.PositionMicros.Should().Be(120_000_000L);
        }

        [Fact]
        public async Task No_report_publishes_nothing()
        {
            using GroutMprisBridge bridge = Bridge();
            _browser.Report(Report());
            await bridge.SettledAsync();

            _browser.Report(null);
            await bridge.SettledAsync();

            _ = Export.Published.Should().HaveCount(2);
            _ = Export.Last.Should().BeNull();
        }

        [Fact]
        public async Task The_desktop_entry_is_resolved_from_the_session_and_browser_process()
        {
            _media.Current = new MediaSessionInfo("Vision", "Pìjus", "edge", true);
            using GroutMprisBridge bridge = new(
                _browser, _media, Create, _cache, () => 13951,
                (appId, pid) => $"{appId}-resolved-{pid}");

            _browser.Report(Report());
            await bridge.SettledAsync();

            _ = Export.Last!.DesktopEntry.Should().Be("edge-resolved-13951");
        }

        [Fact]
        public async Task A_native_player_session_lends_no_desktop_entry()
        {
            _media.Current = new MediaSessionInfo("Song", "Band", "spotify", true);
            using GroutMprisBridge bridge = Bridge();

            _browser.Report(Report());
            await bridge.SettledAsync();

            _ = Export.Last!.DesktopEntry.Should().BeNull();
        }

        [Fact]
        public async Task Only_the_current_and_previous_covers_are_kept()
        {
            using GroutMprisBridge bridge = Bridge();

            foreach (byte[] cover in new[] { CoverA, CoverA, CoverB, CoverC })
            {
                _browser.Report(Report(cover: cover));
                await bridge.SettledAsync();
            }

            string[] files = [.. Directory.GetFiles(_cache).Select(Path.GetFileName)!];
            _ = files.Should().BeEquivalentTo([MprisExportPolicy.ArtFileName(CoverB), MprisExportPolicy.ArtFileName(CoverC)]);
        }

        [Fact]
        public async Task A_report_without_a_cover_is_published_without_art()
        {
            using GroutMprisBridge bridge = Bridge();

            _browser.Report(new BrowserPlayerSnapshot { Name = "YouTube Music", Title = "Vision", State = BrowserPlaybackState.Playing });
            await bridge.SettledAsync();

            _ = Export.Last!.Track.ArtUrl.Should().BeNull();
        }

        [Fact]
        public async Task Transport_goes_through_the_media_service_while_the_session_is_the_browser()
        {
            _media.Current = new MediaSessionInfo("Vision", "Pìjus", "microsoft-edge", IsPlaying: true);
            using GroutMprisBridge bridge = Bridge();
            _browser.Report(Report());
            await bridge.SettledAsync();
            IMprisExportCommands commands = Export.Commands;

            await commands.PlayPauseAsync();
            await commands.NextAsync();
            await commands.PreviousAsync();
            await commands.SeekToAsync(42);
            await commands.PlayAsync();
            await commands.PauseAsync();

            _ = _media.Calls.Should().Equal("PlayPause", "Next", "Previous", "Seek 42", "PlayPause");
        }

        [Fact]
        public async Task Play_starts_a_paused_browser_session()
        {
            _media.Current = new MediaSessionInfo("Vision", "Pìjus", "microsoft-edge", IsPlaying: false);
            using GroutMprisBridge bridge = Bridge();
            _browser.Report(Report());
            await bridge.SettledAsync();
            IMprisExportCommands commands = Export.Commands;

            await commands.PauseAsync();
            await commands.PlayAsync();

            _ = _media.Calls.Should().Equal("PlayPause");
        }

        [Fact]
        public async Task Transport_never_steers_a_native_player()
        {
            // The media service follows whichever player is current; pressing play on the browser's published
            // player must not pause Spotify.
            _media.Current = new MediaSessionInfo("Song", "Band", "spotify", IsPlaying: true);
            using GroutMprisBridge bridge = Bridge();
            _browser.Report(Report());
            await bridge.SettledAsync();
            IMprisExportCommands commands = Export.Commands;

            await commands.PlayPauseAsync();
            await commands.NextAsync();

            _ = _media.Calls.Should().BeEmpty();
        }

        [Fact]
        public async Task Disposing_the_bridge_disposes_the_export()
        {
            GroutMprisBridge bridge = Bridge();
            _browser.Report(Report());
            await bridge.SettledAsync();

            bridge.Dispose();

            _ = Export.Disposed.Should().BeTrue();
        }

        [Fact]
        public async Task Each_tab_is_published_as_its_own_player()
        {
            FakeTabs tabs = new();
            using GroutMprisBridge bridge = new(tabs, _media, Create, _cache);

            tabs.Set(("c1_t7", Report("Vision", CoverA)), ("c1_t9", Report("A video", CoverB)));
            await bridge.SettledAsync();

            _ = _exports.Keys.Should().BeEquivalentTo([MprisExportPolicy.BusNameFor("c1_t7"), MprisExportPolicy.BusNameFor("c1_t9")]);
            _ = _exports[MprisExportPolicy.BusNameFor("c1_t7")].Last!.Track.Title.Should().Be("Vision");
            _ = _exports[MprisExportPolicy.BusNameFor("c1_t9")].Last!.Track.Title.Should().Be("A video");
            _ = Directory.GetFiles(_cache).Select(Path.GetFileName).Should().BeEquivalentTo(
                [MprisExportPolicy.ArtFileName(CoverA), MprisExportPolicy.ArtFileName(CoverB)], "every tab keeps its own cover");
        }

        [Fact]
        public async Task A_tab_that_goes_away_withdraws_its_player()
        {
            FakeTabs tabs = new();
            using GroutMprisBridge bridge = new(tabs, _media, Create, _cache);
            tabs.Set(("c1_t7", Report("Vision")), ("c1_t9", Report("A video")));
            await bridge.SettledAsync();

            tabs.Set(("c1_t7", Report("Vision")));
            await bridge.SettledAsync();

            FakeExport gone = _exports[MprisExportPolicy.BusNameFor("c1_t9")];
            _ = gone.Last.Should().BeNull();
            _ = gone.Disposed.Should().BeTrue();
            _ = _exports[MprisExportPolicy.BusNameFor("c1_t7")].Disposed.Should().BeFalse();
        }

        [Fact]
        public async Task Only_the_tab_the_browser_is_playing_takes_transport()
        {
            _media.Current = new MediaSessionInfo("Vision", "Pìjus", "edge", IsPlaying: true);
            FakeTabs tabs = new();
            using GroutMprisBridge bridge = new(tabs, _media, Create, _cache);
            tabs.Set(("c1_t7", Report("Vision")), ("c1_t9", Report("A video")));
            await bridge.SettledAsync();

            await _exports[MprisExportPolicy.BusNameFor("c1_t9")].Commands.PlayPauseAsync();
            await _exports[MprisExportPolicy.BusNameFor("c1_t7")].Commands.PlayPauseAsync();

            _ = _media.Calls.Should().Equal("PlayPause");
            _ = _exports[MprisExportPolicy.BusNameFor("c1_t7")].Last!.Controllable.Should().BeTrue();
            _ = _exports[MprisExportPolicy.BusNameFor("c1_t9")].Last!.Controllable.Should().BeFalse();
        }

        [Fact]
        public async Task The_last_browser_seen_keeps_its_icon_and_process_while_another_app_plays()
        {
            _media.Current = new MediaSessionInfo("Vision", "Pìjus", "edge", true);
            int? pid = 13951;
            FakeTabs tabs = new();
            using GroutMprisBridge bridge = new(tabs, _media, Create, _cache, () => pid, (appId, _) => appId + ".desktop");
            tabs.Set(("c1_t7", Report("Vision")));
            await bridge.SettledAsync();

            // Spotify is now the platform session; its process must never be claimed.
            _media.Current = new MediaSessionInfo("Song", "Band", "spotify", true);
            pid = 777;
            _media.Raise();
            await bridge.SettledAsync();

            MprisExportedState state = _exports[MprisExportPolicy.BusNameFor("c1_t7")].Last!;
            _ = state.DesktopEntry.Should().Be("edge.desktop");
            _ = state.Track.KdePid.Should().Be(13951);
            _ = state.Controllable.Should().BeFalse();
        }

        [Theory]
        [InlineData("/custom/cache", "/home/u", "/custom/cache/MosaicShell/mpris-art")]
        [InlineData(null, "/home/u", "/home/u/.cache/MosaicShell/mpris-art")]
        [InlineData("relative", "/home/u", "/home/u/.cache/MosaicShell/mpris-art")]
        public void The_art_cache_follows_XDG(string? xdgCacheHome, string home, string expected)
        {
            _ = MprisExportPolicy.ArtCacheDirectory(xdgCacheHome, home).Should().Be(expected);
        }

        private sealed class FakeExport(IMprisExportCommands commands) : IMprisExport
        {
            private readonly List<MprisExportedState?> _published = [];

            public IMprisExportCommands Commands { get; } = commands;

            public IReadOnlyList<MprisExportedState?> Published
            {
                get
                {
                    lock (_published)
                    {
                        return [.. _published];
                    }
                }
            }

            public MprisExportedState? Last => Published[^1];

            public bool Disposed { get; private set; }

            public Task PublishAsync(MprisExportedState? state)
            {
                lock (_published)
                {
                    _published.Add(state);
                }

                return Task.CompletedTask;
            }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        private sealed class FakeTabs : IBrowserMediaSource, IBrowserPlayerList
        {
            public IReadOnlyList<BrowserTabPlayer> Players { get; private set; } = [];

            public BrowserPlayerSnapshot? Active => Players.Count > 0 ? Players[0].Snapshot : null;

            public event EventHandler? Changed;

            public void Set(params (string Key, BrowserPlayerSnapshot Report)[] tabs)
            {
                Players = [.. tabs.Select(t => new BrowserTabPlayer(t.Key, t.Report))];
                Changed?.Invoke(this, EventArgs.Empty);
            }

            public Task SetLikedAsync(bool liked)
            {
                return Task.CompletedTask;
            }

            public Task SetDislikedAsync(bool disliked)
            {
                return Task.CompletedTask;
            }

            public Task ToggleShuffleAsync()
            {
                return Task.CompletedTask;
            }

            public Task ToggleRepeatAsync()
            {
                return Task.CompletedTask;
            }

            public void Dispose()
            {
            }
        }

        private sealed class FakeBrowser : IBrowserMediaSource
        {
            public BrowserPlayerSnapshot? Active { get; private set; }

            public event EventHandler? Changed;

            public void Report(BrowserPlayerSnapshot? report)
            {
                Active = report;
                Changed?.Invoke(this, EventArgs.Empty);
            }

            public Task SetLikedAsync(bool liked)
            {
                return Task.CompletedTask;
            }

            public Task SetDislikedAsync(bool disliked)
            {
                return Task.CompletedTask;
            }

            public Task ToggleShuffleAsync()
            {
                return Task.CompletedTask;
            }

            public Task ToggleRepeatAsync()
            {
                return Task.CompletedTask;
            }

            public void Dispose()
            {
            }
        }

        private sealed class FakeMedia : IMediaSessionService
        {
            public MediaSessionInfo? Current { get; set; }

            public List<string> Calls { get; } = [];

            public event EventHandler? Changed;

            public event EventHandler? ProgressChanged;

            public void Raise()
            {
                Changed?.Invoke(this, EventArgs.Empty);
                ProgressChanged?.Invoke(this, EventArgs.Empty);
            }

            public void PumpTimeline()
            {
            }

            public Task PlayPauseAsync()
            {
                Calls.Add("PlayPause");
                return Task.CompletedTask;
            }

            public Task NextAsync()
            {
                Calls.Add("Next");
                return Task.CompletedTask;
            }

            public Task PreviousAsync()
            {
                Calls.Add("Previous");
                return Task.CompletedTask;
            }

            public Task SeekAsync(double positionSeconds)
            {
                Calls.Add($"Seek {positionSeconds:0.###}");
                return Task.CompletedTask;
            }

            public Task ToggleShuffleAsync()
            {
                return Task.CompletedTask;
            }

            public Task ToggleRepeatAsync()
            {
                return Task.CompletedTask;
            }

            public Task ToggleLikeAsync(bool wantLiked)
            {
                return Task.CompletedTask;
            }

            public Task ToggleDislikeAsync(bool wantDisliked)
            {
                return Task.CompletedTask;
            }

            public void Dispose()
            {
            }
        }
    }
}
