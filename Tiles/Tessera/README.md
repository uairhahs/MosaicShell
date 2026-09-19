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

Title, artist and cover come from Windows' media session, which Edge and Chrome feed from the page, so nothing needs
installing. Like and dislike for YouTube Music are read from the player's buttons through Windows accessibility.
If the flyout shows only a title, a browser extension that replaces the page's Media Session (for example KDE Plasma
Integration) is probably turned on; turn it off.

Details: [`docs/parity/smtc-album-art.md`](../../docs/parity/smtc-album-art.md).

## References

- Visual layouts: [Jax-Core/YourFlyouts](https://github.com/Jax-Core/YourFlyouts)
- OEM / volume OSD hide: [ModernFlyouts-Community/ModernFlyouts](https://github.com/ModernFlyouts-Community/ModernFlyouts) (`NativeFlyoutHandler`)

Promised Rainmeter-era behavior: [`docs/legacy/tessera.md`](../../docs/legacy/tessera.md).
