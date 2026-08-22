# Substrate (native-only)

Substrate runs as an **Avalonia capability** inside `MosaicShell.Host` - not as a Rainmeter skin.

| Path | Role |
|------|------|
| `host/MosaicShell.Core/Capabilities/BuiltIn/` | Arm / hotkey (default Ctrl+Alt+Q) |
| `host/MosaicShell.Host/Tiles/Surfaces/` | Quick-settings shade overlay |
| This folder | Install stub so `install-module Substrate` creates `Modules/Substrate` |

## Install / arm

```powershell
cd host
dotnet run --project Mosaicist -- install-module Substrate
dotnet run --project MosaicShell.Host
```

In Host: Library → Substrate → Arm, then press the configured hotkey.

Promised Rainmeter-era behavior: [`docs/legacy/substrate.md`](../../docs/legacy/substrate.md).
