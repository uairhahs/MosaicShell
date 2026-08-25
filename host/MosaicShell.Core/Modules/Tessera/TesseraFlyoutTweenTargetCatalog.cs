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
        StyleIds.Normalize(styleId) switch
        {
            StyleIds.Fluent =>
            [
                new("MediaC", TesseraTweenChannel.ClipWidth),
                new("MediaC", TesseraTweenChannel.ContentOpacity),
                new("MediaB", TesseraTweenChannel.DividerHeight)
            ],
            StyleIds.Windows11 =>
            [
                new("StrokeB", TesseraTweenChannel.ShellHeight),
                new("MediaC", TesseraTweenChannel.ClipHeight),
                new("MediaC", TesseraTweenChannel.ContentOpacity)
            ],
            StyleIds.Gnome =>
            [
                new("MediaB", TesseraTweenChannel.ContentScale),
                new("MediaC", TesseraTweenChannel.ContentOpacity),
                new("VolumeC", TesseraTweenChannel.VolumeFillOpacity)
            ],
            StyleIds.Square =>
            [
                new("VolumeIcon", TesseraTweenChannel.LabelScale),
                new("VolumeString", TesseraTweenChannel.LabelScale)
            ],
            StyleIds.CoreUI =>
            [
                new("VolumeBar", TesseraTweenChannel.VolumeBarScale),
                new("MediaC", TesseraTweenChannel.ClipWidth),
                new("MediaC", TesseraTweenChannel.ContentOpacity)
            ],
            StyleIds.PlainText =>
            [
                new("MediaB", TesseraTweenChannel.SlideOffset),
                new("MediaB", TesseraTweenChannel.ContentOpacity)
            ],
            StyleIds.Meter =>
            [
                new("MediaB", TesseraTweenChannel.SlideOffset),
                new("MediaC", TesseraTweenChannel.ContentOpacity)
            ],
            StyleIds.Compact =>
            [
                new("MediaB", TesseraTweenChannel.SlideOffset),
                new("MediaC", TesseraTweenChannel.ContentOpacity)
            ],
            StyleIds.ModernFlyouts =>
            [
                new("MediaC", TesseraTweenChannel.ClipHeight),
                new("MediaC", TesseraTweenChannel.ContentOpacity)
            ],
            StyleIds.MaterialYou =>
            [
                new("MediaB", TesseraTweenChannel.SlideOffset),
                new("MediaC", TesseraTweenChannel.ContentOpacity)
            ],
            _ => []
        };

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
        switch (StyleIds.Normalize(styleId))
        {
            case StyleIds.Fluent:
                widthDip = TesseraFluentLayoutSpec.MediaWidthDip;
                heightDip = TesseraFluentLayoutSpec.HeightDip;
                return true;
            case StyleIds.Meter:
                widthDip = TesseraStackedPlacementSpec.MeterMediaWidthDip;
                heightDip = TesseraStackedPlacementSpec.MeterMediaHeightDip;
                return true;
            case StyleIds.Windows11:
                widthDip = TesseraStackedPlacementSpec.Win11WidthDip;
                heightDip = TesseraStackedPlacementSpec.Win11MediaHeightDip;
                return true;
            case StyleIds.Gnome:
                widthDip = TesseraStackedPlacementSpec.GnomeMediaWidthDip;
                heightDip = TesseraStackedPlacementSpec.GnomeMediaHeightDip;
                return true;
            case StyleIds.PlainText:
                widthDip = TesseraStackedPlacementSpec.PlainTextWidthDip;
                heightDip = TesseraStackedPlacementSpec.PlainTextMediaHeightDip;
                return true;
            case StyleIds.Compact:
                widthDip = TesseraStackedPlacementSpec.CompactMediaWidthDip;
                heightDip = TesseraStackedPlacementSpec.CompactMediaHeightDip;
                return true;
            case StyleIds.ModernFlyouts:
                widthDip = TesseraStackedPlacementSpec.ModernFlyoutsMediaWidthDip;
                heightDip = TesseraStackedPlacementSpec.ModernFlyoutsMediaHeightDip;
                return true;
            case StyleIds.CoreUI:
                widthDip = TesseraCoreUiLayoutSpec.InnerRowWidthDip;
                heightDip = TesseraCoreUiLayoutSpec.MediaHeightDip;
                return true;
            case StyleIds.MaterialYou:
                widthDip = TesseraFlyoutAnimatedTargetSpec.MaterialYouColumnWidthDip;
                heightDip = double.NaN;
                return true;
            default:
                widthDip = double.NaN;
                heightDip = double.NaN;
                return false;
        }
    }
}
