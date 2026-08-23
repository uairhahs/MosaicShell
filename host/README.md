# MosaicShell native host (spike)

Avalonia hub + `Mosaicist` installer. See [`.cursor/docs/architecture-native.md`](../.cursor/docs/architecture-native.md).

```powershell
dotnet build MosaicShell.sln
dotnet test MosaicShell.Core.Tests
dotnet run --project MosaicShell.Host
dotnet run --project Mosaicist -- list
dotnet run --project Mosaicist -- install-module Tessera
dotnet run --project Mosaicist -- install-module Canvas
```

Tessera and widgets (Chrono / Phono / Pulse / Canvas) are **native-only** (`Tiles/{Id}` = install stubs). Mixdeck hotkey/Material You open the **native overlay** (MVP bar in `.cursor/docs/parity`). Layout/OSD: [YourFlyouts](https://github.com/Jax-Core/YourFlyouts), [ModernFlyouts](https://github.com/ModernFlyouts-Community/ModernFlyouts). Honesty flags: [`.cursor/docs/parity/README.md`](../.cursor/docs/parity/README.md). Roadmap: [`.cursor/docs/native-rewrite.md`](../.cursor/docs/native-rewrite.md).

Parity is driven by tests - see [`.cursor/docs/parity/README.md`](../.cursor/docs/parity/README.md).
