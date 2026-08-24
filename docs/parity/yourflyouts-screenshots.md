# YourFlyouts official style screenshots

Canonical gallery: https://github.com/Jax-Core/YourFlyouts/blob/main/Screenshots.md

Local mirrors (downloaded for layout QA): `.local/Tessera/yourflyouts-official/yf01.png` … `yf12.png`

Also see cropped refs in `.local/Tessera/ref*.png`.

Target on-desktop scale: flyouts are a small fraction of the screen (~1/5 width max). YourFlyouts used heavy acrylic; MosaicShell Host may keep soft frost / its own material language.

## Animation parity sign-off (Fancy / Ani=2)

Do **not** mark Tessera animation Hub parity until manual compare passes for:

| Style | Phase 1 (slide/fade) | Phase 2 (media reveal) | Reference |
|-------|----------------------|-------------------------|-----------|
| Fluent | Whole surface ~40ms stepped OutQuart | Media width grows after 100ms pause | `yf01.png`, `yf02.png` |
| Windows 11 | Same | Media clip height grows vertically | `yf03.png` |
| Gnome | Same | Scale 0.5..1 + opacity on media pill | layout gallery |
| PlainText | Same | Panel slide-in on media block | layout gallery |

Core contracts: `TesseraFlyoutAnimationPolicy`, `TesseraFlyoutTweenEngine`, `TesseraFlyoutRevealSpec`. Smouti and other no-op styles degrade to phase-1 only (matches YourFlyouts Smouti).

## Stacked OS acrylic (Fancy) known delta

When Hub **Stacked OS acrylic** is enabled, Tessera uses **split HWNDs** (volume + media slots). This path intentionally differs from YourFlyouts single-surface Fancy:

| Aspect | YourFlyouts (single skin) | MosaicShell stacked acrylic |
|--------|---------------------------|-----------------------------|
| Phase 1 | Whole card slide/fade | Both HWNDs animate in parallel (`PresenterDrivesMotion`) |
| Phase 2 | Shell border/XOR clip grows with media | Media HWND only: `TesseraRevealHost` clip on media panel |
| Win11 XOR / StrokeB | One border height tracks `(VolumeH+MediaH)*TweenNode1` | Volume HWND static; media HWND vertical clip only |
| Fluent StrokeB width | Static full width when music visible | Volume + media shells stay separate; no shared border growth |
| Dismiss | Single meter hide | `PrepareExitMotion` + parallel exit per slot; `Hide()` after fade (`TransientDismissMustHideAllSlots`) |

Do **not** expect stacked Fancy to match single-HWND shell clip parity. Single-HWND paths (Fluent, Win11, CoreUI without stacked acrylic) carry the P1-P3 binders in `TesseraRevealHost`.

Sign-off for stacked acrylic: phase-1 sync, clean dismiss (no acrylic ghost), media phase-2 clip on media slot only.
