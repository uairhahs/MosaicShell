# Native architecture

- **Host process** = tray CapabilityDaemon (`ShutdownMode.OnExplicitShutdown`)
- **Widgets** = TileRuntime overlays (`ITileViewFactory` / `TileViewRegistry`)
- **Capabilities** (Tessera, Mixdeck, …) = `IModuleCapability` armed in-process
- **Settings** = JSON via `ModuleSettingsStore`
- **Styles** = `StyleCatalog` + per-module manifest `Styles`
- **Third-party modules** = see [`module-sdk.md`](module-sdk.md) (manifest discovery + package install)

## Tessera install vs runtime

| Piece | Location |
|-------|----------|
| Runtime | `TesseraCapability` + `host/MosaicShell.Host/Tiles/Tessera/` + `MosaicShell.Core.Modules.Tessera` |
| Install stub | `Tiles/Tessera/` (`module.native.json` + README) → copied to `%…/Modules/Tessera` |
| Arm gate | `ModuleCatalog.IsInstalled` = directory exists under `Modules/` |

`ModuleInstaller` copies native stubs from `Tiles/{Id}/` or installs a folder/zip package (`InstallFromPackageAsync`). Mosaicist: `install-module` / `install-package`.

## Install stubs

First-party modules use `Tiles/{Id}/` stubs. Widget runtime is `TileViewRegistry` → tile views via `TileRuntime`; capabilities arm via `CapabilityDaemon`. Hub lists built-ins plus installed manifests.

Tessera flow: OS events / ShellHook → `TesseraCapability` → OSD suppress → `AvaloniaFlyoutPresenter` → layout control.
ShellHook: `WindowsShellFlyoutHook` (ModernFlyouts-compatible SHELLHOOK decode) alongside audio/brightness change sources.
