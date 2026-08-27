# Architecture

MosaicShell is a **tray host process** written in Avalonia + Windows APIs only: no Rainmeter bridge, no `.ini` interpreter. It runs armed capabilities (background HUD/OSD behaviors) and widget overlays (desktop tiles) in one process. It is not a separate Windows service today: everything runs in-process with shared platform services owned by `CapabilityDaemon`.

This page supersedes `docs/native-rewrite.md` and `docs/architecture-native.md`; both are now folded in here.

## Process layers

```text
MosaicShell.Host (tray, Avalonia UI thread)
  CapabilityDaemon          arm/disarm, persist, shared platform
    CapabilityFlyoutPlatform  Present/Patch/SoftRefresh, dismiss suppress
    MediaSessionPlatform      SMTC/WNP signal classification
  HostServices                OS adapters (audio, SMTC, shell hook, locks, and more)
  IModuleCapability            per-module: settings + build FlyoutRequest + hooks
  IFlyoutPresenter (Host)      Avalonia windows, coalesced patch queue
```

`CapabilityDaemon` is the lifecycle coordinator, not the whole host; do not conflate the two. The daemon owns platform services, and modules only ever see them through `ICapabilityContext`.

### Entry points

| Process                            | Role                                                                     |
| ---------------------------------- | ------------------------------------------------------------------------ |
| `MosaicShell.Host.exe`             | Full Hub + tray + flyout IPC server (default)                            |
| `MosaicShell.Host.exe --tray-only` | Tray + capabilities; Hub hidden until opened                             |
| `MosaicShell.Worker.exe`           | Headless daemon; flyouts via IPC to Host                                 |
| `Mosaicist.exe`                    | Install CLI (`install-module`, `install-package`): Core only, no Host UI |

`CapabilityStore` persists a **list** of armed module ids; one daemon runs Tessera, Mixdeck, Slate, and any other armed capabilities together, and that's the normal mode. Worker requires Host running as the IPC server, and the two must not both own the daemon at once: start Worker first (or exit Host) when splitting them.

## Dependency hierarchy

```text
Tiles/{Id}/                 install stubs only (module.native.json + README)
        |
        v
MosaicShell.Core            catalogs, *Spec / *Policy, services, capabilities, settings
        ^
        |  ProjectReference only this way
        |
MosaicShell.Core.Tests      asserts Core; Hub parity honesty gates
MosaicShell.Host             Avalonia hub, flyouts, tile surfaces (reads Core)
Mosaicist                    install CLI (Core, not Host UI)
```

**Hard rules:**

1. **Core never references Avalonia or Host.** Pure .NET + Windows service adapters, UI-agnostic.
2. **Host never invents a second source of truth** for alphas, sizes, hex, glass modes, glyph ids, or composition hints: those live in Core `*Spec` / `*Policy` / `*Catalog` / `HostPlatform/*Policy` types. A Host binder reads the contract; it does not restate the literal.
3. **`Tiles/{Id}/` is not the runtime.** Runtime lives under `host/`. The stub exists only so install / `ModuleCatalog.IsInstalled` works.

Every behavior change that affects chrome, policy, parity, or capability shape is expected to follow this repo's TDD loop: add/extend a Core contract, write a failing test in `MosaicShell.Core.Tests`, confirm red, implement the minimal Core change, then wire Host to read it.

## Module kinds

| Kind       | Runtime                                          | Example                 |
| ---------- | ------------------------------------------------ | ----------------------- |
| Capability | `IModuleCapability` armed via `CapabilityDaemon` | Tessera, Mixdeck, Slate |
| Widget     | `ITileViewFactory` + `TileRuntime`               | Chrono, Canvas          |
| Hybrid     | Both                                             | (future)                |

- **Widgets** = `TileRuntime` overlays (`ITileViewFactory` / `TileViewRegistry`), driven by `TileSurfaceFactory` -> `LiveTilesA`.
- **Capabilities** = `IModuleCapability` armed in-process; complex flyout/media routing lives in Core platform (`Capabilities/Platform/*`), not duplicated per module.
- **Settings** = JSON via `ModuleSettingsStore`.
- **Styles** = `StyleCatalog` (JaxCore-derived ids) mapped to per-module Avalonia layout factories.
- **Third-party modules**: the SDK contract must never require Host to special-case one tile. See `docs/module-sdk.md`.

### Install stubs and arming

All ten catalog modules (Tessera, Mixdeck, Chrono, Phono, Pulse, Canvas, Inlay, Chord, Substrate, Slate) use a `Tiles/{Id}/` stub: `module.native.json` + README, nothing else. `ModuleInstaller` copies that stub (or an external package) into `%LocalAppData%\MosaicShell\Modules\{Id}`; `ModuleCatalog.IsInstalled` is just "does that directory exist." Arming a capability adds its id to `CapabilityStore`'s persisted list.

## Event flow (Tessera example)

```text
OS (volume key, SMTC, shell hook)
  -> HostServices
  -> TesseraCapability (module hooks + TesseraFlyoutRequestBuilder)
  -> CapabilityFlyoutSession.Route (Core platform)
  -> IFlyoutPresenter (Host Avalonia)
```

Media timeline polling is owned by `IMediaSessionService` + `MediaSessionPlatform`, one place, not duplicated per module. OSD suppression uses `WindowsShellFlyoutHook` (ModernFlyouts-compatible SHELLHOOK decode) alongside the audio/brightness change sources.

## Tessera (the flagship capability)

Tessera is an **armed capability**, not a widget overlay: while armed it replaces the OS volume/brightness HUD on a best-effort basis, plus drives media/lock/airplane flyouts. It is a from-scratch reimplementation aimed at [JaxCore/YourFlyouts](https://github.com/Jax-Core/YourFlyouts) parity, not a port; the Rainmeter Tessera skin was removed from the working tree at wave B0.

Runtime: `TesseraCapability` (thin) plus `Capabilities/Platform/*` (Core platform) plus `MosaicShell.Core.Modules.Tessera` (request builder, per-style `*Spec`/`*Policy`/`*Catalog` contracts) plus `MosaicShell.Host/Tiles/Tessera/*` (Avalonia layouts, reveal hosts, tween-driven animation).

### Status (honest)

| Area                    | Status                                                                                                                  |
| ----------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| Flyout kinds            | `vol`, `bright`, `media`, `locks`, `flight`                                                                             |
| Media backend           | SMTC + WebNowPlaying (browser covers; CLI adapter port **5468**)                                                        |
| Layouts                 | All 11 catalog styles visually signed off (`tessera_layout_fidelity`); Radial and PlainText remain lighter Host layouts |
| Placement               | Default top-left; 9-point `Position`; re-anchors after measure                                                          |
| Settings                | Host Tessera panel: flyout scale %, soft frost / baked frost / focus dim                                                |
| OSD suppression         | WinEvent Z-band hide + ShellHook + burst re-resolve; vendor OEM HUDs unsupported                                        |
| Material You to Mixdeck | Opens the native Mixdeck overlay (MVP)                                                                                  |

### Animation model

Every style's per-style facts (which "Animated" meters it addresses, its reveal kind, its rest dimensions, whether it uses stacked HWNDs vs in-layout media, corner radii, and so on) are meant to flow from one place, `TesseraFlyoutTweenTargetCatalog.ResolveProfile`, with every consumer reading that profile rather than re-deriving the same answer locally. This is an actively enforced convention, not just a preference: duplicated per-style dispatch is exactly the failure mode that produces silent drift between what a style's layout does and what its animation policy assumes.

Motion runs in two phases. Phase 1 is a window-level slide and fade driven by Avalonia's real `Animation`/`KeyFrame` API (`FlyoutMotionController.AnimateSteppedAsync`). Phase 2 (when a style has animated targets) reveals per-control content, such as clip, divider, or scale, keyed off a shared `RevealProgress`. Media content fades and clips in place; it does not carry an independent slide direction of its own, since that would disagree with whichever direction the window's own phase-1 motion is configured to enter from.

### Known gaps vs YourFlyouts

- Soft frost / focus dim are Host-native look (Avalonia tint), not a skin port; optional Skia "baked frost" wash is opt-in.
- Full appearance DLC (colors/sizes beyond what Host settings expose) is not implemented.
- Brightness / airplane-mode control has the same Win11-build caveats YourFlyouts itself documents upstream.
- Vendor laptop OEM OSDs (Dell/HP, and others) are not suppressed.
- Only WebNowPlaying and SMTC are supported NowPlaying sources: no Rainmeter-style multi-player `Auto` detection.

External references: [Jax-Core/YourFlyouts](https://github.com/Jax-Core/YourFlyouts) (visual), [ModernFlyouts-Community/ModernFlyouts](https://github.com/ModernFlyouts-Community/ModernFlyouts) (OSD/ShellHook), [WebNowPlaying](https://wnp.keifufu.dev/) (browser media/art, CLI port 5468, see `docs/parity/smtc-album-art.md`).

## Other modules

The remaining nine catalog modules follow the same shape (thin `Tiles/{Id}` stub + real code in `host/`), at varying maturity. Their MVP bars and honesty flags (`*_skeleton` / `*_mvp`, verified by companion tests in `HubParityBacklogTests`) are the living source of truth; see `docs/parity/README.md`. This page only summarizes:

| Module    | Kind                | Current shape                                                     |
| --------- | ------------------- | ----------------------------------------------------------------- |
| Mixdeck   | Capability          | Per-app audio session overlay (mute + volume slider), MVP         |
| Chrono    | Widget              | Live clock/date overlay via `TileRuntime`, MVP                    |
| Phono     | Widget              | SMTC now-playing overlay with transport controls, MVP             |
| Pulse     | Widget              | Audio-level visualizer (bar/round) from `IAudioLevelService`, MVP |
| Canvas    | Widget              | CPU/RAM/disk/host system metrics overlay, MVP                     |
| Inlay     | Capability (hotkey) | Hotkey-triggered launcher overlay, MVP                            |
| Chord     | Capability (hotkey) | Hotkey-triggered action list, MVP                                 |
| Substrate | Capability (hotkey) | Hotkey-triggered mute/volume/brightness tile, MVP                 |
| Slate     | Capability          | Idle-triggered overlay with live clock, MVP                       |

Legacy Rainmeter-era promised behavior for each module (what the original skins did, not how) lives in `docs/legacy/{module}.md`.

## Releases and packaging

End users install via Inno Setup (`MosaicShell-Setup-*.exe`); see `packaging/README.md`. Version tags are date-build (`yyyy.M.d-bN`), not semver. The portable layout (also the Setup staging folder):

```text
Host/MosaicShell.Host.exe   (self-contained win-x64)
Mosaicist/Mosaicist.exe
Tiles/{Id}/...
VERSION.txt
```

Mosaicist installs modules into `%LocalAppData%\MosaicShell\Modules` from the release `Tiles/` tree next to Host.

## Roadmap

Full native supersession (each wave: thin `Tiles/{Id}` stub + real Core/Host code + honest `*_skeleton`/`*_mvp` flags):

| Wave | Module              | Status | Notes                                         |
| ---- | ------------------- | ------ | --------------------------------------------- |
| B0   | Tessera             | Done   | Host capability; stub install                 |
| B1   | Mixdeck             | MVP    | `tile_mixdeck_mvp`; native overlay            |
| B2   | Widgets             | MVP    | Chrono/Phono/Pulse/Canvas stubs + TileRuntime |
| B3   | Hotkey capabilities | MVP    | Inlay/Chord/Substrate                         |
| B4   | Slate               | MVP    | Idle overlay                                  |
| B5   | Hub / repo          | Done   | Host-only docs; Rainmeter trees removed       |

Phase C is post-B5 optional polish, gated by `*_layout_fidelity` flags; never flip one true without a screenshot-level proof:

| Phase | Focus                                               | Reference archive                                             | Exit signal                                                        |
| ----- | --------------------------------------------------- | ------------------------------------------------------------- | ------------------------------------------------------------------ |
| C0    | Archive cross-refs in `docs/legacy/`                | Jax-Core org                                                  | Every legacy page links the upstream repo                          |
| C1    | Tessera named styles beyond Fluent/Windows11/Square | YourFlyouts                                                   | Done: `tessera_layout_fidelity`; proofs in `.github/res/Tessera/`  |
| C2    | Widget StyleCatalog skins                           | ModularClocks / ModularPlayers / ModularVisualizer / Plainext | Per-widget `*_layout_fidelity` flags                               |
| C3    | Capability overlay polish                           | YourMixer / ValliStart / Keylaunch / MIUI-Shade / IdleStyle   | Richer Mixdeck/Inlay UX; Chord motion; Substrate tiles; Slate idle |
| C4    | Installer / release                                 | Local `Tiles/` stubs + Host zip                               | Mosaicist copies native stub only; no `.rmskin`                    |

**Non-goals:** Rainmeter plugins, the CoreShell hub, remote `iwr|iex` install paths, MagickMeter wallpaper blur, full JaxCore GitBook settings parity.

## Further reading

- `docs/module-sdk.md`: third-party module contract (manifest, install, factory signatures).
- `docs/capability-platform.md`: the `ICapabilityContext`/`CapabilityFlyoutSession`/`MediaSessionPlatform` API surface.
- `docs/parity/README.md`: living per-module MVP/skeleton bars and honesty-flag conventions.
- `docs/legacy/README.md`: what the Rainmeter-era modules promised, for modules still catching up.
- `samples/ExternalSampleModule/`: worked example of the third-party module contract.
