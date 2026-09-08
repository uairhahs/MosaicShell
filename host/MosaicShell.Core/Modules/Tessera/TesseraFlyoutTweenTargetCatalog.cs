using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>YourFlyouts TweenNode1 property driven on an Animated meter.</summary>
    public enum TesseraTweenChannel
    {
        ClipWidth,
        ClipHeight,
        DividerHeight,
        ContentOpacity,
        VolumeFillOpacity,
        ContentScale,
        VolumeBarScale,
        LabelScale,
        SlideOffset,
        ShellHeight,

        /// <summary>Radial ring fill arc extent (0 = no arc, 1 = the live volume level).</summary>
        RingSweep
    }

    /// <summary>One Animated-group meter/channel pair from a YourFlyouts layout inc.</summary>
    public readonly record struct TesseraTweenTarget(string MeterId, TesseraTweenChannel Channel);

    public enum TesseraFlyoutRevealKind
    {
        None,
        Fluent,
        Windows11,
        Gnome,
        Square,
        CoreUi,
        PlainText,
        Meter,
        Compact,
        ModernFlyouts,
        MaterialYou,
        Radial,
    }

    /// <summary>
    /// Animated targets plus rest DIP for clip, HWND region, and stacked placement.
    /// <c>LayoutKind</c>, <c>SupportsStackedOsAcrylic</c>, and <c>StackedGapDip</c> back
    /// <see cref="TesseraStackedPlacementPolicy"/>'s per-style switches; the consistency test
    /// suite in MosaicShell.Core.Tests asserts they never drift from that policy's own answers.
    /// </summary>
    public readonly record struct TesseraFlyoutStyleProfile(
        IReadOnlyList<TesseraTweenTarget> Targets,
        TesseraFlyoutRevealKind RevealKind,
        double VolumeWidthDip,
        double VolumeHeightDip,
        double MediaWidthDip,
        double MediaHeightDip,
        TesseraStackedLayoutKind LayoutKind,
        bool SupportsStackedOsAcrylic,
        double StackedGapDip,
        float VolumeCornerRadiusDip,
        float MediaCornerRadiusDip,
        bool SupportsCoreUiMultiTile,
        bool PhaseTwoWithoutMediaStrip,
        bool PhaseTwoUsesInLayoutMedia,
        float StatusChipCornerRadiusDip,
        bool NeedsStrokeBRegion,
        bool RequiresMatteChrome);

    /// <summary>
    /// Fancy phase-2 addressable components per Tessera style. Source of truth is YourFlyouts
    /// <c>Group=Animated</c> meters. Nested MediaC children stay Standard and inherit the mask.
    /// </summary>
    public static class TesseraFlyoutTweenTargetCatalog
    {
        /// <summary>
        /// Album art, title, transport, and scrubber live inside MediaC. They must not get
        /// their own TweenNode1 binders.
        /// </summary>
        public const bool IndependentMediaChildrenMustInheritContainer = true;

        public static IReadOnlyList<TesseraTweenTarget> ResolveAnimated(string? styleId)
        {
            return ResolveProfile(styleId).Targets;
        }

        /// <summary>Host reveal appliers and rest sizes share this profile; do not fork literals.</summary>
        public static TesseraFlyoutStyleProfile ResolveProfile(string? styleId)
        {
            string id = StyleIds.Normalize(styleId);
            return id switch
            {
                StyleIds.Fluent => new(

                    [
                        new("MediaC", TesseraTweenChannel.ClipWidth),
                    new("MediaC", TesseraTweenChannel.ContentOpacity),
                    new("MediaB", TesseraTweenChannel.DividerHeight)
                    ],
                TesseraFlyoutRevealKind.Fluent,
                TesseraFluentLayoutSpec.VolumeWidthDip,
                TesseraFluentLayoutSpec.HeightDip,
                TesseraFluentLayoutSpec.MediaWidthDip,
                TesseraFluentLayoutSpec.HeightDip,
                TesseraStackedLayoutKind.HorizontalVolumeFirst,
                SupportsStackedOsAcrylic: false,
                StackedGapDip: TesseraStackedPlacementPolicy.VerticalGapDip,
                VolumeCornerRadiusDip: TesseraOsAcrylicTrialPolicy.SpikeCornerRadius,
                MediaCornerRadiusDip: TesseraOsAcrylicTrialPolicy.SpikeCornerRadius,
                SupportsCoreUiMultiTile: false,
                PhaseTwoWithoutMediaStrip: false,
                PhaseTwoUsesInLayoutMedia: false,
                StatusChipCornerRadiusDip: TesseraStatusFlyoutPolicy.ChipCornerRadiusDip,
                NeedsStrokeBRegion: false,
                RequiresMatteChrome: false),
                StyleIds.Windows11 => new(
                    [
                        new("StrokeB", TesseraTweenChannel.ShellHeight),
                        new("MediaC", TesseraTweenChannel.ClipHeight),
                        new("MediaC", TesseraTweenChannel.ContentOpacity)
                    ],
                    TesseraFlyoutRevealKind.Windows11,
                    TesseraStackedPlacementSpec.Win11WidthDip,
                    TesseraStackedPlacementSpec.Win11VolumeHeightDip,
                    TesseraStackedPlacementSpec.Win11WidthDip,
                    TesseraStackedPlacementSpec.Win11MediaHeightDip,
                    TesseraStackedLayoutKind.VerticalWin11,
                    SupportsStackedOsAcrylic: false,
                    StackedGapDip: TesseraStackedPlacementPolicy.VerticalGapDip,
                    VolumeCornerRadiusDip: TesseraOsAcrylicTrialPolicy.SpikeCornerRadius,
                    MediaCornerRadiusDip: TesseraOsAcrylicTrialPolicy.SpikeCornerRadius,
                    SupportsCoreUiMultiTile: false,
                    PhaseTwoWithoutMediaStrip: false,
                    PhaseTwoUsesInLayoutMedia: false,
                    StatusChipCornerRadiusDip: TesseraStatusFlyoutPolicy.ChipCornerRadiusDip,
                    NeedsStrokeBRegion: true,
                    RequiresMatteChrome: false),
                StyleIds.Gnome => new(
                    [
                        new("MediaB", TesseraTweenChannel.ContentScale),
                        new("MediaC", TesseraTweenChannel.ContentOpacity),
                        new("VolumeC", TesseraTweenChannel.VolumeFillOpacity)
                    ],
                    TesseraFlyoutRevealKind.Gnome,
                    TesseraStackedPlacementSpec.GnomeVolumeWidthDip,
                    TesseraStackedPlacementSpec.GnomeVolumeHeightDip,
                    TesseraStackedPlacementSpec.GnomeMediaWidthDip,
                    TesseraStackedPlacementSpec.GnomeMediaHeightDip,
                    TesseraStackedLayoutKind.VerticalMediaFirst,
                    SupportsStackedOsAcrylic: true,
                    StackedGapDip: TesseraStackedPlacementPolicy.VerticalGapDip,
                    VolumeCornerRadiusDip: TesseraStackedPlacementSpec.GnomePillCornerRadiusDip,
                    MediaCornerRadiusDip: TesseraStackedPlacementSpec.GnomePillCornerRadiusDip,
                    SupportsCoreUiMultiTile: false,
                    PhaseTwoWithoutMediaStrip: false,
                    PhaseTwoUsesInLayoutMedia: false,
                    StatusChipCornerRadiusDip: 24f,
                    NeedsStrokeBRegion: false,
                    RequiresMatteChrome: false),
                StyleIds.Square => new(
                    [
                        new("VolumeIcon", TesseraTweenChannel.LabelScale),
                        new("VolumeString", TesseraTweenChannel.LabelScale)
                    ],
                    TesseraFlyoutRevealKind.Square,
                    72,
                    TesseraFluentLayoutSpec.HeightDip,
                    double.NaN,
                    double.NaN,
                    TesseraStackedLayoutKind.VerticalVolumeFirst,
                    SupportsStackedOsAcrylic: false,
                    StackedGapDip: TesseraStackedPlacementPolicy.VerticalGapDip,
                    VolumeCornerRadiusDip: TesseraOsAcrylicTrialPolicy.SpikeCornerRadius,
                    MediaCornerRadiusDip: TesseraOsAcrylicTrialPolicy.SpikeCornerRadius,
                    SupportsCoreUiMultiTile: false,
                    PhaseTwoWithoutMediaStrip: true,
                    PhaseTwoUsesInLayoutMedia: false,
                    StatusChipCornerRadiusDip: 24f,
                    NeedsStrokeBRegion: false,
                    RequiresMatteChrome: false),
                StyleIds.CoreUI => new(
                    [
                        new("VolumeBar", TesseraTweenChannel.VolumeBarScale),
                        new("MediaC", TesseraTweenChannel.ClipWidth),
                        new("MediaC", TesseraTweenChannel.ContentOpacity)
                    ],
                    TesseraFlyoutRevealKind.CoreUi,
                    TesseraCoreUiLayoutSpec.WidthDip,
                    TesseraCoreUiLayoutSpec.VolumeHeightDip,
                    TesseraCoreUiLayoutSpec.InnerRowWidthDip,
                    TesseraCoreUiLayoutSpec.MediaHeightDip,
                    TesseraStackedLayoutKind.VerticalVolumeFirst,
                    SupportsStackedOsAcrylic: false,
                    StackedGapDip: TesseraStackedPlacementPolicy.VerticalGapDip,
                    VolumeCornerRadiusDip: TesseraOsAcrylicTrialPolicy.SpikeCornerRadius,
                    MediaCornerRadiusDip: TesseraOsAcrylicTrialPolicy.SpikeCornerRadius,
                    SupportsCoreUiMultiTile: true,
                    PhaseTwoWithoutMediaStrip: false,
                    PhaseTwoUsesInLayoutMedia: false,
                    StatusChipCornerRadiusDip: 8f,
                    NeedsStrokeBRegion: false,
                    RequiresMatteChrome: false),
                StyleIds.PlainText => new(
                    [
                        new("MediaB", TesseraTweenChannel.SlideOffset),
                        new("MediaB", TesseraTweenChannel.ContentOpacity)
                    ],
                    TesseraFlyoutRevealKind.PlainText,
                    TesseraStackedPlacementSpec.PlainTextWidthDip,
                    TesseraStackedPlacementSpec.PlainTextVolumeHeightDip,
                    TesseraStackedPlacementSpec.PlainTextWidthDip,
                    TesseraStackedPlacementSpec.PlainTextMediaHeightDip,
                    TesseraStackedLayoutKind.PlainTextColumn,
                    SupportsStackedOsAcrylic: false,
                    StackedGapDip: TesseraStackedPlacementPolicy.VerticalGapDip,
                    VolumeCornerRadiusDip: TesseraOsAcrylicTrialPolicy.SpikeCornerRadius,
                    MediaCornerRadiusDip: TesseraOsAcrylicTrialPolicy.SpikeCornerRadius,
                    SupportsCoreUiMultiTile: false,
                    PhaseTwoWithoutMediaStrip: false,
                    PhaseTwoUsesInLayoutMedia: false,
                    StatusChipCornerRadiusDip: 4f,
                    NeedsStrokeBRegion: false,
                    // PlainTextShell clips its card to a slanted PathGeometry (see
                    // TesseraLayouts.PlainText.cs); the sliver that clip cuts away is unpainted
                    // window area, the same shape of exposure as MaterialYou's inter-pill gaps.
                    RequiresMatteChrome: true),
                StyleIds.Meter => new(
                    [
                        new("MediaB", TesseraTweenChannel.SlideOffset),
                        new("MediaC", TesseraTweenChannel.ContentOpacity)
                    ],
                    TesseraFlyoutRevealKind.Meter,
                    TesseraStackedPlacementSpec.MeterVolumeWidthDip,
                    TesseraStackedPlacementSpec.MeterVolumeHeightDip,
                    TesseraStackedPlacementSpec.MeterMediaWidthDip,
                    TesseraStackedPlacementSpec.MeterMediaHeightDip,
                    TesseraStackedLayoutKind.HorizontalMediaFirst,
                    SupportsStackedOsAcrylic: true,
                    StackedGapDip: TesseraStackedPlacementPolicy.VerticalGapDip,
                    VolumeCornerRadiusDip: TesseraStackedPlacementSpec.MeterVolumeCornerRadiusDip,
                    MediaCornerRadiusDip: TesseraStackedPlacementSpec.MeterMediaCornerRadiusDip,
                    SupportsCoreUiMultiTile: false,
                    PhaseTwoWithoutMediaStrip: false,
                    PhaseTwoUsesInLayoutMedia: false,
                    StatusChipCornerRadiusDip: 16f,
                    NeedsStrokeBRegion: false,
                    RequiresMatteChrome: false),
                StyleIds.Compact => new(
                    [
                        new("MediaB", TesseraTweenChannel.SlideOffset),
                        new("MediaC", TesseraTweenChannel.ContentOpacity)
                    ],
                    TesseraFlyoutRevealKind.Compact,
                    TesseraStackedPlacementSpec.CompactVolumeWidthDip,
                    TesseraStackedPlacementSpec.CompactVolumeHeightDip,
                    TesseraStackedPlacementSpec.CompactMediaWidthDip,
                    TesseraStackedPlacementSpec.CompactMediaHeightDip,
                    TesseraStackedLayoutKind.VerticalVolumeFirst,
                    SupportsStackedOsAcrylic: true,
                    StackedGapDip: TesseraStackedPlacementSpec.CompactGapDip,
                    VolumeCornerRadiusDip: TesseraOsAcrylicTrialPolicy.SpikeCornerRadius,
                    MediaCornerRadiusDip: TesseraOsAcrylicTrialPolicy.SpikeCornerRadius,
                    SupportsCoreUiMultiTile: false,
                    PhaseTwoWithoutMediaStrip: false,
                    PhaseTwoUsesInLayoutMedia: false,
                    StatusChipCornerRadiusDip: TesseraStatusFlyoutPolicy.ChipCornerRadiusDip,
                    NeedsStrokeBRegion: false,
                    RequiresMatteChrome: false),
                StyleIds.ModernFlyouts => new(
                    [
                        new("MediaC", TesseraTweenChannel.ClipHeight),
                        new("MediaC", TesseraTweenChannel.ContentOpacity)
                    ],
                    TesseraFlyoutRevealKind.ModernFlyouts,
                    TesseraStackedPlacementSpec.ModernFlyoutsVolumeWidthDip,
                    TesseraStackedPlacementSpec.ModernFlyoutsVolumeHeightDip,
                    TesseraStackedPlacementSpec.ModernFlyoutsMediaWidthDip,
                    TesseraStackedPlacementSpec.ModernFlyoutsMediaHeightDip,
                    TesseraStackedLayoutKind.VerticalVolumeFirst,
                    SupportsStackedOsAcrylic: true,
                    StackedGapDip: TesseraStackedPlacementSpec.ModernFlyoutsGapDip,
                    VolumeCornerRadiusDip: TesseraOsAcrylicTrialPolicy.SpikeCornerRadius,
                    MediaCornerRadiusDip: TesseraOsAcrylicTrialPolicy.SpikeCornerRadius,
                    SupportsCoreUiMultiTile: false,
                    PhaseTwoWithoutMediaStrip: false,
                    PhaseTwoUsesInLayoutMedia: false,
                    StatusChipCornerRadiusDip: TesseraStatusFlyoutPolicy.ChipCornerRadiusDip,
                    NeedsStrokeBRegion: false,
                    RequiresMatteChrome: false),
                StyleIds.MaterialYou => new(
                    [
                        new("MediaB", TesseraTweenChannel.SlideOffset),
                        new("MediaC", TesseraTweenChannel.ContentOpacity)
                    ],
                    TesseraFlyoutRevealKind.MaterialYou,
                    TesseraFlyoutAnimatedTargetSpec.MaterialYouColumnWidthDip,
                    double.NaN,
                    TesseraFlyoutAnimatedTargetSpec.MaterialYouColumnWidthDip,
                    double.NaN,
                    TesseraStackedLayoutKind.VerticalVolumeFirst,
                    SupportsStackedOsAcrylic: false,
                    StackedGapDip: TesseraStackedPlacementPolicy.VerticalGapDip,
                    VolumeCornerRadiusDip: TesseraOsAcrylicTrialPolicy.SpikeCornerRadius,
                    MediaCornerRadiusDip: TesseraOsAcrylicTrialPolicy.SpikeCornerRadius,
                    SupportsCoreUiMultiTile: false,
                    PhaseTwoWithoutMediaStrip: false,
                    PhaseTwoUsesInLayoutMedia: true,
                    StatusChipCornerRadiusDip: 24f,
                    NeedsStrokeBRegion: false,
                    RequiresMatteChrome: true),
                StyleIds.Radial => new(
                    [
                        new("VolumeC", TesseraTweenChannel.RingSweep),
                        new("MediaB", TesseraTweenChannel.SlideOffset),
                        new("MediaC", TesseraTweenChannel.ContentOpacity)
                    ],
                    TesseraFlyoutRevealKind.Radial,
                    TesseraStackedPlacementSpec.RadialVolumeWidthDip,
                    TesseraStackedPlacementSpec.RadialPanelHeightDip,
                    TesseraStackedPlacementSpec.RadialMediaWidthDip,
                    TesseraStackedPlacementSpec.RadialPanelHeightDip,
                    TesseraStackedLayoutKind.HorizontalRadial,
                    SupportsStackedOsAcrylic: false,
                    StackedGapDip: TesseraStackedPlacementPolicy.VerticalGapDip,
                    VolumeCornerRadiusDip: TesseraOsAcrylicTrialPolicy.SpikeCornerRadius,
                    MediaCornerRadiusDip: TesseraOsAcrylicTrialPolicy.SpikeCornerRadius,
                    SupportsCoreUiMultiTile: false,
                    PhaseTwoWithoutMediaStrip: false,
                    PhaseTwoUsesInLayoutMedia: false,
                    StatusChipCornerRadiusDip: 10f,
                    NeedsStrokeBRegion: false,
                    RequiresMatteChrome: false),
                _ => new(
                    [],
                    TesseraFlyoutRevealKind.None,
                    72,
                    TesseraFluentLayoutSpec.HeightDip,
                    double.NaN,
                    double.NaN,
                    TesseraStackedLayoutKind.VerticalVolumeFirst,
                    SupportsStackedOsAcrylic: false,
                    StackedGapDip: TesseraStackedPlacementPolicy.VerticalGapDip,
                    VolumeCornerRadiusDip: TesseraOsAcrylicTrialPolicy.SpikeCornerRadius,
                    MediaCornerRadiusDip: TesseraOsAcrylicTrialPolicy.SpikeCornerRadius,
                    SupportsCoreUiMultiTile: false,
                    PhaseTwoWithoutMediaStrip: false,
                    PhaseTwoUsesInLayoutMedia: false,
                    StatusChipCornerRadiusDip: TesseraStatusFlyoutPolicy.ChipCornerRadiusDip,
                    NeedsStrokeBRegion: false,
                    RequiresMatteChrome: false),
            };
        }

        public static bool HasChannel(string? styleId, TesseraTweenChannel channel)
        {
            foreach (TesseraTweenTarget target in ResolveAnimated(styleId))
            {
                if (target.Channel == channel)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool StyleSupportsPhase2(string? styleId)
        {
            return ResolveAnimated(styleId).Count > 0;
        }

        /// <summary>
        /// A style with no animated targets has nothing for phase 2 to drive. Derived from the
        /// profile rather than naming a style (ADR-0001). Radial used to be listed here because
        /// Smouti.inc comments out its TweenNode1 binders, but Host addresses its ring and side
        /// media directly, so it is no longer a no-op.
        /// </summary>
        public static bool StyleIsPhase2NoOp(string? styleId)
        {
            return !StyleSupportsPhase2(styleId);
        }

        public static bool StylePhase2WithoutMediaStrip(string? styleId)
        {
            return ResolveProfile(styleId).PhaseTwoWithoutMediaStrip;
        }

        /// <summary>Pixel.inc media column lives in the volume HWND (Host does not stack Material You).</summary>
        public static bool StylePhase2UsesInLayoutMedia(string? styleId)
        {
            return ResolveProfile(styleId).PhaseTwoUsesInLayoutMedia;
        }

        /// <summary>
        /// Volume flyout may show media chrome (stacked HWND or in-layout column) exactly when the
        /// style animates a media meter. Square runs Animated fonts on the volume card with no media
        /// strip; Radial has no phase-2 targets. Derived, not restated (ADR-0001); the previous
        /// <c>UsesStackedMediaStrip || UsesInLayoutMedia</c> form was a tautology and always true.
        /// </summary>
        public static bool StyleRequestsVolumeMediaChrome(string? styleId)
        {
            foreach (TesseraTweenTarget target in ResolveAnimated(styleId))
            {
                if (target.MeterId.StartsWith("Media", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool VolumeStaysOpaqueDuringPhase2(string? styleId)
        {
            return !HasChannel(styleId, TesseraTweenChannel.VolumeFillOpacity);
        }

        /// <summary>Rest media panel size for stacked HWND clip/slide binders. NaN when unused.</summary>
        public static bool TryResolveMediaRestSizeDip(string? styleId, out double widthDip, out double heightDip)
        {
            TesseraFlyoutStyleProfile profile = ResolveProfile(styleId);
            widthDip = profile.MediaWidthDip;
            heightDip = profile.MediaHeightDip;
            return profile.MediaWidthDip > 1 || profile.MediaHeightDip > 1;
        }
    }
}
