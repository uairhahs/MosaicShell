# Canvas (native-only)

Canvas (Plainext) runs as an Avalonia **widget** via `TileRuntime`.

| Path | Role |
|------|------|
| `host/MosaicShell.Host/Tiles/Surfaces/LiveTilesA.cs` (`CanvasTileView`) | CPU / RAM / disk / host text |
| `host/MosaicShell.Core/Settings/ModuleSettings.cs` (`CanvasSettings`) | Section toggles + Compact/DEFAULT |
| This folder | Install stub for `install-module Canvas` |

## Install / start

```powershell
cd host
dotnet run --project Mosaicist -- install-module Canvas
dotnet run --project MosaicShell.Host
```

In Host: Library → Canvas → Start.

Promised Rainmeter-era behavior: [`docs/legacy/canvas.md`](../../docs/legacy/canvas.md).
