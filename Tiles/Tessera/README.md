# Tessera (native-only)

Tessera runs as an **Avalonia capability** inside `MosaicShell.Host`.

| Path | Role |
|------|------|
| `host/MosaicShell.Core/Capabilities/BuiltIn/TesseraCapability.cs` | Arm / events / OSD burst |
| `host/MosaicShell.Host/Tiles/Tessera/` | Flyout layouts (Fluent, Win11, …) |
| This folder | Install stub so `install-module Tessera` creates `Modules/Tessera` for `CapabilityDaemon` |

## Install / arm

```powershell
cd host
dotnet run --project Mosaicist -- install-module Tessera
dotnet run --project MosaicShell.Host
```

In Host: Library → Tessera → Arm, then use system volume / brightness / media keys.

## Browser album art (YouTube Music)

Tessera merges **WebNowPlaying** covers when SMTC has no thumbnail.

1. Install the [WebNowPlaying](https://chromewebstore.google.com/detail/webnowplaying/jfakgfcdgpghbbefmdfjkbdlibjgnbli) browser extension.
2. Enable the built-in **CLI** adapter (port **5468** - same as [WebNowPlaying-CLI](https://github.com/keifufu/WebNowPlaying-CLI)).
3. Run MosaicShell Host, play YTM in that browser.

Details: [`docs/parity/smtc-album-art.md`](../../docs/parity/smtc-album-art.md).

## References

- Visual layouts: [Jax-Core/YourFlyouts](https://github.com/Jax-Core/YourFlyouts)
- OEM / volume OSD hide: [ModernFlyouts-Community/ModernFlyouts](https://github.com/ModernFlyouts-Community/ModernFlyouts) (`NativeFlyoutHandler`)

Promised Rainmeter-era behavior: [`docs/legacy/tessera.md`](../../docs/legacy/tessera.md).
