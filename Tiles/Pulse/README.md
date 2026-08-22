# Pulse (native-only)

Pulse runs as an Avalonia **widget** via `TileRuntime`.

| Path | Role |
|------|------|
| `host/MosaicShell.Host/Tiles/Surfaces/LiveTilesA.cs` (`PulseTileView`) | Visualizer from `IAudioLevelService` |
| `host/MosaicShell.Core/Settings/ModuleSettings.cs` (`PulseSettings`) | Style / Bar vs Round |
| This folder | Install stub for `install-module Pulse` |

Bands come from WASAPI loopback levels - not RNG. StyleCatalog ids are chrome labels.

## Install / start

```powershell
cd host
dotnet run --project Mosaicist -- install-module Pulse
dotnet run --project MosaicShell.Host
```

In Host: Library → Pulse → Start.

Promised Rainmeter-era behavior: [`docs/legacy/pulse.md`](../../docs/legacy/pulse.md).
