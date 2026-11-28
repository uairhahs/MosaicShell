# Tessera native translucency spike (H1 to H4)

Before 2026-11-28 the default Frosted was **Skia frost** with no OS blur (see the section on the two finishes below for today). This spike proved Window-level `AcrylicBlur` + `WinUICompositionBackdropCornerRadius` on a **single-shell** flyout only.

**Platform honesty:** Windows 11 is the supported target for evaluation and any future ship decision. Windows 10 may still hit the technical acrylic gate on some builds, but Microsoft no longer fully supports Win10; MosaicShell makes **no promises** for Win10 acrylic or glass (best-effort only, frost fallback expected).

## Enable (not alpha default)

Hub: Tessera module config, **Native Translucency** (visible only after Win11 eval sign-off). Requires restart for process-wide corner radius.

CLI (dev override, same effect):

```powershell
dotnet run --project host/MosaicShell.Host -- --tessera-native-translucency
```

H2 Software fallback eval:

```powershell
dotnet run --project host/MosaicShell.Host -- --tessera-software-render
dotnet run --project host/MosaicShell.Host -- --tessera-native-translucency --tessera-software-render
```

Rollback: turn off in hub and restart. Pre-sign-off eval may still use `--tessera-native-translucency`; after Win11 sign-off the hub toggle is authoritative and the CLI flag is ignored.

Compile kill-switch: `TesseraNativeTranslucencyPolicy.Available` in Core (set `false` to disable the flag path in a hotfix).

## H1 scope (implemented)

- `FlyoutWindow` is a top-level `Window` (not Popup).
- AngleEgl pinned process-wide (`AngleEgl` then `Software`) via [`Win32HostCompositionPolicy`](../../host/MosaicShell.Core/HostPlatform/Win32HostCompositionPolicy.cs).
- Corner radius `12` only when `--tessera-native-translucency` is on (process-wide).
- Eligible only when: trial flag, technical WinUIComposition floor (build 17134+), soft frost on, **not** stacked (`showMediaStrip != 1`).
- `HideUntilCompositionReady` + generation-gated reveal unchanged.
- Skia frost slab skipped when `TesseraFlyoutGlassMode.NativeTranslucency`; edge chrome only.
- BitBlt / live backdrop sampling stays forbidden.

**Manual fixture:** Fluent or Windows11 **volume without media strip**, or Material You (no stacked strip).

## H1 smoke (2026-08-23, informal)

Win11 manual pass on `--tessera-native-translucency`: eligible single-shell styles look good. Not a formal H2 sign-off.

| Area                                  | Result             | Notes                                                                |
| ------------------------------------- | ------------------ | -------------------------------------------------------------------- |
| NativeTranslucency on eligible styles | Pass               | MaterialYou / single-shell volume                                    |
| Stacked volume+media                  | Pass (frost)       | Correctly stays Skia frost (`showMediaStrip=1`)                      |
| Black flash / settle                  | Partial            | Occasional black frame; recovers **faster** than frost-only baseline |
| Radial (Smouti)                       | Pass               | Arc sweep invalidates visual; capture-lost clears drag               |
| Meter/Amber GPU shimmer               | Fixed (H1 session) | Inner Skia glass stack suppressed on live flyouts                    |
| Volume-update dispatcher flood        | Fixed (H1 session) | Coalesce-before-Post + Radial capture-lost                           |

**H1 code scope is done.** Remaining visual gaps above are **H2 validation or style fidelity**, not H1 blockers.

## H2: Manual evaluation checklist (before ship)

**Status: PASS (2026-08-23, Win11).** Recorded results are summarized in the tables below.

Formal sign-off target is **Windows 11**. Optional Win10 smoke is best-effort only; failures on Win10 do not block kill/ship on Win11.

These tables summarize the recorded evaluation; they are not a published raw screenshot archive.

### Acrylic + composition (required Win11)

| Check                       | Flag off (frost)  | Flag on (acrylic)                     | 2026-08-23 formal                          |
| --------------------------- | ----------------- | ------------------------------------- | ------------------------------------------ |
| Cold Show / first arm       | No black flash    | No black flash through settle         | Pass (occasional flash; faster than frost) |
| Try now (config)            | Frost preview OK  | Single-shell live acrylic             | Pass                                       |
| Rapid restyle / volume pump | No stacked black  | No 1 s acrylic brush death            | Pass                                       |
| Material You (no strip)     | Frost             | Acrylic if eligible                   | Pass                                       |
| Fluent + media strip        | Frost (stacked)   | **Must stay frost** (not eligible)    | Pass                                       |
| Software rendering fallback | Frost presentable | Frost fallback if acrylic unavailable | Pass (`--tessera-software-render`)         |

### Style fidelity (parallel track, not acrylic ship gate)

These affect frost baseline too. Fix or accept before alpha glass sign-off; they do **not** block trying H3 if Win11 acrylic rows pass.

| Check                       | Notes                                                                   | 2026-08-23 informal                                                  |
| --------------------------- | ----------------------------------------------------------------------- | -------------------------------------------------------------------- |
| Radial volume arc live sync | Ring `%` / arc should follow keys, wheel, and pump while flyout visible | **Pass**: arc sweep invalidates visual on change (2026-08-23 verify) |
| Radial + media strip        | Stacked layout; always frost/acrylic-ineligible                         | Pass (arc sync fixed)                                                |
| Meter/Amber inner glass     | No GPU shimmer on live flyout                                           | Pass (inner Skia suppressed)                                         |

**If any required Win11 acrylic row fails, skip H3 and go to the H4 kill.** Alpha is unaffected (flag off).

**If every required Win11 acrylic row passes, make the H3 decision** (stacked cards fork). Style fidelity rows can stay open on a separate track.

**H2 signed 2026-08-23.** H3 decision recorded below.

## H3: Stacked cards (only if H2 passes)

**Decision (2026-08-23): option (a) N FlyoutWindows.**

Per-panel rounded native translucency on Win11. Implementation plan: [tessera-native-translucency-h3-n-windows.md](tessera-native-translucency-h3-n-windows.md).

| Option                        | Pros                                 | Cons                                                           | Status     |
| ----------------------------- | ------------------------------------ | -------------------------------------------------------------- | ---------- |
| **(a) N FlyoutWindows**       | Per-card rounded native translucency | Z-order, placement, focus-passthrough, outside-click, FocusDim | **Chosen** |
| **(b) One HWND, inner seams** | Cheap                                | Square/mismatched inner card corners on volume+media           | Rejected   |

CoreUI multi-tile shells follow the same fork (phase 2 after volume+media strip).

**Phase 0 (Core, in tree):** [`TesseraNativeTranslucencyStackedPolicy`](../../host/MosaicShell.Core/Modules/Tessera/TesseraNativeTranslucencyStackedPolicy.cs), [`TesseraFlyoutOutsideClickPolicy`](../../host/MosaicShell.Core/Modules/Tessera/TesseraFlyoutOutsideClickPolicy.cs).

**Phase 1 (Host, signed off 2026-08-23):** split stacked volume+media into `Tessera:vol` + `Tessera:media` HWNDs under `--tessera-native-translucency`. Win11 manual sign-off rows in [tessera-native-translucency-h3-n-windows.md](tessera-native-translucency-h3-n-windows.md) **PASS** (Meter, Gnome, Compact, Modern Flyouts).

Visual evaluation is complete on Win11, and H4 shipped as a persisted hub opt-in (see below).

## H4: Ship or kill

**Outcome: Ship.** The Hub **Native Translucency** toggle persists `UseNativeTranslucency` (default off, so frost stays the default look) and is the only opt-in once Win11 sign-off is recorded; `--tessera-native-translucency` is ignored after that. Both `Available` kill switches remain `true`.

| Outcome  | Action                                                                                                             |
| -------- | ------------------------------------------------------------------------------------------------------------------ |
| **Kill** | Leave AngleEgl pin; `Available = false` or remove flag; frost remains alpha look                                   |
| **Ship** | Persisted hub opt-in (`UseNativeTranslucency`, default frost); document Win11 as supported; Win10 best-effort only |

Do **not** flip `tessera_layout_fidelity` or any Hub `*_mvp` flag for acrylic. Glass authenticity is separate from layout sign-off.

## What Windows draws, and the two finishes (2026-11-28)

Avalonia's `AcrylicBlur` level on Windows (12.2, `WinUiCompositionUtils.CreateAcrylicBlurBackdropBrush`) is a Gaussian
blur of the host backdrop and nothing else: no tint, luminosity or noise. So Windows gives Tessera the same blur-only base
KWin gives it on Plasma, and the card paints the material over it from `NativeTranslucencyAcrylicSpec`, as on Plasma:

- **Frosted**: the blur, then a light shade, white frost, a highlight from the top, a crisp light edge and faint grain.
  On Windows there is no edge: the window's hard-edged region and the flyout scale left it smeared or harsh.
- **Native Translucency**: on Plasma, the blur under a plain light veil and a faint edge. On Windows, Windows' blur
  bare, with nothing painted over it.
- **Both**: a shadow behind the text and icons, so thin glass stays readable over a bright backdrop. Frosted's is
  lighter and tighter, since its shade already sits under the text.

Frosted therefore asks for the blur on Windows too (it was the app's own frost with no OS blur). Moving between Frosted
and Native needs no restart; Solid to either, or back, does, since the rounded blur's corner radius is read at start.

Windows' own acrylic material (DWM, `DWMWA_SYSTEMBACKDROP_TYPE` = `DWMSBT_TRANSIENTWINDOW`) was tried first, as it
carries the tint, luminosity and noise and follows the theme. On Windows 11 it drew only its flat fallback for a Tessera-
like window: borderless, topmost, never activated, under WinUIComposition, DirectComposition or RedirectionSurface, with
or without a frame, and even when brought to the foreground. It is not used.

Windows' **Transparency effects** switch (Settings, Personalization, Colors) is followed: off, Frosted and Native draw as
Solid, as Windows' own flyouts turn opaque (`ITransparencyEffects`, read at each flyout).

## Core contracts

- [`TesseraNativeTranslucencyPolicy`](../../host/MosaicShell.Core/Modules/Tessera/TesseraNativeTranslucencyPolicy.cs): single-shell acrylic
- [`TesseraNativeTranslucencyStackedPolicy`](../../host/MosaicShell.Core/Modules/Tessera/TesseraNativeTranslucencyStackedPolicy.cs): H3 N-window stacked acrylic
- [`TesseraFlyoutOutsideClickPolicy`](../../host/MosaicShell.Core/Modules/Tessera/TesseraFlyoutOutsideClickPolicy.cs): union bounds
- [`TesseraFlyoutGlassPolicy`](../../host/MosaicShell.Core/Modules/Tessera/TesseraFlyoutGlassPolicy.cs): `NativeTranslucency` mode
- Tests: [`TesseraNativeTranslucencyPolicyTests`](../../host/MosaicShell.Core.Tests/TesseraNativeTranslucencyPolicyTests.cs), [`TesseraNativeTranslucencyStackedPolicyTests`](../../host/MosaicShell.Core.Tests/TesseraNativeTranslucencyStackedPolicyTests.cs), [`TesseraNativeTranslucencySignOffPolicyTests`](../../host/MosaicShell.Core.Tests/TesseraNativeTranslucencySignOffPolicyTests.cs)
