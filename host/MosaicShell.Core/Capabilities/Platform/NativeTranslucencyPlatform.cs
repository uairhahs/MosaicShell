using MosaicShell.Core.Modules.Tessera;

namespace MosaicShell.Core.Capabilities.Platform
{
    /// <summary>The OS mechanism that draws Native Translucency behind a flyout window.</summary>
    public enum NativeTranslucencyBackend
    {
        /// <summary>None on this system; flyouts keep the Skia frost.</summary>
        None,

        /// <summary>Windows: Avalonia's AcrylicBlur transparency level over WinUI composition.</summary>
        WindowsAcrylic,

        /// <summary>
        /// KDE Plasma on X11 or XWayland: a Transparent window whose <c>_KDE_NET_WM_BLUR_BEHIND_REGION</c> the Host
        /// sets to the card's rounded shape, and KWin's blur effect draws.
        /// </summary>
        KWinBlur,
    }

    /// <summary>
    /// The backend for this process. Windows knows its answer from the OS version; Linux needs the Host to ask KWin
    /// whether its blur effect is loaded, so the Host sets it once at startup with <see cref="Set"/>. Until then (and
    /// in a Worker or test that never sets it) Linux has none, which is the safe answer: the frost stays.
    /// </summary>
    public static class NativeTranslucencyPlatform
    {
        public static NativeTranslucencyBackend Backend { get; private set; } = Default();

        public static bool IsAvailable => Backend != NativeTranslucencyBackend.None;

        /// <summary>Raised when <see cref="Set"/> changes the backend, on the caller's thread.</summary>
        public static event EventHandler? Changed;

        /// <summary>Pure decision from the platform facts, so both platforms' answers are tested on any OS.</summary>
        public static NativeTranslucencyBackend Resolve(
            bool isWindows,
            bool windowsSupportsWinUiAcrylic,
            bool isLinux,
            bool hasX11Display,
            bool kwinBlurLoaded)
        {
            return isWindows
                ? windowsSupportsWinUiAcrylic ? NativeTranslucencyBackend.WindowsAcrylic : NativeTranslucencyBackend.None
                : isLinux && hasX11Display && kwinBlurLoaded ? NativeTranslucencyBackend.KWinBlur : NativeTranslucencyBackend.None;
        }

        public static void Set(NativeTranslucencyBackend backend)
        {
            if (Backend == backend)
            {
                return;
            }

            Backend = backend;
            Changed?.Invoke(null, EventArgs.Empty);
        }

        internal static void ResetForTests()
        {
            Backend = Default();
        }

        private static NativeTranslucencyBackend Default()
        {
            return Resolve(
                OperatingSystem.IsWindows(),
                TesseraNativeTranslucencyPolicy.OsSupportsWinUiAcrylic,
                isLinux: false,
                hasX11Display: false,
                kwinBlurLoaded: false);
        }
    }
}
