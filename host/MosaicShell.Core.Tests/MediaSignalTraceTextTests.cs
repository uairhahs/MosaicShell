using FluentAssertions;
using MosaicShell.Core.Capabilities.BuiltIn;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The `media signal` trace line is how a track-change problem is told apart from a repeat of
    /// the same track. A title cut at 28 characters made two different videos look the same, and
    /// artwork recorded only by length could not show that the picture had changed.
    /// </summary>
    public sealed class MediaSignalTraceTextTests
    {
        [Fact]
        public void A_long_title_is_kept_whole_for_the_log_to_cap()
        {
            string title = "Why China Can Tax its Rich But The West Cannot Do The Same Thing";

            _ = MediaSignalTraceText.Text(title).Should().Be(title);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void A_missing_value_is_a_dash(string? value)
        {
            _ = MediaSignalTraceText.Text(value).Should().Be("-");
        }

        [Fact]
        public void Art_is_a_dash_when_there_is_none()
        {
            _ = MediaSignalTraceText.Art(null).Should().Be("-");
        }

        [Fact]
        public void Art_names_its_content_so_a_changed_picture_of_the_same_size_shows()
        {
            byte[] a = [1, 2, 3, 4];
            byte[] b = [4, 3, 2, 1];

            _ = MediaSignalTraceText.Art(a).Should().StartWith("4:");
            _ = MediaSignalTraceText.Art(a).Should().NotBe(MediaSignalTraceText.Art(b));
            _ = MediaSignalTraceText.Art(a).Should().Be(MediaSignalTraceText.Art((byte[])a.Clone()));
        }

        [Fact]
        public void Art_does_not_put_the_picture_bytes_in_the_log()
        {
            _ = MediaSignalTraceText.Art(new byte[100_000]).Length.Should().BeLessThan(32);
        }
    }
}
