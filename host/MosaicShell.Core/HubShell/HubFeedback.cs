using MosaicShell.Core.HostPlatform;

namespace MosaicShell.Core.HubShell
{
    /// <summary>
    /// A short message at the bottom of the Hub for an action that finished, in place of the old status line. One that
    /// can be undone offers Undo and stays longer so there is time to use it.
    /// </summary>
    public sealed record HubToast(string Message, string? UndoLabel, int StaysMs)
    {
        public const int PlainMs = 4000;
        public const int UndoableMs = 8000;

        public static HubToast Done(string message)
        {
            return new HubToast(message, null, PlainMs);
        }

        public static HubToast Undoable(string message)
        {
            return new HubToast(message, "Undo", UndoableMs);
        }
    }

    /// <summary>Home's banner: something that needs attention, until it is dealt with.</summary>
    public static class HubBanner
    {
        public static string ModuleFailed(string moduleName)
        {
            return $"{moduleName} could not start. Open {moduleName} for details.";
        }

        public static string UpdateReady(string version)
        {
            return $"MosaicShell {version} is ready to install.";
        }
    }

    /// <summary>Browser media's status light: green while Grout is connected, red while not, none without a bridge.</summary>
    public enum GroutLight
    {
        None,
        Connected,
        Disconnected,
    }

    /// <summary>One choice for what closing the Hub window does.</summary>
    public sealed record CloseChoice(bool KeepsRunning, string Label, string Consequence);

    /// <summary>
    /// The Settings page: its sections in order, which start folded, and texts that differ by desktop. "When I close this
    /// window" is a real choice of two, each saying what happens, and never promises a tray the desktop does not have.
    /// </summary>
    public static class HubSettingsLayout
    {
        public static IReadOnlyList<string> Sections { get; } = ["General", "Updates", "Browser media", "Diagnostics"];

        public const string UpdateActionLabel = "Check for updates";

        public static bool FoldedByDefault(string section)
        {
            return section == "Diagnostics";
        }

        public static IReadOnlyList<CloseChoice> CloseChoices(DesktopFeatures features)
        {
            CloseChoice keep = features.MissingText(DesktopFeature.Tray) is string noTray
                ? new CloseChoice(
                    true,
                    "Keep running in the background",
                    $"Closing hides this window. {noTray[..^1]}, so open MosaicShell again from your apps to bring it back.")
                : features.TrayIsTopBar
                    ? new CloseChoice(true, "Keep running in the top bar", "Closing hides this window. Open it again from the MosaicShell icon in the top bar.")
                    : new CloseChoice(true, "Keep running in the tray", "Closing hides this window. Open it again from the tray icon.");
            return [keep, new CloseChoice(false, "Quit MosaicShell", "Closing this window stops every module.")];
        }

        /// <summary>Where to get Grout, MosaicShell's browser extension (the README links the same listing).</summary>
        public const string GroutStoreUrl = "https://chromewebstore.google.com/detail/grout/pcjkacalabdgejinbmfdejicfhlonnpf";

        /// <summary>
        /// Browser media's line: whether Grout is connected now. <paramref name="connections"/> is null when this build
        /// has no browser bridge.
        /// </summary>
        public static string BrowserMediaStatus(int? connections)
        {
            return connections switch
            {
                null => "This build does not read browser media.",
                0 => "Grout is not connected. Install it in your browser, then open YouTube Music, YouTube, Spotify or SoundCloud.",
                1 => "Grout is connected.",
                _ => $"Grout is connected in {connections} browsers.",
            };
        }

        public static GroutLight GroutLightFor(int? connections)
        {
            return connections switch
            {
                null => GroutLight.None,
                0 => GroutLight.Disconnected,
                _ => GroutLight.Connected,
            };
        }

        /// <summary>Whether Browser media offers the Get Grout link: only while it is not connected.</summary>
        public static bool OffersGroutInstall(int? connections)
        {
            return connections == 0;
        }

        public static string UpdateHint(bool isWindows)
        {
            return isWindows
                ? "Checks GitHub for a newer release and installs it."
                : "Checks GitHub for a newer release. On Linux, install it the way you installed MosaicShell.";
        }
    }
}
