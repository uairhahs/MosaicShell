# Slate (native-only)

Slate runs as an **Avalonia capability** inside `MosaicShell.Host`.

| Path                                          | Role               |
| --------------------------------------------- | ------------------ |
| `host/MosaicShell.Core/Capabilities/BuiltIn/` | Arm / idle watch   |
| `host/MosaicShell.Host/Tiles/Surfaces/`       | Idle clock overlay |

## Install / arm

```powershell
cd host
dotnet run --project Mosaicist -- install-module Slate
dotnet run --project MosaicShell.Host
```

Host: Library → Slate → Arm. Shows after the idle timeout (default 5 minutes).

Promised Rainmeter-era behavior: [`docs/legacy/slate.md`](../../docs/legacy/slate.md).
