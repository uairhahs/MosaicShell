namespace MosaicShell.Core.Services;

/// <summary>
/// Like / unlike semantics for browser players via WebNowPlaying (WNPLIB rev 3).
/// Windows SMTC has no standard like API; PWAs expose like through the extension.
/// </summary>
/// <remarks>
/// WNP rating values for LIKE_DISLIKE sites (YouTube Music):
/// 0 unrated, 1 disliked, 5 liked.
/// YTM <c>setRating(0)</c> on an unrated track triggers thumbs-down, so unlike must
/// only be sent when the host knows the track is liked (rating 5).
/// Spotify web uses LIKE-only (Library add/remove) inside the extension.
/// </remarks>
public static class MediaLikePolicy
{
    public const int Unrated = 0;
    public const int Disliked = 1;
    public const int Liked = 5;

    public static bool IsLiked(int? rating) => rating == Liked;

    public static bool IsDisliked(int? rating) => rating == Disliked;

    /// <summary>Heart should reflect liked state only (not disliked).</summary>
    public static bool ShouldShowLikedHeart(int? rating) => IsLiked(rating);

    /// <summary>Thumbs-down should reflect disliked state only.</summary>
    public static bool ShouldShowDislikedThumb(int? rating) => IsDisliked(rating);

    /// <summary>YouTube Music exposes like and dislike; most other players are like-only.</summary>
    public static bool SupportsDislike(string? appId, string? playerName = null) =>
        IsYouTubeMusic(playerName) || IsYouTubeMusicAppId(appId);

    /// <summary>Default rating for generic like intent (Spotify, etc.).</summary>
    public const int DefaultLikeRequestRating = Liked;

    public static int UnlikeRequestRating => Unrated;

    /// <summary>
    /// Whether sending TRY_SET_RATING 0 is safe for unlike intent on generic sites.
    /// False for unrated host state (YTM would thumbs-down).
    /// </summary>
    public static bool MaySendUnlikeRating(int hostLikeRating) => hostLikeRating == Liked;

    /// <summary>Whether clearing a known dislike is meaningful (host rating was disliked).</summary>
    public static bool MaySendUndislikeRating(int hostLikeRating) => hostLikeRating == Disliked;

    /// <summary>
    /// Maps UI like/unlike intent to WNP TRY_SET_RATING data for the active player.
    /// </summary>
    public static int ResolveLikeRequestRating(string? playerName, bool wantLiked, int hostLikeRating)
    {
        if (IsYouTubeMusic(playerName))
        {
            // WNP YouTubeMusic.ts (pre PR #53): setRating(5) calls toggleLike on
            // button[1], which is the thumbs-down control on current YTM layouts.
            // likeDislike(rating &lt; 3) calls toggleDislike on button[0] (thumbs-up).
            // Send 1 so the extension toggles the like button; it reads live DOM state.
            // See https://github.com/keifufu/WebNowPlaying/issues/52
            if (wantLiked || MaySendUnlikeRating(hostLikeRating))
                return Disliked;
            return Unrated;
        }

        return wantLiked ? DefaultLikeRequestRating : UnlikeRequestRating;
    }

    /// <summary>
    /// Maps UI dislike/undislike intent for YouTube Music (like-dislike WNP sites only).
    /// </summary>
    public static int ResolveDislikeRequestRating(string? playerName, bool wantDisliked, int hostLikeRating)
    {
        if (!IsYouTubeMusic(playerName))
            return wantDisliked ? Disliked : UnlikeRequestRating;

        if (wantDisliked)
        {
            // Liked -> dislike: likeDislike(0) with live rating 5 calls toggleLike (dislike btn).
            if (hostLikeRating == Liked)
                return Unrated;
            // Unrated / already disliked: toggleLike via rating 5 hits the dislike button.
            return Liked;
        }

        // Clear dislike: toggleLike via 5 when extension sees live rating 1.
        if (MaySendUndislikeRating(hostLikeRating))
            return Liked;
        return Unrated;
    }

    public static bool IsYouTubeMusic(string? playerName) =>
        !string.IsNullOrWhiteSpace(playerName)
        && playerName.Contains("YouTube Music", StringComparison.OrdinalIgnoreCase);

    public static bool IsYouTubeMusicAppId(string? appId) =>
        !string.IsNullOrWhiteSpace(appId)
        && (appId.Contains("music.youtube", StringComparison.OrdinalIgnoreCase)
            || appId.Contains("youtube", StringComparison.OrdinalIgnoreCase));
}
