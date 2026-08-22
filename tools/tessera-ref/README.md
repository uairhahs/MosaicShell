# tessera-ref

Pillow utilities for Tessera flyout reference analysis. **Use [uv](https://docs.astral.sh/uv/) for all Python commands** — no bare `python` / `pip`.

## Setup

```powershell
cd tools/tessera-ref
uv sync
```

## Analyze deviated refs

Pre-cropped refs live in `.local/Tessera/deviated/`. This writes annotated PNGs + JSON stats to `.local/Tessera/deviated/analysis/`:

```powershell
cd tools/tessera-ref
uv run analyze_deviated.py
```

Custom paths:

```powershell
uv run analyze_deviated.py --in-dir ../../.local/Tessera/deviated --out-dir ../../.local/Tessera/deviated/analysis
```

## Outputs

| File | Description |
|------|-------------|
| `ref-{style}_annotated.png` | Full-UI bounding boxes + stats strip |
| `deviated_stats.json` | Dimensions, margins, dominant colors, per-region stats |
