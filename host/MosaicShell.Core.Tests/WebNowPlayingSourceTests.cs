using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.WebNowPlaying;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The bridge that presents the WebNowPlaying host as an IBrowserMediaSource. It exists so the rest of the app
    /// never sees WebNowPlaying types, and it is deleted with WebNowPlaying.
    /// </summary>
    public class WebNowPlayingSourceTests
    {
        [Fact]
        public void No_player_maps_to_no_snapshot()
        {
            _ = WebNowPlayingSource.ToBrowser(null).Should().BeNull();
        }

        [Fact]
        public void The_fields_the_flyout_reads_are_carried_over()
        {
            byte[] cover = new byte[64];
            WnpPlayerSnapshot wnp = new()
            {
                Name = "YouTube Music",
                Title = "Humid",
                Artist = "Moody Good",
                Album = "Album",
                State = WnpState.Playing,
                PositionSeconds = 17,
                DurationSeconds = 258,
                CoverPng = cover,
            };

            BrowserPlayerSnapshot browser = WebNowPlayingSource.ToBrowser(wnp)!;

            _ = browser.Name.Should().Be("YouTube Music");
            _ = browser.Title.Should().Be("Humid");
            _ = browser.Artist.Should().Be("Moody Good");
            _ = browser.Album.Should().Be("Album");
            _ = browser.PositionSeconds.Should().Be(17);
            _ = browser.DurationSeconds.Should().Be(258);
            _ = browser.CoverPng.Should().BeSameAs(cover);
            _ = browser.IsPlaying.Should().BeTrue();
        }

        [Theory]
        [InlineData(WnpState.Playing, BrowserPlaybackState.Playing)]
        [InlineData(WnpState.Paused, BrowserPlaybackState.Paused)]
        [InlineData(WnpState.Stopped, BrowserPlaybackState.Stopped)]
        public void Playback_state_is_mapped(WnpState state, BrowserPlaybackState expected)
        {
            _ = WebNowPlayingSource.ToBrowser(new WnpPlayerSnapshot { State = state })!.State.Should().Be(expected);
        }

        [Theory]
        [InlineData(0, BrowserRating.None)]
        [InlineData(5, BrowserRating.Liked)]
        [InlineData(1, BrowserRating.Disliked)]
        [InlineData(3, BrowserRating.None)]
        public void The_wire_rating_becomes_a_neutral_rating(int wire, BrowserRating expected)
        {
            _ = WebNowPlayingSource.ToBrowser(new WnpPlayerSnapshot { Rating = wire })!.Rating.Should().Be(expected);
        }

        [Fact]
        public void YouTube_Music_declares_dislike_as_well_as_like_shuffle_and_repeat()
        {
            BrowserMediaCapabilities caps = WebNowPlayingSource.ToBrowser(new WnpPlayerSnapshot { Name = "YouTube Music" })!.Capabilities;

            _ = caps.Should().HaveFlag(BrowserMediaCapabilities.Rating);
            _ = caps.Should().HaveFlag(BrowserMediaCapabilities.Dislike);
            _ = caps.Should().HaveFlag(BrowserMediaCapabilities.Shuffle);
            _ = caps.Should().HaveFlag(BrowserMediaCapabilities.Repeat);
        }

        [Fact]
        public void A_like_only_player_does_not_declare_dislike()
        {
            BrowserMediaCapabilities caps = WebNowPlayingSource.ToBrowser(new WnpPlayerSnapshot { Name = "Spotify" })!.Capabilities;

            _ = caps.Should().HaveFlag(BrowserMediaCapabilities.Rating);
            _ = caps.Should().NotHaveFlag(BrowserMediaCapabilities.Dislike);
        }
    }
}
