namespace MosaicShell.Core.Services.Kde
{
    /// <summary>One display as PowerDevil's org.kde.ScreenBrightness lists it.</summary>
    public sealed record KdeDisplay(string DbusName, bool IsInternal, int Brightness, int MaxBrightness);

    /// <summary>
    /// Screen brightness through PowerDevil, following the brightness keys (user decision, 2026-11-22): the value is
    /// the built-in panel's (else the first display's), and a change moves every display by the same step with
    /// AdjustBrightnessRatio, as the keys do, so the flyout and the keys never disagree. Kept pure so it is tested on
    /// every OS.
    /// </summary>
    public static class KdeScreenBrightnessPolicy
    {
        public const string BusName = "org.kde.Solid.PowerManagement";
        public const string ObjectPath = "/org/kde/ScreenBrightness";
        public const string Interface = "org.kde.ScreenBrightness";
        public const string DisplayInterface = "org.kde.ScreenBrightness.Display";

        /// <summary>
        /// AdjustBrightnessRatio flag that keeps PowerDevil from showing Plasma's pop-up (checked live on Plasma 6.8:
        /// 0 pop-up calls with it, 2 without). The slider is in Tessera's own flyout, as on Windows.
        /// </summary>
        public const uint SuppressIndicatorFlag = 1;

        /// <summary>A reference change smaller than this is the echo of a rounded write, not a change.</summary>
        public const double ChangeThreshold = 0.005;

        public static KdeDisplay? Reference(IReadOnlyList<KdeDisplay> displays)
        {
            return displays.FirstOrDefault(static d => d.IsInternal) ?? displays.FirstOrDefault();
        }

        public static double Ratio(KdeDisplay display)
        {
            return display.MaxBrightness <= 0 ? 0 : Math.Clamp(display.Brightness / (double)display.MaxBrightness, 0, 1);
        }

        /// <summary>The step every display moves by so the reference reaches the target.</summary>
        public static double DeltaFor(double current, double target)
        {
            return Math.Clamp(target, 0, 1) - current;
        }

        public static string DisplayPath(string dbusName)
        {
            return ObjectPath + "/" + dbusName;
        }
    }
}
