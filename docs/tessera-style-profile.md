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

| Field | Answers |
| --- | --- |
| `Targets` | Which `Group=Animated` meters/channels phase 2 drives |
| `RevealKind` | Which `Apply*Reveal` path renders this style |
| `VolumeWidthDip` / `VolumeHeightDip` | Rest size of the volume panel |
| `MediaWidthDip` / `MediaHeightDip` | Rest size of the media panel (`NaN` when unused) |
| `LayoutKind` | H3 stacked cluster shape (horizontal/vertical, who goes first) |
| `SupportsStackedOsAcrylic` | Eligible for H3 multi-window OS acrylic |
| `StackedGapDip` | Gap used by the `VerticalVolumeFirst` layout kind |
| `VolumeCornerRadiusDip` / `MediaCornerRadiusDip` | Design corner radius per stacked panel role, pre-cap |
| `SupportsCoreUiMultiTile` | Is this the CoreUI multi-tile style |
| `PhaseTwoWithoutMediaStrip` | Phase 2 runs on the volume card with no media strip (Square) |
| `PhaseTwoUsesInLayoutMedia` | Media column lives in the volume HWND, not a stacked strip |
| `StatusChipCornerRadiusDip` | Status chip corner radius |
| `NeedsStrokeBRegion` | Needs a dedicated StrokeB HWND region (Windows11 only) |
| `RequiresMatteChrome` | Chrome doesn't cover its full window bounds (MaterialYou's pills, PlainText's slant-clipped card), so it must never request OS Acrylic or Skia soft-frost glass - a failed OS composite in the unpainted gap would show raw desktop passthrough |

Fields are added per consolidation batch (see below); this table tracks what exists today.

## Migration inventory

Each row is a method that currently decides per-style behavior on its own, and must become an
accessor over a profile field. Batch order matches the remediation plan.

| Batch | File | Members to fold in |
| --- | --- | --- |
| 1 done | Host `TesseraRevealHostFactory` | `WrapMediaFromCatalog` in-layout-media special case — **done**: call site now reads `profile.MediaWidthDip`, and `ResolveMaterialYouMediaSlideOffsetDip` takes the width instead of restating the constant, so the profile value is load-bearing |
| 2 done | `TesseraFlyoutRevealSpec` | **done, by deletion.** `ResolveMediaWidthFactor`, `ResolveShellHeightFactor`, `ResolveMediaClipHeightFactor`, `ResolveContentScale`, `ResolvePlainTextSlideFactor` had zero production callers — a dead parallel table superseded by `TesseraFlyoutAnimatedTargetSpec`'s DIP methods (which Host actually calls, and which already carry equivalent test coverage). Two were degenerate: `ResolveMediaClipHeightFactor` returned `revealProgress` on every arm, and `ResolveShellHeightFactor` was identical to `ResolveContentScale`. `ResolveHostKind` was already a profile accessor. `ResolveDividerScale` / `ResolveMediaOpacity` / `ResolveHorizontalReveal` are live (legacy `RevealKind.None` path) and stay. |
| 3 done | `TesseraFlyoutHwndRegionSpec` | `StyleNeedsRevealRegion` — **done**: derives from `StyleSupportsPhase2 && !StylePhase2WithoutMediaStrip` instead of a 9-style hand-listed chain. `StyleNeedsStrokeBRegion` — **done**: one-line accessor over the new `NeedsStrokeBRegion` field (true only for Windows11). `ResolveRevealRegionDip` — **done**: its style-identity dispatch now switches on the profile's already-resolved `RevealKind` instead of re-deriving style identity via a second normalize-and-compare chain; the per-kind geometry formulas themselves are real runtime computation (progress-dependent clip/slide math), not a per-style fact table, so they correctly stay as computation rather than becoming profile fields |
| 4 partial | `TesseraLayoutCoverage` | `UsesStackedMediaStrip` — **done**: derives from `StylePhase2UsesInLayoutMedia` instead of a MaterialYou-only inversion. `IsPolished` / `IsApproximate`, `IsLayoutFidelitySignedOff` / `IsLayoutFidelityDeviated`, `RequiresLiveVolumePercentLabel` — these are human sign-off/status records, not structural style facts; likely belong with the Exceptions below rather than this migration, pending confirmation |
| 5 done | `TesseraStackedPlacementPolicy` | `ResolveLayoutKind`, `SupportsStackedOsAcrylic`, `VerticalGapForStyle` — **done**: one-line accessors over the new `LayoutKind` / `SupportsStackedOsAcrylic` / `StackedGapDip` fields. `ComputePlacements`/`EstimatePlacements` — the placement-shape functions (`Win11Placements`, `GnomePlacements`, `PlainTextPlacements`, `MeterPlacements`, `RadialPlacements`, `CompactPlacements`/`ModernFlyoutsPlacements` via `VerticalVolumeFirstPlacements`) now read their volume/media rest sizes from the profile instead of restating `TesseraStackedPlacementSpec` constants a second time; the actual cluster-shape math (horizontal/vertical row layout, centering, radial spread) stays as computation since it is genuinely runtime geometry, not a per-style lookup. Fluent's volume-slot width is a documented, verified exception: it legitimately includes the divider-column width (`ResolveFluentCollapsedShellWidthDip`) that the profile's plain rest width does not carry |
| 6 done | `TesseraOsAcrylicStackedPolicy` | `ResolvePanelCornerRadiusDip` — **done**: one-line accessor over the new `VolumeCornerRadiusDip` / `MediaCornerRadiusDip` fields. `IsCoreUiMultiTile` — **done**: one-line accessor over the new `SupportsCoreUiMultiTile` field, gated by the (global, not per-style) `CoreUiMultiWindowEnabled` kill switch. `UseMultiWindow` — already correctly delegates to `TesseraStackedPlacementPolicy.SupportsStackedOsAcrylic`, which batch 5 made a profile accessor; no further change needed |
| 7 pending | `TesseraOsAcrylicSignOffPolicy` | `H3StackedStylesSignedOff` — **confirm first**, may be a deliberate manual QA gate (see Exceptions) |
| 8 done | `TesseraFlyoutTweenTargetCatalog` | `StyleIsPhase2NoOp` — **done**: derives from `!StyleSupportsPhase2`. `StyleRequestsVolumeMediaChrome` — **done**: derives from whether the profile's `Targets` include a `Media*` meter (this also fixed a real bug: the prior form was `UsesStackedMediaStrip \|\| UsesInLayoutMedia`, which is `!X \|\| X` — always `true`, so Square and Radial wrongly advertised media chrome). `StylePhase2WithoutMediaStrip`, `StylePhase2UsesInLayoutMedia` — **done**: one-line accessors over the new `PhaseTwoWithoutMediaStrip` / `PhaseTwoUsesInLayoutMedia` fields |
| 8 done | `TesseraStatusFlyoutPolicy` | `ResolveChipCornerRadiusDip` — **done**: one-line accessor over the new `StatusChipCornerRadiusDip` field |

Host `TesseraRevealHost.ApplyReveal` holds a second copy of this branching one layer down. It is
consolidated onto the same profile in its own phase, after the Core batches land.

Batch 7 is the only Core batch left unstarted, deliberately: `H3StackedStylesSignedOff` looks like
a manual QA sign-off record rather than a structural style fact, and the plan calls for confirming
that reading before folding it in. Batch 4's four sign-off/status fields carry the same open
question. Everything else the remediation plan named for Core (batches 1, 2, 3, 5, 6, 8) is done,
each pinned by an invariant in `TesseraFlyoutStyleProfileConsistencyTests` so the profile and its
former hand-written switches cannot drift apart again.
