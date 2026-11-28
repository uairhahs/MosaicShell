using Microsoft.Win32;

namespace MosaicShell.Core.Services
{
    /// <summary>
    /// The desktop's own switch for see-through surfaces. Off, MosaicShell draws its flyouts opaque, as the desktop's own
    /// pop-ups are.
    /// </summary>
    public interface ITransparencyEffects
    {
        /// <summary>Read each time it is asked, so a change in the desktop's settings takes effect at the next flyout.</summary>
        bool Enabled { get; }
    }

    /// <summary>A desktop with no such switch: transparency is always allowed.</summary>
    public sealed class AlwaysTransparencyEffects : ITransparencyEffects
    {
        public bool Enabled => true;
    }

    /// <summary>
    /// Windows' Transparency effects (Settings, Personalization, Colors), the per-user <c>EnableTransparency</c> value that
    /// Windows' own flyouts and the taskbar follow.
    /// </summary>
    public sealed class WindowsTransparencyEffects : ITransparencyEffects
    {
        private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        public bool Enabled
        {
            get
            {
                if (!OperatingSystem.IsWindows())
                {
                    return true;
                }

                try
                {
                    using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
                    return FromRegistryValue(key?.GetValue("EnableTransparency") as int?);
                }
                catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
                {
                    return true;
                }
            }
        }

        /// <summary>On unless the value says 0: a missing value is Windows' default, which is on.</summary>
        public static bool FromRegistryValue(int? value)
        {
            return value != 0;
        }
    }
}
