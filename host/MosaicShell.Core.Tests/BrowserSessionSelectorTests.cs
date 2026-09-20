using FluentAssertions;
using MosaicShell.Core.Services;
using MosaicShell.Core.Services.BrowserBridge;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Several tabs in several browsers can publish media at once. The selector decides which one the flyout
    /// shows: audible playback first, then the tab Windows agrees with, then the newest report.
    /// </summary>
    public class BrowserSessionSelectorTests
    {
        private static readonly DateTimeOffset Now = new(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);

        private static BrowserSessionEntry Entry(
            string title,
            BrowserPlaybackState state = BrowserPlaybackState.Playing,
            bool audible = true,
            int minutesAgo = 0,
            int connection = 1,
            int tab = 1,
            int heardSecondsAgo = 0)
        {
            BrowserSessionReport report = new(
                tab, 1, "https://music.youtube.com", title, "Artist", "", [], state, audible,
                BrowserRating.None, BrowserMediaCapabilities.None);
            return new BrowserSessionEntry(connection, report, Now.AddMinutes(-minutesAgo), Now.AddSeconds(-heardSecondsAgo));
        }

        private static string? Pick(string? smtcTitle, params BrowserSessionEntry[] entries)
        {
            return BrowserSessionSelector.Select(entries, Now, smtcTitle)?.Report.Title;
        }

        [Fact]
        public void No_sessions_selects_nothing()
        {
            _ = BrowserSessionSelector.Select([], Now, "anything").Should().BeNull();
        }

        [Fact]
        public void A_single_playing_session_is_selected()
        {
            _ = Pick(null, Entry("Only")).Should().Be("Only");
        }

        [Fact]
        public void Audible_playback_beats_a_paused_tab_even_when_the_paused_tab_is_newer()
        {
            _ = Pick(null,
                Entry("Paused", BrowserPlaybackState.Paused, minutesAgo: 0),
                Entry("Playing", minutesAgo: 30)).Should().Be("Playing");
        }

        [Fact]
        public void Audible_playback_beats_playback_nobody_can_hear()
        {
            _ = Pick(null,
                Entry("Muted", audible: false, minutesAgo: 0),
                Entry("Loud", minutesAgo: 10)).Should().Be("Loud");
        }

        [Fact]
        public void Playing_but_silent_beats_paused()
        {
            _ = Pick(null,
                Entry("Paused", BrowserPlaybackState.Paused),
                Entry("Muted", audible: false, minutesAgo: 5)).Should().Be("Muted");
        }

        [Fact]
        public void A_paused_tab_beats_a_stopped_one_that_still_has_a_title()
        {
            _ = Pick(null,
                Entry("Ended", BrowserPlaybackState.Stopped, audible: false),
                Entry("Paused", BrowserPlaybackState.Paused, audible: false, minutesAgo: 20)).Should().Be("Paused");
        }

        [Fact]
        public void A_stopped_tab_with_a_title_is_selectable_when_it_is_all_there_is()
        {
            _ = Pick(null, Entry("Ended", BrowserPlaybackState.Stopped, audible: false)).Should().Be("Ended");
        }

        [Fact]
        public void A_stopped_tab_with_no_title_is_never_selected()
        {
            _ = BrowserSessionSelector.Select([Entry("", BrowserPlaybackState.Stopped, audible: false)], Now, null).Should().BeNull();
        }

        [Fact]
        public void Within_a_tier_the_tab_Windows_agrees_with_beats_a_newer_one()
        {
            _ = Pick("Old song | YouTube Music",
                Entry("Old song", minutesAgo: 10, tab: 1),
                Entry("Newer song", minutesAgo: 0, tab: 2)).Should().Be("Old song");
        }

        [Fact]
        public void Within_a_tier_without_agreement_the_newest_report_wins()
        {
            _ = Pick("Something else",
                Entry("A", minutesAgo: 10, tab: 1),
                Entry("B", minutesAgo: 2, tab: 2),
                Entry("C", minutesAgo: 6, tab: 3)).Should().Be("B");
        }

        [Fact]
        public void Agreement_with_Windows_never_lifts_a_lower_tier_over_a_higher_one()
        {
            _ = Pick("Paused song",
                Entry("Paused song", BrowserPlaybackState.Paused),
                Entry("Playing song", minutesAgo: 30)).Should().Be("Playing song");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void No_Windows_title_falls_back_to_recency(string? smtcTitle)
        {
            _ = Pick(smtcTitle,
                Entry("", minutesAgo: 0, tab: 1),
                Entry("Older", minutesAgo: 5, tab: 2)).Should().Be("");
        }

        [Fact]
        public void The_choice_does_not_depend_on_the_order_the_sessions_arrive_in()
        {
            BrowserSessionEntry a = Entry("A", connection: 1, tab: 1);
            BrowserSessionEntry b = Entry("B", connection: 1, tab: 2);
            BrowserSessionEntry c = Entry("C", connection: 2, tab: 1);

            string?[] picks =
            [
                Pick(null, a, b, c), Pick(null, a, c, b), Pick(null, b, a, c),
                Pick(null, b, c, a), Pick(null, c, a, b), Pick(null, c, b, a),
            ];

            _ = picks.Should().AllBe("A", "equal candidates fall back to the lowest connection then the lowest tab id");
        }

        [Fact]
        public void A_connection_not_heard_from_within_the_timeout_is_ignored()
        {
            int stale = (int)BrowserSessionSelector.StalenessTimeout.TotalSeconds + 1;

            _ = Pick(null,
                Entry("Ghost", heardSecondsAgo: stale, tab: 1),
                Entry("Alive", BrowserPlaybackState.Paused, audible: false, tab: 2)).Should().Be("Alive");
        }

        [Fact]
        public void A_connection_heard_from_exactly_at_the_timeout_is_still_live()
        {
            int exactly = (int)BrowserSessionSelector.StalenessTimeout.TotalSeconds;

            _ = Pick(null, Entry("Edge of it", heardSecondsAgo: exactly)).Should().Be("Edge of it");
        }

        [Fact]
        public void Only_stale_connections_select_nothing()
        {
            int stale = (int)BrowserSessionSelector.StalenessTimeout.TotalSeconds + 1;

            _ = BrowserSessionSelector.Select([Entry("Ghost", heardSecondsAgo: stale)], Now, null).Should().BeNull();
        }

        [Fact]
        public void A_report_may_be_old_as_long_as_its_connection_is_live()
        {
            _ = Pick(null, Entry("Long song", minutesAgo: 240, heardSecondsAgo: 5)).Should().Be("Long song");
        }
    }
}
