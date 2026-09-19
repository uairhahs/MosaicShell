using FluentAssertions;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    public class MediaArtworkCacheTests
    {
        // The cache is process-wide, so every test uses titles no other test does.
        private static byte[] Image(byte fill = 7)
        {
            byte[] bytes = new byte[64];
            Array.Fill(bytes, fill);
            return bytes;
        }

        [Fact]
        public void A_stored_cover_is_found_by_its_title()
        {
            byte[] image = Image();
            MediaArtworkCache.Store("Artwork cache test A", image);

            _ = MediaArtworkCache.TryGet("Artwork cache test A", out byte[]? found).Should().BeTrue();
            _ = found.Should().BeSameAs(image);
        }

        [Fact]
        public void The_lookup_ignores_case_and_the_site_suffix()
        {
            byte[] image = Image();
            MediaArtworkCache.Store("Artwork Cache Test B | YouTube Music", image);

            _ = MediaArtworkCache.TryGet("artwork cache test b", out byte[]? found).Should().BeTrue();
            _ = found.Should().BeSameAs(image);
        }

        [Fact]
        public void An_unknown_title_is_not_found()
        {
            _ = MediaArtworkCache.TryGet("Artwork cache test never stored", out byte[]? found).Should().BeFalse();
            _ = found.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void A_blank_title_is_never_stored_or_found(string? title)
        {
            MediaArtworkCache.Store(title, Image());

            _ = MediaArtworkCache.TryGet(title, out byte[]? found).Should().BeFalse();
            _ = found.Should().BeNull();
        }

        [Fact]
        public void An_image_too_small_to_be_one_is_not_stored()
        {
            MediaArtworkCache.Store("Artwork cache test C", new byte[MediaArtworkCache.MinimumImageBytes - 1]);

            _ = MediaArtworkCache.TryGet("Artwork cache test C", out _).Should().BeFalse();
        }

        [Fact]
        public void A_later_cover_replaces_an_earlier_one_for_the_same_title()
        {
            MediaArtworkCache.Store("Artwork cache test D", Image(1));
            byte[] newer = Image(2);
            MediaArtworkCache.Store("Artwork cache test D | YouTube Music", newer);

            _ = MediaArtworkCache.TryGet("Artwork cache test D", out byte[]? found).Should().BeTrue();
            _ = found.Should().BeSameAs(newer);
        }
    }
}
