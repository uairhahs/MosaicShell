# YourFlyouts reference screenshots

Canonical gallery: https://github.com/Jax-Core/YourFlyouts/blob/main/Screenshots.md

Local mirrors for manual layout work (not automated in CI):

- Signed-off refs: `.local/Tessera/original/ref-{style}.png`
- Still deviating: `.local/Tessera/deviated/ref-{style}.png` (CoreUI, Pixel, Smouti, Win11)
- Full-desktop context: `.local/Tessera/yourflyouts-official/{style}.png`
- Rainmeter sources: `.local/Tessera/original/{Style}.inc`, `Vars/{Style}.inc`

Per-style sign-off is tracked in `TesseraLayoutCoverage` (`IsLayoutFidelitySignedOff` / `IsLayoutFidelityDeviated`).
Flip global `tessera_layout_fidelity` in `HubParityBacklogTests` only when every style is signed off.

Statistical annotation of deviated refs (Pillow, via **uv**):

```powershell
cd tools/tessera-ref
uv sync
uv run analyze_deviated.py
```

Outputs: `.local/Tessera/deviated/analysis/` (`*_annotated.png`, `deviated_stats.json`).

See [screenshots/README.md](screenshots/README.md) if you want to archive before/after PNG pairs in-repo.
