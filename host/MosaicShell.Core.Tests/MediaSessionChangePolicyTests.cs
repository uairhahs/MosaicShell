using FluentAssertions;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests;

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
        double prev, double next, bool expected) =>
        MediaSessionChangePolicy.LooksLikeNewTrackPosition(prev, next).Should().Be(expected);

    [Fact]
    public void Timeline_poll_must_run_without_visible_flyout()
    {
        MediaSessionChangePolicy.MustPollTimelineIndependentlyOfFlyout.Should().BeTrue();
        MediaSessionChangePolicy.TimelinePollMs.Should().BePositive();
    }

    [Fact]
    public void Playing_scrubber_must_not_rewind_on_sticky_api()
    {
        MediaSessionChangePolicy.MustNotRewindPlayingScrubber.Should().BeTrue();
        MediaSessionChangePolicy.PlayingPositionJitterSeconds.Should().BeGreaterThan(0);

        MediaSessionChangePolicy.ResolvePlayingPosition(
                committedSeconds: 50,
                incomingSeconds: 10,
                playing: true,
                incomingReportedChange: false)
            .Should().BeApproximately(50, 0.01);
    }

    [Fact]
    public void Playing_scrubber_commits_real_skip_to_start()
    {
        MediaSessionChangePolicy.ResolvePlayingPosition(
                committedSeconds: 50,
                incomingSeconds: 0.4,
                playing: true,
                incomingReportedChange: true)
            .Should().BeApproximately(0.4, 0.01);
    }

    [Fact]
    public void Playing_scrubber_commits_forward_seek()
    {
        MediaSessionChangePolicy.ResolvePlayingPosition(
                committedSeconds: 10,
                incomingSeconds: 40,
                playing: true,
                incomingReportedChange: true)
            .Should().BeApproximately(40, 0.01);
    }

    [Fact]
    public void Paused_scrubber_follows_incoming()
    {
        MediaSessionChangePolicy.ResolvePlayingPosition(
                committedSeconds: 50,
                incomingSeconds: 10,
                playing: false,
                incomingReportedChange: false)
            .Should().BeApproximately(10, 0.01);
    }

    [Fact]
    public void Lagging_wnp_tick_must_not_rewind_playing_smtc()
    {
        MediaSessionChangePolicy.ResolvePlayingPosition(
                committedSeconds: 40,
                incomingSeconds: 38,
                playing: true,
                incomingReportedChange: true)
            .Should().BeApproximately(40, 0.01);
    }
}
