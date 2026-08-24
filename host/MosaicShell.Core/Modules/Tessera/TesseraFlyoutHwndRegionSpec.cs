using MosaicShell.Core.Styles;

namespace MosaicShell.Core.Modules.Tessera;

/// <summary>
/// Win32 region height for Win11 Fancy StrokeB without SetWindowPos.
/// Physical conversion only; Host calls CreateRoundRectRgn / SetWindowRgn.
/// </summary>
public static class TesseraFlyoutHwndRegionSpec
{
    /// <summary>Animate HRGN height in place. Do not SizeToContent / SetWindowPos per tick.</summary>
    public const bool Phase2MustAnimateRegionHeightInPlace = true;

    /// <summary>GDI CreateRoundRectRgn right/bottom are exclusive; Host adds this padding.</summary>
    public const int RegionRectInclusivePaddingPx = 1;

    public static bool StyleNeedsStrokeBRegion(string? styleId, bool musicVisible)
    {
        if (!musicVisible)
            return false;
        return StyleIds.Normalize(styleId)
            .Equals(StyleIds.Windows11, StringComparison.OrdinalIgnoreCase);
    }

    public static double ResolveRegionHeightDip(
        double progress,
        bool phase2Engaged,
        bool musicVisible)
    {
        var volume = TesseraStackedPlacementSpec.Win11VolumeHeightDip;
        var media = TesseraStackedPlacementSpec.Win11MediaHeightDip;
        if (!musicVisible)
            return volume;
        if (!phase2Engaged)
            return volume;
        return TesseraFlyoutAnimatedTargetSpec.ResolveWin11BorderHeightDip(
            volume, media, progress, musicVisible);
    }

    public static (int WidthPx, int HeightPx, int CornerRadiusPx) ResolveRoundRectPhysical(
        double widthDip,
        double heightDip,
        double cornerRadiusDip,
        double monitorScale)
    {
        var scale = monitorScale > 0.1 ? monitorScale : 1.0;
        var widthPx = Math.Max(1, (int)Math.Ceiling(Math.Max(0, widthDip) * scale));
        var heightPx = Math.Max(1, (int)Math.Ceiling(Math.Max(0, heightDip) * scale));
        var radiusPx = Math.Max(1, (int)Math.Round(Math.Max(0, cornerRadiusDip) * scale));
        var cap = Math.Max(1, Math.Min(widthPx, heightPx) / 2);
        radiusPx = Math.Clamp(radiusPx, 1, cap);
        return (widthPx, heightPx, radiusPx);
    }
}
