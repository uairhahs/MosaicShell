# Album art: JaxCore vs native Tessera

## How JaxCore got YTM covers

YourFlyouts **Auto** mode used **WebNowPlaying**: a browser extension reads
`navigator.mediaSession.metadata.artwork` on the page (YouTube Music site script),
downloads the image, and sends PNG bytes to an adapter over WebSocket.

That is **not** Windows SMTC `Thumbnail`. The YT Music PWA often leaves SMTC
`Thumbnail` null while still exposing Media Session artwork in-page - which is why
JaxCore showed art and SMTC-only hosts did not.

## What MosaicShell does now

1. **SMTC** (`WindowsMediaSessionService`) - title/timeline/transport for the OS session.
2. **WebNowPlaying Redux host** (`WebNowPlayingReduxHost`, WNPLIB revision **3**) -
   listens on `ws://127.0.0.1:5468/` - the built-in **CLI** adapter port
   ([WebNowPlaying-CLI](https://github.com/keifufu/WebNowPlaying-CLI)), so Rainmeter
   can keep **8974** without a port fight.
3. **`CompositeMediaSessionService`** - overlays WNP cover (and browser title/artist)
   when SMTC has no thumbnail.

### One-time setup

1. Install the [WebNowPlaying](https://chromewebstore.google.com/detail/webnowplaying/jfakgfcdgpghbbefmdfjkbdlibjgnbli) extension (Chrome/Edge).
2. Enable the built-in **CLI** adapter (port **5468**).
3. Keep MosaicShell Host running.
4. Play YouTube Music in that browser - flyout album art should populate.

**Do not** also run `wnpcli start-daemon` while Host is listening on 5468 (same port).
Rainmeter’s WNP plugin on **8974** is fine alongside MosaicShell.

## Flags

- `tessera_media_wnp` = true (adapter wired)
- `tessera_media_smtc_only` = false
