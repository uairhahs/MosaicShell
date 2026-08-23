# YourFlyouts reference screenshots

Canonical gallery: https://github.com/Jax-Core/YourFlyouts/blob/main/Screenshots.md

In-repo Host proofs (CI companion for `tessera_layout_fidelity`):

- Signed-off Host captures: `.github/res/Tessera/{StyleId}.png` (all 11 StyleCatalog ids)

Local mirrors for manual layout work (not automated in CI):

- Upstream cropped refs: `.local/Tessera/original/ref-{style}.png`
- Full-desktop context: `.local/Tessera/yourflyouts-official/{style}.png`
- Rainmeter sources: `.local/Tessera/original/{Style}.inc`, `Vars/{Style}.inc`

Per-style sign-off is tracked in `TesseraLayoutCoverage` (`IsLayoutFidelitySignedOff` / `IsLayoutFidelityDeviated`).
`tessera_layout_fidelity` in `HubParityBacklogTests` is **true**: every style is signed off.

Statistical annotation of deviated refs (Pillow, via **uv**):

```powershell
cd tools/tessera-ref
uv sync
uv run analyze_deviated.py
```

Outputs: `.local/Tessera/deviated/analysis/` (`*_annotated.png`, `deviated_stats.json`).

See [screenshots/README.md](screenshots/README.md) if you want to archive before/after PNG pairs in-repo.
