using FluentAssertions;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// One press of a flyout like or dislike button must reach the intended state from any starting state.
    /// A model of the stock WebNowPlaying 3.1.0 extension on current YouTube Music (upstream issue #52) drives
    /// the real policy through the host's press logic: the extension cannot read the state (every track looks
    /// unrated), so its helper clicks button [1] (Dislike) for a request of 5 and button [0] (Like) for 1 or 0.
    /// YouTube Music's own buttons toggle, and clicking one while the other is active switches to it.
    /// </summary>
    public class MediaLikeStockExtensionModelTests
    {
        private const string Player = "YouTube Music";

        public enum Track
        {
            None,
            Liked,
            Disliked,
        }

        private static int RatingOf(Track t)
        {
            return t switch
            {
                Track.Liked => MediaLikePolicy.Liked,
                Track.Disliked => MediaLikePolicy.Disliked,
                _ => MediaLikePolicy.Unrated,
            };
        }

        /// <summary>The extension's helper with an unreadable state: it always believes the track is unrated.</summary>
        private static Track StockExtensionClick(Track page, int requestedRating)
        {
            return requestedRating >= 3
                ? page == Track.Disliked ? Track.None : Track.Disliked
                : page == Track.Liked ? Track.None : Track.Liked;
        }

        /// <summary>Mirrors <c>TesseraFlyoutViewModel.ToggleLikeAsync</c> plus <c>TrySetLikeAsync</c>.</summary>
        private static (Track Page, int Host) PressHeart(Track page, int host)
        {
            bool wantLiked = host != MediaLikePolicy.Liked; // the icon is filled exactly when the host rating is liked
            if (!wantLiked && !MediaLikePolicy.MaySendUnlikeRating(host))
            {
                return (page, host);
            }

            int data = MediaLikePolicy.ResolveLikeRequestRating(Player, wantLiked, host);
            return (StockExtensionClick(page, data), wantLiked ? MediaLikePolicy.Liked : MediaLikePolicy.Unrated);
        }

        /// <summary>Mirrors <c>TesseraFlyoutViewModel.ToggleDislikeAsync</c> plus <c>TrySetDislikeAsync</c>.</summary>
        private static (Track Page, int Host) PressThumbDown(Track page, int host)
        {
            bool wantDisliked = host != MediaLikePolicy.Disliked;
            if (!wantDisliked && !MediaLikePolicy.MaySendUndislikeRating(host))
            {
                return (page, host);
            }

            int data = MediaLikePolicy.ResolveDislikeRequestRating(Player, wantDisliked, host);
            return (StockExtensionClick(page, data), wantDisliked ? MediaLikePolicy.Disliked : MediaLikePolicy.Unrated);
        }

        [Theory]
        [InlineData(Track.None, Track.Liked)]
        [InlineData(Track.Liked, Track.None)]
        [InlineData(Track.Disliked, Track.Liked)]
        public void One_heart_press_reaches_the_intended_state(Track start, Track expected)
        {
            (Track page, int host) = PressHeart(start, RatingOf(start));

            _ = page.Should().Be(expected);
            _ = host.Should().Be(RatingOf(expected), "the flyout icons follow the host rating");
        }

        [Theory]
        [InlineData(Track.None, Track.Disliked)]
        [InlineData(Track.Liked, Track.Disliked)]
        [InlineData(Track.Disliked, Track.None)]
        public void One_thumb_down_press_reaches_the_intended_state(Track start, Track expected)
        {
            (Track page, int host) = PressThumbDown(start, RatingOf(start));

            _ = page.Should().Be(expected, "a dislike on a liked track switches it, it does not just clear the like");
            _ = host.Should().Be(RatingOf(expected), "a disliked track must show a filled thumbs down");
        }

        [Fact]
        public void The_host_state_never_drifts_from_the_page_over_a_run_of_presses()
        {
            Track page = Track.None;
            int host = MediaLikePolicy.Unrated;
            bool[] presses = [true, false, false, true, true, false, true, false, false, false, true];

            foreach (bool heart in presses)
            {
                (page, host) = heart ? PressHeart(page, host) : PressThumbDown(page, host);

                _ = host.Should().Be(RatingOf(page));
            }
        }
    }
}
