# Native architecture

- **Host process** = tray CapabilityDaemon (`ShutdownMode.OnExplicitShutdown`)
- **Widgets** = TileRuntime overlays
- **Capabilities** (Tessera, Mixdeck, …) = `IModuleCapability` armed in-process
- **Settings** = JSON via `ModuleSettingsStore`
- **Styles** = `StyleCatalog` JaxCore ids → Avalonia factories

## Tessera install vs runtime

| Piece | Location |
|-------|----------|
| Runtime | `TesseraCapability` + `host/MosaicShell.Host/Tiles/Tessera/` |
| Install stub | `Tiles/Tessera/` (`module.native.json` + README) → copied to `%…/Modules/Tessera` |
| Arm gate | `ModuleCatalog.IsInstalled` = directory exists under `Modules/` |

`ModuleInstaller` treats `module.native.json` / `native.marker` as a valid module root (no Rainmeter `Main.ini` required).

## Widget install stubs (B2)

Chrono / Phono / Pulse / Canvas use the same stub pattern under `Tiles/{Id}/`. Runtime is `TileSurfaceFactory` → `LiveTilesA` via `TileRuntime`.

Tessera flow: OS events / ShellHook → `TesseraCapability` → OSD suppress → `AvaloniaFlyoutPresenter` → layout control.
ShellHook: `WindowsShellFlyoutHook` (ModernFlyouts-compatible SHELLHOOK decode) alongside audio/brightness change sources.
