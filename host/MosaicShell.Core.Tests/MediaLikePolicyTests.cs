using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.WebNowPlaying;

namespace MosaicShell.Core.Tests
{
    public class MediaLikePolicyTests
    {
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
        [InlineData(5, true)]
        [InlineData(1, false)]
        [InlineData(0, false)]
        public void MaySendUnlikeRating_only_when_host_knows_liked(int hostRating, bool expected)
        {
            _ = MediaLikePolicy.MaySendUnlikeRating(hostRating).Should().Be(expected);
        }

        [Fact]
        public void LikeRequestRating_is_five()
        {
            _ = MediaLikePolicy.DefaultLikeRequestRating.Should().Be(5);
        }

        [Theory]
        [InlineData("YouTube Music", true, 0, 1)]
        [InlineData("YouTube Music", true, 5, 1)]
        [InlineData("YouTube Music", false, 5, 1)]
        [InlineData("YouTube Music", false, 0, 0)]
        [InlineData("Spotify", true, 0, 5)]
        [InlineData("Spotify", false, 5, 0)]
        public void ResolveLikeRequestRating_matches_player_semantics(
            string player,
            bool wantLiked,
            int hostRating,
            int expected)
        {
            _ = MediaLikePolicy.ResolveLikeRequestRating(player, wantLiked, hostRating).Should().Be(expected);
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

        [Theory]
        [InlineData("music.youtube.com-x!App", null, true)]
        [InlineData("Spotify.exe", null, false)]
        [InlineData(null, "YouTube Music", true)]
        [InlineData(null, "Spotify", false)]
        public void SupportsDislike_for_youtube_music_only(string? appId, string? playerName, bool expected)
        {
            _ = MediaLikePolicy.SupportsDislike(appId, playerName).Should().Be(expected);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(5, false)]
        [InlineData(0, false)]
        public void MaySendUndislikeRating_only_when_host_knows_disliked(int hostRating, bool expected)
        {
            _ = MediaLikePolicy.MaySendUndislikeRating(hostRating).Should().Be(expected);
        }

        [Theory]
        [InlineData("YouTube Music", true, 0, 5)]
        [InlineData("YouTube Music", true, 5, 0)]
        [InlineData("YouTube Music", true, 1, 5)]
        [InlineData("YouTube Music", false, 1, 5)]
        [InlineData("YouTube Music", false, 0, 0)]
        [InlineData("Spotify", true, 0, 1)]
        [InlineData("Spotify", false, 1, 0)]
        public void ResolveDislikeRequestRating_matches_player_semantics(
            string player,
            bool wantDisliked,
            int hostRating,
            int expected)
        {
            _ = MediaLikePolicy.ResolveDislikeRequestRating(player, wantDisliked, hostRating).Should().Be(expected);
        }
    }

    public class CompositeMediaLikeRatingTests
    {
        [Fact]
        public void Merge_attaches_wnp_rating_for_browser_sessions()
        {
            MediaSessionInfo smtc = new(
                "Song", "Artist", "music.youtube.com-x!App", true, null, 10, 180);
            WnpPlayerSnapshot wnp = new()
            {
                Title = "Song",
                Rating = 5,
            };

            MediaSessionInfo? merged = CompositeMediaSessionService.Merge(smtc, wnp);

            _ = merged!.LikeRating.Should().Be(5);
        }

        [Fact]
        public void Merge_omits_like_rating_for_native_smtc_only()
        {
            MediaSessionInfo smtc = new("Song", "Artist", "Spotify.exe", true, null, 10, 180);

            MediaSessionInfo? merged = CompositeMediaSessionService.Merge(smtc, wnp: null);

            _ = merged!.LikeRating.Should().BeNull();
        }
    }
}
