using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.BrowserBridge;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Track change on the YouTube Music web app, desktop report 2026-10-04: the flyout title flashed
    /// through the "YouTube Music" placeholder and the previous title before the new title and artist,
    /// while the art (Grout only) changed once. The log (run of 17:35) showed, per skip: Grout reports
    /// the new title and artist first; Windows (SMTC) still has the old page title, then the bare
    /// "YouTube Music" placeholder, then "New | YouTube Music" 100 to 300 ms later. The merge took the
    /// SMTC title whenever the two disagreed, so it showed old, placeholder, then new, and dropped the
    /// artist each time. Grout is the in-page reader and the source to trust for its own site.
    /// </summary>
    public class BrowserTrackChangeMetadataTests
    {
        private const string YtmApp = "music.youtube.com-5929F88E_vezhnr0wkvrcy!App";

        [Theory]
        [InlineData("YouTube Music", true)]
        [InlineData("YouTube", true)]
        [InlineData(" youtube music ", true)]
        [InlineData("Pyro", false)]
        [InlineData("Pyro | YouTube Music", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void The_bare_site_name_is_a_placeholder_not_a_title(string? title, bool placeholder)
        {
            _ = MediaTitleNormalizer.IsSitePlaceholder(title).Should().Be(placeholder);
        }

        [Theory]
        [InlineData(YtmApp, "YouTube Music")]
        [InlineData("www.youtube.com-1A2B3C_xyz!App", "YouTube")]
        [InlineData("open.spotify.com-AB12_x!App", "Spotify")]
        [InlineData("MSEdge", null)]
        [InlineData("Spotify.exe", null)]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void An_installed_web_app_names_its_site_and_a_browser_does_not(string? appId, string? site)
        {
            _ = BrowserSessionPolicy.SiteNameOfApp(appId).Should().Be(site);
        }

        [Fact]
        public void A_placeholder_smtc_title_yields_to_the_extensions_title_and_artist()
        {
            MediaSessionInfo smtc = new("YouTube Music", null, YtmApp, true, null, 1, 109);
            BrowserPlayerSnapshot browser = Ytm("Pyro", "Basstripper");

            MediaSessionInfo merged = CompositeMediaSessionService.Merge(smtc, browser)!;

            _ = merged.Title.Should().Be("Pyro");
            _ = merged.Artist.Should().Be("Basstripper");
        }

        [Fact]
        public void A_placeholder_stays_when_there_is_nothing_better()
        {
            MediaSessionInfo smtc = new("YouTube Music", null, YtmApp, true, null, 1, 109);

            _ = CompositeMediaSessionService.Merge(smtc, null)!.Title.Should().Be("YouTube Music");
        }

        [Fact]
        public void When_the_extension_moves_first_the_flyout_never_shows_the_old_title_or_the_placeholder()
        {
            SteppingSmtc smtc = new();
            SteppingBrowser browser = new();
            using CompositeMediaSessionService composite = new(smtc, browser);
            List<(string? Title, string? Artist)> shown = [];
            composite.Changed += (_, _) => shown.Add((composite.Current?.Title, composite.Current?.Artist));

            browser.Set(Ytm("One More Time", "NECROLX"));
            smtc.Set(new MediaSessionInfo("One More Time | YouTube Music", null, YtmApp, true, null, 100, 150));
            shown.Clear();

            browser.Set(Ytm("HORIZON", "Msand"));
            _ = composite.Current!.Title.Should().Be("HORIZON", "the extension reported the new track first");
            _ = composite.Current.Artist.Should().Be("Msand");

            smtc.Set(new MediaSessionInfo("YouTube Music", null, YtmApp, true, null, 0, 0));
            smtc.Set(new MediaSessionInfo("HORIZON | YouTube Music", null, YtmApp, true, null, 1, 180));

            _ = composite.Current.Title.Should().Be("HORIZON");
            _ = composite.Current.Artist.Should().Be("Msand");
            _ = shown.Should().OnlyContain(s => s.Title == "HORIZON" && s.Artist == "Msand",
                "no update may carry the old title, the placeholder or a missing artist");
        }

        [Fact]
        public void When_windows_moves_first_its_new_title_is_shown_not_the_extensions_old_one()
        {
            SteppingSmtc smtc = new();
            SteppingBrowser browser = new();
            using CompositeMediaSessionService composite = new(smtc, browser);

            browser.Set(Ytm("Old Track", "Old Artist"));
            smtc.Set(new MediaSessionInfo("Old Track | YouTube Music", null, YtmApp, true, null, 100, 150));

            smtc.Set(new MediaSessionInfo("New Track | YouTube Music", null, YtmApp, true, null, 0.5, 180));
            _ = composite.Current!.Title.Should().Be("New Track");
            _ = composite.Current.Artist.Should().BeNull("the extension's artist belongs to the old track");

            browser.Set(Ytm("New Track", "New Artist"));
            _ = composite.Current.Title.Should().Be("New Track");
            _ = composite.Current.Artist.Should().Be("New Artist");
        }

        [Fact]
        public void The_tab_of_the_site_windows_is_playing_wins_over_another_sites_paused_tab()
        {
            // Mid-skip the YouTube Music tab briefly reports stopped; a paused YouTube video in another
            // tab must not take over the flyout (it blanked the artist and showed its title).
            DateTimeOffset now = new(2026, 10, 4, 17, 35, 0, TimeSpan.Zero);
            BrowserSessionEntry ytm = Entry(1, "https://music.youtube.com", "Baile Rally", BrowserPlaybackState.Stopped, now);
            BrowserSessionEntry youtube = Entry(2, "https://www.youtube.com", "Australia May Have Just Solved The Housing Crisis", BrowserPlaybackState.Paused, now.AddMinutes(-5));

            BrowserSessionEntry? chosen = BrowserSessionSelector.Select([youtube, ytm], now, "Baile Rally | YouTube Music", "YouTube Music");

            _ = chosen!.Report.Title.Should().Be("Baile Rally");
        }

        [Fact]
        public void Without_a_known_site_the_ranking_is_unchanged()
        {
            DateTimeOffset now = new(2026, 10, 4, 17, 35, 0, TimeSpan.Zero);
            BrowserSessionEntry stopped = Entry(1, "https://music.youtube.com", "Ended", BrowserPlaybackState.Stopped, now);
            BrowserSessionEntry paused = Entry(2, "https://www.youtube.com", "Paused", BrowserPlaybackState.Paused, now.AddMinutes(-5));

            _ = BrowserSessionSelector.Select([stopped, paused], now, null, null)!.Report.Title.Should().Be("Paused");
        }

        private static readonly DateTimeOffset Heard = new(2026, 10, 4, 17, 35, 0, TimeSpan.Zero);

        private static BrowserSessionEntry Entry(int tab, string origin, string title, BrowserPlaybackState state, DateTimeOffset at)
        {
            BrowserSessionReport report = new(
                tab, 1, origin, title, "Artist", "", [], state, state == BrowserPlaybackState.Playing,
                BrowserRating.None, BrowserMediaCapabilities.None);
            // Updated at that time, heard just now: the connection is alive.
            return new BrowserSessionEntry(1, report, at, Heard);
        }

        private static BrowserPlayerSnapshot Ytm(string title, string artist)
        {
            return new BrowserPlayerSnapshot
            {
                Title = title,
                Artist = artist,
                Name = "YouTube Music",
                State = BrowserPlaybackState.Playing,
            };
        }

        private sealed class SteppingSmtc : IMediaSessionService
        {
            public MediaSessionInfo? Current { get; private set; }
            public event EventHandler? Changed;
            public event EventHandler? ProgressChanged { add { } remove { } }

            public void Set(MediaSessionInfo info)
            {
                Current = info;
                Changed?.Invoke(this, EventArgs.Empty);
            }

            public void PumpTimeline() { }
            public Task PlayPauseAsync() { return Task.CompletedTask; }
            public Task NextAsync() { return Task.CompletedTask; }
            public Task PreviousAsync() { return Task.CompletedTask; }
            public Task SeekAsync(double positionSeconds) { return Task.CompletedTask; }
            public Task ToggleShuffleAsync() { return Task.CompletedTask; }
            public Task ToggleRepeatAsync() { return Task.CompletedTask; }
            public Task ToggleLikeAsync(bool wantLiked) { return Task.CompletedTask; }
            public Task ToggleDislikeAsync(bool wantDisliked) { return Task.CompletedTask; }
            public void Dispose() { }
        }

        private sealed class SteppingBrowser : IBrowserMediaSource
        {
            public BrowserPlayerSnapshot? Active { get; private set; }
            public event EventHandler? Changed;

            public void Set(BrowserPlayerSnapshot snapshot)
            {
                Active = snapshot;
                Changed?.Invoke(this, EventArgs.Empty);
            }

            public Task SetLikedAsync(bool liked) { return Task.CompletedTask; }
            public Task SetDislikedAsync(bool disliked) { return Task.CompletedTask; }
            public Task ToggleShuffleAsync() { return Task.CompletedTask; }
            public Task ToggleRepeatAsync() { return Task.CompletedTask; }
            public void Dispose() { }
        }
    }
}
