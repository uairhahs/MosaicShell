using FluentAssertions;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    public class MediaSessionChangePolicyTests
    {
        [Theory]
        [InlineData(30, 0, true)]
        [InlineData(12, 1.5, true)]
        [InlineData(5, 2.5, true)]
        [InlineData(4.9, 0, false)]
        [InlineData(30, 25, false)]
        [InlineData(20, 11, true)] // backward jump >= 8s
        [InlineData(20, 13, false)]
        public void LooksLikeNewTrackPosition_detects_skip_and_large_rewind(
            double prev, double next, bool expected)
        {
            _ = MediaSessionChangePolicy.LooksLikeNewTrackPosition(prev, next).Should().Be(expected);
        }

        [Fact]
        public void Timeline_poll_must_run_without_visible_flyout()
        {
            _ = MediaSessionChangePolicy.MustPollTimelineIndependentlyOfFlyout.Should().BeTrue();
            _ = MediaSessionChangePolicy.TimelinePollMs.Should().BePositive();
        }

        /// <summary>
        /// Measured 2026-08-29 against YouTube Music: one skip emits a settling sequence of
        /// title changes, and every transitional snapshot carries an empty artist while every
        /// settled one carries a real artist (14 boundaries observed, zero counterexamples in
        /// either direction). Title alone therefore cannot classify a track boundary.
        /// </summary>
        [Theory]
        [InlineData("YouTube Music", "", true)]                     // placeholder, app re-attaching
        [InlineData("no hesi! | YouTube Music", "", true)]           // browser tab title
        [InlineData("no hesi!", "Chow Lee & Synthetic", false)]      // settled
        [InlineData("PRETTY", "Gvlli3", false)]
        [InlineData("YouTube Music", "   ", true)]                   // whitespace artist
        [InlineData("", "", false)]                                  // nothing to settle
        [InlineData(null, null, false)]
        public void IsMetadataSettling_flags_titles_that_arrive_before_their_artist(
            string? title, string? artist, bool expected)
        {
            _ = MediaSessionChangePolicy.IsMetadataSettling(title, artist).Should().Be(expected);
        }

        [Fact]
        public void Metadata_settle_window_covers_measured_spread()
        {
            // Observed settle latency (placeholder -> real metadata) was 50-122ms across six
            // skips; the window must clear that with headroom while staying far below the
            // ~420ms entrance so a deferred present is not perceived as lag.
            _ = MediaSessionChangePolicy.MetadataSettleMs.Should().BeGreaterThanOrEqualTo(150);
            _ = MediaSessionChangePolicy.MetadataSettleMs.Should().BeLessThan(420);
        }

        [Fact]
        public void Null_session_grace_is_positive_and_covers_measured_ytm_gap()
        {
            // .local/smtc-probe measured ~650-950ms between session close and reattach.
            _ = MediaSessionChangePolicy.NullSessionGraceMs.Should().BeGreaterThan(950);
        }

        [Theory]
        [InlineData(0, 0, false)]
        [InlineData(120, 120, false)]
        [InlineData(0, 5000, true)]
        [InlineData(5000, 0, true)]
        [InlineData(4000, 5000, true)]
        public void IsArtOnlyRefresh_detects_any_thumbnail_length_difference(
            int prevLength, int nextLength, bool expected)
        {
            _ = MediaSessionChangePolicy.IsArtOnlyRefresh(prevLength, nextLength).Should().Be(expected);
        }

        [Fact]
        public void Playing_scrubber_must_not_rewind_on_sticky_api()
        {
            _ = MediaSessionChangePolicy.MustNotRewindPlayingScrubber.Should().BeTrue();
            _ = MediaSessionChangePolicy.PlayingPositionJitterSeconds.Should().BeGreaterThan(0);

            _ = MediaSessionChangePolicy.ResolvePlayingPosition(
                    committedSeconds: 50,
                    incomingSeconds: 10,
                    playing: true,
                    incomingReportedChange: false)
                .Should().BeApproximately(50, 0.01);
        }

        [Fact]
        public void Playing_scrubber_commits_real_skip_to_start()
        {
            _ = MediaSessionChangePolicy.ResolvePlayingPosition(
                    committedSeconds: 50,
                    incomingSeconds: 0.4,
                    playing: true,
                    incomingReportedChange: true)
                .Should().BeApproximately(0.4, 0.01);
        }

        [Fact]
        public void Playing_scrubber_commits_forward_seek()
        {
            _ = MediaSessionChangePolicy.ResolvePlayingPosition(
                    committedSeconds: 10,
                    incomingSeconds: 40,
                    playing: true,
                    incomingReportedChange: true)
                .Should().BeApproximately(40, 0.01);
        }

        [Fact]
        public void Paused_scrubber_follows_incoming()
        {
            _ = MediaSessionChangePolicy.ResolvePlayingPosition(
                    committedSeconds: 50,
                    incomingSeconds: 10,
                    playing: false,
                    incomingReportedChange: false)
                .Should().BeApproximately(10, 0.01);
        }

        [Fact]
        public void Lagging_wnp_tick_must_not_rewind_playing_smtc()
        {
            _ = MediaSessionChangePolicy.ResolvePlayingPosition(
                    committedSeconds: 40,
                    incomingSeconds: 38,
                    playing: true,
                    incomingReportedChange: true)
                .Should().BeApproximately(40, 0.01);
        }

    }
}
