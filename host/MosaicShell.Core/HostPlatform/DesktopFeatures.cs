using MosaicShell.Core.Services.Xdg;

namespace MosaicShell.Core.HostPlatform
{
    /// <summary>
    /// What the running desktop lets MosaicShell do, matching the routes <c>HostServices</c> really wires: Windows and
    /// Plasma have them all; GNOME and other Linux desktops have none of them yet. Kept pure so it is tested on every OS.
    /// </summary>
    public sealed class DesktopFeatures
    {
        private static readonly DesktopFeature[] Every = Enum.GetValues<DesktopFeature>();
        private static readonly Lazy<DesktopFeatures> CurrentLazy = new(() =>
            Resolve(OperatingSystem.IsWindows(), XdgCurrentDesktop.Current));

        private readonly HashSet<DesktopFeature> _available;

        private DesktopFeatures(IEnumerable<DesktopFeature> available)
        {
            _available = [.. available];
        }

        /// <summary>Every feature: Windows, Plasma, and the fakes tests build services from.</summary>
        public static DesktopFeatures All { get; } = new(Every);

        /// <summary>No feature: a desktop MosaicShell has no routes for yet.</summary>
        public static DesktopFeatures None { get; } = new([]);

        /// <summary>This process's desktop, read once.</summary>
        public static DesktopFeatures Current => CurrentLazy.Value;

        public static DesktopFeatures Resolve(bool isWindows, string? xdgCurrentDesktop)
        {
            return isWindows || XdgCurrentDesktop.IsPlasma(xdgCurrentDesktop) ? All : None;
        }

        public bool Has(DesktopFeature feature)
        {
            return _available.Contains(feature);
        }
    }
}
