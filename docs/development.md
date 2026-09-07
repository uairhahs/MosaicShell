# Development guide (TDD, hierarchy, extensibility)

This guide describes MosaicShell's development contracts: test-first behavior changes,
the Core-to-Host dependency hierarchy, and extensible module design. The requirements
and commands below are self-contained and apply to a standard repository checkout.

Goal: **generated and hand-written code stay in the same shape**, so each feature leaves a testable contract instead of Host-only tech debt.

---

## Dependency hierarchy

```text
Tiles/{Id}/                 install stubs only (module.native.json + README)
        │
        ▼
MosaicShell.Core            catalogs, *Spec / *Policy, services, capabilities, settings
        ▲
        │  ProjectReference only this way
        │
MosaicShell.Core.Tests      asserts Core; Hub parity honesty gates
MosaicShell.Host            Avalonia hub, flyouts, tile surfaces (reads Core)
Mosaicist                   install CLI (Core, not Host UI)
```

## Hard rules

1. **Core never references Avalonia or Host.** Pure .NET + Windows service adapters that stay UI-agnostic.
2. **Host never invents a second source of truth** for alphas, sizes, hex, glass modes, glyph ids, or composition hints. Those belong in Core `*Spec` / `*Policy` / `*Catalog` / `HostPlatform/*Policy`.
3. **`Tiles/{Id}/` is not the runtime.** Runtime lives under `host/`. Stubs exist so install/`ModuleCatalog.IsInstalled` works.

See also [`architecture.md`](architecture.md), [`module-sdk.md`](module-sdk.md), and
[`tessera-style-profile.md`](tessera-style-profile.md) (per-style capability consolidation).

---

## TDD workflow (required)

Every behavior change that affects chrome, policy, parity, or capability shape:

1. **Contract**: add or extend a type in `host/MosaicShell.Core` (`*Spec`, `*Policy`, catalog entry, builder, pure helper).
2. **Failing test**: `host/MosaicShell.Core.Tests` asserts the contract (FluentAssertions + xUnit).
3. **Confirm red**: `dotnet test` on the new/changed tests.
4. **Green**: minimal Core implementation, then Host wiring that _reads_ the contract.
5. **Refactor**: only while green; no drive-by cleanups in the same step.

```powershell
dotnet test host/MosaicShell.Core.Tests --filter "FullyQualifiedName~YourNewTests"
dotnet build host/MosaicShell.Host
# optional local run
dotnet run --project host/MosaicShell.Host
```

**Diagnosis vs proof:** `flyout.log`, screenshots, and “it crashed when I opened config” tell you _what_ broke. They do **not** replace a failing Core test before the fix.

**Parity honesty:** do not flip `*_mvp` or `*_layout_fidelity` in Hub tests without the documented bar and proofs in [`parity/`](parity/).

---

## Where to put new code

| You are changing…                                         | Prefer                                                                                                                                                                                                     |
| --------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Colors, thickness, glass stage, dim alpha, scrollbar look | Core `*Spec` / `*Policy` + Host binder that materializes brushes/windows                                                                                                                                   |
| Module list, icons/glyphs, style ids                      | `ModuleCatalog` / `HubGlyphCatalog` / `StyleCatalog`                                                                                                                                                       |
| Armed background behavior                                 | `IModuleCapability` + factory; **platform** in `Capabilities/Platform/` (`ICapabilityContext`, `CapabilityFlyoutSession`, `MediaSessionPlatform`). See [`capability-platform.md`](capability-platform.md). |
| Overlay / config / flyout from Core                       | `IHostUiBridge`, `IFlyoutPresenter`, `FlyoutRequest`                                                                                                                                                       |
| Process-wide Win32 composition                            | `HostPlatform/Win32HostCompositionPolicy`: `Program` reads that, not Tessera SoftFrost types                                                                                                               |
| Hub page layout / ViewModels                              | Host only _after_ Core contract exists for any new magic values                                                                                                                                            |

### Good pattern (scrollbar chrome)

- Core: `HostScrollbarChromeSpec` (four `bool` consts) says Host must not restyle Avalonia's
  `FluentTheme` scrollbar chrome, not what its colors are.
- Tests: assert every const stays `false` (see `HostScrollbarChromeSpecTests`), so a regression
  that starts overriding chrome fails in Core before it reaches Host.
- Host: read the spec's consts, do not add a parallel opinion about scrollbar chrome in XAML.

### Bad pattern (creates debt)

- Patch `MainWindow.axaml` with `#585B70` hard-coded while Core already defines the thumb.
- Fix a crash only in Host with no Core assertion that would fail if someone reintroduces string→brush binding.
- Copy Tessera SoftFrost flags into `Program.cs` instead of mapping through `Win32HostCompositionPolicy`.

---

## Style-capability rule (Tessera, and any future per-style module)

`TesseraFlyoutStyleProfile` (`TesseraFlyoutTweenTargetCatalog.ResolveProfile`) is the single source
of truth for "what does style X do". A new fact about a style (host kind, region needs, stacked
layout kind, corner radius, rest size, …) is a **new field on the profile**, not a new
`switch (styleId)` in another file. Full field list and the migration inventory:
[`tessera-style-profile.md`](tessera-style-profile.md).

- [ ] Does this add a `switch` / `if (styleId == …)` outside `ResolveProfile`? Add a profile field instead and make the call site a one-line accessor.
- [ ] Would two style-capability answers disagree for the same style id if someone edited only one of them? If `TesseraFlyoutStyleProfileConsistencyTests` would not fail, the tables are not unified yet.

**Bad pattern seen in practice:** `TesseraRevealHostFactory.WrapMediaFromCatalog` forced
`fullMediaWidth: double.NaN` at the call site for MaterialYou, while
`ResolveProfile(MaterialYou).MediaWidthDip` already held the real answer (60 DIP). The profile knew;
the call site forked it. Harmless only because a _third_ place hardcoded the same 60 independently.

---

## Extensibility checklist

Before merging a feature, answer yes to as many as apply:

- [ ] A second consumer (unit test or alternate Host) can get the same answer from Core alone.
- [ ] New module/style/glyph goes through a catalog, not a `switch` buried only in Host XAML.
- [ ] Capability/widget plugs into factory/registry seams (`module-sdk.md` for third-party).
- [ ] No parallel magic numbers in Host.
- [ ] Every per-style fact this change adds lives on `TesseraFlyoutStyleProfile`, not a new local switch.
- [ ] No skipped/deleted tests to force CI green.
- [ ] Scope limited, no drive-by refactors while greening one contract.

---

## Contribution checks

When contributing UI polish, glass, or tile configuration:

1. Start from Core contract + red test (see TDD rule).
2. Keep Host thin: binders, presenters, ViewModels that call Core.
3. Cite the Core type in the PR description (“Host reads `HostScrollbarChromeSpec`”).
4. Run `dotnet test host/MosaicShell.Core.Tests` before claiming done.

If a change cannot be expressed as a Core contract, stop and redesign, that is usually a smell that Host is accumulating debt.
