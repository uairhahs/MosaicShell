# Parity checklist

Living flags live in `host/MosaicShell.Core.Tests/HubParityBacklogTests.cs`.

**Convention:** `*_skeleton` = wiring exists (arm/hotkey/stub UI). `*_mvp` = JaxCore-comparable user-visible slice (see bars below). Do not mark `_mvp` true without the bar met.

**B5:** Rainmeter hub / skin trees are gone from the repo. Runtime is Host-only; `Tiles/{Id}` are native install stubs. Legacy promises: [`docs/legacy/`](../legacy/).

## Tessera

Runtime is host-only; `Tiles/Tessera` is a native install stub. B0 is **not** full YourFlyouts parity.

| Flag | Meaning |
|------|---------|
| `tessera_osd_flyout` | Armed flyout + OSD suppress (+ ShellHook triggers) |
| `tessera_named_styles` | Style catalog JaxCore ids |
| `tessera_locks_flight` | Lock-key + airplane flyouts |
| `tessera_live_update_multimonitor` | Reuse/update window; monitor + anchor math |
| `tessera_fluent_win11_kit` | Fluent + Win11 transfer kit present |
| `tessera_layout_fidelity` | **false** - non-kit styles still approximate; Host does not require pixel-YourFlyouts |
| `tessera_fluent_yourflyouts` | **true** - Fluent / Win11 / Center tightened for Host identity (compact, soft frost, optional baked wash) |
| `tessera_media_smtc_only` | **false** - SMTC is not the only media path |
| `tessera_media_wnp` | **true** - WebNowPlaying host on CLI port **5468** |
| `tile_tessera_mvp` | Armed flyouts + named styles (Host path) |

References: [YourFlyouts](https://github.com/Jax-Core/YourFlyouts) (visual), [ModernFlyouts](https://github.com/ModernFlyouts-Community/ModernFlyouts) (OSD / ShellHook).

### Known gaps vs YourFlyouts

- Soft frost / focus dim are **Host identity** (Avalonia tint); optional Skia baked frost wash (MagickMeter-style, opt-in)
- Full color/size DLC beyond Host settings pages
- Brightness / airplane caveats on some Win11 builds (see YourFlyouts README)
- Vendor laptop OEM HUDs unsupported
- Multi-player Auto NowPlaying outside WNP + SMTC
- Remaining named styles (Amber/Gnome/Pixel/…) still light approximations (`tessera_layout_fidelity` false)

## Mixdeck MVP bar (must all hold for `tile_mixdeck_mvp`)

- Overlay: per-app sessions from `IAppAudioService`
- Mute toggle + volume slider per session
- StyleCatalog style reflected in chrome
- Hotkey / Pixel deep-link opens **overlay**, not placeholder flyout text

## Widget MVP bars (B2)

`Tiles/{Chrono,Phono,Pulse,Canvas}` are native install stubs. Runtime is `TileRuntime` + `LiveTilesA` only. Full StyleCatalog pixel skins remain later (`layout_fidelity`-style flags if added).

### Chrono (`tile_chrono_mvp`)

- Live clock + date from system time
- `TwentyFourHour` / `ShowSeconds` from `ChronoSettings`
- StyleCatalog style changes chrome (e.g. Center vs Text vs Minimal sizing)
- Library Start opens overlay via TileRuntime

### Phono (`tile_phono_mvp`)

- SMTC title / artist (and thumbnail when present)
- Working prev / play-pause / next via `IMediaSessionService`
- StyleCatalog style reflected in chrome
- No WebNowPlaying required on the Phono path

### Pulse (`tile_pulse_mvp`)

- Bars (or round) driven by `IAudioLevelService` bands/peak - not RNG
- `PulseSettings.VisualizerType` / style affects layout (Bar vs Round minimum)
- Library Start opens overlay via TileRuntime

### Canvas (`tile_canvas_mvp`)

- Live CPU / RAM / disk / host from `ISystemMetricsService`
- Section toggles from `CanvasSettings`
- Compact vs DEFAULT chrome from StyleCatalog
- Library Start opens overlay via TileRuntime

## Desktop widget chrome

Legacy Chrono/Phono/Pulse/Canvas had **no product title strip** - content filled the skin. Shared right-click Ctx offered Configure, Align, Z layer, Refresh, Unload.

Native overlays must match that shape:

| Feature | Status |
|---------|--------|
| Single chrome frame (no nested module title) | **required** - content fills `TileOverlayWindow` |
| Whole-surface drag | **required** (skip interactive controls) |
| Right-click: Configure in Host | **required** |
| Right-click: Align (center / corners) | **required** |
| Right-click: Z layer (desktop / normal / top) | **required** |
| Right-click: Refresh / Unload | **required** |
| Widgets default desktop Z (Pulse was AlwaysOnTop=-2) | **required** |
| Position persist | SessionStore |
| Style-driven layout fidelity | later (`layout_fidelity`) |
| Phono AutoHide when idle | later |
| Canvas DynamicWindowSize / section toggles | partial (settings) |

## Hotkey capability MVP bars (B3 - must all hold for `tile_*_mvp`)

Armed hotkey opens a **Host overlay** via bridge (same pattern as Mixdeck), not a placeholder flyout.

### Inlay (`tile_inlay_mvp`)

- Hotkey opens Host overlay via bridge
- Pins from `InlaySettings` render and launch (`UseShellExecute`)
- Search / Enter launches a target
- StyleCatalog style reflected lightly in chrome
- Escape closes overlay

### Chord (`tile_chord_mvp`)

- Hotkey opens Host overlay via bridge
- `ChordSettings.Actions` listed; Enter matches Name and launches Target (fallback: raw text as path/URI)
- StyleCatalog style reflected lightly
- Escape closes overlay

### Substrate (`tile_substrate_mvp`)

- Hotkey opens Host overlay via bridge
- Mute / volume (±) wired to `IAudioService`; brightness when `IBrightnessService.IsSupported`
- `ShowMute` hides mute tile when false
- StyleCatalog DEFAULT; Escape closes

## Slate MVP bar (B4 - must hold for `tile_slate_mvp`)

- Arm starts idle watch with `SlateSettings.IdleSeconds` (clamped ≥30s)
- Idle opens Host overlay via bridge (not tiny flyout); live clock updates
- When `HideOnFullscreen` is true, suppress idle show if fullscreen probe reports fullscreen
- Disarm stops idle and hides overlay

## Skeleton vs MVP (other tiles)

| Skeleton | MVP |
|----------|-----|
| `tile_mixdeck_skeleton` | `tile_mixdeck_mvp` |
| `tile_chrono/phono/pulse/canvas_skeleton` | corresponding `_mvp` (bars above) |
| `tile_inlay/chord/substrate_skeleton` | corresponding `_mvp` (B3 bars above) |
| `tile_slate_skeleton` | `tile_slate_mvp` (B4 bar above) |

## Companion proofs

True flags map to named tests in `HubParityBacklogTests.CompanionProof` (e.g. `Armed_tessera_shows_flyout_on_volume_change`).

## Supersession waves

See [native-rewrite.md](../native-rewrite.md). B5 (Host-only repo) is complete.

## Phase C layout fidelity flags

Flip to **true** only when a module meets screenshot-level parity vs its [Jax-Core archive](../legacy/README.md). All default **false** in `HubParityBacklogTests`.

| Flag | Module | Upstream archive |
|------|--------|------------------|
| `tessera_layout_fidelity` | Tessera | YourFlyouts |
| `mixdeck_layout_fidelity` | Mixdeck | YourMixer |
| `inlay_layout_fidelity` | Inlay | ValliStart |
| `chord_layout_fidelity` | Chord | Keylaunch |
| `substrate_layout_fidelity` | Substrate | MIUI-Shade |
| `slate_layout_fidelity` | Slate | IdleStyle |
| `chrono_layout_fidelity` | Chrono | ModularClocks |
| `phono_layout_fidelity` | Phono | ModularPlayers |
| `pulse_layout_fidelity` | Pulse | ModularVisualizer |
| `canvas_layout_fidelity` | Canvas | Plainext |

Tessera Phase C1 maturity tiers live in `TesseraLayoutCoverage` (polished vs approximate). `tessera_layout_fidelity` remains false until all StyleCatalog ids pass visual proofs.

| `native_tile_overlay_runtime` | **false** — full StyleCatalog pixel skins as dedicated runtime; not a Rainmeter interpreter |

