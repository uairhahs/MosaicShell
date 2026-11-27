using MosaicShell.Core.HostPlatform;

namespace MosaicShell.Core.Modules.Settings
{
    /// <summary>Whether a row works on this desktop, and the line to show in its place when it does not.</summary>
    public readonly record struct SettingAvailability(bool IsAvailable, string? Reason)
    {
        public static SettingAvailability Of(SettingRow row, DesktopFeatures features)
        {
            string? missing = row.Requires is DesktopFeature needed ? features.MissingText(needed) : null;
            return new(missing is null, missing);
        }
    }
}
