# Agent notes (MosaicShell)

Follow these before changing Host UI, Tessera glass, tile chrome, or capabilities:

1. [`docs/development.md`](docs/development.md) — full TDD + hierarchy + extensibility guide
2. Always-apply Cursor rules in [`.cursor/rules/`](.cursor/rules/):
   - `tdd.mdc`
   - `dependency-hierarchy.mdc`
   - `extensibility.mdc`
3. Architecture snapshot: [`docs/architecture-native.md`](docs/architecture-native.md)
4. Third-party modules: [`docs/module-sdk.md`](docs/module-sdk.md)
5. Parity honesty: [`docs/parity/README.md`](docs/parity/README.md)

**Default loop:** Core contract → failing `MosaicShell.Core.Tests` → green Core → Host reads the contract. Do not patch Avalonia first.
