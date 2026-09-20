using FluentAssertions;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The Host has two ways to learn what a browser is playing. The extension reads the page itself, so it is the
    /// preferred one and works however the window sits; the accessibility tree needs nothing installed, but reading it
    /// makes the browser build that tree and only sees a window that is on screen. So the accessibility source is a
    /// fallback: it is asked to read only while the extension has no player to offer.
    /// </summary>
    public sealed class BrowserSourceStackTests
    {
        private static readonly MediaSessionInfo Playing = new("Humid", "Moody Good", "music.youtube.com-5929F88E_vezhnr0wkvrcy!App", true);

        [Fact]
        public void The_fallback_reads_when_the_extension_has_no_player()
        {
            StubSource extension = new();

            Func<MediaSessionInfo?> forFallback = BrowserSourceStack.FallbackSession(extension, () => Playing);

            _ = forFallback().Should().BeSameAs(Playing);
        }

        [Fact]
        public void The_fallback_is_given_no_session_while_the_extension_has_a_player()
        {
            StubSource extension = new()
            {
                Active = new BrowserPlayerSnapshot { Name = "YouTube Music", Title = "Humid", Artist = "Moody Good" },
            };

            Func<MediaSessionInfo?> forFallback = BrowserSourceStack.FallbackSession(extension, () => Playing);

            _ = forFallback().Should().BeNull("no other application should be made to build an accessibility tree for a rating the extension already has");
        }

        [Fact]
        public void The_fallback_resumes_when_the_extension_loses_its_player()
        {
            StubSource extension = new()
            {
                Active = new BrowserPlayerSnapshot { Name = "YouTube Music", Title = "Humid", Artist = "Moody Good" },
            };
            Func<MediaSessionInfo?> forFallback = BrowserSourceStack.FallbackSession(extension, () => Playing);
            _ = forFallback().Should().BeNull();

            extension.Active = null;

            _ = forFallback().Should().BeSameAs(Playing, "closing the browser must hand the reading back at once");
        }

        private sealed class StubSource : IBrowserMediaSource
        {
            public BrowserPlayerSnapshot? Active { get; set; }

            public event EventHandler? Changed
            {
                add { }
                remove { }
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
    }
}
