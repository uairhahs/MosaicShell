# Chord (native-only)

Chord runs as an **Avalonia capability** inside `MosaicShell.Host` - not as a Rainmeter skin.

| Path | Role |
|------|------|
| `host/MosaicShell.Core/Capabilities/BuiltIn/` | Arm / hotkey (default Ctrl+Alt+K) |
| `host/MosaicShell.Host/Tiles/Surfaces/` | Named macro actions overlay |
| This folder | Install stub so `install-module Chord` creates `Modules/Chord` |

## Install / arm

```powershell
cd host
dotnet run --project Mosaicist -- install-module Chord
dotnet run --project MosaicShell.Host
```

In Host: Library → Chord → Arm, then press the configured hotkey.

Promised Rainmeter-era behavior: [`docs/legacy/chord.md`](../../docs/legacy/chord.md).
