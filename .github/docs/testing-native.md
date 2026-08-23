# Testing the native host

MosaicShell is **Host-only** (Avalonia + .NET 10, Windows). Contracts live in Core; Host reads them.

```powershell
cd host
dotnet test MosaicShell.Core.Tests
```

Filtered example:

```powershell
dotnet test MosaicShell.Core.Tests --filter "FullyQualifiedName~Tessera"
```

Release CI runs the full suite: [`.github/workflows/release.yml`](../workflows/release.yml).

## TDD expectations

1. Extend a contract in `host/MosaicShell.Core` (`*Policy`, `*Spec`, service, capability).
2. Add a failing test in `MosaicShell.Core.Tests`.
3. Confirm red, implement minimal Core + Host wiring, refactor while green.

Tests that touch `%LocalAppData%\MosaicShell` should use `AppPaths.SetRootOverride` (see existing capability tests).

Parity flag rules: [parity.md](parity.md).

## Constraints

- Prefer failing Core tests before Avalonia UI patches.
- No Rainmeter bridge, `.rmskin`, or `.ini` interpreter.
- Capabilities arm through **CapabilityDaemon** (Tessera is not opened like a normal Library window).

## Run the Host locally

```powershell
cd host
dotnet run --project MosaicShell.Host
```

Or build then run `MosaicShell.Host.exe` from `host/MosaicShell.Host/bin/Debug/...`.
