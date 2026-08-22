# MosaicShell native rewrite

Avalonia + Windows APIs only. No Rainmeter bridge, no `.ini` interpreter.

**B5 complete:** this repository is **Host-only**. Legacy Rainmeter trees, CoreShell hub skins, and classic packaging scripts are removed from the working tree. Install stubs remain under `Tiles/{Id}/`. Promised Rainmeter-era behavior is documented in [`docs/legacy/`](legacy/).

## Tessera

Armed capability (not a Library overlay widget). Replaces OS volume/brightness HUD while armed **on a best-effort basis**.

Runtime lives in `host/`; [`Tiles/Tessera`](../Tiles/Tessera) is an install stub (`module.native.json`) only. B0 removed Rainmeter Tessera; it is **not** full [YourFlyouts](https://github.com/Jax-Core/YourFlyouts) parity.

### Status (honest)

| Area | Status |
|------|--------|
| Flyout kinds | `vol`, `bright`, `media`, `locks`, `flight` |
| Media backend | **SMTC + WebNowPlaying** (browser covers; CLI port **5468**) |
| Layouts | Fluent + Win11 + Center **Host-polished**; other styles approximations |
| Placement | Default **TL**; 9-point Position; re-anchor after measure |
| Settings | Host Tessera panel + flyout scale % + soft frost / baked frost / focus dim |
| OSD | WinEvent ZBand hide + ShellHook + burst re-resolve; vendor OEM unsupported |
| Pixel → Mixdeck | Opens **native Mixdeck overlay** (MVP) |

### Known gaps vs YourFlyouts

- Soft frost / focus dim = **Host look**; optional Skia baked frost (opt-in). Own identity is fine.
- Full appearance DLC (colors, sizes beyond Host settings)
- Brightness / airplane limitations on some Win11 builds (upstream YourFlyouts caveat)
- Vendor laptop OSDs (Dell/HP/…)
- Rainmeter NowPlaying multi-player Auto (AIMP/CAD/…) - WNP + SMTC only
- Non-kit StyleCatalog skins still approximate

### External references

- Visual: [Jax-Core/YourFlyouts](https://github.com/Jax-Core/YourFlyouts)
- OEM / volume OSD + ShellHook: [ModernFlyouts-Community/ModernFlyouts](https://github.com/ModernFlyouts-Community/ModernFlyouts)
- Browser media / YTM art: [WebNowPlaying](https://wnp.keifufu.dev/) - Host listens on **5468** (CLI adapter port; see [`docs/parity/smtc-album-art.md`](parity/smtc-album-art.md))

## Full native supersession (roadmap)

Each module follows the Tessera pattern: thin `Tiles/{Id}` stub + real code in `host/` + honest `*_skeleton` / `*_mvp` flags in `HubParityBacklogTests`.

| Wave | Module | Status | Notes |
|------|--------|--------|-------|
| **B0** | Tessera | Done | Host capability; stub install |
| **B1** | Mixdeck | MVP | `tile_mixdeck_mvp`; native overlay |
| **B2** | Widgets | MVP | Chrono/Phono/Pulse/Canvas stubs + TileRuntime |
| **B3** | Hotkey caps | MVP | Inlay/Chord/Substrate |
| **B4** | Slate | MVP | Idle overlay |
| **B5** | Hub / repo | **Done** | Host-only docs; Rainmeter trees removed |

Further fidelity (StyleCatalog skins, YourFlyouts pixel parity) remains iterative — not blocked on Rainmeter trees. Upstream Rainmeter reference: [Jax-Core org](https://github.com/Jax-Core) (archives are reference, not runtime).

## Phase C (post-B5 fidelity)

Archives on GitHub replace in-repo Rainmeter trees. Phase C is **optional polish** gated by `*_layout_fidelity` flags — do not flip true without screenshot-level companion proofs.

| Phase | Focus | Reference archive | Exit signal |
|-------|--------|-------------------|-------------|
| **C0** | Archive cross-refs in `docs/legacy/` | [Jax-Core](https://github.com/Jax-Core) | Every legacy page links upstream repo |
| **C1** | Tessera named styles beyond Fluent/Win11/Center | [YourFlyouts](https://github.com/Jax-Core/YourFlyouts) | `tessera_layout_fidelity` (still false until pixel proofs) |
| **C2** | Widget StyleCatalog skins | ModularClocks / ModularPlayers / ModularVisualizer / Plainext | Per-widget `*_layout_fidelity` flags |
| **C3** | Capability overlay polish | YourMixer / ValliStart / Keylaunch / MIUI-Shade / IdleStyle | Richer Mixdeck/Inlay UX; Chord motion; Substrate tiles; Slate idle |
| **C4** | Installer / release | Local `Tiles/` stubs + Host zip | Mosaicist copies native stub only; no `.rmskin` |

**Non-goals:** Rainmeter plugins, CoreShell hub, remote `iwr|iex`, MagickMeter wallpaper blur, full JaxCore GitBook settings parity. See [`docs/legacy/README.md`](legacy/README.md).

