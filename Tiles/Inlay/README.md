# Inlay (native-only)

Inlay runs as an **Avalonia capability** inside `MosaicShell.Host` - not as a Rainmeter skin.

| Path | Role |
|------|------|
| `host/MosaicShell.Core/Capabilities/BuiltIn/` | Arm / hotkey (default Ctrl+Alt+I) |
| `host/MosaicShell.Host/Tiles/Surfaces/` | Pins + search overlay |
| This folder | Install stub so `install-module Inlay` creates `Modules/Inlay` |

## Install / arm

```powershell
cd host
dotnet run --project Mosaicist -- install-module Inlay
dotnet run --project MosaicShell.Host
```

In Host: Library → Inlay → Arm, then press the configured hotkey.

Promised Rainmeter-era behavior: [`docs/legacy/inlay.md`](../../docs/legacy/inlay.md).
