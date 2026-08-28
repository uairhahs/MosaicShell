# Tessera H3 (a): N FlyoutWindows for stacked OS acrylic

**Decision (2026-08-23):** option **(a)** from [tessera-os-acrylic-spike.md](tessera-os-acrylic-spike.md). Each logical panel gets its own top-level `FlyoutWindow` with rounded OS acrylic. Option (b) one-HWND inner seams is rejected.

**Prerequisite:** H2 PASS on Win11 (signed 2026-08-23).

## Goal

Under `--tessera-os-acrylic`, stacked volume+media (`showMediaStrip=1`) and later CoreUI multi-tile shells render as **N separate acrylic HWNDs** instead of one Skia frost shell.

Alpha default stays frost (flag off). Hub **Windows 11 OS acrylic** toggle persists `UseOsAcrylic` when Win11 eval is signed off. Do not flip Hub `*_mvp` or `tessera_layout_fidelity` for acrylic.

## Core contracts (phase 0, in tree)

| Type                                                                                                                | Role                                                                         |
| ------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| [`TesseraOsAcrylicStackedPolicy`](../../host/MosaicShell.Core/Modules/Tessera/TesseraOsAcrylicStackedPolicy.cs)     | When N-window mode applies; panel roles; slot keys; z-order; live-host owner |
| [`TesseraFlyoutOutsideClickPolicy`](../../host/MosaicShell.Core/Modules/Tessera/TesseraFlyoutOutsideClickPolicy.cs) | Union bounds for multi-window outside-click                                  |
| [`TesseraOsAcrylicTrialPolicy`](../../host/MosaicShell.Core/Modules/Tessera/TesseraOsAcrylicTrialPolicy.cs)         | Single-shell acrylic only (`showMediaStrip=0`)                               |

**Mutual exclusion:** `IsEligible` (single OsAcrylic) and `UseMultiWindow` (stacked OsAcrylic) cannot both be true for the same payload.

## Phased Host work

### Phase 1: volume+media strip (priority)

**Styles:** Fluent, Windows11, Meter, Radial, Square, Compact, ModernFlyouts, Gnome (any layout using `ShowMediaStrip` + horizontal stack).

**Panels:** `Volume` + `Media` (two HWNDs).

**Host seams to extend:**

1. **`AvaloniaFlyoutPresenter`**
   - Register windows by slot key (`Tessera:vol`, `Tessera:media`) not only `Tessera`.
   - `ShowOrUpdateCore`: when `UseMultiWindowFromPayload`, split `BuildContent` into per-panel roots; Present all slots; Patch volume slot only for high-frequency updates.
   - `IsVisible("Tessera")`: true if any Tessera slot is effectively showing.
   - `Hide` / `TransientDismiss`: hide all slots in the session.
   - `PresentMustRestackAllSlots`: restack every slot above FocusDim (reuse `RestackAboveDim` pattern).

2. **Layout split helpers (Host)**
   - Extract volume-only and media-only builders from existing `TesseraLayouts.*` stacked branches (no duplicate VM; shared `TesseraFlyoutViewModel`).
   - Volume slot: `TesseraLiveHost` + pump owner.
   - Media slot: satellite panel; timeline/scrub bindings via shared VM + live ambient.

3. **`TesseraOutsideClickWatcher`**
   - Accept union of multiple `FlyoutWindow` bounds when `MustUseUnionBounds`.
   - Refresh union on Present; Patch does not re-arm (existing live-sync contract).

4. **Placement**
   - Anchor from `FlyoutRequest` (monitor, XPad, YPad, AniDir).
   - Style-specific horizontal offsets so volume+media visually match current single-HWND layout (measure from existing stacked layouts).

5. **Glass**
   - Each slot: `TesseraFlyoutGlassMode.OsAcrylic`, skip inner Skia slab (same as H1 single-shell).
   - Meter inner glass suppression applies per slot.

### Phase 2: CoreUI multi-tile

Flip `TesseraOsAcrylicStackedPolicy.CoreUiMultiWindowEnabled` after phase 1 sign-off.

**Panels:** `Device` + `Volume` + `Media` (three HWNDs).

Same presenter/session rules as phase 1.

## Session invariants (must hold)

| Invariant                                                 | Source                                         |
| --------------------------------------------------------- | ---------------------------------------------- |
| One FocusDim per Tessera session                          | Existing `SyncFocusDim`                        |
| All flyout slots above FocusDim                           | `PresentMustRestackAllSlots`                   |
| Outside click outside union → transient dismiss all slots | `TesseraFlyoutOutsideClickPolicy`              |
| Transient dismiss hides all slots, does not Close         | `TransientDismissMustHideAllSlots` + live-sync |
| Each slot reuses its own HWND                             | `MustReuseRegisteredFlyoutHwndPerSlot`         |
| Volume slot owns live pump                                | `LiveHostOwner`                                |
| High-frequency Update coalesces before UI Post            | `TesseraFlyoutUpdateDispatchGate`              |

## Manual sign-off (H4 ship gate)

**Status: PASS (2026-08-23, Win11).** Styles: Meter, Gnome, Compact, Modern Flyouts (volume+media strip).

Record under `.local/Tessera/os-acrylic-eval/h3-n-windows/`:

| Check                       | Pass criteria                                                          | 2026-08-23 |
| --------------------------- | ---------------------------------------------------------------------- | ---------- |
| Two acrylic corners visible | Volume and media each show rounded OS acrylic on Win11                 | Pass       |
| Z-order                     | Volume thumb usable; no slot paints under FocusDim                     | Pass       |
| Outside click               | Click outside both panels dismisses; click inside either keeps session | Pass       |
| FocusDim                    | Dim + all slots dismiss together on timer / outside click              | Pass       |
| Live pump                   | Media timeline advances; volume keys/wheel update volume slot          | Pass       |
| Rapid volume drag           | No dispatcher flood; no acrylic brush death                            | Pass       |
| Restyle / Try now           | Slot HWNDs reuse; no orphan SoftFrost stack                            | Pass       |
| Frost fallback              | Flag off or `--tessera-software-render` → stacked frost unchanged      | Pass       |

## TDD loop per phase

1. Extend Core policy + failing test in `MosaicShell.Core.Tests`.
2. Implement minimal Host change to go green.
3. Manual Win11 row in table above.
4. Only then enable next style or CoreUI phase 2.

## Related

- Spike tracker: [tessera-os-acrylic-spike.md](tessera-os-acrylic-spike.md)
- H2 eval: `.local/Tessera/os-acrylic-eval/README.md`
