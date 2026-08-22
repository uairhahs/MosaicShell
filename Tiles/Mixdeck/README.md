# Mixdeck (native-only)

Mixdeck runs as an **Avalonia capability** inside `MosaicShell.Host` - not as a Rainmeter skin.

| Path | Role |
|------|------|
| `host/MosaicShell.Core/Capabilities/BuiltIn/` | Arm / hotkey (default Ctrl+Alt+M) |
| `host/MosaicShell.Host/Tiles/Surfaces/LiveTilesB.cs` (`MixdeckTileView`) | Per-app mixer overlay |
| This folder | Install stub so `install-module Mixdeck` creates `Modules/Mixdeck` |

## Install / arm

```powershell
cd host
dotnet run --project Mosaicist -- install-module Mixdeck
dotnet run --project MosaicShell.Host
```

In Host: Library → Mixdeck → Arm (or Tessera Pixel deep-link).

Promised Rainmeter-era behavior: [`docs/legacy/mixdeck.md`](../../docs/legacy/mixdeck.md).
