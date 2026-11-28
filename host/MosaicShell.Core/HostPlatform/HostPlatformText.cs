using MosaicShell.Core.Services.Xdg;

namespace MosaicShell.Core.HostPlatform
{
    /// <summary>
    /// The Hub's texts that name the operating system or describe its own pop-ups, for the system the Host runs on:
    /// Windows, or on Linux the desktop where it is known (<see cref="XdgCurrentDesktop"/>). Kept pure so it is tested on
    /// every OS.
    /// </summary>
    /// <param name="OsName">The system as the texts name it: Windows, KDE Plasma, GNOME or Linux.</param>
    /// <param name="About">The Hub's About text.</param>
    /// <param name="Autostart">The start-at-login toggle's description.</param>
    /// <param name="AccentPlaceholder">The accent box's placeholder, for the System accent.</param>
    /// <param name="ReservedShortcuts">The hotkey capture's note on the shortcuts the system keeps.</param>
    /// <param name="NativePopups">How the system's own volume and brightness pop-ups are kept out of Tessera's way.</param>
    public sealed record HostPlatformText(
        string OsName,
        string About,
        string Autostart,
        string AccentPlaceholder,
        string ReservedShortcuts,
        string NativePopups)
    {
        public const string Windows = "Windows";
        public const string Plasma = "KDE Plasma";
        public const string Gnome = "GNOME";
        public const string Linux = "Linux";

        /// <summary>The texts for this process's system.</summary>
        public static HostPlatformText Current { get; } = For(OperatingSystem.IsWindows(), XdgCurrentDesktop.Current);

        public static string NameFor(bool isWindows, string? xdgCurrentDesktop)
        {
            return isWindows ? Windows
                : XdgCurrentDesktop.IsPlasma(xdgCurrentDesktop) ? Plasma
                : XdgCurrentDesktop.IsGnome(xdgCurrentDesktop) ? Gnome
                : Linux;
        }

        public static HostPlatformText For(bool isWindows, string? xdgCurrentDesktop)
        {
            string os = NameFor(isWindows, xdgCurrentDesktop);
            return new HostPlatformText(
                os,
                $"MosaicShell is a native {os} desktop shell. Add modules for pop-ups, launchers, clocks and overlays, and turn them on: they keep working in the background while MosaicShell runs, with this window closed.",
                $"Opens in the background when you sign in to {os}.",
                "Blank uses the system accent",
                isWindows
                    ? "Capture a shortcut (Ctrl/Alt/Shift/Win + key). Win+S / Win+A / Win+Q are reserved by Windows and will be replaced automatically."
                    : $"Capture a shortcut (Ctrl/Alt/Shift/Win + key; Win is the Super key). Win+S / Win+A / Win+Q are reserved by {os} and will be replaced automatically.",
                NativePopupsFor(os));
        }

        /// <summary>The status line when a captured shortcut is one the system keeps.</summary>
        public string ReservedShortcut(string gesture)
        {
            return $"{gesture} is reserved by {OsName}. Pick Ctrl+Alt+Letter instead.";
        }

        private static string NativePopupsFor(string os)
        {
            return os switch
            {
                Windows => "Stock Windows volume HUD is hidden best-effort (WinEvent + ShellHook) while Tessera is armed. Vendor laptop OSDs (Dell/HP/…) unsupported. Media is SMTC-only. Re-arm after changing hooks.",
                Plasma => "Plasma's own volume and brightness pop-ups are hidden while Tessera is armed. Media comes from MPRIS players.",
                Gnome => "GNOME's own volume and brightness pop-ups are hidden while Tessera is armed, by MosaicShell's GNOME Shell extension, which GNOME loads after you log out and back in once. The microphone pop-up stays. Media comes from MPRIS players.",
                _ => "This desktop's own volume and brightness pop-ups are not hidden. Media comes from MPRIS players.",
            };
        }
    }
}
