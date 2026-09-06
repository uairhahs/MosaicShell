using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// ADR-0001 enforcement. Per-style facts have one owner; these assert the derived answers
    /// agree for every style in the catalog, so two tables cannot drift apart silently.
    /// Add an invariant here whenever a per-style dispatch point is folded into the profile.
    /// </summary>
    public class TesseraFlyoutStyleProfileConsistencyTests
    {
        public static TheoryData<string> TesseraStyleIds()
        {
            TheoryData<string> data = [.. StyleCatalog.IdsFor("Tessera")];

            return data;
        }

        /// <summary>
        /// A style needs an animated HWND reveal region exactly when it has phase-2 targets and
        /// those targets clip real chrome. Square animates labels only, so it has no region.
        /// Guards the former hand-maintained nine-style equality chain.
        /// </summary>
        [Theory]
        [MemberData(nameof(TesseraStyleIds))]
        public void Reveal_region_need_follows_phase2_support(string styleId)
        {
            bool expected = TesseraFlyoutTweenTargetCatalog.StyleSupportsPhase2(styleId)
                           && !TesseraFlyoutTweenTargetCatalog.StylePhase2WithoutMediaStrip(styleId);

            _ = TesseraFlyoutHwndRegionSpec.StyleNeedsRevealRegion(styleId, musicVisible: true, stackedRole: null)
                .Should().Be(expected);
        }

        /// <summary>No style needs a reveal region when the media strip is not showing.</summary>
        [Theory]
        [MemberData(nameof(TesseraStyleIds))]
        public void No_reveal_region_without_music(string styleId)
        {
            _ = TesseraFlyoutHwndRegionSpec.StyleNeedsRevealRegion(styleId, musicVisible: false, stackedRole: null)
                .Should().BeFalse();
        }

        /// <summary>
        /// In-layout media (MaterialYou) lives in the volume HWND, so it must never also claim
        /// the stacked media strip. This is the pairing the pre-ADR MaterialYou bug sat between.
        /// </summary>
        [Theory]
        [MemberData(nameof(TesseraStyleIds))]
        public void In_layout_media_is_never_also_a_stacked_strip(string styleId)
        {
            if (!TesseraFlyoutTweenTargetCatalog.StylePhase2UsesInLayoutMedia(styleId))
            {
                return;
            }

            _ = TesseraLayoutCoverage.UsesStackedMediaStrip(styleId).Should().BeFalse();
        }

        /// <summary>
        /// A style advertises volume media chrome exactly when it animates a media meter.
        /// Square runs Animated fonts on the volume card with no media strip, and Radial has no
        /// phase-2 targets at all, so neither may claim it.
        /// </summary>
        [Theory]
        [MemberData(nameof(TesseraStyleIds))]
        public void Volume_media_chrome_follows_media_targets(string styleId)
        {
            bool animatesMedia = TesseraFlyoutTweenTargetCatalog.ResolveAnimated(styleId)
                .Any(t => t.MeterId.StartsWith("Media", StringComparison.OrdinalIgnoreCase));

            _ = TesseraFlyoutTweenTargetCatalog.StyleRequestsVolumeMediaChrome(styleId)
                .Should().Be(animatesMedia);
        }

        [Fact]
        public void Styles_without_a_media_strip_do_not_request_media_chrome()
        {
            _ = TesseraFlyoutTweenTargetCatalog.StyleRequestsVolumeMediaChrome(StyleIds.Square)
                .Should().BeFalse();
            _ = TesseraFlyoutTweenTargetCatalog.StyleRequestsVolumeMediaChrome(StyleIds.MaterialYou)
                .Should().BeTrue();
            // Radial's side media panel is tween-addressable, so it does present media chrome.
            _ = TesseraFlyoutTweenTargetCatalog.StyleRequestsVolumeMediaChrome(StyleIds.Radial)
                .Should().BeTrue();
            _ = TesseraFlyoutTweenTargetCatalog.StyleRequestsVolumeMediaChrome(StyleIds.Fluent)
                .Should().BeTrue();
        }

        /// <summary>A phase-2 no-op style has no animated targets to drive.</summary>
        [Theory]
        [MemberData(nameof(TesseraStyleIds))]
        public void Phase2_no_op_styles_have_no_animated_targets(string styleId)
        {
            if (!TesseraFlyoutTweenTargetCatalog.StyleIsPhase2NoOp(styleId))
            {
                return;
            }

            _ = TesseraFlyoutTweenTargetCatalog.ResolveAnimated(styleId).Should().BeEmpty();
            _ = TesseraFlyoutTweenTargetCatalog.StyleSupportsPhase2(styleId).Should().BeFalse();
        }

        /// <summary>
        /// A style that animates a media meter must publish a media rest size for the binders to
        /// clip and slide against. Without one the Host falls back to a local literal, which is
        /// exactly how the MaterialYou width came to be stated in three places.
        /// </summary>
        [Theory]
        [MemberData(nameof(TesseraStyleIds))]
        public void Styles_animating_media_meters_publish_a_media_rest_size(string styleId)
        {
            bool animatesMedia = TesseraFlyoutTweenTargetCatalog.ResolveAnimated(styleId)
                .Any(t => t.MeterId.StartsWith("Media", StringComparison.OrdinalIgnoreCase));
            if (!animatesMedia)
            {
                return;
            }

            _ = TesseraFlyoutTweenTargetCatalog.TryResolveMediaRestSizeDip(styleId, out double w, out double h)
                .Should().BeTrue($"{styleId} animates a media meter");
            _ = (w > 1 || h > 1).Should().BeTrue();
        }
    }
}
