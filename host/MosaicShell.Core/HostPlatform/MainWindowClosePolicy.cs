namespace MosaicShell.Core.HostPlatform
{
    public enum MainWindowCloseTrigger
    {
        /// <summary>The user closed the window (title bar, Alt+F4, window menu).</summary>
        User,

        /// <summary>The desktop session is ending: logout, reboot or shutdown.</summary>
        SessionEnding,

        /// <summary>The app itself is shutting down and closing its windows.</summary>
        ApplicationShutdown,
    }

    public enum MainWindowCloseAction
    {
        /// <summary>Cancel the close and hide the window; the tray keeps the Host running.</summary>
        HideToTray,

        /// <summary>Cancel the close and run the Host's own teardown, which then closes the window.</summary>
        ExitApplication,

        /// <summary>Let the close proceed. Cancelling here would veto a logout or stall shutdown.</summary>
        AllowClose,
    }

    /// <summary>
    /// Decides what a close request on the Host's main window does. Only a user close may be cancelled:
    /// session managers treat a cancelled close as a veto of the logout.
    /// </summary>
    public static class MainWindowClosePolicy
    {
        public static MainWindowCloseAction Decide(MainWindowCloseTrigger trigger, bool minimizeToTray)
        {
            return trigger switch
            {
                MainWindowCloseTrigger.User when minimizeToTray => MainWindowCloseAction.HideToTray,
                MainWindowCloseTrigger.User => MainWindowCloseAction.ExitApplication,
                _ => MainWindowCloseAction.AllowClose,
            };
        }
    }
}
