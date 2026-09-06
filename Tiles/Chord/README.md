# Chord (native-only)

Chord runs as an **Avalonia capability** inside `MosaicShell.Host`.

| Path                                          | Role                              |
| --------------------------------------------- | --------------------------------- |
| `host/MosaicShell.Core/Capabilities/BuiltIn/` | Arm / hotkey (default Ctrl+Alt+K) |
| `host/MosaicShell.Host/Tiles/Surfaces/`       | Named macro actions overlay       |

## Install / arm

```powershell
cd host
dotnet run --project Mosaicist -- install-module Chord
dotnet run --project MosaicShell.Host
```

Host: Library → Chord → Arm.

Promised Rainmeter-era behavior: [`docs/legacy/chord.md`](../../docs/legacy/chord.md).
