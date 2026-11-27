# Module SDK (third-party tiles)

MosaicShell discovers modules from `%LocalAppData%\MosaicShell\Modules\{Id}\` (set `MOSAICSHELL_HOME` to move the whole data folder). First-party tiles still ship as repo stubs under `Tiles/{Id}/`; third-party modules install as a **package** (folder or zip).

## Folder layout

```text
Modules/MyTile/
  module.manifest.json   # required
  module.dll             # optional, ICapabilityFactory and/or ITileViewFactory
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
  "Styles": ["DEFAULT"],
  "DefaultStyle": "DEFAULT",
  "DefaultArmed": false
}
```

`Kind` is `Widget`, `Capability`, or `Hybrid`. An installed module appears in the Hub without editing Host source: a
`Widget` on the Widgets page, a `Capability` or `Hybrid` on the Modules page, marked Third-party.

## Settings page (`Settings` in the manifest)

A module can describe its settings page in the manifest. The Hub draws it with the same page the built-in modules
use: an on switch at the top, then your sections, then Remove. Every change is saved as it is made.

```json
"Settings": [
  { "Section": "Look", "Rows": [
    { "Kind": "Style" },
    { "Key": "Theme", "Kind": "Choice", "Label": "Theme", "Default": "dark",
      "Options": [ { "Value": "dark", "Label": "Dark" }, { "Value": "light", "Label": "Light" } ] }
  ] },
  { "Section": "Behaviour", "Rows": [
    { "Key": "Sound", "Kind": "Toggle", "Label": "Play a sound", "Description": "When it opens.", "Default": true },
    { "Key": "Volume", "Kind": "Number", "Label": "Sound volume", "Min": 0, "Max": 1, "Step": 0.1, "Default": 0.5,
      "ShownWhen": "Sound" },
    { "Key": "Shortcut", "Kind": "Hotkey", "Label": "Shortcut", "Default": "Ctrl+Alt+H" },
    { "Key": "Sites", "Kind": "TextList", "Label": "Sites", "Default": [ "example.org" ] },
    { "Key": "Links", "Kind": "ActionList", "Label": "Links" }
  ] }
]
```

| `Kind`       | Control                       | Stored as                                   | Needs                                     |
| ------------ | ----------------------------- | ------------------------------------------- | ----------------------------------------- |
| `Toggle`     | Switch                        | `true` or `false`                           |                                           |
| `Choice`     | Drop-down of `Options`        | The option's `Value`                        | `Options`                                 |
| `Number`     | Slider                        | A number, clamped to `Min` to `Max`         | `Min` and `Max` (`Step`, `Unit` optional) |
| `Hotkey`     | Shortcut with a Change button | A gesture such as `Ctrl+Alt+H`              |                                           |
| `Style`      | Drop-down of your `Styles`    | The style id, under the key `Style`         | `Styles` in the manifest                  |
| `TextList`   | Search box and list           | An array of strings                         |                                           |
| `ActionList` | Name, search box and list     | An array of `{ "Name": ..., "Value": ... }` |                                           |

- **Where the values go.** `Config/modules/{Id}.json` under the data folder, as plain JSON keys: read them with any
  JSON reader, or `ModuleSettingsStore.Load<Dictionary<string, JsonElement>>("{Id}", ...)`. Keys the page does not
  show are kept, so the module can store its own there too.
- **Defaults.** `Default` is the value before anything is saved. Without one: off, the first option, `Min`, empty.
- **When a change takes effect.** `Apply` is `Live` (read the next time it opens), `ReArm`, `RelaunchWidget` or
  `RestartHost`. Without one, a widget is relaunched and a capability is armed again; a `Hotkey` always re-arms.
- **`ShownWhen`** names a `Toggle` on the same page: the row shows only while that toggle is on.
- **Shortcuts on desktops without them.** A `Hotkey` row is greyed out where the desktop gives MosaicShell no
  global shortcuts (GNOME today), with a line saying so.
- **What is skipped.** A row with an unknown `Kind`, no `Key`, a `Key` already used (or `Style` on a row that is not
  the style), a `Choice` without `Options` or a `Number` without a range is left out rather than failing the page.
  A manifest whose rows are all left out gets the plain page (what it does, style, Save).
- **Ids.** A built-in module id keeps the built-in page whatever the manifest says.

## Contracts

| Concern                                  | Contract                                                                | Assembly           |
| ---------------------------------------- | ----------------------------------------------------------------------- | ------------------ |
| Platform (flyout routing, media signals) | `ICapabilityContext`, `CapabilityFlyoutSession`, `MediaSessionPlatform` | `MosaicShell.Core` |
| Overlay / flyout / config                | `IHostUiBridge`, `IFlyoutPresenter`, `FlyoutRequest`                    | `MosaicShell.Core` |
| Armed background logic                   | `IModuleCapability`, `ICapabilityFactory`                               | `MosaicShell.Core` |
| Tile chrome UI                           | `ITileViewFactory`, returning an Avalonia `Control`                     | `MosaicShell.Host` |

Full platform guide: [`capability-platform.md`](capability-platform.md).

### Capability factory signature

```csharp
IModuleCapability Create(ModuleManifest manifest, ICapabilityContext context);
```

Use `context.Flyouts.Route(...)` for flyouts; do not call `IFlyoutPresenter` directly for routed sessions. Use `context.Media.Acquire()` / `Signal` for media instead of duplicating SMTC polls.

Built-in factories always win on id collision. External DLLs load best-effort: arming looks for `module.dll`, then `capability.dll`; showing an overlay looks for `module.dll`, then `tile.dll`.

`IHostUiBridge.PreviewFlyout(FlyoutRequest)` is module-agnostic; build the request in your module (see `TesseraFlyoutRequestBuilder` as a first-party example).

## Install

A package is a folder, or a `.zip` of one, with `module.manifest.json` at its root or one folder down. Installing
copies it to `Modules/{Id}` under the data folder, replacing an earlier install of the same `Id`. A package cannot use
the `Id` of a module MosaicShell ships (Tessera, Mixdeck and the rest); give it its own.

- **From the Hub.** On the Modules or Widgets page, choose **Add from file…**, then a `.zip` package or a folder. The
  module's page opens when it is installed, wherever its `Kind` lists it.
- **From the command line**, for scripts and CI. The Host installs the package and exits without opening the Hub:

  ```bash
  MosaicShell.Host --install-package ./samples/ExternalSampleModule
  MosaicShell.Host --install-package ./HelloTile.zip
  ```

  It prints `Installed HelloTile. It is on the Widgets page.` and exits 0, or prints why it could not and exits 1
  (2 when no path follows the flag). A Hub that is already open lists the module the next time its page is shown.

- **By hand.** Copy the folder to `Modules/{Id}` under the data folder (`%LocalAppData%\MosaicShell` on Windows,
  `~/.local/share/MosaicShell` on Linux, or `MOSAICSHELL_HOME`). It shows when the Hub next lists modules.

To try a module without touching your own setup, point `MOSAICSHELL_HOME` at an empty folder for that run.

## Checking a manifest

If any `Settings` rows are left out of the module's page, the page says which and why under **For the module's
developer**, in the manifest's own terms (for example `Dial: "Knob" is not a row kind.`). A manifest with no such
notes is drawn in full.

## Sample

See [`samples/ExternalSampleModule`](../samples/ExternalSampleModule) for a manifest-only widget. After installing it
appears on the Widgets page; without a `module.dll` / `tile.dll` the Host shows `GenericTileView` placeholder chrome
until you ship a view factory.

## Scale / DPI

Do **not** multiply OS DPI into layout. Avalonia uses per-monitor DIPs ([docs](https://docs.avaloniaui.net/docs/platform-specific-guides/windows#high-dpi-and-per-monitor-scaling)). User zoom is `UserScale` only (`ScaleContract`).

## Flyout transparency

Prefer Avalonia `TransparencyLevelHint` + `Background=Transparent` + `TransparencyBackgroundFallback`. GDI BitBlt glass is Tessera-only and opt-in (`TesseraGlass.AllowGdiScreenCapture`).
