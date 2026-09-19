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



        /// <summary>Which cluster shape a style uses. Owned by the profile (ADR-0001).</summary>
        public static TesseraStackedLayoutKind ResolveLayoutKind(string? styleId)
        {
            return TesseraFlyoutTweenTargetCatalog.ResolveProfile(styleId).LayoutKind;
        }

        /// <summary>H3 multi-window OS-acrylic eligibility. Owned by the profile (ADR-0001).</summary>
        public static bool SupportsStackedOsAcrylic(string? styleId)
        {
            return TesseraFlyoutTweenTargetCatalog.ResolveProfile(styleId).SupportsStackedOsAcrylic;
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

                // Unlisted styles never reach here through the live H3 path (SupportsStackedOsAcrylic
                // gates it); this mirrors Compact's shape so an unexpected id still has a sane estimate.
                _ => VerticalVolumeFirstPlacements(StyleIds.Compact),

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



        /// <summary>
        /// Gap used by the <see cref="TesseraStackedLayoutKind.VerticalVolumeFirst"/> arm only
        /// (Compact, ModernFlyouts, and the shared default). Other layout kinds (Win11, Gnome,
        /// PlainText, Meter) keep their own dedicated gap constants below - owned by the profile
        /// (ADR-0001), not restated as a second switch.
        /// </summary>
        private static double VerticalGapForStyle(string? styleId)
        {
            return TesseraFlyoutTweenTargetCatalog.ResolveProfile(styleId).StackedGapDip;
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
            TesseraFlyoutStyleProfile profile = TesseraFlyoutTweenTargetCatalog.ResolveProfile(StyleIds.Meter);
            double mediaW = profile.MediaWidthDip;

            // Width stays on the signed reference (transient measure parked the pill under media).

            // Height may grow when unconstrained content exceeds the content budget.

            double mediaH = Math.Max(

                profile.MediaHeightDip,

                Math.Max(0, measuredMediaHeightDip));

            double volW = profile.VolumeWidthDip;

            double volH = profile.VolumeHeightDip;

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
            TesseraFlyoutStyleProfile profile = TesseraFlyoutTweenTargetCatalog.ResolveProfile(StyleIds.Windows11);
            double w = profile.VolumeWidthDip;
            double volH = profile.VolumeHeightDip;
            double mediaH = profile.MediaHeightDip;
            return
            [
                new(TesseraStackedPanelRole.Volume, 0, 0, w, volH),
                new(TesseraStackedPanelRole.Media, 0, volH, w, mediaH),
            ];
        }



        private static IReadOnlyList<TesseraStackedPanelPlacement> RadialPlacements()
        {
            TesseraFlyoutStyleProfile profile = TesseraFlyoutTweenTargetCatalog.ResolveProfile(StyleIds.Radial);
            double volW = profile.VolumeWidthDip;
            double mediaW = profile.MediaWidthDip;
            double h = profile.VolumeHeightDip;
            // Cluster width is placement-only spread math, not a style rest size; stays local.
            double clusterW = TesseraStackedPlacementSpec.RadialClusterWidthDip;

            return

            [

                new(TesseraStackedPanelRole.Volume, 0, 0, volW, h),

                new(TesseraStackedPanelRole.Media, clusterW - mediaW, 0, mediaW, h),

            ];

        }



        private static IReadOnlyList<TesseraStackedPanelPlacement> PlainTextPlacements()
        {
            TesseraFlyoutStyleProfile profile = TesseraFlyoutTweenTargetCatalog.ResolveProfile(StyleIds.PlainText);
            double w = profile.VolumeWidthDip;
            double volH = profile.VolumeHeightDip;
            double gap = TesseraStackedPlacementSpec.PlainTextGapDip;
            double mediaH = profile.MediaHeightDip;
            return

            [

                new(TesseraStackedPanelRole.Volume, 0, 0, w, volH),

                new(TesseraStackedPanelRole.Media, 0, volH + gap, w, mediaH),

            ];

        }



        private static IReadOnlyList<TesseraStackedPanelPlacement> GnomePlacements()
        {
            TesseraFlyoutStyleProfile profile = TesseraFlyoutTweenTargetCatalog.ResolveProfile(StyleIds.Gnome);
            return VerticalColumn(
                (TesseraStackedPanelRole.Media, profile.MediaWidthDip, profile.MediaHeightDip),
                (TesseraStackedPanelRole.Volume, profile.VolumeWidthDip, profile.VolumeHeightDip),
                TesseraStackedPlacementSpec.GnomeGapDip,
                centerHorizontally: true);
        }

        private static IReadOnlyList<TesseraStackedPanelPlacement> CompactPlacements()
        {
            return VerticalVolumeFirstPlacements(StyleIds.Compact);
        }

        private static IReadOnlyList<TesseraStackedPanelPlacement> ModernFlyoutsPlacements()
        {
            return VerticalVolumeFirstPlacements(StyleIds.ModernFlyouts);
        }

        private static IReadOnlyList<TesseraStackedPanelPlacement> VerticalVolumeFirstPlacements(string? styleId)
        {
            TesseraFlyoutStyleProfile profile = TesseraFlyoutTweenTargetCatalog.ResolveProfile(styleId);
            return VerticalColumn(
                (TesseraStackedPanelRole.Volume, profile.VolumeWidthDip, profile.VolumeHeightDip),
                (TesseraStackedPanelRole.Media, profile.MediaWidthDip, profile.MediaHeightDip),
                profile.StackedGapDip,
                centerHorizontally: true);
        }
    }


}
