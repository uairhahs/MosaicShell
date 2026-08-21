# Scaling

MosaicShell UI size is driven by a **host-agnostic scale contract**, not screen resolution.

## Contract

```text
UiScale = DpiScale × UserScale
```

| Name | Meaning | Persistence |
|------|---------|-------------|
| `DpiScale` | Windows display scale (`1.0` = 100%, `1.5` = 150%) | [`@Resources/ScaleVars.inc`](../@Resources/ScaleVars.inc) |
| `UserScale` | User override multiplier (default `1.0`) | same file |
| `UiScale` | Effective scale layouts should use | [`@Resources/Includes/ScaleContract.inc`](../@Resources/Includes/ScaleContract.inc) |
| `LastUiScale` | `UiScale` that `Set.W` / `Set.H` were last sized for | same file |

Design space is **logical pixels at 96 DPI / 100%**. Multiply `X Y W H FontSize StrokeWidth` (and Shape geometry) by `#UiScale#` / `[Set.S]`.

Core shell **window size tracks `UiScale`**: when DPI or user scale changes, `Apply-ScaleContract` rescales `Set.W` / `Set.H` by `newUi / LastUiScale` so padding, fonts, and chrome stay proportional to the window (avoids clipped headers, overlapping settings rows, and footer collisions).

Rainmeter.exe is kept **high-DPI aware** (`~HIGHDPIAWARE`) so Windows does not bitmap-upscale skins. MosaicShell owns visual size via `UiScale`.

## Rainmeter aliases (migration)

Until call sites are fully renamed, these equal `UiScale`:

| Alias | Where |
|-------|--------|
| `[Set.S]` | Core shell (`Formula=(#DpiScale#*#UserScale#)`) |
| `#Sec.S#` | Modals / accessories |
| `#Ctx.S#` | Context menus |
| `#Scale#` | Tiles |
| `#Style.Scale#` | Chord launch styles |

## User controls

**Settings → Appearance → UI scale**

- Shows detected Windows DPI and effective `%`
- Edit `UserScale` (`0.75`–`2.0`)
- **Match Windows** → `UserScale = 1` and re-apply contract
- **Re-detect DPI** → probe display scale again

**Settings → General → Repair DPI override** (advanced) re-applies the Rainmeter high-DPI registry flag if something cleared it.

## Probe / apply

[`@Resources/Powershell/ScaleContract.ps1`](../@Resources/Powershell/ScaleContract.ps1) probes DPI (GDI physical/logical height), writes `ScaleVars.inc`, syncs tile `Scale` / Chord `Style.Scale` aliases, and ensures HIGHDPIAWARE. It runs on Main load (`Initialize-ScaleContract`) and from Appearance actions.

Installer path: [`RunMosaicist.ps1`](../RunMosaicist.ps1) writes the same contract on module install (no more `min(W/1920, H/1080)`).

## Expected results (regression matrix)

| Display | Expectation |
|---------|-------------|
| 1080p @ 100% | `DpiScale≈1`, UI at design size when `UserScale=1` |
| 1080p @ 150% | `DpiScale≈1.5`, UI ~1.5× design size (readable, not tiny) |
| 1440p @ 125% | `DpiScale≈1.25` |
| 4K @ 150% | `DpiScale≈1.5` (not “huge because 4K pixels”) |
| Mixed DPI laptop + external | Best-effort primary probe; re-detect after moving primary / changing scale. Per-monitor live DPI is limited on Rainmeter. |

## What this is not

Rainmeter cannot match FL Studio’s true vector host. This contract makes size **correct and consistent** on Rainmeter and is the same math a future native host should keep.

## Phase 3 (native host) — in progress

Rainmeter cannot match a true vector host. Decoupling is underway under [`host/`](../host/).

See **[`architecture-native.md`](architecture-native.md)** for:

1. Avalonia + Skia hub (`MosaicShell.Host`) with Discover / Library / Settings spike
2. Shared `MosaicShell.Core` scale contract + module catalog
3. `Mosaicist` CLI installer (zip + SHA-256; **no** `iwr|iex`)
4. Path to tile runtime and dropping Rainmeter as a prerequisite

### Rainmeter-era notes (until host ships)

Until call sites are fully migrated, Rainmeter aliases still map toward `UiScale`, but **do not** auto-grow `Set.W`/`Set.H` by DPI on 1080p displays (that overflowed the work area). Prefer `UserScale` for density tweaks in Appearance once Track A triage lands.

Prefer PowerShell + declarative vars over bang-only UI logic so logic stays portable into `MosaicShell.Core`.
