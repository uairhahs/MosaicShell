# Native host architecture

MosaicShell is moving off Rainmeter as the UI/runtime host. Work lives under [`host/`](../host/).

## Honesty (adversarial)

The Avalonia host currently provides:

- Hub shell (Discover / Library / Settings / Welcome / About)
- Safe installer (`Mosaicist`, SHA-256, no `iwr|iex`)
- **Tile session manager** (`TileRuntime` + borderless overlays)

It does **not** yet provide Rainmeter functional parity. Catalog overlays started as mock surfaces; installed Rainmeter skin trees under `%LocalAppData%\MosaicShell\Modules` are **assets/settings packages**, not executable `.ini` scripts. There is **no Rainmeter bridge** and **no `.ini` interpreter**.

Parity is rebuilt as **host OS services + native Avalonia tiles**. See the adversarial plan and [`testing-native.md`](testing-native.md). HubParity flags stay false until each MVP acceptance passes.

## Why

| Pain | Rainmeter reality | Native direction |
|------|-------------------|------------------|
| DPI / scale | Bang/measure math + `HIGHDPIAWARE` workarounds | Avalonia DPI + `UserScale` |
| Installer / Defender | `powershell -ExecutionPolicy Bypass` + `iwr \| iex` | `Mosaicist` downloads + SHA-256 |
| Extensibility | Plugin DLL zoo | Host services + module packages |

## Solution layout

```text
host/
  MosaicShell.sln
  MosaicShell.Core/     # scale, catalog, install, TileRuntime, OS service interfaces/impls
  MosaicShell.Host/     # Avalonia hub + tile surfaces
  Mosaicist/            # CLI installer (no iwr|iex)
```

## Native rewrite decision

| Rainmeter | Native |
|-----------|--------|
| `Main.ini` + bangs + plugins | Avalonia surface in Host |
| `@Resources/Vars.inc` | `%LocalAppData%\MosaicShell\Config\modules\<Id>.json` |
| Plugin DLLs | `MosaicShell.Core/Services/*` |
| Packages | `module.manifest.json` + assets; not skin entrypoints |

## Scale contract

```text
UiScale = DpiScale × UserScale
```

Persisted at `%LocalAppData%\MosaicShell\Config\scale.json`. Overlays must apply `UserScale`.

## Installer contract (anti-Commando)

Allowed: `Mosaicist install-module … --zip` / `--url --sha256`  
Forbidden: `iwr|iex`, ExecutionPolicy Bypass remote script execution

## Migration phases

0. Honesty reset (flags/docs)
1. OS capability services
2. Runtime hardening (sessions, uninstall, settings JSON, tray)
3. Tile MVPs (Chrono → … → Slate)
4. Hub product parity
5. SHP module-subset import
6. Cutover (signed Host; legacy Rainmeter path documented)

## Run (dev)

```powershell
cd host
dotnet test MosaicShell.Core.Tests
dotnet run --project MosaicShell.Host
dotnet run --project Mosaicist -- install-module Canvas
```
