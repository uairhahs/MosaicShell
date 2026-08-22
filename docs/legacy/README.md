# Legacy Rainmeter promises

This folder documents **what the Rainmeter-era MosaicShell tiles promised** (user-visible behavior), not how they were implemented.

## Why this exists

As of supersession wave **B5**, this repository is **Host-only** (Avalonia + Mosaicist). Rainmeter hub trees and full skin trees were removed from the working tree. **[Jax-Core](https://github.com/Jax-Core)** on GitHub is the canonical, read-only archive for upstream Rainmeter sources (hub archived November 2024).

Install stubs remain at `Tiles/{Id}/` (`module.native.json` + README) so Mosaicist can still populate `Modules/{Id}` for the Host.

## Upstream archives (Jax-Core)

| MosaicShell id | Jax-Core repo | Role | Screenshots |
|----------------|---------------|------|-------------|
| *(hub)* | [JaxCore](https://github.com/Jax-Core/JaxCore) | Installer / settings shell | [Screenshots.md](https://github.com/Jax-Core/JaxCore/blob/main/Screenshots.md) |
| Tessera | [YourFlyouts](https://github.com/Jax-Core/YourFlyouts) | Flyouts / OSD | [Screenshots.md](https://github.com/Jax-Core/YourFlyouts/blob/main/Screenshots.md) |
| Mixdeck | [YourMixer](https://github.com/Jax-Core/YourMixer) | Per-app mixer | [Screenshots.md](https://github.com/Jax-Core/YourMixer/blob/main/Screenshots.md) |
| Inlay | [ValliStart](https://github.com/Jax-Core/ValliStart) | Start menu | [Screenshots.md](https://github.com/Jax-Core/ValliStart/blob/main/Screenshots.md) |
| Chord | [Keylaunch](https://github.com/Jax-Core/Keylaunch) | Macro launcher | *(see repo README)* |
| Slate | [IdleStyle](https://github.com/Jax-Core/IdleStyle) | Idle / screensaver | *(see repo README)* |
| Substrate | [MIUI-Shade](https://github.com/Jax-Core/MIUI-Shade) | Control center | *(see repo README)* |
| Chrono | [ModularClocks](https://github.com/Jax-Core/ModularClocks) | Clock gallery | *(see repo README)* |
| Phono | [ModularPlayers](https://github.com/Jax-Core/ModularPlayers) | Media widget | *(see repo README)* |
| Pulse | [ModularVisualizer](https://github.com/Jax-Core/ModularVisualizer) | Visualizer | *(see repo README)* |
| Canvas | [Plainext](https://github.com/Jax-Core/Plainext) | System text | *(see repo README)* |

Pre-B5 Rainmeter trees from **this fork** are also recoverable via git history:

```bash
git log --oneline -- Tiles/Tessera/
git show <pre-B5-commit>:Tiles/Mixdeck/Main.ini
```

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

## Non-goals

Do not expect this repo to re-ship Rainmeter plugins, CoreShell hub skins, `RunMosaicist.ps1`, or ImageMagick wallpaper-sync blur. Host uses its own material language (see Phase C in [`native-rewrite.md`](../native-rewrite.md)).
