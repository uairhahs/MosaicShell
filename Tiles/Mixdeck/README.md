# Mixdeck (native-only)

Mixdeck runs as an **Avalonia capability** inside `MosaicShell.Host`.

| Path                                                                     | Role                              |
| ------------------------------------------------------------------------ | --------------------------------- |
| `host/MosaicShell.Core/Capabilities/BuiltIn/`                            | Arm / hotkey (default Ctrl+Alt+M) |
| `host/MosaicShell.Host/Tiles/Surfaces/LiveTilesB.cs` (`MixdeckTileView`) | Per-app mixer overlay             |

## Install / arm

```powershell
cd host
dotnet run --project Mosaicist -- install-module Mixdeck
dotnet run --project MosaicShell.Host
```

Host: Library → Mixdeck → Arm. Also reachable via the Tessera Material You deep-link.

Promised Rainmeter-era behavior: [`docs/legacy/mixdeck.md`](../../docs/legacy/mixdeck.md).
