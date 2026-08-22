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

`ModuleInstaller` treats `module.native.json` / `native.marker` as a valid module root. Local `Tiles/{Id}` installs are always native stubs (B5 Host-only repo).

## Install stubs

All ten catalog modules use `Tiles/{Id}/` stubs. Widget runtime is `TileSurfaceFactory` → `LiveTilesA` via `TileRuntime`; capabilities arm via `CapabilityDaemon`.

Tessera flow: OS events / ShellHook → `TesseraCapability` → OSD suppress → `AvaloniaFlyoutPresenter` → layout control.
ShellHook: `WindowsShellFlyoutHook` (ModernFlyouts-compatible SHELLHOOK decode) alongside audio/brightness change sources.
