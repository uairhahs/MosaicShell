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
                : new CloseChoice(true, "Keep running in the tray", "Closing hides this window. Open it again from the tray icon.");
            return [keep, new CloseChoice(false, "Quit MosaicShell", "Closing this window stops every module.")];
        }

        public static string UpdateHint(bool isWindows)
        {
            return isWindows
                ? "Checks GitHub for a newer release and installs it."
                : "Checks GitHub for a newer release. On Linux, install it the way you installed MosaicShell.";
        }
    }
}
