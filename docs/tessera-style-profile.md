# Tessera style profile (per-style capability consolidation)

`TesseraFlyoutStyleProfile` — the record struct returned by
`TesseraFlyoutTweenTargetCatalog.ResolveProfile` in
[`host/MosaicShell.Core/Modules/Tessera/TesseraFlyoutTweenTargetCatalog.cs`](../host/MosaicShell.Core/Modules/Tessera/TesseraFlyoutTweenTargetCatalog.cs)
— is the **single source of truth** for "what does style X do".

Rule and rationale: [`development.md`](development.md) → "Style-capability rule". This page is the concrete map: check it **before** writing another
`switch (styleId)`.

## The rule in one line

A new fact about a style is a **new field on the profile**. Every consumer is a one-line accessor.
There is exactly one `switch (styleId)` in the Tessera module, and it lives in `ResolveProfile`.

## Why this page exists

The question "what does style X do" was being answered independently in ~25 places across ~10
files, with nothing checking they agreed. That produced a real defect:
`TesseraRevealHostFactory.WrapMediaFromCatalog` forced `FullMediaWidth` to `NaN` for MaterialYou
while `ResolveProfile(MaterialYou).MediaWidthDip` already held `60`, and a third site
(`ResolveMaterialYouMediaSlideOffsetDip`) hardcoded `60` again. Three places knowing one number,
one of them lying. No test failed.

## Profile fields

| Field                                | Answers                                               |
| ------------------------------------ | ----------------------------------------------------- |
| `Targets`                            | Which `Group=Animated` meters/channels phase 2 drives |
| `RevealKind`                         | Which `Apply*Reveal` path renders this style          |
| `VolumeWidthDip` / `VolumeHeightDip` | Rest size of the volume panel                         |
| `MediaWidthDip` / `MediaHeightDip`   | Rest size of the media panel (`NaN` when unused)      |

Fields are added per consolidation batch (see below); this table tracks what exists today.

## Migration inventory

Each row is a method that currently decides per-style behavior on its own, and must become an
accessor over a profile field. Batch order matches the remediation plan.

| Batch     | File                              | Members to fold in                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| --------- | --------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1 done    | Host `TesseraRevealHostFactory`   | `WrapMediaFromCatalog` in-layout-media special case — **done**: call site now reads `profile.MediaWidthDip`, and `ResolveMaterialYouMediaSlideOffsetDip` takes the width instead of restating the constant, so the profile value is load-bearing                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| 2 done    | `TesseraFlyoutRevealSpec`         | **done, by deletion.** `ResolveMediaWidthFactor`, `ResolveShellHeightFactor`, `ResolveMediaClipHeightFactor`, `ResolveContentScale`, `ResolvePlainTextSlideFactor` had zero production callers — a dead parallel table superseded by `TesseraFlyoutAnimatedTargetSpec`'s DIP methods (which Host actually calls, and which already carry equivalent test coverage). Two were degenerate: `ResolveMediaClipHeightFactor` returned `revealProgress` on every arm, and `ResolveShellHeightFactor` was identical to `ResolveContentScale`. `ResolveHostKind` was already a profile accessor. `ResolveDividerScale` / `ResolveMediaOpacity` / `ResolveHorizontalReveal` are live (legacy `RevealKind.None` path) and stay. |
| 3 partial | `TesseraFlyoutHwndRegionSpec`     | `StyleNeedsRevealRegion` — **done**: derives from `StyleSupportsPhase2 && !StylePhase2WithoutMediaStrip` instead of a 9-style hand-listed chain. `StyleNeedsStrokeBRegion`, `ResolveRevealRegionDip` — not yet folded in                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |
| 4 partial | `TesseraLayoutCoverage`           | `UsesStackedMediaStrip` — **done**: derives from `StylePhase2UsesInLayoutMedia` instead of a MaterialYou-only inversion. `IsPolished` / `IsApproximate`, `IsLayoutFidelitySignedOff` / `IsLayoutFidelityDeviated`, `RequiresLiveVolumePercentLabel` — these are human sign-off/status records, not structural style facts; likely belong with the Exceptions below rather than this migration, pending confirmation                                                                                                                                                                                                                                                                                                   |
| 5 pending | `TesseraStackedPlacementPolicy`   | `ResolveLayoutKind`, `SupportsStackedOsAcrylic`, `ComputePlacements`, `VerticalGapForStyle`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| 6 pending | `TesseraOsAcrylicStackedPolicy`   | `ResolvePanelCornerRadiusDip`, `IsCoreUiMultiTile`, `UseMultiWindow`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| 7 pending | `TesseraOsAcrylicSignOffPolicy`   | `H3StackedStylesSignedOff` — **confirm first**, may be a deliberate manual QA gate (see Exceptions)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| 8 partial | `TesseraFlyoutTweenTargetCatalog` | `StyleIsPhase2NoOp` — **done**: derives from `!StyleSupportsPhase2`. `StyleRequestsVolumeMediaChrome` — **done**: derives from whether the profile's `Targets` include a `Media*` meter (this also fixed a real bug: the prior form was `UsesStackedMediaStrip \|\| UsesInLayoutMedia`, which is `!X \|\| X` — always `true`, so Square and Radial wrongly advertised media chrome). `StylePhase2WithoutMediaStrip`, `StylePhase2UsesInLayoutMedia` — not yet folded in                                                                                                                                                                                                                                               |
| 8 pending | `TesseraStatusFlyoutPolicy`       | `ResolveChipCornerRadiusDip` — not yet folded in                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |

Host `TesseraRevealHost.ApplyReveal` holds a second copy of this branching one layer down. It is
consolidated onto the same profile in its own phase, after the Core batches land.

## Enforcement

`TesseraFlyoutStyleProfileConsistencyTests` iterates `StyleCatalog.IdsFor("Tessera")` and asserts
cross-table invariants — two sources of truth disagreeing for any style fails the build. Invariants
are added per batch as the fields they relate appear. Examples:

- A style reporting `UsesInLayoutMedia` must not also report `UsesStackedMediaStrip`.
- A style with a real `MediaWidthDip` either uses it, or explicitly opts into in-layout media — it
  cannot be silently forked to `NaN` elsewhere.

If you add a profile field and no invariant relates it to anything, that is fine. If you add a
field that _duplicates_ an answer another field already implies, add the invariant.

## Styles

The 11 ids in `StyleCatalog.IdsFor("Tessera")`: Fluent, Windows11, Gnome, Square, CoreUI,
PlainText, Meter, Compact, ModernFlyouts, MaterialYou, Radial.

Two are structurally unlike the rest and are where drift concentrates:

- **MaterialYou** — the only in-layout media style (single HWND; media is a column inside the
  volume panel, not a stacked slot). Three tables already special-case it consistently; it has no
  explicit stacked-placement entry and falls through to a default.
- **Radial** — `Smouti.inc` comments out the TweenNode1 binders, but Host addresses the ring
  (`RingSweep`) and side media directly, so it is phase-2 capable, not a no-op. `StyleIsPhase2NoOp`
  is now derived (`!StyleSupportsPhase2`), so this no longer needs cross-checking by hand; it has
  no explicit stacked-placement-kind entry yet and falls through to a default there.

## Exceptions and known recurrences

Deliberately **not** consolidated. Each was considered; the reason is recorded so it does not read
as an oversight.

| Item                                                               | Why not                                                                                                                                                                                                                                                                  |
| ------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `TesseraOsAcrylicSignOffPolicy.H3StackedStylesSignedOff`           | Plausibly a human QA sign-off record, not a structural fact about a style. Confirm intent before folding in; if it stays separate, that is a decision, not a miss.                                                                                                       |
| Host `PhonoStyleFactory`, `ChronoStyleFactory`                     | Same "decide per-style locally" shape, but neither module has a profile type. Consolidating means designing a new abstraction for a different subsystem — premature per `development.md`. Revisit only if those modules get their own pass.                              |
| Phase-2 wipe-region animation path                                 | Stays on the manual step loop. An HWND-resize-based phase 2 was tried and abandoned (SoftFrost transparent swapchain clears to black on `SetWindowPos`); see `Phase2MustNotResizeHwnd` / `RelayoutAllowedDuringPhase2`. Unifying it needs its own spike, not a drive-by. |
| `AvaloniaFlyoutPresenter` synchronous `Dispatcher.UIThread.Invoke` | Fixing it means widening `IHostUiBridge` / `IFlyoutPresenter` from `Action`-based to `Func<Task>`-based repo-wide — out of proportion to a non-bug idiom nit. Known, understood deviation.                                                                               |
