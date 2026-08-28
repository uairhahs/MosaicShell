# Release follow-ups

Outstanding work after the Inno Setup release path. Packaging: [`packaging/README.md`](../packaging/README.md). Versioning is **date-build** (`yyyy.M.d-bN`), not semver.

**Code signing:** deferred. CI publishes unsigned until Azure is configured. Guide: [`.github/docs/release-signing.md`](../.github/docs/release-signing.md).

---

## Outstanding tasks

| Priority | Task                                    | Notes                                                                                          |
| -------- | --------------------------------------- | ---------------------------------------------------------------------------------------------- |
| High     | Smoke Setup (unsigned is fine)          | Install Setup from a release run, confirm Tessera/Mixdeck, Check Updates                       |
| Low      | Enable Azure Trusted Signing later      | Optional; ~monthly fee. Checklist in release-signing.md. CI auto-signs when secrets/vars exist |
| Medium   | Background update check on Host startup | Today: Settings / Check Updates only                                                           |
| Medium   | WinGet package id                       | Manifest pointing at Setup.exe                                                                 |
| Low      | Rebuild Host after icon regen           | Multi-size `mosaicshell.ico` is on disk; need a fresh Host/Setup build for tray/taskbar        |

When an item ships, remove or strike it here.

---

## Related paths

- Signing docs: [`.github/docs/release-signing.md`](../.github/docs/release-signing.md)
- Workflow: [`.github/workflows/release.yml`](../.github/workflows/release.yml)
- Inno: [`packaging/MosaicShell.iss`](../packaging/MosaicShell.iss)
- Update apply: `host/MosaicShell.Core/Update/`
