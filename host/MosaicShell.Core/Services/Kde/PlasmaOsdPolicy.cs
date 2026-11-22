namespace MosaicShell.Core.Services.Kde
{
    /// <summary>
    /// Plasma's own pop-ups (OSDs) are drawn by plasmashell when other parts of the desktop call its
    /// <c>org.kde.osdService</c>: plasma-pa calls volumeChanged for volume and mute, PowerDevil calls
    /// screenBrightnessChanged (or the older brightnessChanged). While Tessera is armed, MosaicShell answers those calls
    /// with the service's own <c>hide</c>, as the Windows suppressor hides the native flyout when it shows. Pop-ups
    /// Tessera does not replace (keyboard backlight, microphone, keyboard layout, text) stay. Kept pure so it is tested
    /// on every OS.
    /// </summary>
    public static class PlasmaOsdPolicy
    {
        public const string BusName = "org.kde.plasmashell";
        public const string ObjectPath = "/org/kde/osdService";
        public const string Interface = "org.kde.osdService";
        public const string HideMember = "hide";

        /// <summary>The osdService calls whose pop-up Tessera replaces, so they are hidden.</summary>
        public static IReadOnlyList<string> HiddenMembers { get; } = ["volumeChanged", "screenBrightnessChanged", "brightnessChanged"];

        public static bool ShouldHide(string member)
        {
            return HiddenMembers.Contains(member, StringComparer.Ordinal);
        }
    }
}
