namespace MosaicShell.Core.Modules.Tessera
{
    /// <summary>
    /// One auto-dismiss clock per Tessera session. Stacked slots suppress per-window
    /// timers; IPC consumers read echoed session snapshots, not optimistic Show flags.
    /// </summary>
    public static class TesseraFlyoutDismissCoordinator
    {
        /// <summary>
        /// Stacked (and session-owned) HUDs arm one presenter clock. Per-HWND
        /// <c>ResetDismissTimer</c> must stay suppressed for those slots.
        /// </summary>
        public const bool SessionOwnsAutoDismissClock = true;

        /// <summary>
        /// Host IPC server must echo <see cref="TesseraFlyoutSessionSnapshot"/> after apply
        /// so Worker <c>IsVisible</c> matches opacity / session showing, not Show-optimistic.
        /// </summary>
        public const bool IpcMustEchoSessionSnapshot = true;

        public static bool WindowMustSuppressAutoDismiss(bool sessionOwnsClock)
        {
            return sessionOwnsClock;
        }

        public static bool ShouldArmSessionAutoDismiss(int autoDismissMs, bool sessionOpen)
        {
            return sessionOpen && autoDismissMs > 0;
        }
    }
}
