using FluentAssertions;
using MosaicShell.Core.Update;

namespace MosaicShell.Core.Tests
{
    public class HostBuildVersionPolicyTests
    {
        [Theory]
        [InlineData("2026.8.21-b13", 2026, 8, 21, 13)]
        [InlineData("v2026.8.21-b13", 2026, 8, 21, 13)]
        [InlineData("2026.08.23-b47", 2026, 8, 23, 47)]
        public void TryParse_release_tag(string raw, int y, int m, int d, int b)
        {
            _ = HostBuildVersionPolicy.TryParse(raw, out HostBuildVersionParts parts).Should().BeTrue();
            _ = parts.Should().Be(new HostBuildVersionParts(y, m, d, b));
            _ = parts.Format().Should().Be($"{y}.{m}.{d}-b{b}");
        }

        [Theory]
        [InlineData("1.2.3")] // semver-style, not used
        [InlineData("0.1.0-native")]
        [InlineData("dev-060eea4")]
        [InlineData("")]
        public void TryParse_rejects_non_release_labels(string raw)
        {
            _ = HostBuildVersionPolicy.TryParse(raw, out _).Should().BeFalse();
        }

        [Fact]
        public void Compare_orders_by_date_then_build_number()
        {
            _ = HostBuildVersionPolicy.Compare("2026.8.21-b13", "2026.8.21-b14").Should().BeNegative();
            _ = HostBuildVersionPolicy.Compare("2026.8.22-b1", "2026.8.21-b99").Should().BePositive();
            _ = HostBuildVersionPolicy.Compare("2026.8.21-b13", "2026.8.21-b13").Should().Be(0);
        }

        [Fact]
        public void Local_dev_is_older_than_any_release_tag()
        {
            _ = HostBuildVersionPolicy.IsLocalDev("0.0.0-dev").Should().BeTrue();
            _ = HostBuildVersionPolicy.Compare("2026.8.21-b1", HostBuildVersionPolicy.LocalDevLabel).Should().BePositive();
            _ = HostBuildVersionPolicy.IsNewer("2026.8.21-b1", HostBuildVersionPolicy.LocalDevLabel).Should().BeTrue();
        }

        [Theory]
        [InlineData("2026.8.23-b1", "2026.8.23.1")]
        [InlineData("v2026.12.1-b47", "2026.12.1.47")]
        [InlineData("1.2.3", "0.0.0.0")]
        public void ToVersionInfoVersion_maps_date_build_for_pe_metadata(string tag, string expected)
        {
            _ = HostBuildVersionPolicy.ToVersionInfoVersion(tag).Should().Be(expected);
        }
    }
}
