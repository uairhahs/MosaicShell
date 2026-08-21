# MosaicShell

> _Your desktop, composed._

<p align="center">
  <img src="https://raw.githubusercontent.com/uairhahs/MosaicShell/master/.github/res/logo-variants/compact-256.png" alt="MosaicShell" width="120" height="120" />
  <br />
</p>

<p align="center">
  <img alt="Version" src="https://img.shields.io/github/v/release/uairhahs/MosaicShell?label=Version&style=for-the-badge" />
  <img alt="Downloads" src="https://img.shields.io/github/downloads/uairhahs/MosaicShell/total?style=for-the-badge" />
  <img alt="Last Update" src="https://img.shields.io/github/release-date/uairhahs/MosaicShell?label=Last%20Update&style=for-the-badge" />
  <img alt="License" src="https://img.shields.io/github/license/uairhahs/MosaicShell?style=for-the-badge" />
</p>

---

## About

MosaicShell is a modular desktop layer for widgets, utilities, and workflows. Arrange the pieces, tune the surface, and build an environment that fits the way you work.

The central hub provides quick access to settings, module management, and updates across the entire ecosystem. Each module can be installed, configured, and updated independently without affecting the others.

Forked from [Jax-Core/JaxCore](https://github.com/Jax-Core/JaxCore), archived November 2024.

---

## Prerequisites

| Requirement | Minimum |
|-------------|---------|
| OS | Windows 10 x64 or later |
| RAM | 6 GB |
| CPU | 4 cores |
| PowerShell | v5.1 or later — [upgrade here](https://docs.microsoft.com/en-us/powershell/scripting/windows-powershell/install/installing-windows-powershell?view=powershell-7.2#upgrading-existing-windows-powershell) |

## Installation

Run the following command in PowerShell to install the latest version of MosaicShell.

> To launch PowerShell as Administrator: `Win + R` → type `powershell` → press `Ctrl + Shift + Enter`

```powershell
iwr -useb "https://raw.githubusercontent.com/uairhahs/MosaicShell/master/CoreInstaller.ps1" | iex
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
| Shade | Notification shade inspired by MIUI | MPL-2.0 |
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
