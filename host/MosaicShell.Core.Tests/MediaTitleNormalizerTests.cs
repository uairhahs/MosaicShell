using FluentAssertions;
using MosaicShell.Core.Services;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Measured 2026-09-19 (YouTube Music PWA): every skip surfaces the tab title "X | YouTube Music"
    /// with an empty artist, and when WebNowPlaying supplies nothing that title is all the flyout gets.
    /// </summary>
    public class MediaTitleNormalizerTests
    {
        [Theory]
        [InlineData("no hesi! | YouTube Music", "no hesi!")]
        [InlineData("JUST DANCE | YouTube Music", "JUST DANCE")]
        [InlineData("Song | YouTube", "Song")]
        [InlineData("Song | youtube music", "Song")]
        [InlineData("A | B | YouTube Music", "A | B")]
        [InlineData("Song   | YouTube Music", "Song")]
        public void Strips_a_trailing_site_suffix(string raw, string expected)
        {
            _ = MediaTitleNormalizer.StripSiteSuffix(raw).Should().Be(expected);
        }

        [Theory]
        [InlineData("YouTube Music")]
        [InlineData("YouTube")]
        [InlineData("| YouTube Music")]
        [InlineData(" | YouTube Music")]
        [InlineData("Song | Some Other Site")]
        [InlineData("Song | YouTube Music Remix")]
        [InlineData("Devin Morrison - That's All ")]
        [InlineData("Plain title")]
        [InlineData("")]
        public void Leaves_everything_else_untouched(string raw)
        {
            _ = MediaTitleNormalizer.StripSiteSuffix(raw).Should().Be(raw);
        }

        [Fact]
        public void Null_stays_null()
        {
            _ = MediaTitleNormalizer.StripSiteSuffix(null).Should().BeNull();
        }

        [Theory]
        [InlineData("Song", "Song")]
        [InlineData("Song | YouTube Music", "Song")]
        [InlineData("Song", "Song | YouTube Music")]
        [InlineData("SONG", "song")]
        [InlineData("  Song ", "Song | YouTube")]
        public void Two_spellings_of_one_title_match(string a, string b)
        {
            _ = MediaTitleNormalizer.LooselyMatch(a, b).Should().BeTrue();
        }

        [Theory]
        [InlineData("Song", "Other")]
        [InlineData("Song", "Song (Remix)")]
        [InlineData("", "")]
        [InlineData(null, null)]
        [InlineData("   ", "   ")]
        [InlineData("Song", null)]
        [InlineData(null, "Song")]
        [InlineData("YouTube Music", "Song")]
        public void Different_or_empty_titles_do_not_match(string? a, string? b)
        {
            _ = MediaTitleNormalizer.LooselyMatch(a, b).Should().BeFalse();
        }
    }
}
