using FluentAssertions;
using MosaicShell.Core.Services.Kde;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The region KWin blurs behind a flyout, as the x, y, width, height rectangles of
    /// <c>_KDE_NET_WM_BLUR_BEHIND_REGION</c>. It must match the card's rounded shape, as the Win32 round region does on
    /// Windows, or the blur shows as a square behind the rounded corners.
    /// </summary>
    public sealed class KWinBlurRegionTests
    {
        /// <summary>Whether a pixel's centre lies in the rounded rectangle: the shape the rectangles must cover.</summary>
        private static bool InsideRoundedRect(int x, int y, int width, int height, int radius)
        {
            double px = x + 0.5;
            double py = y + 0.5;
            double cx = Math.Clamp(px, radius, width - radius);
            double cy = Math.Clamp(py, radius, height - radius);
            double dx = px - cx;
            double dy = py - cy;
            return (dx * dx) + (dy * dy) <= (double)radius * radius;
        }

        private static bool[,] Cover(uint[] region, int width, int height)
        {
            bool[,] covered = new bool[width, height];
            for (int i = 0; i < region.Length; i += 4)
            {
                for (uint x = region[i]; x < region[i] + region[i + 2]; x++)
                {
                    for (uint y = region[i + 1]; y < region[i + 1] + region[i + 3]; y++)
                    {
                        _ = covered[x, y].Should().BeFalse($"rectangles must not overlap at {x},{y}");
                        covered[x, y] = true;
                    }
                }
            }

            return covered;
        }

        [Theory]
        [InlineData(804, 356, 24)]
        [InlineData(40, 30, 8)]
        [InlineData(10, 10, 5)]
        [InlineData(65, 17, 3)]
        [InlineData(3, 3, 1)]
        public void Covers_exactly_the_pixels_inside_the_rounded_card(int width, int height, int radius)
        {
            uint[] region = KWinBlurRegion.FromRoundedRect(width, height, radius);

            _ = (region.Length % 4).Should().Be(0);
            bool[,] covered = Cover(region, width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _ = covered[x, y].Should().Be(InsideRoundedRect(x, y, width, height, radius), $"pixel {x},{y}");
                }
            }
        }

        [Fact]
        public void No_radius_blurs_the_whole_window_as_one_rectangle()
        {
            _ = KWinBlurRegion.FromRoundedRect(402, 178, 0).Should().Equal(0u, 0u, 402u, 178u);
        }

        [Fact]
        public void A_radius_past_half_the_short_side_is_clamped_to_a_pill()
        {
            uint[] clamped = KWinBlurRegion.FromRoundedRect(60, 20, 50);

            _ = clamped.Should().Equal(KWinBlurRegion.FromRoundedRect(60, 20, 10));
        }

        [Fact]
        public void Rows_with_the_same_inset_are_merged()
        {
            // A 24 px corner needs at most one rectangle per row in each corner band, plus the middle.
            uint[] region = KWinBlurRegion.FromRoundedRect(804, 356, 24);

            _ = (region.Length / 4).Should().BeLessThanOrEqualTo((2 * 24) + 1);
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(10, 0)]
        [InlineData(-5, 10)]
        public void An_empty_window_has_no_region(int width, int height)
        {
            _ = KWinBlurRegion.FromRoundedRect(width, height, 4).Should().BeEmpty();
        }
    }
}
