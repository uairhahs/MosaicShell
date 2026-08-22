# External sample module

Manifest-only widget package. Install without rebuilding Host:

```bash
dotnet run --project host/Mosaicist -- install-package samples/ExternalSampleModule
```

Then open Host → Tiles. **Hello Tile** should appear. Overlay chrome is `GenericTileView` until you add a `module.dll` exporting `ITileViewFactory` (see [docs/module-sdk.md](../docs/module-sdk.md)).
