namespace MosaicShell.Core.Services.BrowserUi
{
    /// <summary>
    /// YouTube Music's like and dislike buttons, read from the accessibility tree, each carrying its real On or Off state:
    /// what the extension-free route can read that Windows' media session cannot. They are found by structure, never by
    /// their localized names. Measured in Edge (2026-09-19), each button is the only child of a wrapper whose class is
    /// <c>like ... ytmusic-like-button-renderer</c> or <c>dislike ... ytmusic-like-button-renderer</c>; another view of
    /// the same tree shows them as the first two toggles of the <c>middle-controls-buttons</c> group. Both are accepted.
    /// </summary>
    public sealed class YouTubeMusicControls
    {
        public const string ControlsClass = "middle-controls-buttons";

        /// <summary>The component that holds both buttons; a wrapper class must name it and its own button.</summary>
        public const string LikeRendererClass = "ytmusic-like-button-renderer";

        /// <summary>What a browser window's title mentions when it may be showing YouTube Music.</summary>
        public const string SiteName = "YouTube Music";

        /// <summary>The class of the element that shows the playing track's title in the player bar (measured, 2026-09-19).</summary>
        public const string PlayerTitleClass = "title style-scope ytmusic-player-bar";

        private const int LikeIndex = 0;
        private const int DislikeIndex = 1;

        private readonly UiToggle _like;
        private readonly UiToggle _dislike;

        private YouTubeMusicControls(UiToggle like, UiToggle dislike)
        {
            _like = like;
            _dislike = dislike;
        }

        /// <summary>None when the state cannot be right (both on, or a button that reports neither), so nothing is guessed.</summary>
        public BrowserRating? Rating => (_like.State, _dislike.State) switch
        {
            (UiToggleState.Off, UiToggleState.Off) => BrowserRating.None,
            (UiToggleState.On, UiToggleState.Off) => BrowserRating.Liked,
            (UiToggleState.Off, UiToggleState.On) => BrowserRating.Disliked,
            _ => null,
        };

        /// <summary>The controls, or null when the page shows none.</summary>
        public static YouTubeMusicControls? Find(IEnumerable<UiToggle> toggles)
        {
            List<UiToggle> all = [.. toggles];
            return FindWrapped(all) ?? FindGrouped(all);
        }

        private static YouTubeMusicControls? FindWrapped(List<UiToggle> toggles)
        {
            UiToggle? like = toggles.FirstOrDefault(t => HasClasses(t.ParentClassName, "like", LikeRendererClass));
            UiToggle? dislike = toggles.FirstOrDefault(t => HasClasses(t.ParentClassName, "dislike", LikeRendererClass));
            return like is not null && dislike is not null ? new YouTubeMusicControls(like, dislike) : null;
        }

        /// <summary>Whole class names, so "like" is not found inside "unlike" or "likely".</summary>
        private static bool HasClasses(string classNames, params string[] wanted)
        {
            string[] present = classNames.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return wanted.All(present.Contains);
        }

        /// <summary>The first group that has both buttons as its first two toggles.</summary>
        private static YouTubeMusicControls? FindGrouped(List<UiToggle> toggles)
        {
            foreach (IGrouping<string, UiToggle> group in toggles
                .Where(t => t.ParentClassName.Contains(ControlsClass, StringComparison.Ordinal))
                .GroupBy(t => t.ParentClassName))
            {
                UiToggle? like = group.FirstOrDefault(t => t.IndexInParent == LikeIndex);
                UiToggle? dislike = group.FirstOrDefault(t => t.IndexInParent == DislikeIndex);
                if (like is not null && dislike is not null)
                {
                    return new YouTubeMusicControls(like, dislike);
                }
            }

            return null;
        }

        /// <summary>
        /// The one button to press so the track ends up liked, or no longer liked. Pressing Like on a disliked track
        /// switches it to liked; clearing a like never touches a dislike. Null when nothing needs pressing.
        /// </summary>
        public UiToggle? PressForLike(bool liked)
        {
            return Rating switch
            {
                null => null,
                BrowserRating.Liked => liked ? null : _like,
                _ => liked ? _like : null,
            };
        }

        /// <summary>The counterpart of <see cref="PressForLike"/> for the dislike button.</summary>
        public UiToggle? PressForDislike(bool disliked)
        {
            return Rating switch
            {
                null => null,
                BrowserRating.Disliked => disliked ? null : _dislike,
                _ => disliked ? _dislike : null,
            };
        }
    }
}
