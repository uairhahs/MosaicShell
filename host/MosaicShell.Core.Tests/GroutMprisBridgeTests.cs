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
        private readonly FakeExport _export = new();

        public void Dispose()
        {
            if (Directory.Exists(_cache))
            {
                Directory.Delete(_cache, recursive: true);
            }
        }

        private GroutMprisBridge Bridge()
        {
            return new GroutMprisBridge(_browser, _media, _ => _export, _cache);
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

            MprisExportedState state = _export.Last!;
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

            _ = _export.Last!.Track.LengthMicros.Should().Be(157_000_000L);
            _ = _export.Last.PositionMicros.Should().Be(30_000_000L);
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

            _ = _export.Last!.PositionMicros.Should().Be(120_000_000L);
        }

        [Fact]
        public async Task No_report_publishes_nothing()
        {
            using GroutMprisBridge bridge = Bridge();
            _browser.Report(Report());
            await bridge.SettledAsync();

            _browser.Report(null);
            await bridge.SettledAsync();

            _ = _export.Published.Should().HaveCount(2);
            _ = _export.Last.Should().BeNull();
        }

        [Fact]
        public async Task A_native_player_session_lends_no_desktop_entry()
        {
            _media.Current = new MediaSessionInfo("Song", "Band", "spotify", true);
            using GroutMprisBridge bridge = Bridge();

            _browser.Report(Report());
            await bridge.SettledAsync();

            _ = _export.Last!.DesktopEntry.Should().BeNull();
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

            _ = _export.Last!.Track.ArtUrl.Should().BeNull();
        }

        [Fact]
        public async Task Transport_goes_through_the_media_service_while_the_session_is_the_browser()
        {
            _media.Current = new MediaSessionInfo("Vision", "Pìjus", "microsoft-edge", IsPlaying: true);
            using GroutMprisBridge bridge = Bridge();

            await bridge.PlayPauseAsync();
            await bridge.NextAsync();
            await bridge.PreviousAsync();
            await bridge.SeekToAsync(42);
            await bridge.PlayAsync();
            await bridge.PauseAsync();

            _ = _media.Calls.Should().Equal("PlayPause", "Next", "Previous", "Seek 42", "PlayPause");
        }

        [Fact]
        public async Task Play_starts_a_paused_browser_session()
        {
            _media.Current = new MediaSessionInfo("Vision", "Pìjus", "microsoft-edge", IsPlaying: false);
            using GroutMprisBridge bridge = Bridge();

            await bridge.PauseAsync();
            await bridge.PlayAsync();

            _ = _media.Calls.Should().Equal("PlayPause");
        }

        [Fact]
        public async Task Transport_never_steers_a_native_player()
        {
            // The media service follows whichever player is current; pressing play on the browser's published
            // player must not pause Spotify.
            _media.Current = new MediaSessionInfo("Song", "Band", "spotify", IsPlaying: true);
            using GroutMprisBridge bridge = Bridge();

            await bridge.PlayPauseAsync();
            await bridge.NextAsync();

            _ = _media.Calls.Should().BeEmpty();
        }

        [Fact]
        public void Disposing_the_bridge_disposes_the_export()
        {
            GroutMprisBridge bridge = Bridge();

            bridge.Dispose();

            _ = _export.Disposed.Should().BeTrue();
        }

        [Theory]
        [InlineData("/custom/cache", "/home/u", "/custom/cache/MosaicShell/mpris-art")]
        [InlineData(null, "/home/u", "/home/u/.cache/MosaicShell/mpris-art")]
        [InlineData("relative", "/home/u", "/home/u/.cache/MosaicShell/mpris-art")]
        public void The_art_cache_follows_XDG(string? xdgCacheHome, string home, string expected)
        {
            _ = MprisExportPolicy.ArtCacheDirectory(xdgCacheHome, home).Should().Be(expected);
        }

        private sealed class FakeExport : IMprisExport
        {
            private readonly List<MprisExportedState?> _published = [];

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
