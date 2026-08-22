# Chrono (native-only)

Chrono runs as an Avalonia **widget** via `TileRuntime`.

| Path | Role |
|------|------|
| `host/MosaicShell.Host/Tiles/Surfaces/LiveTilesA.cs` (`ChronoTileView`) | Clock + date overlay |
| `host/MosaicShell.Core/Settings/ModuleSettings.cs` (`ChronoSettings`) | Style / 24h / seconds |
| This folder | Install stub so `install-module Chrono` creates `Modules/Chrono` |

Style ids live in `StyleCatalog` (JaxCore names). Chrome variants are MVP approximations.

## Install / start

```powershell
cd host
dotnet run --project Mosaicist -- install-module Chrono
dotnet run --project MosaicShell.Host
```

In Host: Library → Chrono → Start.

Promised Rainmeter-era behavior: [`docs/legacy/chrono.md`](../../docs/legacy/chrono.md).
