# Phono (native-only)

Phono runs as an Avalonia **widget** via `TileRuntime` — not as a Rainmeter skin.

| Path | Role |
|------|------|
| `host/MosaicShell.Host/Tiles/Surfaces/LiveTilesA.cs` (`PhonoTileView`) | SMTC media + transport |
| `host/MosaicShell.Core/Settings/ModuleSettings.cs` (`PhonoSettings`) | Style / show artist |
| This folder | Install stub for `install-module Phono` |

Media is **SMTC only** (same cut as Tessera). WebNowPlaying / Rainmeter NowPlaying are not ported.

## Legacy

The Rainmeter Phono tree was removed on the Avalonia migration branch (supersession wave **B2**). Use **MosaicShell.Host** Library → Start.
