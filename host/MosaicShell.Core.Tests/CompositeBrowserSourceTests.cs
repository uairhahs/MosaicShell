using FluentAssertions;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The composite depends on IBrowserMediaSource and nothing behind it: it picks the active source in order of
    /// preference, sends a command only to the active source and only when that player declares the capability,
    /// and carries the capabilities onto the merged session.
    /// </summary>
    public class CompositeBrowserSourceTests
    {
        private const string PwaApp = "music.youtube.com-5929F88E_v";

        private static BrowserPlayerSnapshot Player(string title, BrowserMediaCapabilities capabilities)
        {
            return new BrowserPlayerSnapshot
            {
                Name = "YouTube Music",
                Title = title,
                Artist = "Artist",
                State = BrowserPlaybackState.Playing,
                Capabilities = capabilities,
            };
        }

        [Fact]
        public async Task A_like_goes_to_the_active_source_when_the_player_declares_rating()
        {
            RecordingSource source = new() { Active = Player("T", BrowserMediaCapabilities.Rating) };
            using CompositeMediaSessionService composite = new(new FakeSmtc(), source);

            await composite.ToggleLikeAsync(true);

            _ = source.Calls.Should().Equal("like:True");
        }

        [Fact]
        public async Task A_like_is_not_sent_when_the_active_player_lacks_the_capability()
        {
            RecordingSource source = new() { Active = Player("T", BrowserMediaCapabilities.None) };
            using CompositeMediaSessionService composite = new(new FakeSmtc(), source);

            await composite.ToggleLikeAsync(true);

            _ = source.Calls.Should().BeEmpty();
        }

        [Fact]
        public async Task A_dislike_needs_the_dislike_capability_not_just_rating()
        {
            RecordingSource likeOnly = new() { Active = Player("T", BrowserMediaCapabilities.Rating) };
            using CompositeMediaSessionService composite = new(new FakeSmtc(), likeOnly);

            await composite.ToggleDislikeAsync(true);

            _ = likeOnly.Calls.Should().BeEmpty();

            RecordingSource full = new() { Active = Player("T", BrowserMediaCapabilities.Rating | BrowserMediaCapabilities.Dislike) };
            using CompositeMediaSessionService composite2 = new(new FakeSmtc(), full);

            await composite2.ToggleDislikeAsync(true);

            _ = full.Calls.Should().Equal("dislike:True");
        }

        [Fact]
        public async Task Shuffle_and_repeat_follow_their_own_capabilities()
        {
            RecordingSource source = new() { Active = Player("T", BrowserMediaCapabilities.Shuffle) };
            using CompositeMediaSessionService composite = new(new FakeSmtc(), source);

            await composite.ToggleShuffleAsync();
            await composite.ToggleRepeatAsync();

            _ = source.Calls.Should().Equal("shuffle");
        }

        [Fact]
        public async Task A_command_goes_only_to_the_source_that_has_the_active_player()
        {
            RecordingSource idle = new() { Active = null };
            RecordingSource active = new() { Active = Player("T", BrowserMediaCapabilities.Rating) };
            using CompositeMediaSessionService composite = new(new FakeSmtc(), idle, active);

            await composite.ToggleLikeAsync(true);

            _ = idle.Calls.Should().BeEmpty();
            _ = active.Calls.Should().Equal("like:True");
        }

        [Fact]
        public void The_first_source_with_an_active_player_wins_over_a_later_one()
        {
            FakeSmtc smtc = new();
            RecordingSource preferred = new() { Active = Player("Preferred title", BrowserMediaCapabilities.None) };
            RecordingSource other = new() { Active = Player("Other title", BrowserMediaCapabilities.None) };
            using CompositeMediaSessionService composite = new(smtc, preferred, other);

            smtc.Set(new MediaSessionInfo("Preferred title | YouTube Music", null, PwaApp, true));

            _ = composite.Current!.Title.Should().Be("Preferred title");
        }

        [Fact]
        public void A_later_source_is_used_when_the_preferred_one_has_nothing()
        {
            FakeSmtc smtc = new();
            RecordingSource preferred = new() { Active = null };
            RecordingSource other = new() { Active = Player("Other title", BrowserMediaCapabilities.None) };
            using CompositeMediaSessionService composite = new(smtc, preferred, other);

            smtc.Set(new MediaSessionInfo("Other title | YouTube Music", null, PwaApp, true));

            _ = composite.Current!.Title.Should().Be("Other title");
            _ = composite.Current.Artist.Should().Be("Artist");
        }

        [Fact]
        public void A_browser_sessions_capabilities_reach_the_merged_session()
        {
            FakeSmtc smtc = new();
            RecordingSource source = new()
            {
                Active = Player("Song", BrowserMediaCapabilities.Rating | BrowserMediaCapabilities.Dislike),
            };
            using CompositeMediaSessionService composite = new(smtc, source);

            smtc.Set(new MediaSessionInfo("Song | YouTube Music", null, PwaApp, true));

            _ = composite.Current!.Capabilities.Should().Be(BrowserMediaCapabilities.Rating | BrowserMediaCapabilities.Dislike);
        }

        [Fact]
        public void A_native_app_session_gets_no_capabilities_even_when_a_browser_player_exists()
        {
            FakeSmtc smtc = new();
            RecordingSource source = new() { Active = Player("Browser song", BrowserMediaCapabilities.Rating) };
            using CompositeMediaSessionService composite = new(smtc, source);

            smtc.Set(new MediaSessionInfo("Native song", "Native artist", "Spotify.exe", true));

            _ = composite.Current!.Capabilities.Should().Be(BrowserMediaCapabilities.None);
        }

        [Fact]
        public void A_change_from_a_browser_source_rebuilds_the_session()
        {
            FakeSmtc smtc = new();
            RecordingSource source = new() { Active = null };
            using CompositeMediaSessionService composite = new(smtc, source);
            smtc.Set(new MediaSessionInfo("Song | YouTube Music", null, PwaApp, true));
            _ = composite.Current!.Artist.Should().BeNull();

            source.Active = Player("Song", BrowserMediaCapabilities.None);
            source.RaiseChanged();

            _ = composite.Current!.Artist.Should().Be("Artist");
        }

        [Fact]
        public void Disposing_the_composite_disposes_every_source()
        {
            RecordingSource first = new();
            RecordingSource second = new();
            CompositeMediaSessionService composite = new(new FakeSmtc(), first, second);

            composite.Dispose();

            _ = first.Disposed.Should().BeTrue();
            _ = second.Disposed.Should().BeTrue();
        }

        private sealed class RecordingSource : IBrowserMediaSource
        {
            public BrowserPlayerSnapshot? Active { get; set; }
            public List<string> Calls { get; } = [];
            public bool Disposed { get; private set; }
            public event EventHandler? Changed;

            public void RaiseChanged()
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }

            public Task SetLikedAsync(bool liked)
            {
                Calls.Add($"like:{liked}");
                return Task.CompletedTask;
            }

            public Task SetDislikedAsync(bool disliked)
            {
                Calls.Add($"dislike:{disliked}");
                return Task.CompletedTask;
            }

            public Task ToggleShuffleAsync()
            {
                Calls.Add("shuffle");
                return Task.CompletedTask;
            }

            public Task ToggleRepeatAsync()
            {
                Calls.Add("repeat");
                return Task.CompletedTask;
            }

            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}
