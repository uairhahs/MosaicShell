# Album art: JaxCore vs native Tessera

## How JaxCore got YTM covers

YourFlyouts used a Rainmeter media plugin fed by a browser extension. The extension read
`navigator.mediaSession.metadata.artwork` on the page, downloaded the image and sent it to Rainmeter over a local
socket.

## What MosaicShell does now

Nothing has to be installed. Edge and Chrome publish a page's Media Session to Windows' own media session (SMTC), and
that carries the title, artist and cover, for tabs and for installed web apps alike.

1. **SMTC** (`WindowsMediaSessionService`) supplies title, artist, cover, timeline and transport.
2. **`CompositeMediaSessionService`** merges SMTC with browser sources.
3. **`BrowserUiSource`** reads YouTube Music's like and dislike buttons through UI Automation, because SMTC has no
   like state. See `host/MosaicShell.Core/Services/BrowserUi/`.

### When the artist and cover are missing

A browser extension that replaces `navigator.mediaSession` (measured with KDE's Plasma Integration, whose page script
redefines `metadata` and `playbackState` without calling the browser's own) stops the browser receiving the page's
metadata, so Windows falls back to the page title with no artist and no cover. Turn such an extension off.

## Flags

- `tessera_media_browser` = true (browser like and dislike read through UI Automation)
- `tessera_media_smtc_only` = false
