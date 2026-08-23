# Agent notes (MosaicShell)

Follow these before changing Host UI, Tessera glass, tile chrome, or capabilities:

1. [`.cursor/docs/development.md`](.cursor/docs/development.md), full TDD + hierarchy + extensibility guide
2. Always-apply Cursor rules in [`.cursor/rules/`](.cursor/rules/):
   - `tdd.mdc`
   - `dependency-hierarchy.mdc`
   - `extensibility.mdc`
3. Architecture snapshot: [`.cursor/docs/architecture-native.md`](.cursor/docs/architecture-native.md)
4. Capability platform API: [`.cursor/docs/capability-platform.md`](.cursor/docs/capability-platform.md)
5. Third-party modules: [`.cursor/docs/module-sdk.md`](.cursor/docs/module-sdk.md)
6. Parity honesty: [`.cursor/docs/parity/README.md`](.cursor/docs/parity/README.md)
7. Release follow-ups: [`.cursor/docs/release-outstanding.md`](.cursor/docs/release-outstanding.md)
8. Code signing (Azure Trusted Signing): [`.github/docs/release-signing.md`](.github/docs/release-signing.md)

**Scratch sandboxes:** [`.local/`](.local/) is gitignored repo-local space (probes, parity refs, `compile-local.ps1`). See [`.cursor/docs/development.md`](.cursor/docs/development.md#local-scratch--sandboxes).

**Default loop:** Core contract → failing `MosaicShell.Core.Tests` → green Core → Host reads the contract. Do not patch Avalonia first.
