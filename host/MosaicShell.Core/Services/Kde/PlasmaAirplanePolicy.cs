namespace MosaicShell.Core.Services.Kde
{
    /// <summary>
    /// Plasma's airplane mode. plasma-nm keeps it as <c>AirplaneModeEnabled</c> in the <c>[General]</c> group of
    /// <c>~/.config/plasma-nm</c>, and on each toggle rewrites the file, then sends KConfig's
    /// <c>org.kde.kconfig.notify.ConfigChanged</c> on <c>/plasma_nm</c> naming the group and key. That key is the state
    /// Plasma's own pop-up reports, as Windows' airplane mode is a setting of its own; the radios alone cannot say it,
    /// since Plasma blocks only the radios it manages (Wi-Fi always, Bluetooth only while bluez runs). Kept pure so it
    /// is tested on every OS.
    /// </summary>
    public static class PlasmaAirplanePolicy
    {
        public const string ConfigFileName = "plasma-nm";
        public const string Group = "General";
        public const string Key = "AirplaneModeEnabled";

        /// <summary>KConfig names the notify path after the file, with characters a D-Bus path refuses as underscores.</summary>
        public const string NotifyPath = "/plasma_nm";
        public const string NotifyInterface = "org.kde.kconfig.notify";
        public const string NotifyMember = "ConfigChanged";

        /// <summary><c>$XDG_CONFIG_HOME/plasma-nm</c>, with ~/.config when it is unset or relative.</summary>
        public static string ConfigPath(string? xdgConfigHome, string home)
        {
            string config = !string.IsNullOrEmpty(xdgConfigHome) && xdgConfigHome.StartsWith('/')
                ? xdgConfigHome.TrimEnd('/')
                : home.TrimEnd('/') + "/.config";
            return config + "/" + ConfigFileName;
        }

        /// <summary>
        /// The key's value in <c>[General]</c>, read as KConfig reads a bool (true, on, yes or 1). No file, no group or no
        /// key is off, which is plasma-nm's default.
        /// </summary>
        public static bool ReadEnabled(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            bool inGroup = false;
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                if (line[0] == '[')
                {
                    inGroup = line == "[" + Group + "]";
                    continue;
                }

                int eq = line.IndexOf('=');
                if (!inGroup || eq < 0)
                {
                    continue;
                }

                // A key may carry an option marker such as [$i] (immutable) before the '='.
                string name = line[..eq].Trim();
                int marker = name.IndexOf("[$", StringComparison.Ordinal);
                if (marker >= 0)
                {
                    name = name[..marker];
                }

                if (name == Key)
                {
                    return IsTrue(line[(eq + 1)..].Trim());
                }
            }

            return false;
        }

        /// <summary>Whether a ConfigChanged entry (a group and the keys written in it) names the airplane key.</summary>
        public static bool Touches(string group, IEnumerable<string> keys)
        {
            return group == Group && keys.Contains(Key, StringComparer.Ordinal);
        }

        /// <summary>Plasma is the desktop when XDG_CURRENT_DESKTOP, a colon-separated list, names KDE.</summary>
        public static bool IsPlasmaSession(string? xdgCurrentDesktop)
        {
            return !string.IsNullOrEmpty(xdgCurrentDesktop)
                && xdgCurrentDesktop.Split(':').Contains("KDE", StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsTrue(string value)
        {
            return value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("on", StringComparison.OrdinalIgnoreCase)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || value == "1";
        }
    }
}
