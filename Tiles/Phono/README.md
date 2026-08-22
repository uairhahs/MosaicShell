# Phono (native-only)

Phono runs as an Avalonia **widget** via `TileRuntime`.

| Path | Role |
|------|------|
| `host/MosaicShell.Host/Tiles/Surfaces/LiveTilesA.cs` (`PhonoTileView`) | SMTC media + transport |
| `host/MosaicShell.Core/Settings/ModuleSettings.cs` (`PhonoSettings`) | Style / show artist |
| This folder | Install stub for `install-module Phono` |

Media is **SMTC only** on the Host path (WebNowPlaying covers are Tessera-side).

## Install / start

```powershell
cd host
dotnet run --project Mosaicist -- install-module Phono
dotnet run --project MosaicShell.Host
```

In Host: Library → Phono → Start.

Promised Rainmeter-era behavior: [`docs/legacy/phono.md`](../../docs/legacy/phono.md).
