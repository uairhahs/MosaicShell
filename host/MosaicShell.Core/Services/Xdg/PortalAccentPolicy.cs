namespace MosaicShell.Core.Services.Xdg
{
    /// <summary>
    /// The desktop's accent colour from the XDG desktop portal (<c>org.freedesktop.portal.Settings</c>, setting
    /// <c>org.freedesktop.appearance accent-color</c>), which Plasma and GNOME 47+ both provide. The value is (ddd),
    /// red, green and blue from 0 to 1; any outside that range means the desktop sets no accent. Kept pure so it is
    /// tested on every OS.
    /// </summary>
    public static class PortalAccentPolicy
    {
        public const string BusName = "org.freedesktop.portal.Desktop";
        public const string ObjectPath = "/org/freedesktop/portal/desktop";
        public const string Interface = "org.freedesktop.portal.Settings";
        public const string SettingChangedMember = "SettingChanged";
        public const string Namespace = "org.freedesktop.appearance";
        public const string Key = "accent-color";

        /// <summary>The accent as bytes, or null when a channel is outside 0 to 1 (no accent).</summary>
        public static (byte R, byte G, byte B)? FromRgb(double r, double g, double b)
        {
            static bool InRange(double v)
            {
                return v is >= 0 and <= 1;
            }

            static byte ToByte(double v)
            {
                return (byte)Math.Round(v * 255, MidpointRounding.AwayFromZero);
            }

            return InRange(r) && InRange(g) && InRange(b) ? (ToByte(r), ToByte(g), ToByte(b)) : null;
        }

        /// <summary>Whether a SettingChanged entry is the accent.</summary>
        public static bool Touches(string ns, string key)
        {
            return ns == Namespace && key == Key;
        }
    }
}
