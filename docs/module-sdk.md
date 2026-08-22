# Module SDK (third-party tiles)

MosaicShell discovers modules from `%LocalAppData%\MosaicShell\Modules\{Id}\` (override via app paths). First-party tiles still ship as repo stubs under `Tiles/{Id}/`; third-party modules install as a **package** (folder or zip).

## Folder layout

```text
Modules/MyTile/
  module.manifest.json   # required
  module.dll             # optional — ICapabilityFactory and/or ITileViewFactory
  capability.dll         # optional legacy alias for capability factory
  tile.dll               # optional legacy alias for tile view factory
  …assets…
```

## Manifest (`module.manifest.json`)

```json
{
  "Id": "HelloTile",
  "Version": "1.0.0",
  "DisplayName": "Hello Tile",
  "Description": "Sample external widget.",
  "UsageSummary": "Install, then open from Tiles.",
  "HowToTrigger": "Use Try overlay from module config.",
  "Kind": "Widget",
  "Styles": [ "DEFAULT" ],
  "DefaultStyle": "DEFAULT",
  "DefaultArmed": false
}
```

`Kind` is `Widget`, `Capability`, or `Hybrid`. Installed manifests appear in Hub/Library without editing Host source.

## Contracts

| Concern | Contract | Assembly |
|---------|----------|----------|
| Overlay / flyout / config | `IHostUiBridge`, `IFlyoutPresenter`, `FlyoutRequest` | `MosaicShell.Core` |
| Armed background logic | `IModuleCapability`, `ICapabilityFactory` | `MosaicShell.Core` |
| Tile chrome UI | `ITileViewFactory` → Avalonia `Control` | `MosaicShell.Host` |

Built-in factories always win on id collision. External DLLs load best-effort when arming (`capability`/`module.dll`) or when showing an overlay (`module`/`tile.dll`).

`IHostUiBridge.PreviewFlyout(FlyoutRequest)` is module-agnostic — build the request in your module (see Tessera’s `TesseraFlyoutRequestBuilder` under `MosaicShell.Core.Modules.Tessera` as a first-party example).

## Install

```bash
# First-party stub from the repo
Mosaicist install-module Canvas

# Third-party package (folder or .zip with module.manifest.json)
Mosaicist install-package .\samples\ExternalSampleModule
Mosaicist install-package .\HelloTile.zip
```

## Sample

See [`samples/ExternalSampleModule`](../samples/ExternalSampleModule) for a manifest-only widget. After `install-package`, it appears in Library; without a `module.dll` / `tile.dll` the Host shows `GenericTileView` placeholder chrome until you ship a view factory.

## Scale / DPI

Do **not** multiply OS DPI into layout. Avalonia uses per-monitor DIPs ([docs](https://docs.avaloniaui.net/docs/platform-specific-guides/windows#high-dpi-and-per-monitor-scaling)). User zoom is `UserScale` only (`ScaleContract`).

## Flyout transparency

Prefer Avalonia `TransparencyLevelHint` + `Background=Transparent` + `TransparencyBackgroundFallback`. GDI BitBlt glass is Tessera-only and opt-in (`TesseraGlass.AllowGdiScreenCapture`).
