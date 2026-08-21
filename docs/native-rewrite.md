# MosaicShell native rewrite

Avalonia + Windows APIs only. No Rainmeter bridge, no `.ini` interpreter.

## Tessera

Armed capability (not a Library overlay widget). Replaces OS volume/brightness HUD while armed **on a best-effort basis**.

**B0 = Rainmeter Tessera removed**, not full [YourFlyouts](https://github.com/Jax-Core/YourFlyouts) parity. Runtime lives in `host/`; [`Tiles/Tessera`](../Tiles/Tessera) is an install stub (`module.native.json`) only.

### Status (honest)

| Area | Status |
|------|--------|
| Flyout kinds | `vol`, `bright`, `media`, `locks`, `flight` |
| Media backend | **SMTC + WebNowPlaying** (browser covers; CLI port **5468**) |
| Layouts | Fluent + Win11 **kit**; other 9 **approximations** |
| Placement | Default **TL**; 9-point Position; re-anchor after measure |
| Settings | Host Tessera panel (subset of JaxCore Core pages) |
| OSD | WinEvent ZBand hide + ShellHook triggers + burst; vendor OEM unsupported |
| Pixel → Mixdeck | Opens **native Mixdeck overlay** (skeleton/MVP), not Rainmeter Mixdeck |

### Known gaps vs YourFlyouts

- FrostedGlass / Focus
- Full appearance DLC (colors, sizes beyond Host settings)
- Brightness / airplane limitations on some Win11 builds (upstream YourFlyouts caveat)
- Vendor laptop OSDs (Dell/HP/…)
- Rainmeter NowPlaying multi-player Auto (AIMP/CAD/…) - WNP + SMTC only

### External references

- Visual: [Jax-Core/YourFlyouts](https://github.com/Jax-Core/YourFlyouts)
- OEM / volume OSD + ShellHook: [ModernFlyouts-Community/ModernFlyouts](https://github.com/ModernFlyouts-Community/ModernFlyouts)
- Browser media / YTM art: [WebNowPlaying](https://wnp.keifufu.dev/) - Host listens on **5468** (CLI adapter port; see [`docs/parity/smtc-album-art.md`](parity/smtc-album-art.md))

## Full native supersession (roadmap)

Each module follows the Tessera pattern: thin `Tiles/{Id}` stub + real code in `host/` + honest `*_skeleton` / `*_mvp` flags in `HubParityBacklogTests`.

| Wave | Module | Today | Target | Exit criteria |
|------|--------|-------|--------|---------------|
| **B0** | Tessera | Rainmeter gone; Host capability | YourFlyouts-class fidelity later | Stub install only |
| **B1** | Mixdeck | Native overlay MVP | Full StyleCatalog skins later | `tile_mixdeck_mvp`; Plugin=Tessera disabled |
| **B2** | Widgets | TileRuntime MVP surfaces | Full Chrono/Phono/Pulse/Canvas skins later | Stub install; flip `tile_*_mvp` |
| **B3** | Hotkey caps | Overlay MVP (Inlay/Chord/Substrate) | Full StyleCatalog skins later | Per-module `_mvp` (see parity bars) |
| **B4** | Slate | Idle overlay MVP | Full screensaver DLC later | `tile_slate_mvp` |
| **B5** | Hub | Avalonia MainWindow + Mosaicist | Retire CoreShell / Rainmeter install as primary | Host-only docs |

**Non-goals until B5:** deleting Mixdeck/Inlay/Chord/Substrate/Slate `@Resources`, `S-Hub` packagers, or CoreShell wholesale. Widget Rainmeter trees (Chrono/Phono/Pulse/Canvas) are retired in **B2** once MVP bars hold.
