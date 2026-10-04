# Tessera (native-only)

Tessera runs as an **Avalonia capability** inside `MosaicShell.Host`.

| Path                                                              | Role                                         |
| ----------------------------------------------------------------- | -------------------------------------------- |
| `host/MosaicShell.Core/Capabilities/BuiltIn/TesseraCapability.cs` | Arm / events / OSD burst                     |
| `host/MosaicShell.Host/Tiles/Tessera/`                            | Flyout layouts (Fluent, Windows11, and more) |

## Install / arm

```powershell
cd host
dotnet run --project Mosaicist -- install-module Tessera
dotnet run --project MosaicShell.Host
```

Host: Library → Tessera → Arm. Triggers on the system volume, brightness, and media keys.

## Browser media (YouTube Music)

For browser tabs and installed web apps (PWAs), install the **Grout** browser extension. Grout reads the title,
artist, cover and like state from the player page itself, so the flyout stays accurate whether the window is covered,
minimised or in another tab. If the flyout shows a missing or wrong title, artist or cover for a web player, install
Grout; that is the supported fix.

Without Grout, title, artist and cover come from Windows' media session, which Edge and Chrome feed from the page, and
like and dislike for YouTube Music are read from the player's buttons through Windows accessibility. Both are less
reliable for web apps.

Details: [`docs/parity/smtc-album-art.md`](../../docs/parity/smtc-album-art.md).

## References

- Visual layouts: [Jax-Core/YourFlyouts](https://github.com/Jax-Core/YourFlyouts)
- OEM / volume OSD hide: [ModernFlyouts-Community/ModernFlyouts](https://github.com/ModernFlyouts-Community/ModernFlyouts) (`NativeFlyoutHandler`)

Promised Rainmeter-era behavior: [`docs/legacy/tessera.md`](../../docs/legacy/tessera.md).
