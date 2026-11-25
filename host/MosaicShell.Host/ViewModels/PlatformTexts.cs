using MosaicShell.Core.HostPlatform;

namespace MosaicShell.Host.ViewModels
{
    /// <summary>
    /// The Hub's system-dependent texts (<see cref="HostPlatformText"/>) as static members, so XAML reads them with
    /// x:Static whatever a panel's data context is.
    /// </summary>
    public static class PlatformTexts
    {
        public static string About => HostPlatformText.Current.About;

        public static string Autostart => HostPlatformText.Current.Autostart;

        public static string AccentPlaceholder => HostPlatformText.Current.AccentPlaceholder;

        public static string ReservedShortcuts => HostPlatformText.Current.ReservedShortcuts;

        public static string NativePopups => HostPlatformText.Current.NativePopups;
    }
}
