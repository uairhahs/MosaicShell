# Layout fidelity screenshot checklist

Side-by-side references for flipping `*_layout_fidelity` flags in `HubParityBacklogTests`.  
Keep every flag **false** until the matching row is checked with PNG evidence in this folder.

Archive references: [Jax-Core](https://github.com/Jax-Core) module READMEs and [docs/legacy/](../legacy/).

| Module | Flagship style | Host surface | Jax-Core reference | Checklist |
|--------|----------------|--------------|-------------------|-----------|
| Tessera | Pixel + Fluent | `TesseraLayouts` | YourFlyouts | [ ] Pixel media row matches density [ ] Fluent shell frost [ ] Win11/Center variants |
| Chrono | Center | `ChronoStyleFactory` | ModularClocks | [ ] Arc decoration [ ] Date block typography [ ] Text/Tech chrome variants |
| Phono | Simple | `PhonoStyleFactory` | ModularPlayers | [ ] Art + transport horizontal [ ] Center/Win11 vertical presets |
| Pulse | Regular | `PulseTileView` | ModularVisualizer | [ ] Bar spectrum layout [ ] Gradient/Chroma tinting |
| Canvas | DEFAULT | `CanvasTileView` | Plainext | [ ] Section density vs Compact |
| Mixdeck | Fluent | `MixdeckTileView` | YourMixer | [ ] Color scheme palettes [ ] Session list polish |
| Inlay | Win11 | `InlayTileView` | ValliStart | [ ] Two-column pins [ ] Catalog search |
| Chord | Center | `ChordTileView` | Keylaunch | [ ] Action list [ ] Animation presets |
| Substrate | DEFAULT | `SubstrateTileView` | MIUI-Shade | [ ] SMTC mini-tile [ ] Quick settings grid |
| Slate | Center | `SlateTileView` | IdleStyle | [ ] Idle date hint [ ] Custom media deferred |

## How to add proof

1. Capture Jax-Core archive screenshot (Rainmeter era) and Host overlay at comparable scale.
2. Save as `{Module}-{Style}-archive.png` and `{Module}-{Style}-host.png`.
3. Link the pair in a PR and only then flip the module's `*_layout_fidelity` flag.

Automated gate: `StyleCatalogCoverageTests` + per-module `*LayoutCoverage.CoversCatalog()` in Core tests.
