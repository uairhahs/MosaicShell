using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>The flyout material, as one Hub choice.</summary>
    public enum TesseraMaterialChoice
    {
        /// <summary>A dense, nearly opaque panel.</summary>
        Solid,

        /// <summary>
        /// Tessera's frosted glass. Where the compositor draws only a blur (KWin), the app paints the frosted layers over
        /// it; elsewhere the app draws its own translucent frost with no OS blur.
        /// </summary>
        Frosted,

        /// <summary>The OS blurs the desktop behind the card (Windows acrylic, KWin blur on KDE Plasma).</summary>
        NativeTranslucency,
    }

    /// <summary>
    /// Maps the Hub's single material choice to the three saved settings the flyouts read
    /// (<see cref="TesseraSettings.UseAcrylicBackdrop"/>, <see cref="TesseraSettings.UseNativeTranslucency"/>,
    /// <see cref="TesseraSettings.UseBackdropBlur"/>) and back. It replaces three checkboxes that looked alike.
    /// </summary>
    public static class TesseraMaterialChoicePolicy
    {
        /// <param name="settings">The saved Tessera settings.</param>
        /// <param name="nativeOffered">
        /// Whether this system can draw Native Translucency; where it cannot, a saved Native choice reads as Frosted,
        /// which is what the flyouts show there.
        /// </param>
        public static TesseraMaterialChoice FromSettings(TesseraSettings settings, bool nativeOffered)
        {
            return !settings.UseAcrylicBackdrop
                ? TesseraMaterialChoice.Solid
                : settings.UseNativeTranslucency && nativeOffered
                ? TesseraMaterialChoice.NativeTranslucency
                : TesseraMaterialChoice.Frosted;
        }

        /// <summary>
        /// Writes a choice. The simulated "backdrop blur" (a lighter frost recipe; it never sampled the desktop) has no
        /// control of its own any more, so it is kept at its default.
        /// </summary>
        public static void Apply(TesseraSettings settings, TesseraMaterialChoice choice)
        {
            settings.UseAcrylicBackdrop = choice != TesseraMaterialChoice.Solid;
            settings.UseNativeTranslucency = choice == TesseraMaterialChoice.NativeTranslucency;
            settings.UseBackdropBlur = true;
        }

        /// <summary>
        /// Whether the material changes how <paramref name="styleId"/> draws. A style whose spec calls for matte chrome
        /// (MaterialYou's pills, PlainText's slanted card) always gets the solid panel, so the Hub disables the choice
        /// there. The saved choice is kept for the other styles.
        /// </summary>
        public static bool IsSelectable(string? styleId)
        {
            return !TesseraFlyoutTweenTargetCatalog.ResolveProfile(styleId).RequiresMatteChrome;
        }

        /// <summary>
        /// The choices the Hub lists: Native Translucency wherever this platform is signed off, drawable or not, so a
        /// system without a backend sees it greyed out with <see cref="NativeUnavailableText"/> instead of not at all.
        /// </summary>
        public static IReadOnlyList<TesseraMaterialChoice> Listed(bool nativeSignedOff)
        {
            return Offered(nativeSignedOff);
        }

        /// <summary>Why Native Translucency cannot be drawn here: what is missing, in the words of the system.</summary>
        public static string NativeUnavailableText(bool isWindows, string? xdgCurrentDesktop)
        {
            return isWindows
                ? "Native Translucency needs Windows 11."
                : Services.Xdg.XdgCurrentDesktop.IsPlasma(xdgCurrentDesktop)
                    ? "Native Translucency needs Plasma's Blur desktop effect."
                    : $"Native Translucency is not available on {HostPlatform.DesktopFeatures.Resolve(false, xdgCurrentDesktop).DesktopName} yet.";
        }

        public static IReadOnlyList<TesseraMaterialChoice> Offered(bool nativeOffered)
        {
            return nativeOffered
                ? [TesseraMaterialChoice.Solid, TesseraMaterialChoice.Frosted, TesseraMaterialChoice.NativeTranslucency]
                : [TesseraMaterialChoice.Solid, TesseraMaterialChoice.Frosted];
        }
    }
}
