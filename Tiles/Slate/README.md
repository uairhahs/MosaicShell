# Slate (native-only)

Slate runs as an **Avalonia capability** inside `MosaicShell.Host` - not as a Rainmeter skin.

| Path | Role |
|------|------|
| `host/MosaicShell.Core/Capabilities/BuiltIn/` | Arm / idle watch |
| `host/MosaicShell.Host/Tiles/Surfaces/` | Idle clock overlay |
| This folder | Install stub so `install-module Slate` creates `Modules/Slate` |

## Install / arm

```powershell
cd host
dotnet run --project Mosaicist -- install-module Slate
dotnet run --project MosaicShell.Host
```

In Host: Library → Slate → Arm, then wait for the idle timeout (default 5 minutes).

Promised Rainmeter-era behavior: [`docs/legacy/slate.md`](../../docs/legacy/slate.md).
