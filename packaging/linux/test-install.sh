#!/usr/bin/env bash
# End-to-end check of mosaicist.sh against a tarball from build-tarball.sh, in a
# throwaway home with a space in its path, served as a local release. Nothing
# outside the throwaway folder is touched.
#
#   packaging/linux/test-install.sh TARBALL
#
# Checks: a fresh install (executables, menu entry, icons, command, default
# modules), a repeat run that changes nothing, an update, a refused downgrade, a
# refused tampered download, and an uninstall that keeps settings.
set -euo pipefail

tarball="$(cd "$(dirname "${1:?usage: test-install.sh TARBALL}")" && pwd)/$(basename "$1")"
here="$(cd "$(dirname "$0")" && pwd)"
base_tag="$(basename "${tarball}")"
base_tag="${base_tag#MosaicShell-}"
base_tag="${base_tag%-linux-x86_64.tar.gz}"

work="$(mktemp -d)"
trap 'rm -rf "${work}"' EXIT
home="${work}/home dir"
rel="${work}/releases"
mkdir -p "${home}"

fail() {
	echo "FAIL: $*" >&2
	exit 1
}
pass() { echo "ok: $*"; }

# Publishes the tarball as release TAG, renamed inside as that version would be.
release() { # tag
	local tag="$1" dir="${rel}/download/$1" re="${work}/repack"
	rm -rf "${re}"
	mkdir -p "${dir}" "${re}"
	tar -xzf "${tarball}" -C "${re}"
	if [[ ${tag} != "${base_tag}" ]]; then
		mv "${re}/mosaicshell-${base_tag}" "${re}/mosaicshell-${tag}"
		printf '%s\n' "${tag}" >"${re}/mosaicshell-${tag}/VERSION.txt"
	fi
	tar -C "${re}" -czf "${dir}/MosaicShell-${tag}-linux-x86_64.tar.gz" "mosaicshell-${tag}"
	# The Windows assets share the file, as in a real release.
	(cd "${dir}" && sha256sum "MosaicShell-${tag}-linux-x86_64.tar.gz" >SHA256SUMS.txt &&
		echo "0000000000000000000000000000000000000000000000000000000000000000  MosaicShell-Setup.exe" >>SHA256SUMS.txt)
}

install_sh() {
	env -i HOME="${home}" PATH=/usr/bin:/bin USER="${USER:-runner}" \
		MOSAICSHELL_RELEASES_URL="file://${rel}" bash "${here}/mosaicist.sh" "$@"
}

app="${home}/.local/lib/mosaicshell"
desktop="${home}/.local/share/applications/mosaicshell.desktop"

release "${base_tag}"
install_sh --version "${base_tag}" >/dev/null || fail "fresh install exited non-zero"
for exe in Host/MosaicShell.Host Host/MosaicShell.BrowserRelay Mosaicist/Mosaicist; do
	[[ -x "${app}/${exe}" ]] || fail "no ${exe} after install"
done
grep -qxF "Exec=\"${app}/Host/MosaicShell.Host\"" "${desktop}" || fail "menu entry does not start this install"
if command -v desktop-file-validate >/dev/null; then
	desktop-file-validate "${desktop}" || fail "menu entry is not valid"
fi
[[ -f "${home}/.local/share/icons/hicolor/256x256/apps/mosaicshell.png" ]] || fail "no icon"
[[ $(readlink "${home}/.local/bin/mosaicshell") == "${app}/Host/MosaicShell.Host" ]] || fail "command link is wrong"
for module in Tessera Mixdeck; do
	[[ -f "${home}/.local/share/MosaicShell/Modules/${module}/module.json" ]] || fail "module ${module} not installed"
done
env -u DOTNET_ROOT "${app}/Mosaicist/Mosaicist" --help >/dev/null || fail "Mosaicist does not run on the bundled runtime"
pass "fresh install"

install_sh --version "${base_tag}" | grep -q "already installed and up to date" || fail "repeat run did something"
pass "repeat run is a no-op"

release 2999.1.1-b1
install_sh --version 2999.1.1-b1 >/dev/null || fail "update exited non-zero"
[[ $(<"${app}/VERSION.txt") == 2999.1.1-b1 ]] || fail "update did not take"
[[ ! -e "${app}.old" && ! -e "${app}.new" ]] || fail "update left a stale folder"
pass "update"

release 2998.1.1-b1
if install_sh --version 2998.1.1-b1 >/dev/null 2>&1; then fail "downgrade was not refused"; fi
pass "downgrade refused"

release 3000.1.1-b1
echo tampered >>"${rel}/download/3000.1.1-b1/MosaicShell-3000.1.1-b1-linux-x86_64.tar.gz"
if install_sh --version 3000.1.1-b1 >/dev/null 2>&1; then fail "tampered download was installed"; fi
[[ $(<"${app}/VERSION.txt") == 2999.1.1-b1 ]] || fail "tampered download changed the install"
pass "tampered download refused"

install_sh --uninstall >/dev/null || fail "uninstall exited non-zero"
[[ ! -e ${app} && ! -e ${desktop} && ! -e "${home}/.local/bin/mosaicshell" ]] || fail "uninstall left files"
[[ -d "${home}/.local/share/MosaicShell/Modules/Tessera" ]] || fail "uninstall removed settings or modules"
pass "uninstall keeps settings"
