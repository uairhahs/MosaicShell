namespace MosaicShell.Core.Services
{
    /// <summary>
    /// Caps/Num/Scroll via GetKeyState polling while armed. Avalonia's UI message pump does not
    /// reliably deliver WH_KEYBOARD_LL callbacks; polling matches airplane-mode sampling.
    /// </summary>
    public static class LockKeyPollPolicy
    {
        public const bool PreferPollOverLowLevelHook = true;

        public const int PollIntervalMs = 100;

        /// <summary>
        /// Do not defer Changed to ThreadPool; marshaling to the Host UI thread belongs in TesseraCapability.
        /// </summary>
        public const bool MustInvokeChangedSynchronously = true;
    }
}
