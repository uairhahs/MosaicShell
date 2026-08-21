# Tessera (native-only)

Tessera runs as an **Avalonia capability** inside `MosaicShell.Host` - not as a Rainmeter skin.

| Path | Role |
|------|------|
| `host/MosaicShell.Core/Capabilities/BuiltIn/TesseraCapability.cs` | Arm / events / OSD burst |
| `host/MosaicShell.Host/Tiles/Tessera/` | Flyout layouts (Fluent, Win11, …) |
| This folder | Install stub so `install-module Tessera` creates `Modules/Tessera` for `CapabilityDaemon` |

## Browser album art (YouTube Music)

Tessera merges **WebNowPlaying** covers when SMTC has no thumbnail.

1. Install the [WebNowPlaying](https://chromewebstore.google.com/detail/webnowplaying/jfakgfcdgpghbbefmdfjkbdlibjgnbli) browser extension.
2. Enable the built-in **CLI** adapter (port **5468** - same as [WebNowPlaying-CLI](https://github.com/keifufu/WebNowPlaying-CLI); Rainmeter stays on 8974).
3. Run MosaicShell Host, play YTM in that browser.

Details: [`docs/parity/smtc-album-art.md`](../../docs/parity/smtc-album-art.md).

## References

- Visual layouts: [Jax-Core/YourFlyouts](https://github.com/Jax-Core/YourFlyouts)
- OEM / volume OSD hide: [ModernFlyouts-Community/ModernFlyouts](https://github.com/ModernFlyouts-Community/ModernFlyouts) (`NativeFlyoutHandler`)

## Legacy Rainmeter note

The Rainmeter Tessera tree (Main.ini, `Plugin=Tessera`, Lua layouts) was removed on the Avalonia migration branch.

**Mixdeck** and **Inlay** Rainmeter skins still reference `Plugin=Tessera` for volume hooks. Those hooks are **Disabled=1** on this branch. Prefer the native Host for volume/brightness flyouts until Mixdeck/Inlay Rainmeter trees are superseded (see `docs/native-rewrite.md` Phase B).

**Widgets** (Chrono / Phono / Pulse / Canvas) follow the same native-stub pattern as Tessera (wave **B2**).

Use **MosaicShell.Host** (not CoreShell) to configure and arm Tessera.
