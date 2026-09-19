using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.BrowserUi;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The extension-free browser source: for the track Windows says is playing in a browser, it finds that browser
    /// window through the accessibility tree and reads the real like and dislike state of YouTube Music. Nothing has
    /// to be installed or configured, and nothing is read unless a browser session is playing.
    /// </summary>
    public sealed class BrowserUiSourceTests : IDisposable
    {
        private const string PwaApp = "music.youtube.com-5929F88E_vezhnr0wkvrcy!App";
        private const string Controls = "middle-controls-buttons style-scope ytmusic-player-bar";

        private readonly ManualTimeProvider _clock = new();
        private readonly List<IDisposable> _disposables = [];

        public void Dispose()
        {
            foreach (IDisposable d in _disposables)
            {
                d.Dispose();
            }
        }

        private static MediaSessionInfo Session(string? title = "Humid", string appId = PwaApp, bool playing = true)
        {
            return new MediaSessionInfo(title, "Moody Good", appId, playing);
        }

        private MediaSessionInfo? _current;

        private (BrowserUiSource Source, FakeUi Ui) Build(MediaSessionInfo? session)
        {
            FakeUi ui = new();
            _current = session;
            BrowserUiSource source = new(ui, () => _current, _clock);
            _disposables.Add(source);
            return (source, ui);
        }

        [Fact]
        public void Nothing_is_read_and_nothing_is_reported_while_no_session_is_playing()
        {
            (BrowserUiSource source, FakeUi ui) = Build(null);

            source.Refresh();

            _ = source.Active.Should().BeNull();
            _ = ui.WindowCalls.Should().Be(0, "the accessibility tree is not touched without a browser session");
        }

        [Fact]
        public void A_native_players_session_is_left_alone()
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session(appId: "Spotify.exe"));
            ui.Windows.Add(FakeWindow.YouTubeMusic("Humid"));

            source.Refresh();

            _ = source.Active.Should().BeNull();
            _ = ui.WindowCalls.Should().Be(0);
        }

        [Fact]
        public void A_session_with_no_title_cannot_be_matched_to_a_window()
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session(title: ""));
            ui.Windows.Add(FakeWindow.YouTubeMusic("Humid"));

            source.Refresh();

            _ = source.Active.Should().BeNull();
        }

        [Fact]
        public void The_window_whose_player_shows_the_track_is_the_one_read()
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session("Humid"));
            FakeWindow other = FakeWindow.YouTubeMusic("Some other song", like: UiToggleState.On);
            FakeWindow mine = FakeWindow.YouTubeMusic("Humid", dislike: UiToggleState.On);
            ui.Windows.AddRange([other, mine]);

            source.Refresh();

            _ = source.Active!.Rating.Should().Be(BrowserRating.Disliked);
            _ = other.ToggleCalls.Should().Be(0, "a window showing a different track is not read any further");
        }

        [Fact]
        public void A_window_is_matched_by_its_player_even_when_its_title_does_not_name_the_track()
        {
            // Measured: YouTube Music sometimes leaves the page title as just "YouTube Music" while a track plays.
            (BrowserUiSource source, FakeUi ui) = Build(Session("Humid"));
            ui.Windows.Add(FakeWindow.YouTubeMusic("Humid", like: UiToggleState.On, windowTitle: "YouTube Music - Personal - Microsoft Edge"));

            source.Refresh();

            _ = source.Active!.Rating.Should().Be(BrowserRating.Liked);
        }

        [Theory]
        [InlineData("Humid | YouTube Music", "Humid")]
        [InlineData("humid", "HUMID")]
        [InlineData("Humid", "Humid | YouTube Music")]
        public void The_track_is_matched_ignoring_the_site_suffix_and_case(string sessionTitle, string playerTitle)
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session(sessionTitle));
            ui.Windows.Add(FakeWindow.YouTubeMusic("anything", playerTitle: playerTitle));

            source.Refresh();

            _ = source.Active.Should().NotBeNull();
        }

        [Fact]
        public void Windows_that_have_nothing_to_do_with_YouTube_Music_are_never_queried()
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session("Humid"));
            FakeWindow mail = new("Inbox - Mail - Microsoft Edge");
            FakeWindow editor = new("README.md - MosaicShell - Visual Studio Code");
            ui.Windows.AddRange([mail, editor, FakeWindow.YouTubeMusic("Humid")]);

            source.Refresh();

            _ = source.Active.Should().NotBeNull();
            _ = (mail.PlayerTitleCalls + editor.PlayerTitleCalls + mail.ToggleCalls + editor.ToggleCalls)
                .Should().Be(0, "only windows that could be the player are touched, so no other application is made to build an accessibility tree");
        }

        [Fact]
        public void A_window_that_shows_a_different_track_is_not_matched()
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session("Humid"));
            ui.Windows.Add(FakeWindow.YouTubeMusic("Another track"));

            source.Refresh();

            _ = source.Active.Should().BeNull("a background tab is not in the accessibility tree, so nothing is claimed for it");
        }

        [Fact]
        public void The_player_is_presented_with_its_rating_and_the_capabilities_that_follow()
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session("Humid", playing: false));
            ui.Windows.Add(FakeWindow.YouTubeMusic("Humid", like: UiToggleState.On));

            source.Refresh();

            BrowserPlayerSnapshot player = source.Active!;
            _ = player.Name.Should().Be("YouTube Music");
            _ = player.Title.Should().Be("Humid");
            _ = player.Artist.Should().Be("Moody Good");
            _ = player.State.Should().Be(BrowserPlaybackState.Paused);
            _ = player.Rating.Should().Be(BrowserRating.Liked);
            _ = player.Capabilities.Should().Be(BrowserMediaCapabilities.Rating | BrowserMediaCapabilities.Dislike);
            _ = player.CoverPng.Should().BeNull("Windows supplies the cover");
        }

        [Fact]
        public void A_page_without_the_like_buttons_reports_nothing()
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session("Humid"));
            ui.Windows.Add(new FakeWindow("Humid - YouTube - Microsoft Edge"));

            source.Refresh();

            _ = source.Active.Should().BeNull();
        }

        [Fact]
        public void A_state_that_cannot_be_right_reports_nothing_rather_than_a_guess()
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session("Humid"));
            ui.Windows.Add(FakeWindow.YouTubeMusic("Humid", like: UiToggleState.On, dislike: UiToggleState.On));

            source.Refresh();

            _ = source.Active.Should().BeNull();
        }

        [Fact]
        public void Changed_is_raised_when_the_rating_changes_and_not_when_it_does_not()
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session("Humid"));
            FakeWindow window = FakeWindow.YouTubeMusic("Humid");
            ui.Windows.Add(window);
            source.Refresh();
            int changes = 0;
            source.Changed += (_, _) => changes++;

            source.Refresh();
            _ = changes.Should().Be(0);

            window.Like = UiToggleState.On;
            source.Refresh();
            _ = changes.Should().Be(1);

            ui.Windows.Clear();
            source.Refresh();
            _ = changes.Should().Be(2, "the window going away is a change too");
            _ = source.Active.Should().BeNull();
        }

        [Fact]
        public void The_window_is_polled_on_a_timer_until_the_source_is_disposed()
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session("Humid"));
            ui.Windows.Add(FakeWindow.YouTubeMusic("Humid"));

            _clock.Advance(BrowserUiSource.PollInterval * 3);
            int polled = ui.WindowCalls;
            source.Dispose();
            _clock.Advance(BrowserUiSource.PollInterval * 3);

            _ = polled.Should().BeGreaterThanOrEqualTo(3, "it polls repeatedly, the first time straight away");
            _ = ui.WindowCalls.Should().Be(polled);
        }

        [Fact]
        public void The_rating_of_the_previous_track_is_never_shown_for_the_next_one()
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session("Humid"));
            FakeWindow window = FakeWindow.YouTubeMusic("Humid", like: UiToggleState.On);
            ui.Windows.Add(window);
            source.Refresh();
            _ = source.Active!.Rating.Should().Be(BrowserRating.Liked);

            _current = Session("Walk Away");

            _ = source.Active.Should().BeNull("the page has not been read for the new track yet");
        }

        [Fact]
        public void A_track_change_is_rechecked_soon_rather_than_at_the_next_poll()
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session("Humid"));
            ui.Windows.Add(FakeWindow.YouTubeMusic("Humid", like: UiToggleState.On));
            source.Refresh();
            ui.Windows.Clear();
            ui.Windows.Add(FakeWindow.YouTubeMusic("Walk Away"));
            _current = Session("Walk Away");
            int changes = 0;
            source.Changed += (_, _) => changes++;

            _ = source.Active;
            _clock.Advance(TimeSpan.FromMilliseconds(600));

            _ = source.Active!.Rating.Should().Be(BrowserRating.None);
            _ = source.Active.Title.Should().Be("Walk Away");
            _ = changes.Should().BeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public void Repeated_reads_during_a_track_change_do_not_keep_postponing_the_recheck()
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session("Humid"));
            ui.Windows.Add(FakeWindow.YouTubeMusic("Humid"));
            _clock.Advance(TimeSpan.FromMilliseconds(1)); // the immediate first poll, so the next one is 2 s away
            ui.Windows.Clear();
            ui.Windows.Add(FakeWindow.YouTubeMusic("Walk Away"));
            _current = Session("Walk Away");

            _ = source.Active;
            _clock.Advance(TimeSpan.FromMilliseconds(200));
            _ = source.Active;
            _clock.Advance(TimeSpan.FromMilliseconds(200));

            _ = source.Active.Should().NotBeNull("the composite reads Active many times, and that must not push the recheck back");
        }

        [Fact]
        public async Task A_slow_browser_never_stalls_a_reader_of_Active()
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session("Humid"));
            ui.Windows.Add(FakeWindow.YouTubeMusic("Humid"));
            source.Refresh();
            ui.Hold = new ManualResetEventSlim(false);
            Task slowRefresh = Task.Run(source.Refresh);
            _ = ui.Entered.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

            Task<BrowserPlayerSnapshot?> read = Task.Run(() => source.Active);
            Task first = await Task.WhenAny(read, Task.Delay(TimeSpan.FromSeconds(2)));
            ui.Hold.Set();
            await slowRefresh;

            _ = first.Should().BeSameAs(read, "the flyout reads Active on its UI thread and must not wait for the browser");
        }

        [Fact]
        public async Task A_refresh_that_arrives_while_one_is_still_running_is_skipped()
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session("Humid"));
            ui.Windows.Add(FakeWindow.YouTubeMusic("Humid"));
            ui.Hold = new ManualResetEventSlim(false);
            Task first = Task.Run(source.Refresh);
            _ = ui.Entered.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
            int callsWhileRunning = ui.WindowCalls;

            source.Refresh();

            _ = ui.WindowCalls.Should().Be(callsWhileRunning, "a second read of a browser that is already being read only piles up");
            ui.Hold.Set();
            await first;
        }

        [Fact]
        public void A_failure_reading_the_tree_is_swallowed_and_recovered_from()
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session("Humid"));
            ui.Windows.Add(FakeWindow.YouTubeMusic("Humid"));
            ui.Throw = true;

            Action refresh = source.Refresh;

            _ = refresh.Should().NotThrow();
            _ = source.Active.Should().BeNull();

            ui.Throw = false;
            source.Refresh();
            _ = source.Active.Should().NotBeNull();
        }

        [Theory]
        [InlineData(UiToggleState.Off, UiToggleState.Off, true, "like")]
        [InlineData(UiToggleState.On, UiToggleState.Off, true, null)]
        [InlineData(UiToggleState.Off, UiToggleState.On, true, "like")]
        [InlineData(UiToggleState.On, UiToggleState.Off, false, "like")]
        [InlineData(UiToggleState.Off, UiToggleState.On, false, null)]
        public async Task Liking_presses_the_like_button_once_or_not_at_all(UiToggleState like, UiToggleState dislike, bool liked, string? expected)
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session("Humid"));
            FakeWindow window = FakeWindow.YouTubeMusic("Humid", like, dislike);
            ui.Windows.Add(window);

            await source.SetLikedAsync(liked);

            _ = window.Presses.Should().Equal(expected is null ? [] : [expected]);
        }

        [Theory]
        [InlineData(UiToggleState.Off, UiToggleState.Off, true, "dislike")]
        [InlineData(UiToggleState.Off, UiToggleState.On, true, null)]
        [InlineData(UiToggleState.On, UiToggleState.Off, true, "dislike")]
        [InlineData(UiToggleState.Off, UiToggleState.On, false, "dislike")]
        [InlineData(UiToggleState.On, UiToggleState.Off, false, null)]
        public async Task Disliking_presses_the_dislike_button_once_or_not_at_all(UiToggleState like, UiToggleState dislike, bool disliked, string? expected)
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session("Humid"));
            FakeWindow window = FakeWindow.YouTubeMusic("Humid", like, dislike);
            ui.Windows.Add(window);

            await source.SetDislikedAsync(disliked);

            _ = window.Presses.Should().Equal(expected is null ? [] : [expected]);
        }

        [Fact]
        public async Task A_command_acts_on_the_state_now_not_the_state_at_the_last_poll()
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session("Humid"));
            FakeWindow window = FakeWindow.YouTubeMusic("Humid");
            ui.Windows.Add(window);
            source.Refresh();
            window.Like = UiToggleState.On;

            await source.SetLikedAsync(true);

            _ = window.Presses.Should().BeEmpty("the track had already been liked in the page");
        }

        [Fact]
        public async Task A_command_with_no_matching_window_presses_nothing()
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session("Humid"));
            FakeWindow window = FakeWindow.YouTubeMusic("Somewhere else");
            ui.Windows.Add(window);

            await source.SetLikedAsync(true);

            _ = window.Presses.Should().BeEmpty();
        }

        [Fact]
        public async Task The_new_state_is_read_back_shortly_after_a_command()
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session("Humid"));
            FakeWindow window = FakeWindow.YouTubeMusic("Humid");
            ui.Windows.Add(window);
            source.Refresh();
            int changes = 0;
            source.Changed += (_, _) => changes++;

            await source.SetLikedAsync(true);
            _clock.Advance(TimeSpan.FromMilliseconds(500));

            _ = source.Active!.Rating.Should().Be(BrowserRating.Liked);
            _ = changes.Should().BeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task A_failing_press_is_swallowed()
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session("Humid"));
            FakeWindow window = FakeWindow.YouTubeMusic("Humid");
            window.PressThrows = true;
            ui.Windows.Add(window);

            Func<Task> act = async () => await source.SetLikedAsync(true);

            _ = await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task Shuffle_and_repeat_are_not_offered_and_do_nothing()
        {
            (BrowserUiSource source, FakeUi ui) = Build(Session("Humid"));
            FakeWindow window = FakeWindow.YouTubeMusic("Humid");
            ui.Windows.Add(window);
            source.Refresh();

            await source.ToggleShuffleAsync();
            await source.ToggleRepeatAsync();

            _ = window.Presses.Should().BeEmpty();
            _ = source.Active!.Capabilities.Should().NotHaveFlag(BrowserMediaCapabilities.Shuffle);
        }

        [Fact]
        public void Through_the_composite_the_flyout_gets_the_real_rating_without_any_extension()
        {
            FakeSmtc smtc = new();
            FakeUi ui = new();
            ui.Windows.Add(FakeWindow.YouTubeMusic("Humid", dislike: UiToggleState.On));
            BrowserUiSource source = new(ui, () => smtc.Current, _clock);
            using CompositeMediaSessionService composite = new(smtc, source);

            smtc.Set(Session("Humid | YouTube Music"));
            source.Refresh();

            _ = composite.Current!.LikeRating.Should().Be(MediaLikePolicy.Disliked);
            _ = composite.Current.Capabilities.Should().Be(BrowserMediaCapabilities.Rating | BrowserMediaCapabilities.Dislike);
        }

        private sealed class FakeUi : IBrowserUi
        {
            private int _windowCalls;

            public List<IUiWindow> Windows { get; } = [];
            public int WindowCalls => _windowCalls;
            public bool Throw { get; set; }

            /// <summary>When set, a read of the windows waits here, like a browser that is slow to answer.</summary>
            public ManualResetEventSlim? Hold { get; set; }

            public ManualResetEventSlim Entered { get; } = new(false);

            IReadOnlyList<IUiWindow> IBrowserUi.Windows()
            {
                _ = Interlocked.Increment(ref _windowCalls);
                Entered.Set();
                _ = Hold?.Wait(TimeSpan.FromSeconds(10));
                return Throw ? throw new InvalidOperationException("the element is gone") : [.. Windows];
            }
        }

        private sealed class FakeWindow(string title) : IUiWindow
        {
            public string Title { get; } = title;
            public UiToggleState Like { get; set; }
            public UiToggleState Dislike { get; set; }
            public bool HasControls { get; init; }
            public bool PressThrows { get; set; }
            public int ToggleCalls { get; private set; }
            public List<string> Presses { get; } = [];

            public string? PlayerTitle { get; init; }
            public int PlayerTitleCalls { get; private set; }

            public static FakeWindow YouTubeMusic(string track, UiToggleState like = UiToggleState.Off, UiToggleState dislike = UiToggleState.Off, string? windowTitle = null, string? playerTitle = null)
            {
                return new FakeWindow(windowTitle ?? (track + " | YouTube Music - Personal - Microsoft Edge"))
                {
                    HasControls = true,
                    Like = like,
                    Dislike = dislike,
                    PlayerTitle = playerTitle ?? track,
                };
            }

            string? IUiWindow.PlayerTitle()
            {
                PlayerTitleCalls++;
                return PlayerTitle;
            }

            public IReadOnlyList<UiToggle> Toggles()
            {
                ToggleCalls++;
                return HasControls
                    ?
                    [
                        new UiToggle("autoplay style-scope ytmusic-player-page", 0, UiToggleState.On, () => Presses.Add("autoplay")),
                        new UiToggle(Controls, 0, Like, () => PressButton("like", isLike: true)),
                        new UiToggle(Controls, 1, Dislike, () => PressButton("dislike", isLike: false)),
                    ]
                    : [];
            }

            /// <summary>Pressing behaves as on the site: it toggles that button and switches off the other one.</summary>
            private void PressButton(string name, bool isLike)
            {
                if (PressThrows)
                {
                    throw new InvalidOperationException("the element is gone");
                }

                Presses.Add(name);
                if (isLike)
                {
                    bool on = Like != UiToggleState.On;
                    Like = on ? UiToggleState.On : UiToggleState.Off;
                    Dislike = on ? UiToggleState.Off : Dislike;
                }
                else
                {
                    bool on = Dislike != UiToggleState.On;
                    Dislike = on ? UiToggleState.On : UiToggleState.Off;
                    Like = on ? UiToggleState.Off : Like;
                }
            }
        }
    }
}
