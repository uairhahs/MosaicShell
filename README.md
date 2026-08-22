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

MosaicShell is a configurable desktop shell built from composable surfaces. The **Avalonia Host** manages settings, module install, and armed capabilities; each tile is a native capability or widget.

Forked from [Jax-Core/JaxCore](https://github.com/Jax-Core/JaxCore), archived November 2024. Rainmeter-era promises are archived under [docs/legacy/](docs/legacy/).

---

## Prerequisites

| Requirement | Minimum |
|-------------|---------|
| OS | Windows 10 x64 or later |
| .NET SDK | 8.0 |
| RAM | 6 GB |

## Install (Host)

```powershell
cd host
dotnet test MosaicShell.Core.Tests
dotnet run --project Mosaicist -- install-module Tessera
dotnet run --project Mosaicist -- install-module Mixdeck
dotnet run --project MosaicShell.Host
```

See [docs/architecture-native.md](docs/architecture-native.md), [docs/native-rewrite.md](docs/native-rewrite.md), and [docs/parity/README.md](docs/parity/README.md).

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

MosaicShell is a fork of [JaxCore](https://github.com/Jax-Core/JaxCore) by [@EnhancedJax](https://github.com/EnhancedJax), archived November 2024. Historical Rainmeter plugin credits: [docs/legacy/README.md](docs/legacy/README.md).

---

## Contributing

Issues and pull requests are welcome. If you are building a module or widget compatible with MosaicShell, open an issue to discuss integration.

---

## License

See [LICENSE](./LICENSE).
