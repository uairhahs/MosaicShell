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

## Linux

A per-user tarball and an install script, modeled on SyncLens's.

```bash
packaging/linux/build-tarball.sh 2026.8.23-b1          # -> packaging/output/MosaicShell-2026.8.23-b1-linux-x86_64.tar.gz
packaging/linux/test-install.sh packaging/output/MosaicShell-2026.8.23-b1-linux-x86_64.tar.gz
```

The tarball holds the same `Host/`, `Mosaicist/`, `Tiles/` and `VERSION.txt` as the Windows stage, plus `dotnet/`, a
private copy of the .NET runtime that every executable finds relative to itself (`AppHostDotNetSearch=AppRelative`). So
the browser relay and the login entry start without a system .NET or `DOTNET_ROOT`. The browser relay is published into
`Host/` on its own, because `dotnet publish` copies a referenced program's assembly but not its apphost.

[`linux/mosaicist.sh`](linux/mosaicist.sh) installs the latest release for the current user, with no root:

```bash
curl -fsSL https://raw.githubusercontent.com/uairhahs/MosaicShell/main/packaging/linux/mosaicist.sh | bash
```

It installs into `~/.local/lib/mosaicshell`, adds a menu entry, icons and `~/.local/bin/mosaicshell`, installs the
default modules with Mosaicist, and restarts a running copy on the new version. The download is checked against the
release's `SHA256SUMS.txt`. Options: `--version TAG`, `--force`, `--no-restart`, `--uninstall` (keeps settings and
modules). `MOSAICSHELL_RELEASES_URL` points it at another release source; `test-install.sh` uses that to serve a local
release.

CI builds the tarball and runs `test-install.sh` on every push to `dev` or `main` (`package (linux)` in `ci.yml`). The
release workflow builds it, tests it, and attaches it to the release beside Setup.exe, in the same `SHA256SUMS.txt`.

## Signing (deferred)

When you want Authenticode later: [`.github/docs/release-signing.md`](../.github/docs/release-signing.md). Completing A–C there turns signing on automatically.

Local agent follow-ups: [`docs/release-outstanding.md`](../docs/release-outstanding.md).
