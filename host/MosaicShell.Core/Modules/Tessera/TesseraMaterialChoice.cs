using MosaicShell.Core.Settings;

namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>The flyout material, as one Hub choice.</summary>
    public enum TesseraMaterialChoice
    {
        /// <summary>A dense, nearly opaque panel.</summary>
        Solid,

        /// <summary>Tessera's own frost: a translucent crust drawn by the app, no OS blur.</summary>
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

        public static IReadOnlyList<TesseraMaterialChoice> Offered(bool nativeOffered)
        {
            return nativeOffered
                ? [TesseraMaterialChoice.Solid, TesseraMaterialChoice.Frosted, TesseraMaterialChoice.NativeTranslucency]
                : [TesseraMaterialChoice.Solid, TesseraMaterialChoice.Frosted];
        }
    }
}
