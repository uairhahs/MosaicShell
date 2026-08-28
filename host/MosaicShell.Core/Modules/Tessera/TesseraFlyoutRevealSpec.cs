namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// Fancy phase-2 layout reveal (TweenNode1 0..1) per YourFlyouts layout Animated meter groups.
    /// </summary>
    public static class TesseraFlyoutRevealSpec
    {
        public const string StyleFluent = "fluent";
        public const string StyleWindows11 = "windows11";
        public const string StyleGnome = "gnome";
        public const string StylePlainText = "plaintext";
        public const string StyleCoreUi = "coreui";
        public const string StyleSquare = "square";
        public const string StyleSmouti = "smouti";
        public const string StyleMeter = "meter";
        public const string StyleCompact = "compact";
        public const string StyleModernFlyouts = "modernflyouts";
        public const string StyleMaterialYou = "materialyou";
        public const string StyleRadial = "radial";

        /// <summary>
        /// YourFlyouts rest pose: TweenNode1 stays at 1 after Fancy until hide.
        /// Hub preview and non-Fancy live must start here, not at animation-start.
        /// </summary>
        public const double RestRevealProgress = 1;

        /// <summary>Ani2 TweenNode1 at Fancy phase-2 start (skin still collapsed).</summary>
        public const double FancyPhase2StartProgress = 0;

        /// <summary>Static Hub / exporter trees never run motion, so they must use rest.</summary>
        public const bool PreviewMustUseRestReveal = true;

        /// <summary>Normalized TweenNode1 (0..1) from stepped tween value 0..100.</summary>
        public static double NormalizeRevealProgress(double tweenNode1)
        {
            return Math.Clamp(tweenNode1, 0, 100) / 100.0;
        }

        /// <summary>
        /// Initial TweenNode1 for a newly built reveal host.
        /// Preview and non-phase-2 live are rest; live Fancy starts at 0 through phase 1.
        /// Visible rebuilds stay at rest (YourFlyouts does not reset TweenNode1 while shown).
        /// </summary>
        public static double ResolveInitialRevealProgress(bool isPreview, bool willRunPhase2)
        {
            return ResolveInitialRevealProgress(isPreview, willRunPhase2, sessionAlreadyShowing: false);
        }

        public static double ResolveInitialRevealProgress(
            bool isPreview,
            bool willRunPhase2,
            bool sessionAlreadyShowing)
        {
            return sessionAlreadyShowing
                ? RestRevealProgress
                : isPreview && PreviewMustUseRestReveal ? RestRevealProgress : !willRunPhase2 ? RestRevealProgress : FancyPhase2StartProgress;
        }

        /// <summary>Win11 media clip is live only while Fancy phase 2 is engaged or at rest.</summary>
        public static bool ResolveInitialPhase2Engaged(
            bool isPreview,
            bool willRunPhase2,
            bool sessionAlreadyShowing = false)
        {
            return ResolveInitialRevealProgress(isPreview, willRunPhase2, sessionAlreadyShowing) >= RestRevealProgress;
        }

        /// <summary>
        /// Hide/revive pose. Fancy must start TweenNode1 at 0. Fast and fade keep rest so
        /// the next Show does not flash a collapsed media strip.
        /// </summary>
        public static double ResolveHideRevealProgress(bool willRunPhase2)
        {
            return willRunPhase2 ? FancyPhase2StartProgress : RestRevealProgress;
        }

        /// <summary>
        /// Show must start TweenNode1 at 0 when Fancy phase 2 will run. A rest pose during
        /// phase 1 paints a finished media card, so in cannot match the hide dissolve.
        /// </summary>
        public static double ResolveShowRevealProgress(bool willRunPhase2)
        {
            return ResolveHideRevealProgress(willRunPhase2);
        }

        /// <summary>
        /// Show collapsed pose still uses clip binders. Engaged=false until rest made
        /// Win11/Fluent treat phase 1 as a finished card, then phase 2 had nothing to wipe.
        /// </summary>
        public static bool ResolveMotionPhase2Engaged(double progress, bool willRunPhase2, bool entrance)
        {
            return !willRunPhase2 || entrance || progress >= RestRevealProgress - 0.001;
        }

        public static bool ResolveHidePhase2Engaged(bool willRunPhase2)
        {
            return ResolveHideRevealProgress(willRunPhase2) >= RestRevealProgress;
        }

        public static bool StyleSupportsPhase2(string? styleId)
        {
            return TesseraFlyoutTweenTargetCatalog.StyleSupportsPhase2(styleId);
        }

        /// <summary>YF Center.inc Animated fonts run on the volume card without a media strip.</summary>
        public static bool StylePhase2WithoutMediaStrip(string? styleId)
        {
            return TesseraFlyoutTweenTargetCatalog.StylePhase2WithoutMediaStrip(styleId);
        }

        /// <summary>Known binders; Host must not take the legacy MaxWidth/MaxHeight path.</summary>
        public static bool UsesDedicatedRevealBinder(string? styleId)
        {
            return StyleSupportsPhase2(styleId);
        }

        public static bool StyleIsPhase2NoOp(string? styleId)
        {
            return TesseraFlyoutTweenTargetCatalog.StyleIsPhase2NoOp(styleId);
        }

        /// <summary>Host ApplyReveal / wrap factory must switch on catalog kind, not a parallel style string table.</summary>
        public const bool HostMustDispatchRevealFromCatalogKind = true;

        public static TesseraFlyoutRevealKind ResolveHostKind(string? styleId)
        {
            return TesseraFlyoutTweenTargetCatalog.ResolveProfile(styleId).RevealKind;
        }

        /// <summary>Legacy (<see cref="TesseraFlyoutRevealKind.None"/>) divider scale.</summary>
        public static double ResolveDividerScale(string? styleId, double revealProgress, bool musicVisible)
        {
            return ResolveHorizontalReveal(styleId, revealProgress, musicVisible);
        }

        public static double ResolveMediaOpacity(string? styleId, double revealProgress, bool musicVisible)
        {
            return !musicVisible
                ? 1
                : TesseraFlyoutTweenTargetCatalog.HasChannel(styleId, TesseraTweenChannel.ContentOpacity) ? revealProgress : 1;
        }

        private static double ResolveHorizontalReveal(string? styleId, double revealProgress, bool musicVisible)
        {
            if (!musicVisible)
            {
                return 1;
            }

            string id = (styleId ?? string.Empty).ToLowerInvariant();
            return id switch
            {
                StyleFluent => revealProgress,
                StyleCoreUi => revealProgress,
                _ => 1,
            };
        }
    }
}
