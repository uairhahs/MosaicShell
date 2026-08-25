using FluentAssertions;
using MosaicShell.Core.Modules.Tessera;
using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Tests;

public class TesseraFlyoutTweenTargetCatalogTests
{
    [Fact]
    public void Catalog_covers_every_tessera_style_id()
    {
        var ids = StyleCatalog.IdsFor("Tessera");
        ids.Should().HaveCount(11);
        foreach (var id in ids)
        {
            var targets = TesseraFlyoutTweenTargetCatalog.ResolveAnimated(id);
            targets.Should().NotBeNull(id);
            TesseraFlyoutRevealSpec.StyleSupportsPhase2(id)
                .Should().Be(TesseraFlyoutTweenTargetCatalog.StyleSupportsPhase2(id), id);
            TesseraFlyoutRevealSpec.StyleIsPhase2NoOp(id)
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
        TesseraFlyoutTweenTargetCatalog.StyleSupportsPhase2(styleId).Should().BeTrue();
        TesseraFlyoutTweenTargetCatalog.ResolveAnimated(styleId).Should().NotBeEmpty();
        TesseraFlyoutTweenTargetCatalog.StyleIsPhase2NoOp(styleId).Should().BeFalse();
    }

    [Theory]
    [InlineData(StyleIds.Radial)]
    [InlineData("Smouti")]
    public void Radial_has_no_tween_node1_meters(string styleId)
    {
        TesseraFlyoutTweenTargetCatalog.StyleIsPhase2NoOp(styleId).Should().BeTrue();
        TesseraFlyoutTweenTargetCatalog.StyleSupportsPhase2(styleId).Should().BeFalse();
        TesseraFlyoutTweenTargetCatalog.ResolveAnimated(styleId).Should().BeEmpty();
        TesseraFlyoutAnimationPolicy.Phase2RequiresAnimatedLayout(2, styleId, showMediaStrip: true)
            .Should().BeFalse();
    }

    [Fact]
    public void Square_tweens_volume_labels_without_media_strip()
    {
        TesseraFlyoutTweenTargetCatalog.StylePhase2WithoutMediaStrip(StyleIds.Square).Should().BeTrue();
        TesseraFlyoutTweenTargetCatalog.HasChannel(StyleIds.Square, TesseraTweenChannel.LabelScale)
            .Should().BeTrue();
        TesseraFlyoutTweenTargetCatalog.HasChannel(StyleIds.Square, TesseraTweenChannel.ContentOpacity)
            .Should().BeFalse();
        TesseraFlyoutAnimatedTargetSpec.ResolveSquareLabelScale(0).Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveSquareLabelScale(1).Should().Be(1);
    }

    [Fact]
    public void Fluent_addresses_media_clip_divider_and_mask_not_volume()
    {
        AssertChannels(StyleIds.Fluent,
            TesseraTweenChannel.ClipWidth,
            TesseraTweenChannel.DividerHeight,
            TesseraTweenChannel.ContentOpacity);
        TesseraFlyoutTweenTargetCatalog.HasChannel(StyleIds.Fluent, TesseraTweenChannel.VolumeFillOpacity)
            .Should().BeFalse();
        TesseraFlyoutTweenTargetCatalog.VolumeStaysOpaqueDuringPhase2(StyleIds.Fluent).Should().BeTrue();
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaContentOpacity(0, true).Should().Be(1);
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentMediaContentOpacity(1, true).Should().Be(1);
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerHeightDip(148, 0, true).Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveFluentDividerHeightDip(148, 1, true).Should().Be(148);
    }

    [Fact]
    public void Windows11_addresses_shell_height_media_clip_and_mask_not_volume()
    {
        AssertChannels(StyleIds.Windows11,
            TesseraTweenChannel.ShellHeight,
            TesseraTweenChannel.ClipHeight,
            TesseraTweenChannel.ContentOpacity);
        TesseraFlyoutTweenTargetCatalog.VolumeStaysOpaqueDuringPhase2(StyleIds.Windows11).Should().BeTrue();
        TesseraFlyoutAnimatedTargetSpec.ResolveWin11MediaClipHeightDip(175, 0, true).Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveWin11MediaClipHeightDip(175, 1, true).Should().Be(175);
        TesseraFlyoutAnimatedTargetSpec.ResolveMediaMaskOpacity(0, true).Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveWin11BorderHeightDip(50, 175, 0, true).Should().Be(50);
    }

    [Fact]
    public void Gnome_addresses_media_scale_mask_and_volume_fill()
    {
        AssertChannels(StyleIds.Gnome,
            TesseraTweenChannel.ContentScale,
            TesseraTweenChannel.ContentOpacity,
            TesseraTweenChannel.VolumeFillOpacity);
        TesseraFlyoutTweenTargetCatalog.VolumeStaysOpaqueDuringPhase2(StyleIds.Gnome).Should().BeFalse();
        TesseraFlyoutAnimatedTargetSpec.ResolveGnomeContentScale(0, true).Should().Be(0.5);
        TesseraFlyoutAnimatedTargetSpec.ResolveMediaMaskOpacity(0, true).Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveGnomeVolumeFillOpacity(0, true).Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveGnomeVolumeFillOpacity(1, true).Should().Be(1);
    }

    [Fact]
    public void CoreUi_addresses_volume_bar_scale_and_media_clip_mask()
    {
        AssertChannels(StyleIds.CoreUI,
            TesseraTweenChannel.VolumeBarScale,
            TesseraTweenChannel.ClipWidth,
            TesseraTweenChannel.ContentOpacity);
        TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiVolumeBarScaleFactor(0, true).Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaClipWidthDip(280, 0, true).Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaContentOpacityFactor(0, true).Should().Be(1);
        TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaContentOpacityFactor(1, true).Should().Be(1);
        TesseraFlyoutAnimatedTargetSpec.ResolveCoreUiMediaSlideOffsetDip(388, 0.5, true).Should().Be(0);
    }

    [Fact]
    public void PlainText_addresses_media_slide_and_fill()
    {
        AssertChannels(StyleIds.PlainText,
            TesseraTweenChannel.SlideOffset,
            TesseraTweenChannel.ContentOpacity);
        var w = TesseraStackedPlacementSpec.PlainTextWidthDip;
        TesseraFlyoutAnimatedTargetSpec.ResolvePlainTextSlideOffsetDip(w, 1, 0, true)
            .Should().BeApproximately(-(w - 25), 0.01);
        TesseraFlyoutAnimatedTargetSpec.ResolvePlainTextFillOpacityFactor(0, true).Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolvePlainTextFillOpacityFactor(1, true).Should().Be(1);
    }

    [Fact]
    public void Meter_addresses_media_slide_and_mask()
    {
        AssertChannels(StyleIds.Meter,
            TesseraTweenChannel.SlideOffset,
            TesseraTweenChannel.ContentOpacity);
        TesseraFlyoutTweenTargetCatalog.VolumeStaysOpaqueDuringPhase2(StyleIds.Meter).Should().BeTrue();
        TesseraFlyoutAnimatedTargetSpec.ResolveMeterMediaSlideOffsetDip(0, true)
            .Should().Be(TesseraFlyoutAnimatedTargetSpec.MeterMediaSlideRestDip);
        TesseraFlyoutAnimatedTargetSpec.ResolveMeterMediaSlideOffsetDip(1, true).Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveMediaMaskOpacity(0, true).Should().Be(0);
    }

    [Fact]
    public void Compact_addresses_media_slide_and_mask()
    {
        AssertChannels(StyleIds.Compact,
            TesseraTweenChannel.SlideOffset,
            TesseraTweenChannel.ContentOpacity);
        TesseraFlyoutAnimatedTargetSpec.ResolveCompactMediaSlideOffsetDip(
                TesseraStackedPlacementSpec.CompactVolumeWidthDip, 20, 0, true)
            .Should().BeApproximately(
                -(TesseraStackedPlacementSpec.CompactVolumeWidthDip / 2 + 10), 0.01);
        TesseraFlyoutAnimatedTargetSpec.ResolveCompactMediaSlideOffsetDip(
                TesseraStackedPlacementSpec.CompactVolumeWidthDip, 20, 1, true)
            .Should().BeApproximately(0, 0.01);
    }

    [Fact]
    public void ModernFlyouts_addresses_media_clip_height_and_mask()
    {
        AssertChannels(StyleIds.ModernFlyouts,
            TesseraTweenChannel.ClipHeight,
            TesseraTweenChannel.ContentOpacity);
        var h = TesseraStackedPlacementSpec.ModernFlyoutsMediaHeightDip;
        TesseraFlyoutAnimatedTargetSpec.ResolveModernMediaClipHeightDip(h, 0, true).Should().Be(0);
        TesseraFlyoutAnimatedTargetSpec.ResolveModernMediaClipHeightDip(h, 1, true).Should().Be(h);
    }

    [Fact]
    public void MaterialYou_addresses_in_layout_media_slide_and_mask()
    {
        TesseraFlyoutTweenTargetCatalog.StylePhase2UsesInLayoutMedia(StyleIds.MaterialYou)
            .Should().BeTrue();
        TesseraFlyoutTweenTargetCatalog.StyleRequestsVolumeMediaChrome(StyleIds.MaterialYou)
            .Should().BeTrue();
        AssertChannels(StyleIds.MaterialYou,
            TesseraTweenChannel.SlideOffset,
            TesseraTweenChannel.ContentOpacity);
        var col = TesseraFlyoutAnimatedTargetSpec.MaterialYouColumnWidthDip;
        TesseraFlyoutAnimatedTargetSpec.ResolveMaterialYouMediaSlideOffsetDip(0, true)
            .Should().Be(col);
        TesseraFlyoutAnimatedTargetSpec.ResolveMaterialYouMediaSlideOffsetDip(1, true).Should().Be(0);
        TesseraFlyoutAnimationPolicy.Phase2RequiresAnimatedLayout(2, StyleIds.MaterialYou, true)
            .Should().BeTrue();
        TesseraFlyoutAnimationPolicy.Phase2RequiresAnimatedLayout(2, StyleIds.MaterialYou, false)
            .Should().BeFalse();
    }

    [Fact]
    public void Nested_media_children_are_not_independent_tween_targets()
    {
        foreach (var id in StyleCatalog.IdsFor("Tessera"))
        {
            foreach (var target in TesseraFlyoutTweenTargetCatalog.ResolveAnimated(id))
            {
                TesseraFlyoutTweenTargetCatalog.IndependentMediaChildrenMustInheritContainer
                    .Should().BeTrue();
                var nested = new[]
                {
                    "MediaImage", "MediaTrack", "MediaArtist", "MediaPlayPause",
                    "ProgBar", "Previous", "Next", "MediaHeart"
                };
                nested.Should().NotContain(target.MeterId);
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
        var fancy = TesseraFlyoutAnimationPolicy.Phase2RequiresAnimatedLayout(2, style, media);
        TesseraFlyoutRevealSpec.ResolveHideRevealProgress(fancy).Should().Be(expected);
    }

    [Fact]
    public void Media_opacity_follows_tween_node1_for_animated_masks()
    {
        foreach (var id in new[]
                 {
                     StyleIds.Fluent, StyleIds.Windows11, StyleIds.Gnome, StyleIds.PlainText,
                     StyleIds.CoreUI, StyleIds.Meter, StyleIds.Compact, StyleIds.ModernFlyouts,
                     StyleIds.MaterialYou
                 })
        {
            TesseraFlyoutRevealSpec.ResolveMediaOpacity(id, 0, musicVisible: true)
                .Should().Be(0, id);
            TesseraFlyoutRevealSpec.ResolveMediaOpacity(id, 1, musicVisible: true)
                .Should().Be(1, id);
        }

        TesseraFlyoutRevealSpec.ResolveMediaOpacity(StyleIds.Square, 0, true).Should().Be(1);
        TesseraFlyoutRevealSpec.ResolveMediaOpacity(StyleIds.Radial, 0, true).Should().Be(1);
    }

    [Fact]
    public void Style_profile_rest_sizes_match_layout_specs()
    {
        TesseraFlyoutRevealSpec.HostMustDispatchRevealFromCatalogKind.Should().BeTrue();
        var fluent = TesseraFlyoutTweenTargetCatalog.ResolveProfile(StyleIds.Fluent);
        fluent.RevealKind.Should().Be(TesseraFlyoutRevealKind.Fluent);
        fluent.MediaWidthDip.Should().Be(TesseraFluentLayoutSpec.MediaWidthDip);
        fluent.MediaHeightDip.Should().Be(TesseraFluentLayoutSpec.HeightDip);
        var coreUi = TesseraFlyoutTweenTargetCatalog.ResolveProfile(StyleIds.CoreUI);
        coreUi.VolumeWidthDip.Should().Be(TesseraCoreUiLayoutSpec.WidthDip);
        coreUi.VolumeHeightDip.Should().Be(TesseraCoreUiLayoutSpec.VolumeHeightDip);
        coreUi.MediaWidthDip.Should().Be(TesseraCoreUiLayoutSpec.InnerRowWidthDip);
        coreUi.MediaHeightDip.Should().Be(TesseraCoreUiLayoutSpec.MediaHeightDip);
        TesseraFlyoutRevealSpec.ResolveHostKind(StyleIds.Radial).Should().Be(TesseraFlyoutRevealKind.None);
        TesseraFlyoutTweenTargetCatalog.TryResolveMediaRestSizeDip(StyleIds.Fluent, out var w, out var h)
            .Should().BeTrue();
        w.Should().Be(TesseraFluentLayoutSpec.MediaWidthDip);
        h.Should().Be(TesseraFluentLayoutSpec.HeightDip);
    }

    private static void AssertChannels(string styleId, params TesseraTweenChannel[] expected)
    {
        foreach (var channel in expected)
            TesseraFlyoutTweenTargetCatalog.HasChannel(styleId, channel).Should().BeTrue(channel.ToString());
    }
}
