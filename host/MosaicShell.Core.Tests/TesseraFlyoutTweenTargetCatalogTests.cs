using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests
{
    public class TesseraFlyoutTweenTargetCatalogTests
    {
        [Fact]
        public void Catalog_covers_every_tessera_style_id()
        {
            IReadOnlyList<string> ids = StyleCatalog.IdsFor("Tessera");
            _ = ids.Should().HaveCount(11);
            foreach (string id in ids)
            {
                IReadOnlyList<TesseraTweenTarget> targets = TesseraFlyoutTweenTargetCatalog.ResolveAnimated(id);
                _ = targets.Should().NotBeNull(id);
                _ = TesseraFlyoutRevealSpec.StyleSupportsPhase2(id)
                    .Should().Be(TesseraFlyoutTweenTargetCatalog.StyleSupportsPhase2(id), id);
                _ = TesseraFlyoutRevealSpec.StyleIsPhase2NoOp(id)
                    .Should().Be(TesseraFlyoutTweenTargetCatalog.StyleIsPhase2NoOp(id), id);
            }
        }

        [Theory]
        [InlineData(StyleIds.Fluent)]
        [InlineData(StyleIds.Windows11)]
        [InlineData(StyleIds.Gnome)]
        [InlineData(StyleIds.PlainText)]
        [InlineData(StyleIds.CoreUI)]
        [InlineData(StyleIds.Square)]
        [InlineData(StyleIds.Meter)]
        [InlineData(StyleIds.Compact)]
        [InlineData(StyleIds.ModernFlyouts)]
        [InlineData(StyleIds.MaterialYou)]
        public void Fancy_phase2_styles_have_animated_targets(string styleId)
        {
            _ = TesseraFlyoutTweenTargetCatalog.StyleSupportsPhase2(styleId).Should().BeTrue();
            _ = TesseraFlyoutTweenTargetCatalog.ResolveAnimated(styleId).Should().NotBeEmpty();
            _ = TesseraFlyoutTweenTargetCatalog.StyleIsPhase2NoOp(styleId).Should().BeFalse();
        }

        /// <summary>
        /// Radial (upstream "Smouti") comments its TweenNode1 binders out, but Host addresses the
        /// ring and side media directly, so both aliases resolve to a phase-2 capable profile.
        /// </summary>
        [Theory]
        [InlineData(StyleIds.Radial)]
        [InlineData("Smouti")]
        public void Radial_addresses_tween_node1_meters(string styleId)
        {
            _ = TesseraFlyoutTweenTargetCatalog.StyleIsPhase2NoOp(styleId).Should().BeFalse();
            _ = TesseraFlyoutTweenTargetCatalog.StyleSupportsPhase2(styleId).Should().BeTrue();
            _ = TesseraFlyoutTweenTargetCatalog.ResolveAnimated(styleId).Should().NotBeEmpty();
            _ = TesseraFlyoutAnimationPolicy.Phase2RequiresAnimatedLayout(2, styleId, showMediaStrip: true)
                .Should().BeTrue();
        }

        /// <summary>No style is a phase-2 no-op now that Radial is addressable.</summary>
        [Fact]
        public void Phase2_no_op_is_derived_from_having_no_targets()
        {
            foreach (string id in StyleCatalog.IdsFor("Tessera"))
            {
                _ = TesseraFlyoutTweenTargetCatalog.StyleIsPhase2NoOp(id)
                    .Should().Be(TesseraFlyoutTweenTargetCatalog.ResolveAnimated(id).Count == 0, id);
            }

            _ = TesseraFlyoutTweenTargetCatalog.StyleIsPhase2NoOp("unknown-style").Should().BeTrue();
        }

        [Fact]
        public void Square_tweens_volume_labels_without_media_strip()
        {
            _ = TesseraFlyoutTweenTargetCatalog.StylePhase2WithoutMediaStrip(StyleIds.Square).Should().BeTrue();
            _ = TesseraFlyoutTweenTargetCatalog.HasChannel(StyleIds.Square, TesseraTweenChannel.LabelScale)
                .Should().BeTrue();
            _ = TesseraFlyoutTweenTargetCatalog.HasChannel(StyleIds.Square, TesseraTweenChannel.ContentOpacity)
                .Should().BeFalse();
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveSquareLabelScale(0).Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveSquareLabelScale(1).Should().Be(1);
        }

        [Fact]
        public void Fluent_addresses_media_clip_divider_and_mask_not_volume()
        {
            AssertChannels(StyleIds.Fluent,
                TesseraTweenChannel.ClipWidth,
                TesseraTweenChannel.DividerHeight,
                TesseraTweenChannel.ContentOpacity);
            _ = TesseraFlyoutTweenTargetCatalog.HasChannel(StyleIds.Fluent, TesseraTweenChannel.VolumeFillOpacity)
                .Should().BeFalse();
            _ = TesseraFlyoutTweenTargetCatalog.VolumeStaysOpaqueDuringPhase2(StyleIds.Fluent).Should().BeTrue();
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaContentOpacity(0, true).Should().Be(1);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaContentOpacity(1, true).Should().Be(1);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerHeightDip(148, 0, true).Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerHeightDip(148, 1, true).Should().Be(148);
        }

        [Fact]
        public void Windows11_addresses_shell_height_media_clip_and_mask_not_volume()
        {
            AssertChannels(StyleIds.Windows11,
                TesseraTweenChannel.ShellHeight,
                TesseraTweenChannel.ClipHeight,
                TesseraTweenChannel.ContentOpacity);
            _ = TesseraFlyoutTweenTargetCatalog.VolumeStaysOpaqueDuringPhase2(StyleIds.Windows11).Should().BeTrue();
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveWin11MediaClipHeightDip(175, 0, true).Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveWin11MediaClipHeightDip(175, 1, true).Should().Be(175);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveMediaMaskOpacity(0, true).Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveWin11BorderHeightDip(50, 175, 0, true).Should().Be(50);
        }

        [Fact]
        public void Gnome_addresses_media_scale_mask_and_volume_fill()
        {
            AssertChannels(StyleIds.Gnome,
                TesseraTweenChannel.ContentScale,
                TesseraTweenChannel.ContentOpacity,
                TesseraTweenChannel.VolumeFillOpacity);
            _ = TesseraFlyoutTweenTargetCatalog.VolumeStaysOpaqueDuringPhase2(StyleIds.Gnome).Should().BeFalse();
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveGnomeContentScale(0, true).Should().Be(0.5);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveMediaMaskOpacity(0, true).Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveGnomeVolumeFillOpacity(0, true).Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveGnomeVolumeFillOpacity(1, true).Should().Be(1);
        }

        [Fact]
        public void CoreUi_addresses_volume_bar_scale_and_media_clip_mask()
        {
            AssertChannels(StyleIds.CoreUI,
                TesseraTweenChannel.VolumeBarScale,
                TesseraTweenChannel.ClipWidth,
                TesseraTweenChannel.ContentOpacity);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiVolumeBarScaleFactor(0, true).Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaClipWidthDip(280, 0, true).Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaContentOpacityFactor(0, true).Should().Be(1);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaContentOpacityFactor(1, true).Should().Be(1);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaSlideOffsetDip(388, 0.5, true).Should().Be(0);
        }

        /// <summary>
        /// ADR-0001: PlainText/Meter/Compact/MaterialYou/Radial declare a SlideOffset meter (the
        /// upstream skin animates it), but Host renders it as fade/clip only, matching every other
        /// style: a second, independently-directioned translate on top of the window's own
        /// phase-1 slide made the entrance appear to reverse direction mid-flight.
        /// </summary>
        [Fact]
        public void PlainText_addresses_media_fill_without_a_second_slide()
        {
            AssertChannels(StyleIds.PlainText,
                TesseraTweenChannel.SlideOffset,
                TesseraTweenChannel.ContentOpacity);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolvePlainTextFillOpacityFactor(0, true).Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolvePlainTextFillOpacityFactor(1, true).Should().Be(1);
        }

        [Fact]
        public void Meter_addresses_media_mask_without_a_second_slide()
        {
            AssertChannels(StyleIds.Meter,
                TesseraTweenChannel.SlideOffset,
                TesseraTweenChannel.ContentOpacity);
            _ = TesseraFlyoutTweenTargetCatalog.VolumeStaysOpaqueDuringPhase2(StyleIds.Meter).Should().BeTrue();
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveMediaMaskOpacity(0, true).Should().Be(0);
        }

        [Fact]
        public void Compact_addresses_media_mask_without_a_second_slide()
        {
            AssertChannels(StyleIds.Compact,
                TesseraTweenChannel.SlideOffset,
                TesseraTweenChannel.ContentOpacity);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveMediaMaskOpacity(0, true).Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveMediaMaskOpacity(1, true).Should().Be(1);
        }

        [Fact]
        public void ModernFlyouts_addresses_media_clip_height_and_mask()
        {
            AssertChannels(StyleIds.ModernFlyouts,
                TesseraTweenChannel.ClipHeight,
                TesseraTweenChannel.ContentOpacity);
            double h = TesseraStackedPlacementSpec.ModernFlyoutsMediaHeightDip;
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveModernMediaClipHeightDip(h, 0, true).Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveModernMediaClipHeightDip(h, 1, true).Should().Be(h);
        }

        [Fact]
        public void MaterialYou_addresses_in_layout_media_mask_without_a_second_slide()
        {
            _ = TesseraFlyoutTweenTargetCatalog.StylePhase2UsesInLayoutMedia(StyleIds.MaterialYou)
                .Should().BeTrue();
            _ = TesseraFlyoutTweenTargetCatalog.StyleRequestsVolumeMediaChrome(StyleIds.MaterialYou)
                .Should().BeTrue();
            AssertChannels(StyleIds.MaterialYou,
                TesseraTweenChannel.SlideOffset,
                TesseraTweenChannel.ContentOpacity);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveMediaMaskOpacity(0, true).Should().Be(0);
            _ = TesseraFlyoutAnimationPolicy.Phase2RequiresAnimatedLayout(2, StyleIds.MaterialYou, true)
                .Should().BeTrue();
            _ = TesseraFlyoutAnimationPolicy.Phase2RequiresAnimatedLayout(2, StyleIds.MaterialYou, false)
                .Should().BeFalse();
        }

        /// <summary>
        /// ADR-0001: the media rest width has one owner (the profile). Host must not restate it
        /// at a call site.
        /// </summary>
        [Fact]
        public void MaterialYou_media_rest_width_has_a_single_source_of_truth()
        {
            TesseraFlyoutStyleProfile profile = TesseraFlyoutTweenTargetCatalog.ResolveProfile(StyleIds.MaterialYou);
            _ = profile.MediaWidthDip.Should().Be(TesseraFlyoutAnimatedTargetSpec.MaterialYouColumnWidthDip);

            _ = TesseraFlyoutTweenTargetCatalog.TryResolveMediaRestSizeDip(
                StyleIds.MaterialYou, out double widthDip, out _).Should().BeTrue();
            _ = widthDip.Should().Be(profile.MediaWidthDip);
        }

        /// <summary>
        /// Radial's hero is the volume ring, and its media panel sits beside it. Both must be
        /// addressable by the tween engine: the ring arc sweeps up on entry, the side media
        /// slides and fades. Previously Radial fell through to the default catalog arm with no
        /// targets at all, so phase 2 had nothing to drive and the media panel never revealed.
        /// </summary>
        [Fact]
        public void Radial_addresses_ring_sweep_and_side_media()
        {
            AssertChannels(StyleIds.Radial,
                TesseraTweenChannel.RingSweep,
                TesseraTweenChannel.SlideOffset,
                TesseraTweenChannel.ContentOpacity);

            TesseraFlyoutStyleProfile profile = TesseraFlyoutTweenTargetCatalog.ResolveProfile(StyleIds.Radial);
            _ = profile.RevealKind.Should().Be(TesseraFlyoutRevealKind.Radial);
            _ = profile.MediaWidthDip.Should().Be(TesseraStackedPlacementSpec.RadialMediaWidthDip);
            _ = profile.VolumeWidthDip.Should().Be(TesseraStackedPlacementSpec.RadialVolumeWidthDip);

            // Arc sweeps from nothing to the live level; the ring's own Value owns it at rest.
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveRadialRingSweepFactor(0).Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveRadialRingSweepFactor(1).Should().Be(1);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveRadialRingSweepFactor(0.5).Should().Be(0.5);

            // Side media fades/clips only (ADR-0001): a second slide independent of the window's
            // own phase-1 motion made the entrance appear to reverse direction mid-flight.
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveMediaMaskOpacity(0, true).Should().Be(0);
            _ = TesseraFlyoutAnimatedTargetSpec.ResolveMediaMaskOpacity(1, true).Should().Be(1);

            _ = TesseraFlyoutTweenTargetCatalog.StyleIsPhase2NoOp(StyleIds.Radial).Should().BeFalse();
            _ = TesseraFlyoutTweenTargetCatalog.StyleSupportsPhase2(StyleIds.Radial).Should().BeTrue();
            _ = TesseraFlyoutTweenTargetCatalog.StyleRequestsVolumeMediaChrome(StyleIds.Radial)
                .Should().BeTrue("the side media panel must not render blank");
        }

        [Fact]
        public void Nested_media_children_are_not_independent_tween_targets()
        {
            foreach (string id in StyleCatalog.IdsFor("Tessera"))
            {
                foreach (TesseraTweenTarget target in TesseraFlyoutTweenTargetCatalog.ResolveAnimated(id))
                {
                    _ = TesseraFlyoutTweenTargetCatalog.IndependentMediaChildrenMustInheritContainer
                        .Should().BeTrue();
                    string[] nested =
                    [
                        "MediaImage", "MediaTrack", "MediaArtist", "MediaPlayPause",
                        "ProgBar", "Previous", "Next", "MediaHeart"
                    ];
                    _ = nested.Should().NotContain(target.MeterId);
                }
            }
        }

        [Theory]
        [InlineData(StyleIds.Meter, true, 0)]
        [InlineData(StyleIds.Compact, true, 0)]
        [InlineData(StyleIds.ModernFlyouts, true, 0)]
        [InlineData(StyleIds.MaterialYou, true, 0)]
        [InlineData(StyleIds.Fluent, false, 1)]
        public void Hide_pose_collapses_new_phase2_styles_when_fancy(
            string style, bool media, double expected)
        {
            bool fancy = TesseraFlyoutAnimationPolicy.Phase2RequiresAnimatedLayout(2, style, media);
            _ = TesseraFlyoutRevealSpec.ResolveHideRevealProgress(fancy).Should().Be(expected);
        }

        [Fact]
        public void Media_opacity_follows_tween_node1_for_animated_masks()
        {
            foreach (string? id in new[]
                     {
                         StyleIds.Fluent, StyleIds.Windows11, StyleIds.Gnome, StyleIds.PlainText,
                         StyleIds.CoreUI, StyleIds.Meter, StyleIds.Compact, StyleIds.ModernFlyouts,
                         StyleIds.MaterialYou, StyleIds.Radial
                     })
            {
                _ = TesseraFlyoutRevealSpec.ResolveMediaOpacity(id, 0, musicVisible: true)
                    .Should().Be(0, id);
                _ = TesseraFlyoutRevealSpec.ResolveMediaOpacity(id, 1, musicVisible: true)
                    .Should().Be(1, id);
            }

            // Square animates volume labels only, so its media opacity is not tween-driven.
            _ = TesseraFlyoutRevealSpec.ResolveMediaOpacity(StyleIds.Square, 0, true).Should().Be(1);
        }

        [Fact]
        public void Style_profile_rest_sizes_match_layout_specs()
        {
            _ = TesseraFlyoutRevealSpec.HostMustDispatchRevealFromCatalogKind.Should().BeTrue();
            TesseraFlyoutStyleProfile fluent = TesseraFlyoutTweenTargetCatalog.ResolveProfile(StyleIds.Fluent);
            _ = fluent.RevealKind.Should().Be(TesseraFlyoutRevealKind.Fluent);
            _ = fluent.MediaWidthDip.Should().Be(TesseraFluentLayoutSpec.MediaWidthDip);
            _ = fluent.MediaHeightDip.Should().Be(TesseraFluentLayoutSpec.HeightDip);
            TesseraFlyoutStyleProfile coreUi = TesseraFlyoutTweenTargetCatalog.ResolveProfile(StyleIds.CoreUI);
            _ = coreUi.VolumeWidthDip.Should().Be(TesseraCoreUiLayoutSpec.WidthDip);
            _ = coreUi.VolumeHeightDip.Should().Be(TesseraCoreUiLayoutSpec.VolumeHeightDip);
            _ = coreUi.MediaWidthDip.Should().Be(TesseraCoreUiLayoutSpec.InnerRowWidthDip);
            _ = coreUi.MediaHeightDip.Should().Be(TesseraCoreUiLayoutSpec.MediaHeightDip);
            _ = TesseraFlyoutRevealSpec.ResolveHostKind(StyleIds.Radial).Should().Be(TesseraFlyoutRevealKind.Radial);
            _ = TesseraFlyoutRevealSpec.ResolveHostKind("unknown-style").Should().Be(TesseraFlyoutRevealKind.None);
            _ = TesseraFlyoutTweenTargetCatalog.TryResolveMediaRestSizeDip(StyleIds.Fluent, out double w, out double h)
                .Should().BeTrue();
            _ = w.Should().Be(TesseraFluentLayoutSpec.MediaWidthDip);
            _ = h.Should().Be(TesseraFluentLayoutSpec.HeightDip);
        }

        private static void AssertChannels(string styleId, params TesseraTweenChannel[] expected)
        {
            foreach (TesseraTweenChannel channel in expected)
            {
                _ = TesseraFlyoutTweenTargetCatalog.HasChannel(styleId, channel).Should().BeTrue(channel.ToString());
            }
        }
    }
}
