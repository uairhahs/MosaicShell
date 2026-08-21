# Chrono (native-only)

Chrono runs as an Avalonia **widget** via `TileRuntime` — not as a Rainmeter skin.

| Path | Role |
|------|------|
| `host/MosaicShell.Host/Tiles/Surfaces/LiveTilesA.cs` (`ChronoTileView`) | Clock + date overlay |
| `host/MosaicShell.Core/Settings/ModuleSettings.cs` (`ChronoSettings`) | Style / 24h / seconds |
| This folder | Install stub so `install-module Chrono` creates `Modules/Chrono` |

Style ids live in `StyleCatalog` (JaxCore names). Chrome variants are MVP approximations, not full Rainmeter skins.

## Legacy

The Rainmeter Chrono tree was removed on the Avalonia migration branch (supersession wave **B2**). Use **MosaicShell.Host** Library → Start.
