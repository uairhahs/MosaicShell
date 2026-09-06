# Substrate (native-only)

Substrate runs as an **Avalonia capability** inside `MosaicShell.Host`.

| Path                                          | Role                              |
| --------------------------------------------- | --------------------------------- |
| `host/MosaicShell.Core/Capabilities/BuiltIn/` | Arm / hotkey (default Ctrl+Alt+Q) |
| `host/MosaicShell.Host/Tiles/Surfaces/`       | Quick-settings shade overlay      |

## Install / arm

```powershell
cd host
dotnet run --project Mosaicist -- install-module Substrate
dotnet run --project MosaicShell.Host
```

Host: Library → Substrate → Arm.

Promised Rainmeter-era behavior: [`docs/legacy/substrate.md`](../../docs/legacy/substrate.md).
