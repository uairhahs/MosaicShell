# Agent notes (MosaicShell)

Follow these before changing Host UI, Tessera glass, tile chrome, or capabilities:

1. [`.cursor/docs/development.md`](.cursor/docs/development.md), full TDD + hierarchy + extensibility guide
2. Always-apply Cursor rules in [`.cursor/rules/`](.cursor/rules/):
   - `tdd.mdc`
   - `dependency-hierarchy.mdc`
   - `extensibility.mdc`
3. Architecture snapshot: [`.cursor/docs/architecture-native.md`](.cursor/docs/architecture-native.md)
4. Third-party modules: [`.cursor/docs/module-sdk.md`](.cursor/docs/module-sdk.md)
5. Parity honesty: [`.cursor/docs/parity/README.md`](.cursor/docs/parity/README.md)

**Default loop:** Core contract → failing `MosaicShell.Core.Tests` → green Core → Host reads the contract. Do not patch Avalonia first.
