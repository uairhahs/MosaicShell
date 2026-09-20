using MosaicShell.Core.Services.BrowserBridge;
using MosaicShell.Core.Services.BrowserUi;

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
        public const string RelayFileName = "MosaicShell.BrowserRelay.exe";

        /// <summary>
        /// The session the fallback may read: none while <paramref name="preferred"/> has a player of its own, so the
        /// browser is not made to build an accessibility tree for a rating the extension already supplies, and the
        /// current session again the moment the extension has nothing.
        /// </summary>
        public static Func<MediaSessionInfo?> FallbackSession(IBrowserMediaSource preferred, Func<MediaSessionInfo?> session)
        {
            return () => preferred.Active is null ? session() : null;
        }

        /// <summary>The extension source first, then the accessibility fallback, both reading Windows' current session.</summary>
        public static IBrowserMediaSource[] Create(Func<MediaSessionInfo?> session)
        {
            _ = NativeHostRegistration.Register(
                Path.Combine(AppContext.BaseDirectory, RelayFileName),
                NativeHostRegistration.DefaultDataDirectory(),
                new CurrentUserRegistry());

            BrowserMediaSource extension = BrowserMediaSource.Create(() => session()?.Title);
            BrowserUiSource fallback = new(new WindowsBrowserUi(), FallbackSession(extension, session), TimeProvider.System);
            return [extension, fallback];
        }
    }
}
