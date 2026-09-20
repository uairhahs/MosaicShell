using FluentAssertions;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    public class MediaLikePolicyTests
    {
        [Theory]
        [InlineData(BrowserRating.None, 0)]
        [InlineData(BrowserRating.Liked, 5)]
        [InlineData(BrowserRating.Disliked, 1)]
        public void ToLikeRating_maps_a_browser_rating_to_the_value_the_flyout_icons_read(BrowserRating rating, int expected)
        {
            _ = MediaLikePolicy.ToLikeRating(rating).Should().Be(expected);
        }

        [Theory]
        [InlineData(5, true)]
        [InlineData(1, false)]
        [InlineData(0, false)]
        [InlineData(null, false)]
        public void ShouldShowLikedHeart(int? rating, bool expected)
        {
            _ = MediaLikePolicy.ShouldShowLikedHeart(rating).Should().Be(expected);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(5, false)]
        [InlineData(0, false)]
        [InlineData(null, false)]
        public void ShouldShowDislikedThumb(int? rating, bool expected)
        {
            _ = MediaLikePolicy.ShouldShowDislikedThumb(rating).Should().Be(expected);
        }
    }

    public class CompositeMediaLikeRatingTests
    {
        [Fact]
        public void Merge_attaches_the_browser_rating_for_browser_sessions()
        {
            MediaSessionInfo smtc = new(
                "Song", "Artist", "music.youtube.com-x!App", true, null, 10, 180);
            BrowserPlayerSnapshot browser = new()
            {
                Title = "Song",
                Rating = BrowserRating.Liked,
            };

            MediaSessionInfo? merged = CompositeMediaSessionService.Merge(smtc, browser);

            _ = merged!.LikeRating.Should().Be(5);
        }

        [Fact]
        public void Merge_omits_like_rating_for_native_smtc_only()
        {
            MediaSessionInfo smtc = new("Song", "Artist", "Spotify.exe", true, null, 10, 180);

            MediaSessionInfo? merged = CompositeMediaSessionService.Merge(smtc, browser: null);

            _ = merged!.LikeRating.Should().BeNull();
        }
    }
}
