# Inlay (native-only)

Inlay runs as an **Avalonia capability** inside `MosaicShell.Host`.

| Path                                          | Role                              |
| --------------------------------------------- | --------------------------------- |
| `host/MosaicShell.Core/Capabilities/BuiltIn/` | Arm / hotkey (default Ctrl+Alt+I) |
| `host/MosaicShell.Host/Tiles/Surfaces/`       | Pins + search overlay             |

## Install / arm

```powershell
cd host
dotnet run --project Mosaicist -- install-module Inlay
dotnet run --project MosaicShell.Host
```

Host: Library → Inlay → Arm.

Promised Rainmeter-era behavior: [`docs/legacy/inlay.md`](../../docs/legacy/inlay.md).
