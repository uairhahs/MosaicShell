# Native rewrite decision

MosaicShell’s Avalonia host **does not** run Rainmeter skins and **does not** depend on Rainmeter.exe.

- **Tiles** are rewritten as Avalonia surfaces under `host/MosaicShell.Host/Tiles/`.
- **Capabilities** (audio, media, hotkeys, metrics, …) live in `host/MosaicShell.Core/Services/`.
- **Module packages** under `%LocalAppData%\MosaicShell\Modules\<Id>` supply assets and defaults (`module.manifest.json`), not executable Rainmeter entrypoints.
- **Settings** persist as JSON under `%LocalAppData%\MosaicShell\Config/`.

Rainmeter trees in `Tiles/` remain the behavioral reference for parity, not the runtime.
