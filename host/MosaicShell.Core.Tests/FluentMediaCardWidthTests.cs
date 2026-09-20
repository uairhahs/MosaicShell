using System.Text.RegularExpressions;
using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// The Fluent media card, its reveal host, the window that sizes to it and the HWND region cut from
    /// that window must share one width. The card once subtracted a literal 16 from the shared constant
    /// while everything else used the constant, so the window and region ran 15 dip past the card and
    /// showed a strip of OS acrylic to its right. Every Core-side consumer already read the spec, so
    /// only the Host panel could disagree; these tests pin the value and fail on the shape of that bug.
    /// </summary>
    public class FluentMediaCardWidthTests
    {
        // A media width constant with literal arithmetic applied, e.g. `MediaWidth - 16`.
        private static readonly Regex DerivedMediaWidth = new(
            @"\b\w*MediaWidth(Dip)?\s*[-+*/]\s*\d",
            RegexOptions.Compiled);

        [Fact]
        public void Fluent_media_width_is_the_compact_card_width()
        {
            _ = TesseraFluentLayoutSpec.MediaWidthDip.Should().Be(324);
            _ = TesseraFlyoutTweenTargetCatalog.ResolveProfile(StyleIds.Fluent).MediaWidthDip
                .Should().Be(TesseraFluentLayoutSpec.MediaWidthDip);
        }

        [Fact]
        public void Fluent_window_layout_width_is_the_card_width_plus_the_clip_epsilon()
        {
            double card = TesseraFlyoutTweenTargetCatalog.ResolveProfile(StyleIds.Fluent).MediaWidthDip;

            double layout = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaLayoutWidthDip(card, musicVisible: true);

            _ = (layout - TesseraFlyoutAnimatedTargetSpec.MediaClipWidthEpsilonDip).Should().Be(card);
        }

        [Fact]
        public void Fluent_stacked_media_panel_is_as_wide_as_the_card()
        {
            IReadOnlyList<TesseraStackedPanelPlacement> placements =
                TesseraStackedPlacementPolicy.EstimatePlacements(StyleIds.Fluent);

            TesseraStackedPanelPlacement media = placements.Single(p => p.Role == TesseraStackedPanelRole.Media);

            _ = media.WidthDip.Should().Be(TesseraFluentLayoutSpec.MediaWidthDip);
        }

        [Fact]
        public void Host_tessera_panels_do_not_derive_a_media_width_by_arithmetic()
        {
            List<string> hits = [];
            foreach (string file in SourceTree.EnumerateSources(Path.Combine("MosaicShell.Host", "Tiles", "Tessera")))
            {
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (DerivedMediaWidth.IsMatch(lines[i]))
                    {
                        hits.Add($"{SourceTree.RelativeToHost(file)}:{i + 1}: {lines[i].Trim()}");
                    }
                }
            }

            _ = hits.Should().BeEmpty(
                "a panel narrower than its profile's MediaWidthDip leaves the window and HWND region wider than the card:\n"
                + string.Join('\n', hits));
        }
    }
}
