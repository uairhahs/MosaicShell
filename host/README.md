# MosaicShell Host

Avalonia hub and `Mosaicist` CLI. See [`docs/architecture.md`](../docs/architecture.md).

## Dev loop

```powershell
dotnet test MosaicShell.Core.Tests
dotnet run --project Mosaicist -- install-module Tessera
dotnet run --project MosaicShell.Host
```

## Releases

End users install via **Inno Setup** (`MosaicShell-Setup-*.exe`). See [packaging/README.md](../packaging/README.md).

Version tags are **date-build** (`yyyy.M.d-bN`), not semver.

Portable layout (also inside the Setup staging folder):

```text
Host/MosaicShell.Host.exe   (self-contained win-x64)
Mosaicist/Mosaicist.exe
Tiles/{Id}/...
VERSION.txt
```

Mosaicist installs modules into `%LocalAppData%\MosaicShell\Modules` from the release `Tiles/` tree next to Host.
