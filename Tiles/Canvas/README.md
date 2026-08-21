# Canvas (native-only)

Canvas (Plainext) runs as an Avalonia **widget** via `TileRuntime` — not as a Rainmeter skin.

| Path | Role |
|------|------|
| `host/MosaicShell.Host/Tiles/Surfaces/LiveTilesA.cs` (`CanvasTileView`) | CPU / RAM / disk / host text |
| `host/MosaicShell.Core/Settings/ModuleSettings.cs` (`CanvasSettings`) | Section toggles + Compact/DEFAULT |
| This folder | Install stub for `install-module Canvas` |

## Legacy

The Rainmeter Plainext/Canvas tree was removed on the Avalonia migration branch (supersession wave **B2**). Use **MosaicShell.Host** Library → Start.
