using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Modules.Tessera;

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
    ShellHeight
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
}

/// <summary>Animated targets plus rest DIP for clip, HWND region, and stacked placement.</summary>
public readonly record struct TesseraFlyoutStyleProfile(
    IReadOnlyList<TesseraTweenTarget> Targets,
    TesseraFlyoutRevealKind RevealKind,
    double VolumeWidthDip,
    double VolumeHeightDip,
    double MediaWidthDip,
    double MediaHeightDip);

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

    public static IReadOnlyList<TesseraTweenTarget> ResolveAnimated(string? styleId) =>
        ResolveProfile(styleId).Targets;

    /// <summary>Host reveal appliers and rest sizes share this profile; do not fork literals.</summary>
    public static TesseraFlyoutStyleProfile ResolveProfile(string? styleId)
    {
        var id = StyleIds.Normalize(styleId);
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
                TesseraFluentLayoutSpec.HeightDip),
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
                TesseraStackedPlacementSpec.Win11MediaHeightDip),
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
                TesseraStackedPlacementSpec.GnomeMediaHeightDip),
            StyleIds.Square => new(
                [
                    new("VolumeIcon", TesseraTweenChannel.LabelScale),
                    new("VolumeString", TesseraTweenChannel.LabelScale)
                ],
                TesseraFlyoutRevealKind.Square,
                72,
                TesseraFluentLayoutSpec.HeightDip,
                double.NaN,
                double.NaN),
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
                TesseraCoreUiLayoutSpec.MediaHeightDip),
            StyleIds.PlainText => new(
                [
                    new("MediaB", TesseraTweenChannel.SlideOffset),
                    new("MediaB", TesseraTweenChannel.ContentOpacity)
                ],
                TesseraFlyoutRevealKind.PlainText,
                TesseraStackedPlacementSpec.PlainTextWidthDip,
                TesseraStackedPlacementSpec.PlainTextVolumeHeightDip,
                TesseraStackedPlacementSpec.PlainTextWidthDip,
                TesseraStackedPlacementSpec.PlainTextMediaHeightDip),
            StyleIds.Meter => new(
                [
                    new("MediaB", TesseraTweenChannel.SlideOffset),
                    new("MediaC", TesseraTweenChannel.ContentOpacity)
                ],
                TesseraFlyoutRevealKind.Meter,
                TesseraStackedPlacementSpec.MeterVolumeWidthDip,
                TesseraStackedPlacementSpec.MeterVolumeHeightDip,
                TesseraStackedPlacementSpec.MeterMediaWidthDip,
                TesseraStackedPlacementSpec.MeterMediaHeightDip),
            StyleIds.Compact => new(
                [
                    new("MediaB", TesseraTweenChannel.SlideOffset),
                    new("MediaC", TesseraTweenChannel.ContentOpacity)
                ],
                TesseraFlyoutRevealKind.Compact,
                TesseraStackedPlacementSpec.CompactVolumeWidthDip,
                TesseraStackedPlacementSpec.CompactVolumeHeightDip,
                TesseraStackedPlacementSpec.CompactMediaWidthDip,
                TesseraStackedPlacementSpec.CompactMediaHeightDip),
            StyleIds.ModernFlyouts => new(
                [
                    new("MediaC", TesseraTweenChannel.ClipHeight),
                    new("MediaC", TesseraTweenChannel.ContentOpacity)
                ],
                TesseraFlyoutRevealKind.ModernFlyouts,
                TesseraStackedPlacementSpec.ModernFlyoutsVolumeWidthDip,
                TesseraStackedPlacementSpec.ModernFlyoutsVolumeHeightDip,
                TesseraStackedPlacementSpec.ModernFlyoutsMediaWidthDip,
                TesseraStackedPlacementSpec.ModernFlyoutsMediaHeightDip),
            StyleIds.MaterialYou => new(
                [
                    new("MediaB", TesseraTweenChannel.SlideOffset),
                    new("MediaC", TesseraTweenChannel.ContentOpacity)
                ],
                TesseraFlyoutRevealKind.MaterialYou,
                TesseraFlyoutAnimatedTargetSpec.MaterialYouColumnWidthDip,
                double.NaN,
                TesseraFlyoutAnimatedTargetSpec.MaterialYouColumnWidthDip,
                double.NaN),
            _ => new([], TesseraFlyoutRevealKind.None, 72, TesseraFluentLayoutSpec.HeightDip, double.NaN, double.NaN),
        };
    }

    public static bool HasChannel(string? styleId, TesseraTweenChannel channel)
    {
        foreach (var target in ResolveAnimated(styleId))
        {
            if (target.Channel == channel)
                return true;
        }

        return false;
    }

    public static bool StyleSupportsPhase2(string? styleId) =>
        ResolveAnimated(styleId).Count > 0;

    /// <summary>Smouti.inc comments out MediaB/MediaC TweenNode1; Fancy is phase 1 only.</summary>
    public static bool StyleIsPhase2NoOp(string? styleId) =>
        StyleIds.Normalize(styleId).Equals(StyleIds.Radial, StringComparison.OrdinalIgnoreCase);

    public static bool StylePhase2WithoutMediaStrip(string? styleId) =>
        StyleIds.Normalize(styleId).Equals(StyleIds.Square, StringComparison.OrdinalIgnoreCase);

    /// <summary>Pixel.inc media column lives in the volume HWND (Host does not stack Material You).</summary>
    public static bool StylePhase2UsesInLayoutMedia(string? styleId) =>
        StyleIds.Normalize(styleId).Equals(StyleIds.MaterialYou, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Volume flyout may show media chrome: stacked HWND or in-layout column (Material You).
    /// </summary>
    public static bool StyleRequestsVolumeMediaChrome(string? styleId) =>
        TesseraLayoutCoverage.UsesStackedMediaStrip(StyleIds.Normalize(styleId))
        || StylePhase2UsesInLayoutMedia(styleId);

    public static bool VolumeStaysOpaqueDuringPhase2(string? styleId) =>
        !HasChannel(styleId, TesseraTweenChannel.VolumeFillOpacity);

    /// <summary>Rest media panel size for stacked HWND clip/slide binders. NaN when unused.</summary>
    public static bool TryResolveMediaRestSizeDip(string? styleId, out double widthDip, out double heightDip)
    {
        var profile = ResolveProfile(styleId);
        widthDip = profile.MediaWidthDip;
        heightDip = profile.MediaHeightDip;
        return profile.MediaWidthDip > 1 || profile.MediaHeightDip > 1;
    }
}
