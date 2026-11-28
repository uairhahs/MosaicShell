using FluentAssertions;
using MosaicShell.Core.Modules;

namespace MosaicShell.Core.Tests
{
    public class HubLibrarySearchTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void An_empty_query_shows_everything(string? query)
        {
            _ = HubLibrarySearch.Matches(query, "tessera", "Tessera", "Volume and media pop-ups").Should().BeTrue();
        }

        [Theory]
        [InlineData("tess")]
        [InlineData("TESSERA")]
        [InlineData("  pop-ups ")]
        [InlineData("volume media")]
        [InlineData("media volume")]
        public void Every_word_must_appear_in_the_name_description_or_id_ignoring_case(string query)
        {
            _ = HubLibrarySearch.Matches(query, "tessera", "Tessera", "Volume and media pop-ups").Should().BeTrue();
        }

        [Theory]
        [InlineData("clock")]
        [InlineData("volume clock")]
        public void A_word_found_nowhere_hides_the_row(string query)
        {
            _ = HubLibrarySearch.Matches(query, "tessera", "Tessera", "Volume and media pop-ups").Should().BeFalse();
        }

        [Fact]
        public void No_results_names_the_query()
        {
            _ = HubLibrarySearch.NoResultsText("  clock ").Should().Be("Nothing matches “clock”.");
        }
    }
}
