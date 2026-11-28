using MosaicShell.Core.Services.Xdg;

namespace MosaicShell.Core.HostPlatform
{
    /// <summary>
    /// What the running desktop lets MosaicShell do, matching the routes <c>HostServices</c> really wires: Windows and
    /// Plasma have them all; GNOME has the tray (an icon in its top bar) and nothing else yet; other Linux desktops have
    /// none. Kept pure so it is tested on every OS.
    /// </summary>
    public sealed class DesktopFeatures
    {
        private static readonly DesktopFeature[] Every = Enum.GetValues<DesktopFeature>();
        private static readonly Lazy<DesktopFeatures> CurrentLazy = new(() =>
            Resolve(OperatingSystem.IsWindows(), XdgCurrentDesktop.Current));

        private readonly HashSet<DesktopFeature> _available;

        private DesktopFeatures(string desktopName, IEnumerable<DesktopFeature> available, bool trayIsTopBar = false)
        {
            DesktopName = desktopName;
            _available = [.. available];
            TrayIsTopBar = trayIsTopBar;
        }

        /// <summary>The tray is MosaicShell's own icon in GNOME's top bar, not a system tray.</summary>
        public bool TrayIsTopBar { get; }

        /// <summary>Every feature: Windows, Plasma, and the fakes tests build services from.</summary>
        public static DesktopFeatures All { get; } = new("Windows", Every);

        /// <summary>No feature: a desktop MosaicShell has no routes for yet.</summary>
        public static DesktopFeatures None { get; } = new("this desktop", []);

        /// <summary>This process's desktop, read once.</summary>
        public static DesktopFeatures Current => CurrentLazy.Value;

        /// <summary>The desktop as people know it, for texts such as "not available on GNOME yet".</summary>
        public string DesktopName { get; }

        public static DesktopFeatures Resolve(bool isWindows, string? xdgCurrentDesktop)
        {
            if (isWindows)
            {
                return All;
            }

            // On GNOME, MosaicShell's own Shell extension puts its icon in the top bar: that is its tray.
            return XdgCurrentDesktop.IsPlasma(xdgCurrentDesktop) ? new("Plasma", Every)
                : XdgCurrentDesktop.IsGnome(xdgCurrentDesktop) ? new("GNOME", [DesktopFeature.Tray], trayIsTopBar: true)
                : new(Name(xdgCurrentDesktop), []);
        }

        public bool Has(DesktopFeature feature)
        {
            return _available.Contains(feature);
        }

        /// <summary>What to show where <paramref name="feature"/> would be; null when the desktop has it.</summary>
        public string? MissingText(DesktopFeature feature)
        {
            if (Has(feature))
            {
                return null;
            }

            string what = feature switch
            {
                DesktopFeature.GlobalHotkeys => "Shortcuts are",
                DesktopFeature.MediaKeys => "Media keys are",
                DesktopFeature.FullscreenProbe => "Telling when an app is full screen is",
                DesktopFeature.Tray => "A tray icon is",
                _ => feature + " is",
            };
            return $"{what} not available on {DesktopName} yet.";
        }

        private static string Name(string? xdgCurrentDesktop)
        {
            if (XdgCurrentDesktop.IsGnome(xdgCurrentDesktop))
            {
                return "GNOME";
            }

            string? first = xdgCurrentDesktop?.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            return string.IsNullOrEmpty(first) ? "this desktop" : first;
        }
    }
}
