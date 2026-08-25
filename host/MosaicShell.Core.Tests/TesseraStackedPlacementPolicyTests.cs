using FluentAssertions;

using MosaicShell.Core.Styles;

using MosaicShell.Core.Modules.Tessera;



namespace MosaicShell.Core.Tests;



public class TesseraStackedPlacementPolicyTests

{

    [Theory]

    [InlineData(StyleIds.Fluent, TesseraStackedLayoutKind.HorizontalVolumeFirst)]

    [InlineData(StyleIds.Meter, TesseraStackedLayoutKind.HorizontalMediaFirst)]

    [InlineData(StyleIds.Gnome, TesseraStackedLayoutKind.VerticalMediaFirst)]

    [InlineData(StyleIds.Compact, TesseraStackedLayoutKind.VerticalVolumeFirst)]

    [InlineData(StyleIds.Windows11, TesseraStackedLayoutKind.VerticalWin11)]

    [InlineData(StyleIds.PlainText, TesseraStackedLayoutKind.PlainTextColumn)]

    [InlineData(StyleIds.Radial, TesseraStackedLayoutKind.HorizontalRadial)]

    public void Resolve_layout_kind(string styleId, TesseraStackedLayoutKind expected) =>

        TesseraStackedPlacementPolicy.ResolveLayoutKind(styleId).Should().Be(expected);



    [Fact]

    public void Meter_places_media_before_volume_horizontally()

    {

        var placements = TesseraStackedPlacementPolicy.EstimatePlacements(StyleIds.Meter);

        placements.Should().HaveCount(2);

        placements[0].Role.Should().Be(TesseraStackedPanelRole.Media);

        placements[1].Role.Should().Be(TesseraStackedPanelRole.Volume);

        placements[1].OffsetXDip.Should().BeGreaterThan(placements[0].OffsetXDip);

        (placements[1].OffsetXDip - (placements[0].OffsetXDip + placements[0].WidthDip))
            .Should().Be(TesseraStackedPlacementSpec.MeterGapDip);

    }



    [Fact]

    public void Sanitize_stacked_measure_rejects_screen_bleed()

    {

        TesseraStackedPlacementPolicy.SanitizeStackedPanelMeasure(340, 1646).Should().Be(0);

        TesseraStackedPlacementPolicy.SanitizeStackedPanelMeasure(64, 858).Should().Be(0);

        TesseraStackedPlacementPolicy.SanitizeStackedPanelMeasure(236, 260).Should().Be(260);

    }



    [Fact]

    public void Gnome_compute_ignores_inflated_hwnd_measure()

    {

        var placements = TesseraStackedPlacementPolicy.ComputePlacements(

            StyleIds.Gnome,

            volumeWidthDip: 1646,

            volumeHeightDip: 858,

            mediaWidthDip: 1646,

            mediaHeightDip: 858);

        placements[0].WidthDip.Should().Be(TesseraStackedPlacementSpec.GnomeMediaWidthDip);

        placements[0].HeightDip.Should().Be(TesseraStackedPlacementSpec.GnomeMediaHeightDip);

        placements[1].WidthDip.Should().Be(TesseraStackedPlacementSpec.GnomeVolumeWidthDip);

    }



    [Fact]

    public void Resolve_stacked_client_size_never_shrinks_below_measured()

    {

        TesseraStackedPlacementPolicy.ResolveStackedClientSize(320, 120, 320, 190)
            .Should().Be((320, 190));

        TesseraStackedPlacementPolicy.ResolveStackedClientSize(280, 96, 340, 128)
            .Should().Be((340, 128));

        TesseraStackedPlacementPolicy.ResolveStackedClientSize(220, 236, 210, 250)
            .Should().Be((220, 250));

        TesseraStackedPlacementPolicy.ResolveStackedClientSize(320, 190, 300, 100)
            .Should().Be((320, 190));

        TesseraStackedPlacementPolicy.ResolveStackedClientSize(340, 64, 1646, 858)
            .Should().Be((340, 64));

    }



    [Fact]

    public void Compact_and_modern_media_estimates_clear_known_content_floors()

    {

        TesseraStackedPlacementSpec.CompactMediaHeightDip.Should().BeGreaterThanOrEqualTo(128);

        TesseraStackedPlacementSpec.ModernFlyoutsMediaHeightDip.Should().BeGreaterThanOrEqualTo(190);

        TesseraStackedPlacementSpec.GnomeMediaHeightDip.Should().BeGreaterThanOrEqualTo(64);

    }



    [Fact]

    public void Meter_compute_grows_media_height_from_measured_without_shrinking_width()

    {

        var grown = TesseraStackedPlacementPolicy.ComputePlacements(

            StyleIds.Meter,

            volumeWidthDip: 28,

            volumeHeightDip: 200,

            mediaWidthDip: 28,

            mediaHeightDip: 260);

        grown[0].WidthDip.Should().Be(TesseraStackedPlacementSpec.MeterMediaWidthDip);

        grown[0].HeightDip.Should().Be(260);

        grown[1].OffsetXDip.Should().Be(
            TesseraStackedPlacementSpec.MeterMediaWidthDip + TesseraStackedPlacementSpec.MeterGapDip);

        grown[1].OffsetYDip.Should().Be((260 - TesseraStackedPlacementSpec.MeterVolumeHeightDip) / 2);

    }



    [Fact]

    public void Meter_media_height_clears_content_budget_and_exceeds_volume()

    {

        TesseraStackedPlacementSpec.MeterMediaMinContentHeightDip.Should().Be(224);

        TesseraStackedPlacementSpec.MeterMediaHeightDip
            .Should().BeGreaterThanOrEqualTo(TesseraStackedPlacementSpec.MeterMediaMinContentHeightDip);

        TesseraStackedPlacementSpec.MeterMediaHeightDip
            .Should().BeGreaterThan(TesseraStackedPlacementSpec.MeterVolumeHeightDip);

        var placements = TesseraStackedPlacementPolicy.EstimatePlacements(StyleIds.Meter);

        placements[0].HeightDip.Should().Be(TesseraStackedPlacementSpec.MeterMediaHeightDip);

        placements[1].HeightDip.Should().Be(TesseraStackedPlacementSpec.MeterVolumeHeightDip);

        placements[1].OffsetYDip.Should().Be(
            (TesseraStackedPlacementSpec.MeterMediaHeightDip - TesseraStackedPlacementSpec.MeterVolumeHeightDip) / 2);

    }



    [Fact]

    public void Fluent_places_volume_before_media_horizontally()

    {

        var placements = TesseraStackedPlacementPolicy.EstimatePlacements(StyleIds.Fluent);

        placements[0].Role.Should().Be(TesseraStackedPanelRole.Volume);

        placements[1].Role.Should().Be(TesseraStackedPanelRole.Media);

        placements[1].OffsetXDip.Should().BeGreaterThan(placements[0].OffsetXDip);

        var media = placements[1];
        media.WidthDip.Should().Be(TesseraFluentLayoutSpec.MediaWidthDip);
        media.WidthDip.Should().Be(TesseraFlyoutTweenTargetCatalog.ResolveProfile(StyleIds.Fluent).MediaWidthDip);
    }



    [Fact]

    public void Gnome_stacks_media_above_volume_with_centered_narrow_pill()

    {

        var placements = TesseraStackedPlacementPolicy.EstimatePlacements(StyleIds.Gnome);

        placements[0].Role.Should().Be(TesseraStackedPanelRole.Media);

        placements[1].Role.Should().Be(TesseraStackedPanelRole.Volume);

        placements[1].OffsetYDip.Should().Be(

            TesseraStackedPlacementSpec.GnomeMediaHeightDip + TesseraStackedPlacementSpec.GnomeGapDip);

        placements[1].OffsetXDip.Should().Be(

            (TesseraStackedPlacementSpec.GnomeMediaWidthDip - TesseraStackedPlacementSpec.GnomeVolumeWidthDip) / 2);

    }



    [Fact]

    public void Gnome_estimate_cluster_size_matches_reference_dip()

    {

        var (w, h) = TesseraStackedPlacementPolicy.EstimateClusterSize(StyleIds.Gnome);

        w.Should().Be(TesseraStackedPlacementSpec.GnomeMediaWidthDip);

        h.Should().Be(

            TesseraStackedPlacementSpec.GnomeMediaHeightDip

            + TesseraStackedPlacementSpec.GnomeGapDip

            + TesseraStackedPlacementSpec.GnomeVolumeHeightDip);

    }



    [Fact]

    public void Modern_flyouts_estimate_cluster_size_uses_layout_spacing_gap()

    {

        TesseraStackedPlacementSpec.ModernFlyoutsGapDip.Should().Be(12);

        var (w, h) = TesseraStackedPlacementPolicy.EstimateClusterSize(StyleIds.ModernFlyouts);

        w.Should().Be(TesseraStackedPlacementSpec.ModernFlyoutsMediaWidthDip);

        h.Should().Be(

            TesseraStackedPlacementSpec.ModernFlyoutsVolumeHeightDip

            + TesseraStackedPlacementSpec.ModernFlyoutsGapDip

            + TesseraStackedPlacementSpec.ModernFlyoutsMediaHeightDip);

    }



    [Fact]

    public void Scale_placements_applies_flyout_scale_to_offsets_and_sizes()

    {

        var raw = TesseraStackedPlacementPolicy.EstimatePlacements(StyleIds.ModernFlyouts);

        var scaled = TesseraStackedPlacementPolicy.ScalePlacements(raw, 1.25);

        scaled[0].WidthDip.Should().BeApproximately(320 * 1.25, 0.01);

        scaled[0].HeightDip.Should().BeApproximately(52 * 1.25, 0.01);

        scaled[1].OffsetYDip.Should().BeApproximately((52 + 12) * 1.25, 0.01);

        scaled[1].HeightDip.Should().BeApproximately(190 * 1.25, 0.01);

    }



    [Fact]

    public void Flyout_scale_from_payload_clamps_percent()

    {

        TesseraFlyoutRequestBuilder.FlyoutScaleFromPayload(

            new Dictionary<string, string> { ["flyoutScale"] = "125" }).Should().Be(1.25);

        TesseraFlyoutRequestBuilder.FlyoutScaleFromPayload(null).Should().Be(1.0);

    }



    [Fact]

    public void Plain_text_uses_tight_vertical_gap()

    {

        var placements = TesseraStackedPlacementPolicy.EstimatePlacements(StyleIds.PlainText);

        placements[1].OffsetYDip.Should().Be(

            TesseraStackedPlacementSpec.PlainTextVolumeHeightDip + TesseraStackedPlacementSpec.PlainTextGapDip);

        placements[0].WidthDip.Should().Be(TesseraStackedPlacementSpec.PlainTextWidthDip);

        placements[1].WidthDip.Should().Be(TesseraStackedPlacementSpec.PlainTextWidthDip);

    }



    [Fact]

    public void Compact_centers_narrow_volume_over_wider_media()

    {

        var placements = TesseraStackedPlacementPolicy.ComputePlacements(

            StyleIds.Compact,

            TesseraStackedPlacementSpec.CompactVolumeWidthDip,

            TesseraStackedPlacementSpec.CompactVolumeHeightDip,

            TesseraStackedPlacementSpec.CompactMediaWidthDip,

            TesseraStackedPlacementSpec.CompactMediaHeightDip);



        placements[0].Role.Should().Be(TesseraStackedPanelRole.Volume);

        placements[1].Role.Should().Be(TesseraStackedPanelRole.Media);

        placements[0].OffsetXDip.Should().Be(

            (TesseraStackedPlacementSpec.CompactMediaWidthDip - TesseraStackedPlacementSpec.CompactVolumeWidthDip) / 2);

        placements[1].OffsetYDip.Should().Be(

            TesseraStackedPlacementSpec.CompactVolumeHeightDip + TesseraStackedPlacementSpec.CompactGapDip);

    }



    [Fact]

    public void Win11_stacks_volume_above_media_with_no_gap()

    {

        var placements = TesseraStackedPlacementPolicy.EstimatePlacements(StyleIds.Windows11);

        placements[0].Role.Should().Be(TesseraStackedPanelRole.Volume);

        placements[1].OffsetYDip.Should().Be(TesseraStackedPlacementSpec.Win11VolumeHeightDip);

        placements[0].WidthDip.Should().Be(TesseraStackedPlacementSpec.Win11WidthDip);

        placements[1].WidthDip.Should().Be(TesseraStackedPlacementSpec.Win11WidthDip);

    }



    [Fact]

    public void Radial_right_aligns_media_in_cluster()

    {

        var placements = TesseraStackedPlacementPolicy.EstimatePlacements(StyleIds.Radial);

        placements[0].Role.Should().Be(TesseraStackedPanelRole.Volume);

        placements[1].Role.Should().Be(TesseraStackedPanelRole.Media);

        placements[1].OffsetXDip.Should().Be(

            TesseraStackedPlacementSpec.RadialClusterWidthDip - TesseraStackedPlacementSpec.RadialMediaWidthDip);

    }



    [Fact]

    public void Estimate_cluster_size_covers_all_panel_extents()

    {

        var (w, h) = TesseraStackedPlacementPolicy.EstimateClusterSize(StyleIds.Fluent);

        w.Should().BeGreaterThan(300);

        h.Should().BeGreaterThan(100);

    }



    [Theory]

    [InlineData(StyleIds.MaterialYou, false)]

    [InlineData(StyleIds.Meter, true)]

    [InlineData(StyleIds.Gnome, true)]

    [InlineData(StyleIds.Compact, true)]

    [InlineData(StyleIds.Fluent, false)]

    [InlineData(StyleIds.Windows11, false)]

    [InlineData(StyleIds.PlainText, false)]

    [InlineData(StyleIds.Radial, false)]

    public void Supports_stacked_os_acrylic(string styleId, bool supported) =>

        TesseraStackedPlacementPolicy.SupportsStackedOsAcrylic(styleId).Should().Be(supported);



    [Fact]

    public void Compute_placements_centers_shorter_panel_in_horizontal_row()

    {

        var placements = TesseraStackedPlacementPolicy.ComputePlacements(

            StyleIds.Fluent,

            volumeWidthDip: 72,

            volumeHeightDip: 160,

            mediaWidthDip: 324,

            mediaHeightDip: 176);



        placements.Should().HaveCount(2);

        placements[0].OffsetYDip.Should().Be(8);

        placements[1].OffsetYDip.Should().Be(0);

    }



    [Fact]

    public void Compute_placements_uses_measured_meter_sizes()

    {

        var placements = TesseraStackedPlacementPolicy.ComputePlacements(

            StyleIds.Meter,

            volumeWidthDip: 28,

            volumeHeightDip: 200,

            mediaWidthDip: 208,

            mediaHeightDip: 200);

        placements.Should().HaveCount(2);

        placements[0].Role.Should().Be(TesseraStackedPanelRole.Media);

        placements[1].Role.Should().Be(TesseraStackedPanelRole.Volume);

        placements[1].OffsetXDip.Should().Be(
            TesseraStackedPlacementSpec.MeterMediaWidthDip + TesseraStackedPlacementSpec.MeterGapDip);

        placements[0].HeightDip.Should().Be(TesseraStackedPlacementSpec.MeterMediaHeightDip);

        placements[1].HeightDip.Should().Be(TesseraStackedPlacementSpec.MeterVolumeHeightDip);

    }



    [Fact]

    public void Compute_placements_ignores_transient_meter_measure_for_volume_offset()

    {

        var placements = TesseraStackedPlacementPolicy.ComputePlacements(

            StyleIds.Meter,

            volumeWidthDip: 208,

            volumeHeightDip: 200,

            mediaWidthDip: 28,

            mediaHeightDip: 200);

        placements[1].OffsetXDip.Should().Be(
            TesseraStackedPlacementSpec.MeterMediaWidthDip + TesseraStackedPlacementSpec.MeterGapDip);

        placements[1].WidthDip.Should().Be(TesseraStackedPlacementSpec.MeterVolumeWidthDip);

    }



    [Fact]

    public void Compute_placements_centers_meter_volume_in_taller_media_card()

    {

        var placements = TesseraStackedPlacementPolicy.ComputePlacements(

            StyleIds.Meter,

            volumeWidthDip: 28,

            volumeHeightDip: 200,

            mediaWidthDip: 208,

            mediaHeightDip: 250);

        placements[0].OffsetYDip.Should().Be(0);

        placements[0].HeightDip.Should().Be(250);

        placements[1].OffsetYDip.Should().Be(
            (250 - TesseraStackedPlacementSpec.MeterVolumeHeightDip) / 2);

    }

}


