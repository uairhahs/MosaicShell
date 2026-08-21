# Pulse (native-only)

Pulse runs as an Avalonia **widget** via `TileRuntime` - not as a Rainmeter skin.

| Path | Role |
|------|------|
| `host/MosaicShell.Host/Tiles/Surfaces/LiveTilesA.cs` (`PulseTileView`) | Visualizer from `IAudioLevelService` |
| `host/MosaicShell.Core/Settings/ModuleSettings.cs` (`PulseSettings`) | Style / Bar vs Round |
| This folder | Install stub for `install-module Pulse` |

Bands come from WASAPI loopback levels - not RNG. StyleCatalog ids are chrome labels; full JaxCore visualizer skins are later.

## Legacy

The Rainmeter Pulse tree was removed on the Avalonia migration branch (supersession wave **B2**). Use **MosaicShell.Host** Library → Start.
