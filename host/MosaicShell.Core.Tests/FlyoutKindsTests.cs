using FluentAssertions;
using MosaicShell.Core.Capabilities.Platform;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The flyout kinds as they travel in a request, over the capability IPC and into the logs: fixed strings, so a
    /// renamed constant must not change what is sent.
    /// </summary>
    public sealed class FlyoutKindsTests
    {
        [Fact]
        public void The_kinds_keep_their_wire_values()
        {
            _ = FlyoutKinds.Volume.Should().Be("vol");
            _ = FlyoutKinds.Brightness.Should().Be("bright");
            _ = FlyoutKinds.Media.Should().Be("media");
            _ = FlyoutKinds.Locks.Should().Be("locks");
            _ = FlyoutKinds.Flight.Should().Be("flight");
        }

        [Theory]
        [InlineData("media", FlyoutKinds.Media, true)]
        [InlineData("MEDIA", FlyoutKinds.Media, true)]
        [InlineData("vol", FlyoutKinds.Media, false)]
        [InlineData(null, FlyoutKinds.Media, false)]
        public void A_kind_matches_whatever_its_case(string? kind, string expected, bool matches)
        {
            _ = FlyoutKinds.Is(kind, expected).Should().Be(matches);
        }
    }
}
