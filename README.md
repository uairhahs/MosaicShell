# MosaicShell

> _Your desktop, composed._

<p align="center">
  <img src=".github/res/logo-variants/compact-256.png" alt="MosaicShell" width="120" height="120" />
</p>

<p align="center">
  <img alt="Version" src="https://img.shields.io/github/v/tag/uairhahs/MosaicShell?label=Version&style=for-the-badge" />
  <img alt="Downloads" src="https://img.shields.io/github/downloads/uairhahs/MosaicShell/total?style=for-the-badge" />
  <img alt="Last Update" src="https://img.shields.io/github/release-date/uairhahs/MosaicShell?label=Last%20Update&style=for-the-badge" />
  <img alt="License" src="https://img.shields.io/github/license/uairhahs/MosaicShell?style=for-the-badge" />
</p>

---

## About

MosaicShell is a native Windows desktop shell built from composable tiles. The Avalonia **Host** installs modules, arms tray capabilities, and opens widget overlays, with no Rainmeter and no `.rmskin`.

It continues the JaxCore idea of a modular desktop (forked from [Jax-Core/JaxCore](https://github.com/Jax-Core/JaxCore), archived November 2024). Historical Rainmeter sources live in the [Jax-Core archives](https://github.com/Jax-Core).

---

## Prerequisites

| Requirement | Minimum                                                                                                                           |
| ----------- | --------------------------------------------------------------------------------------------------------------------------------- |
| OS          | Windows 10 x64 or later                                                                                                           |
| .NET        | Not required for the release Setup (self-contained Host). Desktop Runtime 10.0 only if you build framework-dependent from source. |
| RAM         | 6 GB                                                                                                                              |

## Install

### Setup.exe (recommended)

1. Download `MosaicShell-Setup-*.exe` from [Releases](https://github.com/uairhahs/MosaicShell/releases).
2. Run the installer (per-user under `%LocalAppData%\Programs\MosaicShell`).
3. Launch MosaicShell from the Start Menu. Default modules (Tessera, Mixdeck) are offered during setup.

In-app **Check Updates** downloads the latest Setup and runs a silent upgrade.

Release tags use date-build (`yyyy.M.d-bN`, e.g. `2026.8.23-b1`), not semver.

### Portable zip (advanced)

Download `MosaicShell-Portable-*.zip`, extract, and run `Host\MosaicShell.Host.exe`. Use `Mosaicist\` next to Host to install modules from the bundled `Tiles\` folder.

### From source (developers)

```powershell
cd host
dotnet test MosaicShell.Core.Tests
dotnet run --project Mosaicist -- install-module Tessera
dotnet run --project Mosaicist -- install-module Mixdeck
dotnet run --project MosaicShell.Host
```

Local Setup builds: [packaging/README.md](packaging/README.md).

See [host/README.md](host/README.md) and [`.github/docs/parity.md`](.github/docs/parity.md).

**Honest MVP vs fidelity:** `tile_*_mvp` flags mean wiring, settings, and flagship behavior slices are in place, not full Jax-Core visual parity. Tessera layout is signed off (`tessera_layout_fidelity`; proofs in [`.github/res/Tessera/`](.github/res/Tessera/)). Other `*_layout_fidelity` flags stay false until in-repo screenshot proofs exist (see [`.github/docs/parity.md`](.github/docs/parity.md)). Install modules from the bundled `Tiles/{Id}/` stub via `Mosaicist install-module <id>` or the Setup post-install step.

---

## Known issues

### YouTube Music: no artist or album art (temporary workaround)

**Symptom.** In the YouTube Music web app (the installed app window or a browser tab), the flyout shows the
track title but no artist and no album art, on Edge or Chrome.

**Cause.** Windows only receives the page title from the browser for this app, so MosaicShell relies on the
[WebNowPlaying](https://wnp.keifufu.dev/) browser extension for artist and cover. YouTube Music replaced
`playerApi` with an asynchronous `resolvePlayerApi()`, which breaks the extension's YouTube Music adapter
(version 3.1.0). Upstream tracks it as
[keifufu/WebNowPlaying#59](https://github.com/keifufu/WebNowPlaying/issues/59). **Once that is fixed and the
store extension has updated, remove this workaround and re-enable the store extension.**

**Workaround.** Run a local copy of the extension with the adapter fixed. This edits a third-party extension
on your own machine; it is not shipped with MosaicShell. It was tested only against WebNowPlaying 3.1.0 on
Edge 153. If a find string below does not match exactly once, your extension version differs, so do not
apply it. The changes are limited to the adapter fix on purpose, so that going back to the store extension
later changes nothing else.

1. Copy the installed extension folder to a permanent location outside the browser profile. On Edge it is
   `%LOCALAPPDATA%\Microsoft\Edge\User Data\Default\Extensions\jfakgfcdgpghbbefmdfjkbdlibjgnbli\3.1.0_0` (on Chrome, the same
   path under `Google\Chrome`).
2. In the copy, delete the `_metadata` folder. In `manifest.json`, remove the `"key"` and `"update_url"`
   entries, set `"name"` to `WebNowPlaying (YTM patch)` and `"version"` to `3.1.0.1`. This gives the copy its
   own identity so it does not collide with the store extension.
3. In `injected.js`, make two replacements (each find string occurs exactly once):

   <details>
   <summary>Replacement 1: resolve the player API asynchronously and cache it</summary>

   Find:

   ```js
   Be=()=>document.querySelector("ytmusic-player-bar")?.playerApi,
   ```

   Replace with:

   ```js
   Be=(()=>{let b=null,a=null,p=!1;return()=>{const e=document.querySelector("ytmusic-player-bar");if(!e){b=a=null;p=!1;return}if(e.playerApi)return e.playerApi;if(e!==b){b=e;a=null;p=!1}if(!a&&!p&&"function"==typeof e.resolvePlayerApi){p=!0;try{Promise.resolve(e.resolvePlayerApi()).then((t=>{if(b===e&&t){a=t;console.info("[WNP-YTM-patch] playerApi resolved:",["isReady","getPlayerState","getCurrentTime","getVolume"].map((k=>k+"="+typeof t[k])).join(" "))}}),(t=>{console.info("[WNP-YTM-patch] resolvePlayerApi failed:",t)})).finally((()=>{p=!1}))}catch(t){p=!1}}return a||void 0}})(),
   ```

   </details>

   <details>
   <summary>Replacement 2: tolerate a resolved object without <code>isReady</code></summary>

   Find:

   ```js
   ready:()=>Be()?.isReady(),info:m({name:()=>"YouTube Music"
   ```

   Replace with:

   ```js
   ready:()=>{const a=Be();return a?"function"==typeof a.isReady?a.isReady():!0:void 0},info:m({name:()=>"YouTube Music"
   ```

   </details>

4. In `sw.js`, replace `enabledBuiltInAdapters:["Rainmeter Adapter"]` with `enabledBuiltInAdapters:["CLI Adapter"]`. A copy with a new
   identity starts with default settings, and this makes it connect to MosaicShell (CLI adapter, port 5468).
   You can instead enable **CLI Adapter** in the extension's popup.
5. Open `edge://extensions`, turn on Developer mode, choose **Load unpacked**, and select the folder. Turn
   off the store **WebNowPlaying** so only one copy runs.
6. Close and re-open the YouTube Music window or tab. A page that was already open when the extension was
   enabled has no content script and shows nothing until it is re-opened. After editing the copy later,
   choose **Reload** on the extension and re-open the page again.

Artist and cover should now appear in the flyout. To check the extension itself, open the page's DevTools
console (Ctrl+Shift+I) and filter for `WNP-YTM-patch`: one line reports which player methods resolved.

---

## Tiles

Every catalog module ships as a thin `Tiles/{Id}` install stub (`module.native.json` + README). Runtime code lives under `host/`.

| Tile      | Description                                            | License |
| --------- | ------------------------------------------------------ | ------- |
| Tessera   | Volume / brightness / media flyouts (armed capability) | MPL-2.0 |
| Mixdeck   | Per-app audio mixer overlay                            | MPL-2.0 |
| Inlay     | Start-menu launcher (pins + search)                    | MPL-2.0 |
| Slate     | Idle clock overlay                                     | MPL-2.0 |
| Chord     | Macro app launcher                                     | MPL-2.0 |
| Substrate | Quick-settings shade                                   | MPL-2.0 |
| Pulse     | Audio visualizer widget                                | MIT     |
| Chrono    | Clock widget                                           | MIT     |
| Phono     | SMTC media widget                                      | MIT     |
| Canvas    | System-metrics text widget                             | MIT     |

---

## Credits

### Original project

MosaicShell is a fork of [JaxCore](https://github.com/Jax-Core/JaxCore) by [@EnhancedJax](https://github.com/EnhancedJax), archived November 2024. Historical Rainmeter plugin credits: [Jax-Core](https://github.com/Jax-Core).

---

## Contributing

Issues and pull requests are welcome. If you are building a module or widget compatible with MosaicShell, open an issue to discuss integration.

**Development:** [`.github/docs/`](.github/docs/) (testing, parity honesty, scaling), [`docs/architecture.md`](docs/architecture.md) (how the repo is put together), [`docs/parity/README.md`](docs/parity/README.md) (per-module MVP bars), [`docs/legacy/`](docs/legacy/) (what the Rainmeter-era modules promised).

---

## License

See [LICENSE](./LICENSE).
