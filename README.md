# MosaicShell

> _Your desktop, composed._

<p align="center">
  <img src=".github/res/logo-variants/compact-256.png" alt="MosaicShell" width="120" height="120" />
</p>

<p align="center">
  <img alt="Version" src="https://img.shields.io/github/v/tag/uairhahs/MosaicShell?label=Version&style=for-the-badge" />
  <img alt="Downloads" src="https://img.shields.io/github/downloads/uairhahs/MosaicShell/total?style=for-the-badge" />
  <img alt="Last Update" src="https://img.shields.io/github/release-date/uairhahs/MosaicShell?label=Last%20Update&style=for-the-badge" />
  <img alt="License" src="https://img.shields.io/github/license/uairhahs/MosaicShell?style=for-the-badge" />
</p>

---

## About

MosaicShell is a native Windows desktop shell built from composable tiles. The Avalonia **Host** installs modules, arms tray capabilities, and opens widget overlays, with no Rainmeter and no `.rmskin`.

It continues the JaxCore idea of a modular desktop (forked from [Jax-Core/JaxCore](https://github.com/Jax-Core/JaxCore), archived November 2024). Historical Rainmeter sources live in the [Jax-Core archives](https://github.com/Jax-Core).

---

## Prerequisites

| Requirement | Minimum |
|-------------|---------|
| OS | Windows 10 x64 or later |
| .NET SDK | 10.0 |
| RAM | 6 GB |

## Install (Host)

```powershell
cd host
dotnet test MosaicShell.Core.Tests
dotnet run --project Mosaicist -- install-module Tessera
dotnet run --project Mosaicist -- install-module Mixdeck
dotnet run --project MosaicShell.Host
```

See [host/README.md](host/README.md) and [`.github/docs/parity.md`](.github/docs/parity.md).

**Honest MVP vs fidelity:** `tile_*_mvp` flags mean wiring, settings, and flagship behavior slices are in place, not full Jax-Core visual parity. Tessera layout is signed off (`tessera_layout_fidelity`; proofs in [`.github/res/Tessera/`](.github/res/Tessera/)). Other `*_layout_fidelity` flags stay false until in-repo screenshot proofs exist (see [`.github/docs/parity.md`](.github/docs/parity.md)). Install modules from the bundled `Tiles/{Id}/` stub via `Mosaicist install-module <id>`.

---

## Tiles

Every catalog module ships as a thin `Tiles/{Id}` install stub (`module.native.json` + README). Runtime code lives under `host/`.

| Tile | Description | License |
|------|-------------|---------|
| Tessera | Volume / brightness / media flyouts (armed capability) | MPL-2.0 |
| Mixdeck | Per-app audio mixer overlay | MPL-2.0 |
| Inlay | Start-menu launcher (pins + search) | MPL-2.0 |
| Slate | Idle clock overlay | MPL-2.0 |
| Chord | Macro app launcher | MPL-2.0 |
| Substrate | Quick-settings shade | MPL-2.0 |
| Pulse | Audio visualizer widget | MIT |
| Chrono | Clock widget | MIT |
| Phono | SMTC media widget | MIT |
| Canvas | System-metrics text widget | MIT |

---

## Credits

### Original project

MosaicShell is a fork of [JaxCore](https://github.com/Jax-Core/JaxCore) by [@EnhancedJax](https://github.com/EnhancedJax), archived November 2024. Historical Rainmeter plugin credits: [Jax-Core](https://github.com/Jax-Core).

---

## Contributing

Issues and pull requests are welcome. If you are building a module or widget compatible with MosaicShell, open an issue to discuss integration.

**Development:** [`.github/docs/`](.github/docs/) (testing, parity honesty, scaling).

---

## License

See [LICENSE](./LICENSE).
