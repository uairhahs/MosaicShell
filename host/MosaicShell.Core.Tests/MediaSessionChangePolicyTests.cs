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
}
