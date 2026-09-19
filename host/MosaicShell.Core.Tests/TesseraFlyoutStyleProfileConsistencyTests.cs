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

        /// <summary>
        /// Batch 5. <c>TesseraStackedPlacementPolicy.ResolveLayoutKind</c> used to be the sole
        /// owner of "which cluster shape does this style use"; the profile now carries the same
        /// fact so Host reveal code and stacked-placement code cannot disagree about it.
        /// </summary>
        [Theory]
        [MemberData(nameof(TesseraStyleIds))]
        public void Stacked_layout_kind_matches_profile(string styleId)
        {
            _ = TesseraStackedPlacementPolicy.ResolveLayoutKind(styleId)
                .Should().Be(TesseraFlyoutTweenTargetCatalog.ResolveProfile(styleId).LayoutKind);
        }

        /// <summary>Same drift risk as above, for H3 multi-window OS-acrylic eligibility.</summary>
        [Theory]
        [MemberData(nameof(TesseraStyleIds))]
        public void Stacked_os_acrylic_support_matches_profile(string styleId)
        {
            _ = TesseraStackedPlacementPolicy.SupportsStackedOsAcrylic(styleId)
                .Should().Be(TesseraFlyoutTweenTargetCatalog.ResolveProfile(styleId).SupportsStackedOsAcrylic);
        }

        /// <summary>
        /// Styles Square, CoreUI, and MaterialYou never participate in H3 stacked placement
        /// (<c>ResolveLayoutKind</c> and <c>EstimatePlacements</c> both fall through to their
        /// shared default arm for these three), so the rest-size invariant below does not apply
        /// to them - their profile sizes serve reveal binders, not a stacked-cluster estimate.
        /// </summary>
        private static readonly HashSet<string> StylesWithoutStackedPlacement = new(StringComparer.OrdinalIgnoreCase)
        {
            StyleIds.Square, StyleIds.CoreUI, StyleIds.MaterialYou,
        };

        /// <summary>
        /// The placement estimate's volume/media rest sizes must equal the profile's rest sizes -
        /// two independent per-style width/height tables is exactly the shape that let the Win11
        /// media height (and six other styles' dimensions) be restated instead of shared. Fluent's
        /// volume slot is the one documented exception: its placement width folds in the divider
        /// column that lives inside the volume HWND slot (<c>ResolveFluentCollapsedShellWidthDip</c>),
        /// which the profile's plain rest width deliberately does not carry.
        /// </summary>
        [Theory]
        [MemberData(nameof(TesseraStyleIds))]
        public void Stacked_placement_rest_sizes_match_profile(string styleId)
        {
            if (StylesWithoutStackedPlacement.Contains(StyleIds.Normalize(styleId)))
            {
                return;
            }

            TesseraFlyoutStyleProfile profile = TesseraFlyoutTweenTargetCatalog.ResolveProfile(styleId);
            IReadOnlyList<TesseraStackedPanelPlacement> placements = TesseraStackedPlacementPolicy.EstimatePlacements(styleId);

            TesseraStackedPanelPlacement media = placements.Single(p => p.Role == TesseraStackedPanelRole.Media);
            _ = media.WidthDip.Should().Be(profile.MediaWidthDip);
            _ = media.HeightDip.Should().Be(profile.MediaHeightDip);

            if (StyleIds.Normalize(styleId).Equals(StyleIds.Fluent, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            TesseraStackedPanelPlacement volume = placements.Single(p => p.Role == TesseraStackedPanelRole.Volume);
            _ = volume.WidthDip.Should().Be(profile.VolumeWidthDip);
            _ = volume.HeightDip.Should().Be(profile.VolumeHeightDip);
        }

        /// <summary>
        /// Batch 6. <c>TesseraOsAcrylicStackedPolicy.ResolvePanelCornerRadiusDip</c>'s (style,
        /// role) design-radius switch used to be the sole owner of Meter's spine/pill split and
        /// Gnome's pill radius; the profile now carries both per-role values.
        /// </summary>
        [Theory]
        [MemberData(nameof(TesseraStyleIds))]
        public void Panel_corner_radius_matches_profile(string styleId)
        {
            TesseraFlyoutStyleProfile profile = TesseraFlyoutTweenTargetCatalog.ResolveProfile(styleId);
            _ = TesseraOsAcrylicStackedPolicy.ResolvePanelCornerRadiusDip(styleId, TesseraStackedPanelRole.Volume, 1000, 1000)
                .Should().Be(profile.VolumeCornerRadiusDip);
            _ = TesseraOsAcrylicStackedPolicy.ResolvePanelCornerRadiusDip(styleId, TesseraStackedPanelRole.Media, 1000, 1000)
                .Should().Be(profile.MediaCornerRadiusDip);
        }

        /// <summary>
        /// Batch 6. Only CoreUI is the multi-tile style; the profile now carries that fact
        /// instead of <c>IsCoreUiMultiTile</c> re-deriving it via a normalize-and-compare.
        /// </summary>
        [Theory]
        [MemberData(nameof(TesseraStyleIds))]
        public void Core_ui_multi_tile_flag_matches_profile(string styleId)
        {
            _ = TesseraFlyoutTweenTargetCatalog.ResolveProfile(styleId).SupportsCoreUiMultiTile
                .Should().Be(StyleIds.Normalize(styleId).Equals(StyleIds.CoreUI, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Batch 8. <c>StylePhase2WithoutMediaStrip</c> and <c>StylePhase2UsesInLayoutMedia</c>
        /// each re-derived their one-style answer via a normalize-and-compare; the profile now
        /// carries both facts directly.
        /// </summary>
        [Theory]
        [MemberData(nameof(TesseraStyleIds))]
        public void Phase2_without_media_strip_matches_profile(string styleId)
        {
            _ = TesseraFlyoutTweenTargetCatalog.StylePhase2WithoutMediaStrip(styleId)
                .Should().Be(TesseraFlyoutTweenTargetCatalog.ResolveProfile(styleId).PhaseTwoWithoutMediaStrip);
        }

        [Theory]
        [MemberData(nameof(TesseraStyleIds))]
        public void Phase2_uses_in_layout_media_matches_profile(string styleId)
        {
            _ = TesseraFlyoutTweenTargetCatalog.StylePhase2UsesInLayoutMedia(styleId)
                .Should().Be(TesseraFlyoutTweenTargetCatalog.ResolveProfile(styleId).PhaseTwoUsesInLayoutMedia);
        }

        /// <summary>
        /// Batch 8. <c>TesseraStatusFlyoutPolicy.ResolveChipCornerRadiusDip</c>'s per-style switch
        /// used to be the sole owner of the status-chip radius; the profile now carries it.
        /// </summary>
        [Theory]
        [MemberData(nameof(TesseraStyleIds))]
        public void Status_chip_corner_radius_matches_profile(string styleId)
        {
            _ = TesseraStatusFlyoutPolicy.ResolveChipCornerRadiusDip(styleId)
                .Should().Be(TesseraFlyoutTweenTargetCatalog.ResolveProfile(styleId).StatusChipCornerRadiusDip);
        }

        /// <summary>
        /// Batch 3. Only Windows11 needs a dedicated StrokeB HWND region; the profile now
        /// carries that fact instead of <c>StyleNeedsStrokeBRegion</c> re-deriving it via a
        /// normalize-and-compare.
        /// </summary>
        [Theory]
        [MemberData(nameof(TesseraStyleIds))]
        public void Stroke_b_region_need_matches_profile(string styleId)
        {
            _ = TesseraFlyoutHwndRegionSpec.StyleNeedsStrokeBRegion(styleId, musicVisible: true)
                .Should().Be(TesseraFlyoutTweenTargetCatalog.ResolveProfile(styleId).NeedsStrokeBRegion);
            _ = TesseraFlyoutHwndRegionSpec.StyleNeedsStrokeBRegion(styleId, musicVisible: false)
                .Should().BeFalse();
        }
    }
}
