using System.Globalization;
using MosaicShell.Core.Services.Xdg;

namespace MosaicShell.Core.HostPlatform
{
    /// <summary>Who decides how far one volume key press moves the volume.</summary>
    public enum VolumeKeyStepSource
    {
        /// <summary>No one MosaicShell can reach: the desktop keeps its own step.</summary>
        None,

        /// <summary>Tessera's own key hook (Windows), with the step in Tessera's settings.</summary>
        OwnHook,

        /// <summary>GNOME's media keys, with the step in its <c>volume-step</c> setting.</summary>
        Gnome,

        /// <summary>Plasma's audio applet, with the step in <c>plasmaparc</c>.</summary>
        Plasma,
    }

    /// <summary>
    /// Windows lets Tessera intercept the volume keys and step the volume itself. GNOME and Plasma do not, so there the
    /// step a press makes is the desktop's own setting, and Tessera's step row reads and writes that setting instead.
    /// Kept pure so it is tested on every OS.
    /// </summary>
    public static class VolumeKeyStepPolicy
    {
        public const string GnomeSchema = "org.gnome.settings-daemon.plugins.media-keys";
        public const string GnomeKey = "volume-step";
        public const string PlasmaFile = "plasmaparc";
        public const string PlasmaGroup = "General";
        public const string PlasmaKey = "VolumeStep";

        /// <summary>Plasma's step when <c>plasmaparc</c> has no entry.</summary>
        public const int PlasmaDefault = 5;

        public static VolumeKeyStepSource Resolve(bool isWindows, string? xdgCurrentDesktop)
        {
            return isWindows
                ? VolumeKeyStepSource.OwnHook
                : XdgCurrentDesktop.IsPlasma(xdgCurrentDesktop)
                    ? VolumeKeyStepSource.Plasma
                    : XdgCurrentDesktop.IsGnome(xdgCurrentDesktop) ? VolumeKeyStepSource.Gnome : VolumeKeyStepSource.None;
        }

        /// <summary>The steps, in percent points, the owner stores: GNOME's schema allows 1 to 20.</summary>
        public static (int Min, int Max) Range(VolumeKeyStepSource source)
        {
            return source switch
            {
                VolumeKeyStepSource.Gnome => (1, 20),
                VolumeKeyStepSource.Plasma => (1, 100),
                _ => (1, 25),
            };
        }

        public static int Clamp(VolumeKeyStepSource source, int percent)
        {
            (int min, int max) = Range(source);
            return Math.Clamp(percent, min, max);
        }

        /// <summary>Whether the page offers the switch that turns Tessera's own key hook on and off.</summary>
        public static bool HasHookSwitch(VolumeKeyStepSource source)
        {
            return source == VolumeKeyStepSource.OwnHook;
        }

        /// <summary>The step from <c>gsettings get</c> output (<c>6</c>, or <c>int32 6</c>); null when it is not a number.</summary>
        public static int? ParseGnome(string? printed)
        {
            string? last = printed?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            return int.TryParse(last, NumberStyles.Integer, CultureInfo.InvariantCulture, out int step) ? step : null;
        }

        /// <summary>The step from <c>plasmaparc</c>; Plasma's default when the entry is missing or unreadable.</summary>
        public static int ParsePlasma(string? value)
        {
            return int.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int step) ? step : PlasmaDefault;
        }

        /// <summary>The line under the step row.</summary>
        public static string Hint(VolumeKeyStepSource source)
        {
            return source switch
            {
                VolumeKeyStepSource.OwnHook => "How far one press of a volume key moves the volume, while the key hook is on.",
                VolumeKeyStepSource.Gnome => "GNOME's own volume key step: changing it here changes it for GNOME too.",
                VolumeKeyStepSource.Plasma => "Plasma's own volume key step: changing it here changes it for Plasma too.",
                _ => "This desktop does not let MosaicShell change its volume key step.",
            };
        }
    }
}
