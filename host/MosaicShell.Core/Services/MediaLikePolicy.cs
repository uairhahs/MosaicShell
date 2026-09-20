namespace MosaicShell.Core.Services
{
    /// <summary>
    /// What the flyout's heart and thumbs-down icons read. A browser source reports the real state of the player's like
    /// and dislike buttons (<see cref="BrowserRating"/>); this maps it to the value the icons use.
    /// </summary>
    /// <remarks>Rating values: 0 unrated, 1 disliked, 5 liked.</remarks>
    public static class MediaLikePolicy
    {
        public const int Unrated = 0;
        public const int Disliked = 1;
        public const int Liked = 5;

        /// <summary>Maps a browser rating to the value the flyout icons read (0 unrated, 1 disliked, 5 liked).</summary>
        public static int ToLikeRating(BrowserRating rating)
        {
            return rating switch
            {
                BrowserRating.Liked => Liked,
                BrowserRating.Disliked => Disliked,
                _ => Unrated,
            };
        }

        public static bool IsLiked(int? rating)
        {
            return rating == Liked;
        }

        public static bool IsDisliked(int? rating)
        {
            return rating == Disliked;
        }

        /// <summary>Heart should reflect liked state only (not disliked).</summary>
        public static bool ShouldShowLikedHeart(int? rating)
        {
            return IsLiked(rating);
        }

        /// <summary>Thumbs-down should reflect disliked state only.</summary>
        public static bool ShouldShowDislikedThumb(int? rating)
        {
            return IsDisliked(rating);
        }
    }
}
