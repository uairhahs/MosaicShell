# Legacy Rainmeter promises

This folder documents **what the Rainmeter-era MosaicShell tiles promised** (user-visible behavior), not how they were implemented.

## Why this exists

As of supersession wave **B5**, this repository is **Host-only** (Avalonia + Mosaicist). The Rainmeter hub trees (`CoreShell/`, `S-Hub/`, `@Resources/`, `Accessories/`, `Ctx/`, `Core/`, `Main/`, `@Developer/`), classic install scripts (`RunMosaicist.ps1`, `RMSKIN.ini`, …), and full Rainmeter skin trees under `Tiles/` were removed from the working tree.

Install stubs remain at `Tiles/{Id}/` (`module.native.json` + README) so Mosaicist can still populate `Modules/{Id}` for the Host.

## Archives

| Source | Notes |
|--------|--------|
| [Jax-Core/JaxCore](https://github.com/Jax-Core/JaxCore) | Upstream hub (archived November 2024) |
| Git history of this repo | Pre-Avalonia Rainmeter trees lived here before B5; recover via `git log` / `git show <rev>:path` |
| Archived remotes / mirrors | *(placeholder — add concrete URLs when published)* |

## Per-module promised functionality

| Module | Page |
|--------|------|
| Tessera | [tessera.md](tessera.md) |
| Mixdeck | [mixdeck.md](mixdeck.md) |
| Inlay | [inlay.md](inlay.md) |
| Chord | [chord.md](chord.md) |
| Substrate | [substrate.md](substrate.md) |
| Slate | [slate.md](slate.md) |
| Chrono | [chrono.md](chrono.md) |
| Phono | [phono.md](phono.md) |
| Pulse | [pulse.md](pulse.md) |
| Canvas | [canvas.md](canvas.md) |

Host fidelity and honesty flags: [`docs/native-rewrite.md`](../native-rewrite.md), [`docs/parity/README.md`](../parity/README.md).

## Historical plugin credits

Rainmeter-era skins depended on community plugins (AudioAnalyzer, FrostedGlass, MagickMeter, WebNowPlaying, AppVolume, HotKey, and others). Those binaries are **not** part of the Host runtime; see JaxCore / Rainmeter forum archives for provenance.
