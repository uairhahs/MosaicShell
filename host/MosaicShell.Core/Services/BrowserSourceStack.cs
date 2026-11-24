using System.Runtime.InteropServices;
using MosaicShell.Core.Services.BrowserBridge;
#if WINDOWS
using MosaicShell.Core.Services.BrowserUi;
#endif

namespace MosaicShell.Core.Services
{
    /// <summary>
    /// How the Host learns what a browser is playing. The extension reads the page itself, so it works whatever the
    /// window is doing (covered, minimised, another tab) and it is the preferred source. The accessibility tree needs
    /// nothing installed, but reading it makes the browser build that tree and it only sees a window that is on screen,
    /// so it is the fallback for a browser that has no extension.
    /// </summary>
    public static class BrowserSourceStack
    {
        /// <summary>The relay's apphost next to the Host, the program the browser starts for the extension.</summary>
        public static string RelayFileNameFor(bool isWindows)
        {
            return isWindows ? "MosaicShell.BrowserRelay.exe" : "MosaicShell.BrowserRelay";
        }

        /// <summary>
        /// The session the fallback may read: none while <paramref name="preferred"/> has a player of its own, so the
        /// browser is not made to build an accessibility tree for a rating the extension already supplies, and the
        /// current session again the moment the extension has nothing.
        /// </summary>
        public static Func<MediaSessionInfo?> FallbackSession(IBrowserMediaSource preferred, Func<MediaSessionInfo?> session)
        {
            return () => preferred.Active is null ? session() : null;
        }

#if WINDOWS
        /// <summary>The extension source first, then the accessibility fallback, both reading Windows' current session.</summary>
        public static IBrowserMediaSource[] Create(Func<MediaSessionInfo?> session)
        {
            _ = NativeHostRegistration.Register(
                Path.Combine(AppContext.BaseDirectory, RelayFileNameFor(isWindows: true)),
                NativeHostRegistration.DefaultDataDirectory(),
                new CurrentUserRegistry());

            BrowserMediaSource extension = BrowserMediaSource.Create(() => session()?.Title, () => session()?.AppId);
            BrowserUiSource fallback = new(new WindowsBrowserUi(), FallbackSession(extension, session), TimeProvider.System);
            return [extension, fallback];
        }
#else
        /// <summary>
        /// Linux: the extension source, reading the MPRIS session. The browsers find the relay through a manifest in each
        /// one's config folder. There is no accessibility fallback here yet.
        /// </summary>
        public static IBrowserMediaSource[] CreateLinux(Func<MediaSessionInfo?> session)
        {
            string relay = NativeHostRegistration.EnsureLinuxLauncher(
                Path.Combine(AppContext.BaseDirectory, RelayFileNameFor(isWindows: false)),
                NativeHostRegistration.DotnetRootFromRuntimeDirectory(RuntimeEnvironment.GetRuntimeDirectory()),
                NativeHostRegistration.DefaultDataDirectory());
            _ = NativeHostRegistration.RegisterForLinuxBrowsers(relay, NativeHostRegistration.DefaultLinuxConfigHome());
            return [BrowserMediaSource.Create(() => session()?.Title, () => session()?.AppId)];
        }
#endif
    }
}
