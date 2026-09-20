using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.BrowserUi;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// YouTube Music's like and dislike buttons are found by where they sit in the page (measured with UI Automation
    /// on the real page, 2026-09-19), never by their localized names: the first two toggle buttons of the group whose
    /// class is middle-controls-buttons. Their real state says what a press will do, so at most one press is ever needed.
    /// </summary>
    public class YouTubeMusicControlsTests
    {
        private const string Controls = "middle-controls-buttons style-scope ytmusic-player-bar";

        private static UiToggle Toggle(string parent, int index, UiToggleState state, List<string>? log = null, string? name = null)
        {
            return new UiToggle(parent, index, state, () => log?.Add(name ?? $"{parent}[{index}]"));
        }

        private static YouTubeMusicControls Player(UiToggleState like, UiToggleState dislike, List<string>? log = null)
        {
            return YouTubeMusicControls.Find(
            [
                Toggle("autoplay style-scope ytmusic-player-page", 0, UiToggleState.On, log, "autoplay"),
                Toggle(Controls, 0, like, log, "like"),
                Toggle(Controls, 1, dislike, log, "dislike"),
            ])!;
        }

        private const string LikeWrapper = "like style-scope ytmusic-like-button-renderer";
        private const string DislikeWrapper = "dislike style-scope ytmusic-like-button-renderer";

        private static YouTubeMusicControls Wrapped(UiToggleState like, UiToggleState dislike, List<string>? log = null)
        {
            return YouTubeMusicControls.Find(
            [
                Toggle("autoplay style-scope ytmusic-player-page", 0, UiToggleState.On, log, "autoplay"),
                Toggle("scroller style-scope ytmusic-player-page", 5, UiToggleState.On, log, "scroller"),
                Toggle(LikeWrapper, 0, like, log, "like"),
                Toggle(DislikeWrapper, 0, dislike, log, "dislike"),
            ])!;
        }

        [Theory]
        [InlineData(UiToggleState.Off, UiToggleState.Off, BrowserRating.None)]
        [InlineData(UiToggleState.On, UiToggleState.Off, BrowserRating.Liked)]
        [InlineData(UiToggleState.Off, UiToggleState.On, BrowserRating.Disliked)]
        public void In_the_browsers_tree_each_button_can_sit_in_its_own_wrapper(UiToggleState like, UiToggleState dislike, BrowserRating expected)
        {
            // Measured through UI Automation in Edge: the buttons' parents are yt-button-shape wrappers whose classes
            // are "like ..." and "dislike ...", each holding one button.
            _ = Wrapped(like, dislike).Rating.Should().Be(expected);
        }

        [Theory]
        [InlineData(UiToggleState.Off, UiToggleState.Off, true, "like")]
        [InlineData(UiToggleState.Off, UiToggleState.On, true, "like")]
        [InlineData(UiToggleState.On, UiToggleState.Off, true, null)]
        [InlineData(UiToggleState.On, UiToggleState.Off, false, "like")]
        public void Pressing_works_the_same_for_the_wrapped_form(UiToggleState like, UiToggleState dislike, bool liked, string? expected)
        {
            List<string> log = [];

            Wrapped(like, dislike, log).PressForLike(liked)?.Press();

            _ = log.Should().Equal(expected is null ? [] : [expected]);
        }

        [Fact]
        public void A_wrapper_needs_both_its_own_class_and_the_like_renderer_to_count()
        {
            YouTubeMusicControls? controls = YouTubeMusicControls.Find(
            [
                Toggle("like", 0, UiToggleState.On),
                Toggle("dislike", 0, UiToggleState.Off),
                Toggle("unlike style-scope ytmusic-like-button-renderer", 0, UiToggleState.On),
                Toggle("likely style-scope ytmusic-like-button-renderer", 0, UiToggleState.On),
            ]);

            _ = controls.Should().BeNull("a class that merely contains the word, or lacks the renderer, is not a like button");
        }

        [Fact]
        public void Only_one_wrapped_button_is_not_enough()
        {
            _ = YouTubeMusicControls.Find([Toggle(LikeWrapper, 0, UiToggleState.Off)]).Should().BeNull();
        }

        [Fact]
        public void The_like_and_dislike_buttons_are_the_first_two_toggles_of_the_middle_controls()
        {
            YouTubeMusicControls? controls = YouTubeMusicControls.Find(
            [
                Toggle("something else", 0, UiToggleState.Off),
                Toggle(Controls, 0, UiToggleState.On),
                Toggle(Controls, 1, UiToggleState.Off),
            ]);

            _ = controls.Should().NotBeNull();
            _ = controls!.Rating.Should().Be(BrowserRating.Liked);
        }

        [Fact]
        public void A_toggle_elsewhere_on_the_page_is_never_mistaken_for_a_like_button()
        {
            YouTubeMusicControls? controls = YouTubeMusicControls.Find(
            [
                Toggle("autoplay style-scope ytmusic-player-page", 0, UiToggleState.On),
                Toggle("autoplay style-scope ytmusic-player-page", 1, UiToggleState.On),
            ]);

            _ = controls.Should().BeNull();
        }

        [Fact]
        public void Nothing_is_found_when_only_one_of_the_two_buttons_is_there()
        {
            _ = YouTubeMusicControls.Find([Toggle(Controls, 0, UiToggleState.Off)]).Should().BeNull();
            _ = YouTubeMusicControls.Find([Toggle(Controls, 1, UiToggleState.Off)]).Should().BeNull();
        }

        [Fact]
        public void Nothing_is_found_without_any_toggles()
        {
            _ = YouTubeMusicControls.Find([]).Should().BeNull();
        }

        [Fact]
        public void The_first_group_of_controls_is_used_when_there_are_two()
        {
            YouTubeMusicControls? controls = YouTubeMusicControls.Find(
            [
                Toggle(Controls + " a", 0, UiToggleState.On),
                Toggle(Controls + " a", 1, UiToggleState.Off),
                Toggle(Controls + " b", 0, UiToggleState.Off),
                Toggle(Controls + " b", 1, UiToggleState.On),
            ]);

            _ = controls!.Rating.Should().Be(BrowserRating.Liked);
        }

        [Theory]
        [InlineData(UiToggleState.Off, UiToggleState.Off, BrowserRating.None)]
        [InlineData(UiToggleState.On, UiToggleState.Off, BrowserRating.Liked)]
        [InlineData(UiToggleState.Off, UiToggleState.On, BrowserRating.Disliked)]
        public void The_rating_is_what_the_buttons_say(UiToggleState like, UiToggleState dislike, BrowserRating expected)
        {
            _ = Player(like, dislike).Rating.Should().Be(expected);
        }

        [Theory]
        [InlineData(UiToggleState.On, UiToggleState.On)]
        [InlineData(UiToggleState.Indeterminate, UiToggleState.Off)]
        [InlineData(UiToggleState.Off, UiToggleState.Indeterminate)]
        public void A_state_that_cannot_be_right_is_unknown_rather_than_guessed(UiToggleState like, UiToggleState dislike)
        {
            YouTubeMusicControls controls = Player(like, dislike);

            _ = controls.Rating.Should().BeNull();
            _ = controls.PressForLike(true).Should().BeNull("with no known state no press is safe");
            _ = controls.PressForDislike(true).Should().BeNull();
            _ = controls.PressForLike(false).Should().BeNull();
            _ = controls.PressForDislike(false).Should().BeNull();
        }

        [Theory]
        // Wanting a like: press Like unless it is already on. From a dislike one press of Like switches it.
        [InlineData(UiToggleState.Off, UiToggleState.Off, true, "like")]
        [InlineData(UiToggleState.On, UiToggleState.Off, true, null)]
        [InlineData(UiToggleState.Off, UiToggleState.On, true, "like")]
        // Clearing a like: press Like only when a like is what is on.
        [InlineData(UiToggleState.On, UiToggleState.Off, false, "like")]
        [InlineData(UiToggleState.Off, UiToggleState.Off, false, null)]
        [InlineData(UiToggleState.Off, UiToggleState.On, false, null)]
        public void Liking_and_unliking_press_at_most_the_like_button_once(UiToggleState like, UiToggleState dislike, bool liked, string? expected)
        {
            List<string> log = [];
            YouTubeMusicControls controls = Player(like, dislike, log);

            controls.PressForLike(liked)?.Press();

            _ = log.Should().Equal(expected is null ? [] : [expected]);
        }

        [Theory]
        [InlineData(UiToggleState.Off, UiToggleState.Off, true, "dislike")]
        [InlineData(UiToggleState.Off, UiToggleState.On, true, null)]
        [InlineData(UiToggleState.On, UiToggleState.Off, true, "dislike")]
        [InlineData(UiToggleState.Off, UiToggleState.On, false, "dislike")]
        [InlineData(UiToggleState.Off, UiToggleState.Off, false, null)]
        [InlineData(UiToggleState.On, UiToggleState.Off, false, null)]
        public void Disliking_and_undisliking_press_at_most_the_dislike_button_once(UiToggleState like, UiToggleState dislike, bool disliked, string? expected)
        {
            List<string> log = [];
            YouTubeMusicControls controls = Player(like, dislike, log);

            controls.PressForDislike(disliked)?.Press();

            _ = log.Should().Equal(expected is null ? [] : [expected]);
        }
    }
}
