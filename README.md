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

MosaicShell is a configurable desktop shell built from composable surfaces. It includes widgets, utilities, and workflows you arrange to fit how you work.

The hub handles settings, module management, and updates. Each tile installs and updates on its own.

Forked from [Jax-Core/JaxCore](https://github.com/Jax-Core/JaxCore), archived November 2024.

---

## Prerequisites

| Requirement | Minimum |
|-------------|---------|
| OS | Windows 10 x64 or later |
| .NET SDK | 8.0 (native host) |
| RAM | 6 GB |

**Primary (native Avalonia host):**

```powershell
cd host
dotnet test MosaicShell.Core.Tests
dotnet run --project Mosaicist -- install-module Canvas
dotnet run --project MosaicShell.Host
```

See [docs/architecture-native.md](docs/architecture-native.md) and [docs/native-rewrite.md](docs/native-rewrite.md).

The classic Rainmeter + `RunMosaicist.ps1 -Local` path remains available for legacy skins. **Do not use `iwr|iex`** (that path is blocked in shipping installs)

## Installation

Run the following command in PowerShell to install the latest version of MosaicShell.

> To launch PowerShell as Administrator: `Win + R` → type `powershell` → press `Ctrl + Shift + Enter`

```powershell
iwr -useb "https://raw.githubusercontent.com/uairhahs/MosaicShell/master/RunMosaicist.ps1" | iex
```

---

## Tiles

Tiles are Rainmeter skins bundled with MosaicShell under `Tiles/`.

| Tile | Description | License |
|------|-------------|---------|
| Tessera | System flyout replacements for volume, brightness, and media | MPL-2.0 |
| Mixdeck | Per-app audio mixer overlay | MPL-2.0 |
| Inlay | Start menu replacement with hot apps, shortcuts, and modules | MPL-2.0 |
| Slate | Idle / lock screen skin | MPL-2.0 |
| Chord | Keyboard-driven app launcher | MPL-2.0 |
| Substrate | Notification shade / control center | MPL-2.0 |
| Pulse | Audio visualizer with bar, round, and vector styles | MIT |
| Chrono | Clock collection with multiple display styles | MIT |
| Phono | Media player widget with multiple layouts | MIT |
| Canvas | Minimal plain-text information widget | MIT |

---

## Credits

### Plugins

| Plugin | Creator |
|--------|---------|
| [AudioAnalyzer](https://forum.rainmeter.net/viewtopic.php?t=31091) | rxtd |
| [FrostedGlass](https://forum.rainmeter.net/viewtopic.php?t=23106) | theAzack9 |
| [FileChoose](https://forum.rainmeter.net/viewtopic.php?t=33767) | SetSukka |
| [magickmeter](https://github.com/khanhas/MagickMeter) | [@khanhas](https://github.com/khanhas) |
| [ConfigActive](https://forum.rainmeter.net/viewtopic.php?t=28720) | jsMorley |
| [Focus](https://forum.rainmeter.net/viewtopic.php?t=37989) | [@deathcrafter](https://github.com/deathcrafter) |
| [Mouse](https://github.com/NighthawkSLO/Mouse.dll/) | [@NighthawkSLO](https://github.com/NighthawkSLO) |
| [MouseXY](https://forum.rainmeter.net/viewtopic.php?t=22900) | Fawxy |
| [PowershellRM](https://github.com/khanhas/PowershellRM) | [@khanhas](https://github.com/khanhas) |
| [ShowInToolbar](https://forum.rainmeter.net/viewtopic.php?t=25334) | theAzack9 |
| [HotKey](https://github.com/brianferguson/HotKey.dll) | [@brianferguson](https://github.com/brianferguson) |
| [Chameleon](https://github.com/socks-the-fox/Chameleon) | socks-the-fox |
| [IsFullScreen](https://forum.rainmeter.net/viewtopic.php?t=28305) | jsMorley |
| [WebNowPlaying](https://github.com/tjhrulz/WebNowPlaying) | Rainmeter team |
| [Drag&Drop](https://forum.rainmeter.net/viewtopic.php?t=23107) | theAzack9 |
| [MediaPlayer](https://github.com/i2002/RainmeterMediaPlayer) | [@i2002](https://github.com/i2002) |
| [AppVolume](https://github.com/khanhas/AppVolumePlugin) | Original [@khanhas](https://github.com/khanhas), remastered [@deathcrafter](https://github.com/deathcrafter) |
| [TrayIcon](https://github.com/deathcrafter/PluginTrayIcon) | [@deathcrafter](https://github.com/deathcrafter) |
| [SysColor](https://github.com/brianferguson/SysColor.dll) | [@brianferguson](https://github.com/brianferguson) |

### Technologies

| Technology | Creator |
|------------|---------|
| [AutoHotkey](https://www.autohotkey.com/) | AHK Team |
| [RainRGB](https://forum.rainmeter.net/viewtopic.php?t=6215) | jsMorley |

### Original project

MosaicShell is a fork of [JaxCore](https://github.com/Jax-Core/JaxCore) by [@EnhancedJax](https://github.com/EnhancedJax), archived November 2024. The original modules, plugin integrations, and installer architecture are his work.

---

## Contributing

Issues and pull requests are welcome. If you are building a module or widget compatible with MosaicShell, open an issue to discuss integration.

---

## License

See [LICENSE](./LICENSE).
