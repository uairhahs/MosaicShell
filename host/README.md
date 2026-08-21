# MosaicShell native host (spike)

Avalonia hub + `Mosaicist` installer. See [docs/architecture-native.md](../docs/architecture-native.md).

```powershell
dotnet build MosaicShell.sln
dotnet test MosaicShell.Core.Tests
dotnet run --project MosaicShell.Host
dotnet run --project Mosaicist -- list
dotnet run --project Mosaicist -- install-module Tessera
dotnet run --project Mosaicist -- install-module Canvas
```

Tessera and widgets (Chrono / Phono / Pulse / Canvas) are **native-only** (`Tiles/{Id}` = install stubs). Mixdeck hotkey/Pixel open the **native overlay** (MVP bar in docs/parity). Layout/OSD: [YourFlyouts](https://github.com/Jax-Core/YourFlyouts), [ModernFlyouts](https://github.com/ModernFlyouts-Community/ModernFlyouts). Honesty flags: [docs/parity/README.md](../docs/parity/README.md). Roadmap: [docs/native-rewrite.md](../docs/native-rewrite.md).

Parity is driven by tests — see [docs/parity/README.md](../docs/parity/README.md).
