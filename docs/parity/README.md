# Tile MVP notes

Service-bound Avalonia surfaces (not Rainmeter `.ini` ports). Visual DLC and every plugin edge-case remain iterative.

| Tile | MVP delivered |
|------|----------------|
| Chrono | Styles Center/Minimal, 12/24h + seconds via settings JSON |
| Canvas | CPU/RAM/disk via `ISystemMetricsService` |
| Phono | SMTC title/artist + transport |
| Pulse | WASAPI loopback bands |
| Tessera | Master volume/mute + brightness + media title |
| Mixdeck | Live app sessions via NAudio |
| Inlay | Pin/search launch via ShellExecute |
| Chord | Fuzzy launch + hotkey setting stored |
| Substrate | QS mute/vol/brightness/settings URI |
| Slate | Large clock + fullscreen policy flag |

Manual checks: launch each from Library after `Mosaicist install-module <Id>`.
