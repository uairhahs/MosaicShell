# MosaicShell native host (spike)

Avalonia hub + `Mosaicist` installer. See [docs/architecture-native.md](../docs/architecture-native.md).

```powershell
dotnet build MosaicShell.sln
dotnet test MosaicShell.Core.Tests
dotnet run --project MosaicShell.Host
dotnet run --project Mosaicist -- list
dotnet run --project Mosaicist -- install-module Canvas
```

Parity is driven by tests — see [docs/testing-native.md](../docs/testing-native.md).
