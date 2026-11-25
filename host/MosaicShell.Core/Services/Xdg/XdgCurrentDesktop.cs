namespace MosaicShell.Core.Services.Xdg
{
    /// <summary>
    /// The desktop from XDG_CURRENT_DESKTOP, a colon-separated list of names (most specific first, as in
    /// <c>ubuntu:GNOME</c>), for the Linux routes that differ between Plasma and GNOME. Kept pure so it is tested on
    /// every OS.
    /// </summary>
    public static class XdgCurrentDesktop
    {
        public const string Variable = "XDG_CURRENT_DESKTOP";
        public const string Kde = "KDE";
        public const string Gnome = "GNOME";

        /// <summary>This process's XDG_CURRENT_DESKTOP; null when unset.</summary>
        public static string? Current => Environment.GetEnvironmentVariable(Variable);

        public static bool IsPlasma(string? value)
        {
            return Names(value, Kde);
        }

        public static bool IsGnome(string? value)
        {
            return Names(value, Gnome);
        }

        private static bool Names(string? value, string desktop)
        {
            return !string.IsNullOrEmpty(value) && value.Split(':').Contains(desktop, StringComparer.OrdinalIgnoreCase);
        }
    }
}
