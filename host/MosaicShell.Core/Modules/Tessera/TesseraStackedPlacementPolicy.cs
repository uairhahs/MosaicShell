using MosaicShell.Core.Styles;



namespace MosaicShell.Core.Modules.Tessera
{


    /// <summary>

    /// Cluster placement for H3 N-window stacked acrylic. DIP offsets are relative to the

    /// combined-stack anchor origin (same box a single-HWND stacked flyout would occupy).

    /// </summary>

    public enum TesseraStackedLayoutKind

    {

        HorizontalVolumeFirst,

        HorizontalMediaFirst,

        VerticalVolumeFirst,

        VerticalMediaFirst,

        VerticalWin11,

        HorizontalRadial,

        PlainTextColumn,

    }



    public readonly record struct TesseraStackedPanelPlacement(

        TesseraStackedPanelRole Role,

        double OffsetXDip,

        double OffsetYDip,

        double WidthDip,

        double HeightDip);



    public static class TesseraStackedPlacementPolicy

    {

        public const double HorizontalGapDip = 12;

        public const double VerticalGapDip = 10;

        public const double FluentDividerDip = 3;



        public static TesseraStackedLayoutKind ResolveLayoutKind(string? styleId)

        {

            string id = StyleIds.Normalize(styleId ?? StyleIds.Fluent);

            return id switch

            {

                StyleIds.Meter => TesseraStackedLayoutKind.HorizontalMediaFirst,

                StyleIds.Gnome => TesseraStackedLayoutKind.VerticalMediaFirst,

                StyleIds.Fluent => TesseraStackedLayoutKind.HorizontalVolumeFirst,

                StyleIds.Windows11 => TesseraStackedLayoutKind.VerticalWin11,

                StyleIds.Radial => TesseraStackedLayoutKind.HorizontalRadial,

                StyleIds.PlainText => TesseraStackedLayoutKind.PlainTextColumn,

                StyleIds.Compact or StyleIds.ModernFlyouts => TesseraStackedLayoutKind.VerticalVolumeFirst,

                _ => TesseraStackedLayoutKind.VerticalVolumeFirst,

            };

        }



        public static bool SupportsStackedOsAcrylic(string? styleId)

        {

            string id = StyleIds.Normalize(styleId ?? StyleIds.Fluent);

            return id is StyleIds.Meter or StyleIds.Gnome or StyleIds.Compact or StyleIds.ModernFlyouts;

        }



        /// <summary>

        /// Estimate panel sizes + offsets for anchor math before HWND measure.

        /// Host may refine with measured bounds after layout.

        /// </summary>

        public static IReadOnlyList<TesseraStackedPanelPlacement> EstimatePlacements(string? styleId)

        {

            string id = StyleIds.Normalize(styleId ?? StyleIds.Fluent);

            return id switch

            {

                StyleIds.Fluent => FluentPlacements(),

                StyleIds.Meter => MeterPlacements(),

                StyleIds.Windows11 => Win11Placements(),

                StyleIds.Radial => RadialPlacements(),

                StyleIds.PlainText => PlainTextPlacements(),

                StyleIds.Gnome => GnomePlacements(),

                StyleIds.Compact => CompactPlacements(),

                StyleIds.ModernFlyouts => ModernFlyoutsPlacements(),

                _ => VerticalVolumeFirstPlacements(

                    TesseraStackedPlacementSpec.CompactVolumeWidthDip,

                    TesseraStackedPlacementSpec.CompactVolumeHeightDip,

                    TesseraStackedPlacementSpec.CompactMediaWidthDip,

                    TesseraStackedPlacementSpec.CompactMediaHeightDip,

                    TesseraStackedPlacementSpec.CompactGapDip),

            };

        }



        public static (double CombinedWidthDip, double CombinedHeightDip) EstimateClusterSize(string? styleId)

        {

            IReadOnlyList<TesseraStackedPanelPlacement> placements = EstimatePlacements(styleId);

            return ClusterSizeFromPlacements(placements);

        }



        public static (double CombinedWidthDip, double CombinedHeightDip) ClusterSizeFromPlacements(

            IReadOnlyList<TesseraStackedPanelPlacement> placements)

        {

            if (placements.Count == 0)
            {
                return (1, 1);
            }

            double maxX = placements.Max(p => p.OffsetXDip + p.WidthDip);

            double maxY = placements.Max(p => p.OffsetYDip + p.HeightDip);

            return (Math.Max(1, maxX), Math.Max(1, maxY));

        }



        /// <summary>

        /// HWND client floor is the placement estimate; never shrink below unconstrained measure

        /// (MaxWidth/MaxHeight locks to undersized estimates clipped Compact/Gnome/Modern/Meter).

        /// </summary>

        public static (double WidthDip, double HeightDip) ResolveStackedClientSize(

            double placementWidthDip,

            double placementHeightDip,

            double measuredWidthDip,

            double measuredHeightDip)

        {

            double safeW = SanitizeStackedPanelMeasure(placementWidthDip, measuredWidthDip);

            double safeH = SanitizeStackedPanelMeasure(placementHeightDip, measuredHeightDip);

            return (

                Math.Max(Math.Max(1, placementWidthDip), safeW),

                Math.Max(Math.Max(1, placementHeightDip), safeH));

        }



        /// <summary>

        /// Reject screen-bleed HWND bounds after a bad relayout pass. Returns 0 when untrusted.

        /// </summary>

        public static double SanitizeStackedPanelMeasure(double placementFloorDip, double rawMeasuredDip)

        {

            if (rawMeasuredDip <= 0)
            {
                return 0;
            }

            double maxTrusted = Math.Max(placementFloorDip * 1.75, placementFloorDip + 48);

            return rawMeasuredDip > maxTrusted ? 0 : rawMeasuredDip;

        }



        /// <summary>Apply Tessera flyout scale to signed reference placements (LayoutTransformControl path).</summary>

        public static IReadOnlyList<TesseraStackedPanelPlacement> ScalePlacements(

            IReadOnlyList<TesseraStackedPanelPlacement> placements,

            double flyoutScale)

        {

            if (flyoutScale <= 0 || Math.Abs(flyoutScale - 1.0) < 0.001)
            {
                return placements;
            }

            TesseraStackedPanelPlacement[] scaled = new TesseraStackedPanelPlacement[placements.Count];

            for (int i = 0; i < placements.Count; i++)

            {

                TesseraStackedPanelPlacement p = placements[i];

                scaled[i] = new TesseraStackedPanelPlacement(

                    p.Role,

                    p.OffsetXDip * flyoutScale,

                    p.OffsetYDip * flyoutScale,

                    p.WidthDip * flyoutScale,

                    p.HeightDip * flyoutScale);

            }

            return scaled;

        }



        /// <summary>Recompute panel offsets from measured DIP sizes (post-layout).</summary>

        public static IReadOnlyList<TesseraStackedPanelPlacement> ComputePlacements(

            string? styleId,

            double volumeWidthDip,

            double volumeHeightDip,

            double mediaWidthDip,

            double mediaHeightDip)

        {

            string id = StyleIds.Normalize(styleId ?? StyleIds.Meter);

            if (id == StyleIds.Meter)

            {

                double mediaH = SanitizeStackedPanelMeasure(

                    TesseraStackedPlacementSpec.MeterMediaHeightDip,

                    mediaHeightDip);

                return MeterPlacements(

                    mediaH > 0 ? mediaH : TesseraStackedPlacementSpec.MeterMediaHeightDip);

            }

            return id is StyleIds.Gnome or StyleIds.Compact or StyleIds.ModernFlyouts
                ? EstimatePlacements(styleId)
                : ComputePlacementsFromMeasured(

                styleId,

                volumeWidthDip,

                volumeHeightDip,

                mediaWidthDip,

                mediaHeightDip);
        }



        /// <summary>

        /// Meter uses signed reference DIP sizes for placement. Early measure passes can

        /// report transient widths and park the volume pill under the media card.

        /// </summary>

        internal static IReadOnlyList<TesseraStackedPanelPlacement> ComputePlacementsFromMeasured(

            string? styleId,

            double volumeWidthDip,

            double volumeHeightDip,

            double mediaWidthDip,

            double mediaHeightDip)

        {

            double volW = Math.Max(1, volumeWidthDip);

            double volH = Math.Max(1, volumeHeightDip);

            double mediaW = Math.Max(1, mediaWidthDip);

            double mediaH = Math.Max(1, mediaHeightDip);



            return ResolveLayoutKind(styleId) switch

            {

                TesseraStackedLayoutKind.HorizontalVolumeFirst =>

                    HorizontalRow(

                        (TesseraStackedPanelRole.Volume, 0, volW, volH),

                        (TesseraStackedPanelRole.Media, volW, mediaW, mediaH)),

                TesseraStackedLayoutKind.HorizontalMediaFirst =>

                    StyleIds.Normalize(styleId ?? StyleIds.Meter) == StyleIds.Meter

                        ? HorizontalRowTop(

                            (TesseraStackedPanelRole.Media, 0, mediaW, mediaH),

                            (TesseraStackedPanelRole.Volume, mediaW + TesseraStackedPlacementSpec.MeterGapDip, volW, volH))

                        : HorizontalRow(

                            (TesseraStackedPanelRole.Media, 0, mediaW, mediaH),

                            (TesseraStackedPanelRole.Volume, mediaW + TesseraStackedPlacementSpec.MeterGapDip, volW, volH)),

                TesseraStackedLayoutKind.VerticalMediaFirst =>

                    VerticalColumn(

                        (TesseraStackedPanelRole.Media, mediaW, mediaH),

                        (TesseraStackedPanelRole.Volume, volW, volH),

                        TesseraStackedPlacementSpec.GnomeGapDip,

                        centerHorizontally: true),

                TesseraStackedLayoutKind.VerticalWin11 =>

                    VerticalColumn(

                        (TesseraStackedPanelRole.Volume, volW, volH),

                        (TesseraStackedPanelRole.Media, mediaW, mediaH),

                        TesseraStackedPlacementSpec.Win11GapDip,

                        centerHorizontally: false),

                TesseraStackedLayoutKind.HorizontalRadial =>

                    RadialRow(volW, volH, mediaW, mediaH),

                TesseraStackedLayoutKind.PlainTextColumn =>

                    VerticalColumn(

                        (TesseraStackedPanelRole.Volume, volW, volH),

                        (TesseraStackedPanelRole.Media, mediaW, mediaH),

                        TesseraStackedPlacementSpec.PlainTextGapDip,

                        centerHorizontally: false),

                TesseraStackedLayoutKind.VerticalVolumeFirst =>

                    VerticalColumn(

                        (TesseraStackedPanelRole.Volume, volW, volH),

                        (TesseraStackedPanelRole.Media, mediaW, mediaH),

                        VerticalGapForStyle(styleId),

                        centerHorizontally: true),

                _ =>

                    VerticalColumn(

                        (TesseraStackedPanelRole.Volume, volW, volH),

                        (TesseraStackedPanelRole.Media, mediaW, mediaH),

                        VerticalGapDip,

                        centerHorizontally: true),

            };

        }



        private static double VerticalGapForStyle(string? styleId)

        {

            string id = StyleIds.Normalize(styleId ?? StyleIds.Fluent);

            return id switch

            {

                StyleIds.Compact => TesseraStackedPlacementSpec.CompactGapDip,

                StyleIds.ModernFlyouts => TesseraStackedPlacementSpec.ModernFlyoutsGapDip,

                _ => VerticalGapDip,

            };

        }



        private static IReadOnlyList<TesseraStackedPanelPlacement> HorizontalRow(

            (TesseraStackedPanelRole Role, double OffsetX, double Width, double Height) first,

            (TesseraStackedPanelRole Role, double OffsetX, double Width, double Height) second)

        {

            double clusterH = Math.Max(first.Height, second.Height);

            return

            [

                new(first.Role, first.OffsetX, VerticalCenterOffset(first.Height, clusterH), first.Width, first.Height),

                new(second.Role, second.OffsetX, VerticalCenterOffset(second.Height, clusterH), second.Width, second.Height),

            ];

        }



        private static IReadOnlyList<TesseraStackedPanelPlacement> HorizontalRowTop(

            (TesseraStackedPanelRole Role, double OffsetX, double Width, double Height) first,

            (TesseraStackedPanelRole Role, double OffsetX, double Width, double Height) second)

        {

            return

            [

                new(first.Role, first.OffsetX, 0, first.Width, first.Height),

                new(second.Role, second.OffsetX, 0, second.Width, second.Height),

            ];

        }



        private static IReadOnlyList<TesseraStackedPanelPlacement> VerticalColumn(

            (TesseraStackedPanelRole Role, double Width, double Height) first,

            (TesseraStackedPanelRole Role, double Width, double Height) second,

            double gapDip,

            bool centerHorizontally)

        {

            double clusterW = Math.Max(first.Width, second.Width);

            double firstX = centerHorizontally ? HorizontalCenterOffset(first.Width, clusterW) : 0;

            double secondX = centerHorizontally ? HorizontalCenterOffset(second.Width, clusterW) : 0;

            double secondY = first.Height + gapDip;

            return

            [

                new(first.Role, firstX, 0, first.Width, first.Height),

                new(second.Role, secondX, secondY, second.Width, second.Height),

            ];

        }



        private static IReadOnlyList<TesseraStackedPanelPlacement> RadialRow(

            double volumeWidthDip,

            double volumeHeightDip,

            double mediaWidthDip,

            double mediaHeightDip)

        {

            double volW = Math.Max(1, volumeWidthDip);

            double volH = Math.Max(1, volumeHeightDip);

            double mediaW = Math.Max(1, mediaWidthDip);

            double mediaH = Math.Max(1, mediaHeightDip);

            double clusterW = Math.Max(

                TesseraStackedPlacementSpec.RadialClusterWidthDip,

                volW + TesseraStackedPlacementSpec.RadialColumnGapDip + mediaW);

            double clusterH = Math.Max(volH, mediaH);

            double mediaX = clusterW - mediaW;

            return

            [

                new(TesseraStackedPanelRole.Volume, 0, VerticalCenterOffset(volH, clusterH), volW, volH),

                new(TesseraStackedPanelRole.Media, mediaX, VerticalCenterOffset(mediaH, clusterH), mediaW, mediaH),

            ];

        }



        private static double VerticalCenterOffset(double panelHeightDip, double clusterHeightDip)
        {
            return Math.Max(0, (clusterHeightDip - panelHeightDip) / 2);
        }

        private static double HorizontalCenterOffset(double panelWidthDip, double clusterWidthDip)
        {
            return Math.Max(0, (clusterWidthDip - panelWidthDip) / 2);
        }

        private static IReadOnlyList<TesseraStackedPanelPlacement> FluentPlacements()

        {

            double volW = TesseraFlyoutAnimatedTargetSpec.ResolveFluentCollapsedShellWidthDip(true);

            double h = TesseraFluentLayoutSpec.HeightDip;

            double mediaW = TesseraFlyoutTweenTargetCatalog.ResolveProfile(StyleIds.Fluent).MediaWidthDip;

            double mediaX = TesseraFlyoutAnimatedTargetSpec.ResolveFluentStackedMediaOffsetXDip(true);

            return

            [

                new(TesseraStackedPanelRole.Volume, 0, 0, volW, h),

                new(TesseraStackedPanelRole.Media, mediaX, 0, mediaW, h),

            ];

        }



        private static IReadOnlyList<TesseraStackedPanelPlacement> MeterPlacements(

            double measuredMediaHeightDip = 0)

        {

            double mediaW = TesseraStackedPlacementSpec.MeterMediaWidthDip;

            // Width stays on the signed reference (transient measure parked the pill under media).

            // Height may grow when unconstrained content exceeds the content budget.

            double mediaH = Math.Max(

                TesseraStackedPlacementSpec.MeterMediaHeightDip,

                Math.Max(0, measuredMediaHeightDip));

            double volW = TesseraStackedPlacementSpec.MeterVolumeWidthDip;

            double volH = TesseraStackedPlacementSpec.MeterVolumeHeightDip;

            double volX = mediaW + TesseraStackedPlacementSpec.MeterGapDip;

            double volY = VerticalCenterOffset(volH, mediaH);

            return

            [

                new(TesseraStackedPanelRole.Media, 0, 0, mediaW, mediaH),

                new(TesseraStackedPanelRole.Volume, volX, volY, volW, volH),

            ];

        }



        private static IReadOnlyList<TesseraStackedPanelPlacement> Win11Placements()

        {

            double w = TesseraStackedPlacementSpec.Win11WidthDip;

            double volH = TesseraStackedPlacementSpec.Win11VolumeHeightDip;

            double mediaH = TesseraStackedPlacementSpec.Win11MediaHeightDip;

            return

            [

                new(TesseraStackedPanelRole.Volume, 0, 0, w, volH),

                new(TesseraStackedPanelRole.Media, 0, volH, w, mediaH),

            ];

        }



        private static IReadOnlyList<TesseraStackedPanelPlacement> RadialPlacements()

        {

            double volW = TesseraStackedPlacementSpec.RadialVolumeWidthDip;

            double mediaW = TesseraStackedPlacementSpec.RadialMediaWidthDip;

            double h = TesseraStackedPlacementSpec.RadialPanelHeightDip;

            double clusterW = TesseraStackedPlacementSpec.RadialClusterWidthDip;

            return

            [

                new(TesseraStackedPanelRole.Volume, 0, 0, volW, h),

                new(TesseraStackedPanelRole.Media, clusterW - mediaW, 0, mediaW, h),

            ];

        }



        private static IReadOnlyList<TesseraStackedPanelPlacement> PlainTextPlacements()

        {

            double w = TesseraStackedPlacementSpec.PlainTextWidthDip;

            double volH = TesseraStackedPlacementSpec.PlainTextVolumeHeightDip;

            double gap = TesseraStackedPlacementSpec.PlainTextGapDip;

            double mediaH = TesseraStackedPlacementSpec.PlainTextMediaHeightDip;

            return

            [

                new(TesseraStackedPanelRole.Volume, 0, 0, w, volH),

                new(TesseraStackedPanelRole.Media, 0, volH + gap, w, mediaH),

            ];

        }



        private static IReadOnlyList<TesseraStackedPanelPlacement> GnomePlacements()
        {
            return VerticalColumn(

                (TesseraStackedPanelRole.Media,

                    TesseraStackedPlacementSpec.GnomeMediaWidthDip,

                    TesseraStackedPlacementSpec.GnomeMediaHeightDip),

                (TesseraStackedPanelRole.Volume,

                    TesseraStackedPlacementSpec.GnomeVolumeWidthDip,

                    TesseraStackedPlacementSpec.GnomeVolumeHeightDip),

                TesseraStackedPlacementSpec.GnomeGapDip,

                centerHorizontally: true);
        }

        private static IReadOnlyList<TesseraStackedPanelPlacement> CompactPlacements()
        {
            return VerticalVolumeFirstPlacements(

                TesseraStackedPlacementSpec.CompactVolumeWidthDip,

                TesseraStackedPlacementSpec.CompactVolumeHeightDip,

                TesseraStackedPlacementSpec.CompactMediaWidthDip,

                TesseraStackedPlacementSpec.CompactMediaHeightDip,

                TesseraStackedPlacementSpec.CompactGapDip);
        }

        private static IReadOnlyList<TesseraStackedPanelPlacement> ModernFlyoutsPlacements()
        {
            return VerticalVolumeFirstPlacements(

                TesseraStackedPlacementSpec.ModernFlyoutsVolumeWidthDip,

                TesseraStackedPlacementSpec.ModernFlyoutsVolumeHeightDip,

                TesseraStackedPlacementSpec.ModernFlyoutsMediaWidthDip,

                TesseraStackedPlacementSpec.ModernFlyoutsMediaHeightDip,

                TesseraStackedPlacementSpec.ModernFlyoutsGapDip);
        }

        private static IReadOnlyList<TesseraStackedPanelPlacement> VerticalVolumeFirstPlacements(

            double volumeWidth,

            double volumeHeight,

            double mediaWidth,

            double mediaHeight,

            double gapDip)
        {
            return VerticalColumn(

                (TesseraStackedPanelRole.Volume, volumeWidth, volumeHeight),

                (TesseraStackedPanelRole.Media, mediaWidth, mediaHeight),

                gapDip,

                centerHorizontally: true);
        }
    }


}
