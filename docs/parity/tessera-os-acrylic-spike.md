# Tessera OS acrylic spike (H1–H4)

Alpha ships **Skia frost** (default). This spike proves Window-level `AcrylicBlur` + `WinUICompositionBackdropCornerRadius` on a **single-shell** flyout only.

**Platform honesty:** Windows 11 is the supported target for evaluation and any future ship decision. Windows 10 may still hit the technical acrylic gate on some builds, but Microsoft no longer fully supports Win10; MosaicShell makes **no promises** for Win10 acrylic or glass (best-effort only, frost fallback expected).

## Enable (not alpha default)

Hub: Tessera module config, **Windows 11 OS acrylic** (visible only after Win11 eval sign-off). Requires restart for process-wide corner radius.

CLI (dev override, same effect):

```powershell
dotnet run --project host/MosaicShell.Host -- --tessera-os-acrylic
```

H2 Software fallback eval:

```powershell
dotnet run --project host/MosaicShell.Host -- --tessera-software-render
dotnet run --project host/MosaicShell.Host -- --tessera-os-acrylic --tessera-software-render
```

Rollback: turn off in hub and restart. Pre-sign-off eval may still use `--tessera-os-acrylic`; after Win11 sign-off the hub toggle is authoritative and the CLI flag is ignored.

Compile kill-switch: `TesseraOsAcrylicTrialPolicy.Available` in Core (set `false` to disable the flag path in a hotfix).

## H1 scope (implemented)

- `FlyoutWindow` is a top-level `Window` (not Popup).
- AngleEgl pinned process-wide (`AngleEgl` then `Software`) via [`Win32HostCompositionPolicy`](../../host/MosaicShell.Core/HostPlatform/Win32HostCompositionPolicy.cs).
- Corner radius `12` only when `--tessera-os-acrylic` is on (process-wide).
- Eligible only when: trial flag, technical WinUIComposition floor (build 17134+), soft frost on, **not** stacked (`showMediaStrip != 1`).
- `HideUntilCompositionReady` + generation-gated reveal unchanged.
- Skia frost slab skipped when `TesseraFlyoutGlassMode.OsAcrylic`; edge chrome only.
- BitBlt / live backdrop sampling stays forbidden.

**Manual fixture:** Fluent or Windows11 **volume without media strip**, or Material You (no stacked strip).

## H1 smoke (2026-08-23, informal)

Win11 manual pass on `--tessera-os-acrylic` (via `.local/compile-local.ps1`): eligible single-shell styles look good. Not a formal H2 sign-off.

| Area                           | Result             | Notes                                                                |
| ------------------------------ | ------------------ | -------------------------------------------------------------------- |
| OsAcrylic on eligible styles   | Pass               | MaterialYou / single-shell volume                                    |
| Stacked volume+media           | Pass (frost)       | Correctly stays Skia frost (`showMediaStrip=1`)                      |
| Black flash / settle           | Partial            | Occasional black frame; recovers **faster** than frost-only baseline |
| Radial (Smouti)                | Pass               | Arc sweep invalidates visual; capture-lost clears drag               |
| Meter/Amber GPU shimmer        | Fixed (H1 session) | Inner Skia glass stack suppressed on live flyouts                    |
| Volume-update dispatcher flood | Fixed (H1 session) | Coalesce-before-Post + Radial capture-lost                           |

**H1 code scope is done.** Remaining visual gaps above are **H2 validation or style fidelity**, not H1 blockers.

## H2 — Manual evaluation checklist (before ship)

**Status: PASS (2026-08-23, Win11).** Formal table: [`.local/Tessera/os-acrylic-eval/README.md`](../../.local/Tessera/os-acrylic-eval/README.md).

Formal sign-off target is **Windows 11**. Optional Win10 smoke is best-effort only; failures on Win10 do not block kill/ship on Win11.

Record screenshots + notes under `.local/Tessera/os-acrylic-eval/` (gitignored scratch). Suggested files:

```ascii
.local/Tessera/os-acrylic-eval/
  README.md          # copy table below + pass/fail per row
  win11-flag-off/    # frost baseline PNGs
  win11-flag-on/     # acrylic trial PNGs
  notes.md           # timings (black flash ms), repro steps
```

### Acrylic + composition (required Win11)

| Check                       | Flag off (frost)  | Flag on (acrylic)                     | 2026-08-23 formal                          |
| --------------------------- | ----------------- | ------------------------------------- | ------------------------------------------ |
| Cold Show / first arm       | No black flash    | No black flash through settle         | Pass (occasional flash; faster than frost) |
| Try now (config)            | Frost preview OK  | Single-shell live acrylic             | Pass                                       |
| Rapid restyle / volume pump | No stacked black  | No ~1s acrylic brush death            | Pass                                       |
| Material You (no strip)     | Frost             | Acrylic if eligible                   | Pass                                       |
| Fluent + media strip        | Frost (stacked)   | **Must stay frost** (not eligible)    | Pass                                       |
| Software rendering fallback | Frost presentable | Frost fallback if acrylic unavailable | Pass (`--tessera-software-render`)         |

### Style fidelity (parallel track, not acrylic ship gate)

These affect frost baseline too. Fix or accept before alpha glass sign-off; they do **not** block trying H3 if Win11 acrylic rows pass.

| Check                       | Notes                                                                   | 2026-08-23 informal                                                   |
| --------------------------- | ----------------------------------------------------------------------- | --------------------------------------------------------------------- |
| Radial volume arc live sync | Ring `%` / arc should follow keys, wheel, and pump while flyout visible | **Pass** — Arc sweep invalidates visual on change (2026-08-23 verify) |
| Radial + media strip        | Stacked layout; always frost/acrylic-ineligible                         | Pass (arc sync fixed)                                                 |
| Meter/Amber inner glass     | No GPU shimmer on live flyout                                           | Pass (inner Skia suppressed)                                          |

**Fail any required Win11 acrylic row → skip H3, go to H4 kill.** Alpha is unaffected (flag off).

**Pass all required Win11 acrylic rows → H3 decision** (stacked cards fork). Style fidelity rows can stay open on a separate track.

**H2 signed 2026-08-23.** H3 decision recorded below.

## H3 — Stacked cards (only if H2 passes)

**Decision (2026-08-23): option (a) N FlyoutWindows.**

Per-panel rounded OS acrylic on Win11. Implementation plan: [tessera-os-acrylic-h3-n-windows.md](tessera-os-acrylic-h3-n-windows.md).

| Option                        | Pros                        | Cons                                                           | Status     |
| ----------------------------- | --------------------------- | -------------------------------------------------------------- | ---------- |
| **(a) N FlyoutWindows**       | Per-card rounded OS acrylic | Z-order, placement, focus-passthrough, outside-click, FocusDim | **Chosen** |
| **(b) One HWND, inner seams** | Cheap                       | Square/mismatched inner card corners on volume+media           | Rejected   |

CoreUI multi-tile shells follow the same fork (phase 2 after volume+media strip).

**Phase 0 (Core, in tree):** [`TesseraOsAcrylicStackedPolicy`](../../host/MosaicShell.Core/Modules/Tessera/TesseraOsAcrylicStackedPolicy.cs), [`TesseraFlyoutOutsideClickPolicy`](../../host/MosaicShell.Core/Modules/Tessera/TesseraFlyoutOutsideClickPolicy.cs).

**Phase 1 (Host, signed off 2026-08-23):** split stacked volume+media into `Tessera:vol` + `Tessera:media` HWNDs under `--tessera-os-acrylic`. Win11 manual sign-off rows in [tessera-os-acrylic-h3-n-windows.md](tessera-os-acrylic-h3-n-windows.md) **PASS** (Meter, Gnome, Compact, Modern Flyouts).

Do not start H4 ship until product decides persisted opt-in vs kill; visual eval is complete on Win11.

## H4 — Ship or kill

| Outcome  | Action                                                                                                    |
| -------- | --------------------------------------------------------------------------------------------------------- |
| **Kill** | Leave AngleEgl pin; `Available = false` or remove flag; frost remains alpha look                          |
| **Ship** | Persisted hub opt-in (`UseOsAcrylic`, default frost); document Win11 as supported; Win10 best-effort only |

Do **not** flip `tessera_layout_fidelity` or any Hub `*_mvp` flag for acrylic. Glass authenticity is separate from layout sign-off.

## Core contracts

- [`TesseraOsAcrylicTrialPolicy`](../../host/MosaicShell.Core/Modules/Tessera/TesseraOsAcrylicTrialPolicy.cs) — single-shell acrylic
- [`TesseraOsAcrylicStackedPolicy`](../../host/MosaicShell.Core/Modules/Tessera/TesseraOsAcrylicStackedPolicy.cs) — H3 N-window stacked acrylic
- [`TesseraFlyoutOutsideClickPolicy`](../../host/MosaicShell.Core/Modules/Tessera/TesseraFlyoutOutsideClickPolicy.cs) — union bounds
- [`TesseraFlyoutGlassPolicy`](../../host/MosaicShell.Core/Modules/Tessera/TesseraFlyoutGlassPolicy.cs) — `OsAcrylic` mode
- Tests: [`TesseraOsAcrylicTrialPolicyTests`](../../host/MosaicShell.Core.Tests/TesseraOsAcrylicTrialPolicyTests.cs), [`TesseraOsAcrylicStackedPolicyTests`](../../host/MosaicShell.Core.Tests/TesseraOsAcrylicStackedPolicyTests.cs), [`TesseraOsAcrylicSignOffPolicyTests`](../../host/MosaicShell.Core.Tests/TesseraOsAcrylicSignOffPolicyTests.cs)
