# Parity honesty

MosaicShell is a native rewrite of [JaxCore](https://github.com/Jax-Core/JaxCore). We track **what is implemented** vs **what is still backlog** in code, not in marketing copy.

This page covers flag-naming conventions and current status. For the detailed per-module bar each `_mvp` flag must meet, see [`docs/parity/README.md`](../../docs/parity/README.md).

## Source of truth

[`host/MosaicShell.Core.Tests/HubParityBacklogTests.cs`](../../host/MosaicShell.Core.Tests/HubParityBacklogTests.cs)

- `HubCapabilities`: each string flag maps to `true` (shipped) or `false` (backlog).
- `CompanionProof`: every `true` flag that matters for Tessera/tiles should name a proof test.
- `Oversold_mvp_flags_are_false_until_bars_met`: guardrail test; do not weaken without intent.

Run: `dotnet test host/MosaicShell.Core.Tests --filter HubParity`

## Flag naming

| Suffix / pattern    | Meaning                                                                       |
| ------------------- | ----------------------------------------------------------------------------- |
| `*_skeleton`        | Catalog, settings, and Host wiring exist.                                     |
| `*_mvp`             | A JaxCore-comparable **behavior slice** is in place (not full visual parity). |
| `*_layout_fidelity` | Screenshot-level layout sign-off vs reference art.                            |
| `tessera_*`         | Tessera capability / flyout contracts.                                        |
| `service_*`         | Core Windows service adapters.                                                |
| `library_*`         | Mosaicist install + Hub Library.                                              |

**MVP is not “done”.** It means the tile or capability does its flagship job in native Host, with Core tests backing honesty.

## Tessera layout fidelity (signed off)

Flag: `tessera_layout_fidelity` = **true**

Proof PNGs (one per StyleCatalog id): [`.github/res/Tessera/`](../res/Tessera/)

Core registry: [`TesseraLayoutCoverage`](../../host/MosaicShell.Core/Modules/Tessera/TesseraLayoutCoverage.cs) (`LayoutFidelityProofRelativeDirectory`).

Styles: Meter, Square, CoreUI, Fluent, Gnome, ModernFlyouts, MaterialYou, PlainText, Compact, Windows11, Radial.

## Still false (do not oversell)

| Flag                          | Why false                                                                    |
| ----------------------------- | ---------------------------------------------------------------------------- |
| `native_tile_overlay_runtime` | Full historical StyleCatalog / DLC as a Rainmeter-style runtime interpreter. |
| `tessera_media_smtc_only`     | Media flyouts without WebNowPlaying merge (browser/YTM art path).            |
| `chrono_layout_fidelity`      | No in-repo screenshot proof pair yet.                                        |
| `phono_layout_fidelity`       | Same.                                                                        |
| `pulse_layout_fidelity`       | Same.                                                                        |
| `canvas_layout_fidelity`      | Same.                                                                        |
| `mixdeck_layout_fidelity`     | Same.                                                                        |
| `inlay_layout_fidelity`       | Same.                                                                        |
| `chord_layout_fidelity`       | Same.                                                                        |
| `substrate_layout_fidelity`   | Same.                                                                        |
| `slate_layout_fidelity`       | Same.                                                                        |

## Shipped highlights (true)

| Area            | Flags                                                                                                          |
| --------------- | -------------------------------------------------------------------------------------------------------------- |
| Hub + installer | `library_*`, `capability_daemon`, `install_never_uses_iex_or_executionpolicy_bypass`, `product_cutover_no_iex` |
| Tessera flyouts | `tessera_osd_flyout`, `tessera_locks_flight`, `tessera_live_update_multimonitor`, `tile_tessera_mvp`           |
| Tessera media   | `tessera_media_wnp` (SMTC + WNP merge)                                                                         |
| Tile MVPs       | `tile_*_mvp` for all ten catalog modules                                                                       |
| Services        | `service_*`, `os_media_audio_brightness_services`                                                              |

See the full list in `HubCapabilities`.

## How to flip a flag to true

1. Implement the behavior with a **Core contract** (`*Policy`, service, capability) and Host wiring that reads it.
2. Add or extend a **proof test**; register it in `CompanionProof` when the flag is user-visible parity.
3. For `*_layout_fidelity`, add tracked screenshot proof under `.github/res/{Module}/` (follow Tessera layout) and a StyleCatalog / coverage test.
4. Set the flag to `true` in `HubCapabilities` and keep `Oversold_mvp_flags_are_false_until_bars_met` green.

Do not flip `*_mvp` or `*_layout_fidelity` from a screenshot alone without tests.
