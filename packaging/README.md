# Packaging

Builds a UniGetUI-style **Inno Setup** installer and optional portable zip.

## Versioning

Releases use **date-build** tags, not semver:

```text
yyyy.M.d-bN    e.g. 2026.8.23-b1
```

That string is the GitHub release tag, `VERSION.txt`, assembly informational version, and Setup filename (`MosaicShell-Setup-2026.8.23-b1.exe`). Inno `VersionInfoVersion` is a numeric PE field only (`2026.8.23.1`), derived from the same tag.

## Local

1. Install [Inno Setup 7.1+](https://jrsoftware.org/isinfo.php) (`winget install --id JRSoftware.InnoSetup.7 -e`).
2. From repo root:

```powershell
.\packaging\build-setup.ps1 -Version "2026.8.23-b1"
```

Outputs:

- `packaging/stage/` - Host (self-contained), Mosaicist, Tiles
- `packaging/output/MosaicShell-Setup-{version}.exe`

## CI

[`.github/workflows/release.yml`](../.github/workflows/release.yml) installs **Inno Setup 7.1.0** from the upstream GitHub release (SHA-pinned; Chocolatey still only packages 6.x), stamps `yyyy.M.d-b{run_number}`, and attaches Setup.exe + portable zip. Signing is **optional** (deferred): if Azure Trusted Signing secrets/vars are missing, the job publishes unsigned builds.

Unlike UniGetUI's local `scripts/build.ps1` (searches Inno Setup 6 and **soft-skips** if missing), our release job **fails** if `ISCC.exe` is absent.

## Signing (deferred)

When you want Authenticode later: [`.github/docs/release-signing.md`](../.github/docs/release-signing.md). Completing A–C there turns signing on automatically.

Local agent follow-ups: [`docs/release-outstanding.md`](../docs/release-outstanding.md).
