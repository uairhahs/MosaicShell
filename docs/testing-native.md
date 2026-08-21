# Native host testing (TDD toward JaxCore parity)

Tests: [`host/MosaicShell.Core.Tests`](../host/MosaicShell.Core.Tests).

```powershell
cd host
dotnet test MosaicShell.Core.Tests
```

## Honest coverage

| Area | Status |
|------|--------|
| Catalog / scale / installer / downloader | Green |
| Tile **session manager** (start/stop/focus) | Green — not product tile parity |
| OS services / tile MVPs / hub depth / SHP | Tracked in `HubParityBacklogTests` (false until acceptance) |

**Installed ≠ functional** until the tile surface binds host services and the matching `tile_*_mvp` flag is flipped.

## Rules

1. Prefer failing Core tests before UI.
2. Use `AppPaths.SetRootOverride` in tests.
3. Do not mark HubParity true without MVP acceptance from the adversarial plan.
4. No Rainmeter bridge; no `.ini` interpreter.
