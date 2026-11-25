namespace MosaicShell.Core.Services.Gnome
{
    /// <summary>
    /// Idle on GNOME from Mutter's <c>org.gnome.Mutter.IdleMonitor</c> on the session bus, since Mutter offers no
    /// <c>ext_idle_notifier_v1</c>. An idle watch (AddIdleWatch, milliseconds) fires WatchFired each time the user has
    /// been idle that long; a user-active watch (AddUserActiveWatch) fires once, on the next input. Kept pure so it is
    /// tested on every OS.
    /// </summary>
    public static class MutterIdlePolicy
    {
        public const string BusName = "org.gnome.Mutter.IdleMonitor";
        public const string ObjectPath = "/org/gnome/Mutter/IdleMonitor/Core";
        public const string Interface = "org.gnome.Mutter.IdleMonitor";
        public const string AddIdleWatchMember = "AddIdleWatch";
        public const string AddUserActiveWatchMember = "AddUserActiveWatch";
        public const string RemoveWatchMember = "RemoveWatch";
        public const string WatchFiredMember = "WatchFired";

        /// <summary>The idle watch's interval; Mutter takes milliseconds, and a zero interval would never fire.</summary>
        public static ulong WatchIntervalMs(TimeSpan threshold)
        {
            return (ulong)Math.Max(1, (long)threshold.TotalMilliseconds);
        }

        /// <summary>GNOME is the desktop when XDG_CURRENT_DESKTOP, a colon-separated list, names it.</summary>
        public static bool IsGnomeSession(string? xdgCurrentDesktop)
        {
            return !string.IsNullOrEmpty(xdgCurrentDesktop)
                && xdgCurrentDesktop.Split(':').Contains("GNOME", StringComparer.OrdinalIgnoreCase);
        }
    }
}
