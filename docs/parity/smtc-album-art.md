# Album art: JaxCore vs native Tessera

## How JaxCore got YTM covers

YourFlyouts used a Rainmeter media plugin fed by a browser extension. The extension read
`navigator.mediaSession.metadata.artwork` on the page, downloaded the image and sent it to Rainmeter over a local
socket.

## What MosaicShell does now

Edge and Chrome publish a page's Media Session to Windows' own media session (SMTC), and that carries the title,
artist and cover, for tabs and for installed web apps alike, with nothing installed. SMTC has no like state, and the
browser stops updating what it exposes about a window it cannot see, so two browser sources sit beside it.

1. **SMTC** (`WindowsMediaSessionService`) supplies title, artist, cover, timeline and transport.
2. **`CompositeMediaSessionService`** merges SMTC with the browser sources, in order of preference.
3. **`BrowserMediaSource`**, fed by the Grout browser extension (a separate project) over native messaging, a relay
   executable and a per-user pipe. The extension reads the page itself, in the page's own world, so it works whatever
   the window is doing (covered, minimised, another tab) and it supplies like and dislike, and the artist and cover
   when SMTC lacks them. The Host registers the native messaging host for the current user at every start
   (`NativeHostRegistration`), so the extension is the only thing to install. See
   `host/MosaicShell.Core/Services/BrowserBridge/`.
4. **`BrowserUiSource`**, the fallback when the extension is not connected, reads YouTube Music's like and dislike
   buttons through UI Automation. It needs nothing installed but only sees a window that is on screen (a covered or
   minimised window's tree is frozen, measured 2026-09-20; Chromium treats such a window as hidden), so it offers
   nothing rather than a state that may be stale. It is asked to read only while the extension has no player
   (`BrowserSourceStack.FallbackSession`), so no browser is made to build an accessibility tree needlessly.

### When the artist and cover are missing

A browser extension that replaces `navigator.mediaSession` (measured with KDE's Plasma Integration, whose page script
redefines `metadata` and `playbackState` without calling the browser's own) stops the browser receiving the page's
metadata, so Windows falls back to the page title with no artist and no cover. Grout reads the metadata in the page's
own world, where the shim's copy is what the page set, so it is not starved. Without Grout, turn such an extension off.

## Flags

- `tessera_media_browser` = true (browser like and dislike read by the Grout extension, or through UI Automation without it)
- `tessera_media_smtc_only` = false
