# MosaicShell native host

Avalonia hub, `Mosaicist` CLI, and `MosaicShell.Installer` setup wizard. See [`.cursor/docs/architecture-native.md`](../.cursor/docs/architecture-native.md).

```powershell
dotnet build MosaicShell.sln
dotnet test MosaicShell.Core.Tests
dotnet run --project MosaicShell.Host
dotnet run --project Mosaicist -- list
dotnet run --project Mosaicist -- install-module Tessera
dotnet run --project MosaicShell.Installer
```

Release zips ship `Installer\`, `Host-fw\`, `Host-sc\`, `Mosaicist\`, and `Tiles\`. Prefer the GUI installer for end users.
