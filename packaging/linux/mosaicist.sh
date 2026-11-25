#!/usr/bin/env bash
# Install MosaicShell on Linux from a GitHub release, or update it. Run the same
# command again to update.
#
#   curl -fsSL https://raw.githubusercontent.com/uairhahs/MosaicShell/main/packaging/linux/mosaicist.sh | bash
#
# Installs for the current user only, with no root and nothing system-wide: the
# app and its own .NET runtime go to ~/.local/lib/mosaicshell, with a menu entry,
# icons and a `mosaicshell` command in ~/.local/bin. Starting at login is the
# Hub's own toggle, as on Windows.
#
# Options:
#   --version TAG     install that release (e.g. 2026.11.25-b42) instead of the latest
#   --force           reinstall, or downgrade
#   --no-restart      leave a running MosaicShell on the old version
#   --uninstall       remove MosaicShell (settings and modules are kept)
#
# The download is checked against the release's SHA256SUMS.txt.
set -euo pipefail

REPO="uairhahs/MosaicShell"
RELEASES="${MOSAICSHELL_RELEASES_URL:-https://github.com/${REPO}/releases}"
PREFIX="${MOSAICSHELL_PREFIX:-${HOME}/.local}"
APP_DIR="${PREFIX}/lib/mosaicshell"
BIN_LINK="${PREFIX}/bin/mosaicshell"
DATA_HOME="${XDG_DATA_HOME:-${HOME}/.local/share}"
CONFIG_HOME="${XDG_CONFIG_HOME:-${HOME}/.config}"
DESKTOP_FILE="${DATA_HOME}/applications/mosaicshell.desktop"
ICON_SIZES="32 64 128 256 512"
HOST_NAME="com.mosaicshell.grout"
# The browsers the Host registers its native messaging host with (NativeHostRegistration.LinuxBrowserConfigDirs).
BROWSER_DIRS="microsoft-edge microsoft-edge-beta microsoft-edge-dev google-chrome google-chrome-beta google-chrome-unstable chromium BraveSoftware/Brave-Browser vivaldi"

TAG=""
FORCE=0
RESTART=1
UNINSTALL=0

say() { printf '%s\n' "$*"; }
die() {
	say "error: $*" >&2
	exit 1
}
usage() { sed -n '2,19s/^# \{0,1\}//p' "$0"; }

while [[ $# -gt 0 ]]; do
	case "$1" in
	--version)
		TAG="${2:?--version needs a tag, e.g. 2026.11.25-b42}"
		TAG="${TAG#v}"
		shift 2
		;;
	--force)
		FORCE=1
		shift
		;;
	--no-restart)
		RESTART=0
		shift
		;;
	--uninstall)
		UNINSTALL=1
		shift
		;;
	-h | --help)
		usage
		exit 0
		;;
	*)
		say "Unknown option: $1" >&2
		usage >&2
		exit 2
		;;
	esac
done

HOST_EXE="${APP_DIR}/Host/MosaicShell.Host"

running_pids() {
	pgrep -u "${UID}" -f "^${HOST_EXE}( |$)" || true
}

# Asks a running Host to quit and waits up to 10 seconds. Returns 1 if it is still running.
stop_running() {
	local pids i
	pids="$(running_pids)"
	[[ -n ${pids} ]] || return 0
	# shellcheck disable=SC2086 # one PID per word
	kill -TERM ${pids} 2>/dev/null || true
	for ((i = 0; i < 100; i++)); do
		[[ -n "$(running_pids)" ]] || return 0
		sleep 0.1
	done
	return 1
}

refresh_user_caches() {
	if command -v update-desktop-database >/dev/null; then
		update-desktop-database -q "${DATA_HOME}/applications" 2>/dev/null || true
	fi
	if command -v gtk-update-icon-cache >/dev/null && [[ -f "${DATA_HOME}/icons/hicolor/index.theme" ]]; then
		gtk-update-icon-cache -q "${DATA_HOME}/icons/hicolor" 2>/dev/null || true
	fi
	# A running Plasma caches application and icon lookups.
	if pgrep -u "${UID}" -x plasmashell >/dev/null 2>&1; then
		local sycoca
		for sycoca in kbuildsycoca6 kbuildsycoca5; do
			if command -v "${sycoca}" >/dev/null; then
				"${sycoca}" >/dev/null 2>&1 || true
				break
			fi
		done
	fi
}

# The Desktop Entry spec's quoting for an Exec argument: inside double quotes,
# \ " ` and $ take a backslash, and the file's own string escaping then doubles
# every backslash (as XdgAutostartPolicy quotes the Hub's login entry).
desktop_quote() {
	printf '"%s"' "$(printf '%s' "$1" | sed -e 's/\\/\\\\\\\\/g' -e 's/["`$]/\\\\&/g')"
}

# Writes the menu entry from the template with Exec pointing at this install.
write_desktop_entry() {
	local line exec_line
	exec_line="Exec=$(desktop_quote "${HOST_EXE}")"
	mkdir -p "$(dirname "${DESKTOP_FILE}")"
	while IFS= read -r line || [[ -n ${line} ]]; do
		if [[ ${line} == Exec=* ]]; then
			printf '%s\n' "${exec_line}"
		else
			printf '%s\n' "${line}"
		fi
	done <"${APP_DIR}/share/applications/mosaicshell.desktop" >"${DESKTOP_FILE}.new"
	mv -f "${DESKTOP_FILE}.new" "${DESKTOP_FILE}"
}

# ---- uninstall -------------------------------------------------------------

if [[ ${UNINSTALL} -eq 1 ]]; then
	[[ -d ${APP_DIR} ]] || die "MosaicShell is not installed in ${APP_DIR}"
	stop_running || die "MosaicShell did not quit within 10 seconds; quit it from the tray and try again"
	rm -rf "${APP_DIR}"
	rm -f "${DESKTOP_FILE}"
	[[ -L ${BIN_LINK} ]] && rm -f "${BIN_LINK}"
	for size in ${ICON_SIZES}; do
		rm -f "${DATA_HOME}/icons/hicolor/${size}x${size}/apps/mosaicshell.png"
	done
	# The login entry the Hub toggle writes, only when it starts this install.
	autostart="${CONFIG_HOME}/autostart/mosaicshell.desktop"
	if [[ -f ${autostart} ]] && grep -qF "${HOST_EXE}" "${autostart}"; then
		rm -f "${autostart}"
	fi
	# The browser bridge the Host registers when it starts.
	for browser in ${BROWSER_DIRS}; do
		rm -f "${CONFIG_HOME}/${browser}/NativeMessagingHosts/${HOST_NAME}.json"
	done
	rm -f "${DATA_HOME}/MosaicShell/browser/${HOST_NAME}.json" "${DATA_HOME}/MosaicShell/browser/grout-relay.sh"
	refresh_user_caches
	say "==> Removed MosaicShell. Settings and modules in ${DATA_HOME}/MosaicShell were kept."
	exit 0
fi

# ---- find the release ------------------------------------------------------

for tool in curl sha256sum tar; do
	command -v "${tool}" >/dev/null || die "${tool} is required"
done
machine="$(uname -m)"
[[ ${machine} == x86_64 ]] || die "release builds are x86_64 only"

if [[ -z ${TAG} ]]; then
	# GitHub redirects /releases/latest to /releases/tag/<tag>; no API call needed.
	latest="$(curl -fsSL -o /dev/null -w '%{url_effective}' "${RELEASES}/latest")" || die "cannot reach ${RELEASES}"
	TAG="${latest##*/tag/}"
	[[ -n ${TAG} && ${TAG} != "${latest}" ]] || die "cannot tell the latest release from ${latest}"
fi
asset="MosaicShell-${TAG}-linux-${machine}.tar.gz"
base="${RELEASES}/download/${TAG}"

# ---- compare versions ------------------------------------------------------

have=""
if [[ -f "${APP_DIR}/VERSION.txt" ]]; then
	have="$(tr -d '[:space:]' <"${APP_DIR}/VERSION.txt")"
fi
if [[ -n ${have} && ${FORCE} -eq 0 ]]; then
	if [[ ${have} == "${TAG}" ]]; then
		say "==> MosaicShell ${have} is already installed and up to date."
		exit 0
	fi
	# Date-build tags (yyyy.M.d-bN) sort correctly as versions.
	newest="$(printf '%s\n%s\n' "${have}" "${TAG}" | sort -V | tail -n1)"
	[[ ${newest} == "${TAG}" ]] || die "${TAG} is older than the installed ${have}; use --force to downgrade"
fi

# ---- download and verify ---------------------------------------------------

tmp="$(mktemp -d)"
trap 'rm -rf "${tmp}"' EXIT
say "==> Reading ${base}/SHA256SUMS.txt"
sums="$(curl -fsSL "${base}/SHA256SUMS.txt")" || die "cannot read the release checksums (does release ${TAG} exist?)"
sum="$(awk -v f="${asset}" '$2 == f || $2 == "*" f { print $1 }' <<<"${sums}")"
[[ -n ${sum} ]] || die "release ${TAG} has no Linux build (${asset})"
[[ $(wc -l <<<"${sum}") -eq 1 ]] || die "SHA256SUMS.txt lists ${asset} more than once; not guessing"

say "==> Downloading ${asset}"
curl -fL --progress-bar -o "${tmp}/${asset}" "${base}/${asset}" || die "download failed: ${base}/${asset}"
got="$(sha256sum "${tmp}/${asset}" | cut -d' ' -f1)"
[[ ${got} == "${sum,,}" ]] || die "checksum mismatch, not installing (try again)"
say "==> Checksum OK."

tar -xzf "${tmp}/${asset}" -C "${tmp}"
root="${tmp}/mosaicshell-${TAG}"
for exe in Host/MosaicShell.Host Host/MosaicShell.BrowserRelay Mosaicist/Mosaicist; do
	[[ -x "${root}/${exe}" ]] || die "the download has no ${exe}"
done

# ---- install ---------------------------------------------------------------

if [[ -n ${have} ]]; then
	say "==> Updating MosaicShell ${have} -> ${TAG}"
else
	say "==> Installing MosaicShell ${TAG}"
fi

was_running=0
[[ -n "$(running_pids)" ]] && was_running=1
if [[ ${was_running} -eq 1 && ${RESTART} -eq 1 ]]; then
	stop_running || die "MosaicShell did not quit within 10 seconds; quit it from the tray and run this again"
fi

# Swap the whole folder, so an interrupted update never leaves half of each version.
mkdir -p "${PREFIX}/lib"
staged="${APP_DIR}.new"
rm -rf "${staged}" "${APP_DIR}.old"
mv "${root}" "${staged}"
[[ -d ${APP_DIR} ]] && mv "${APP_DIR}" "${APP_DIR}.old"
mv "${staged}" "${APP_DIR}"
rm -rf "${APP_DIR}.old"

# Menu entry and icons. Launchers run without the shell's PATH, so Exec names the Host directly.
write_desktop_entry
for size in ${ICON_SIZES}; do
	install -Dm644 "${APP_DIR}/share/icons/hicolor/${size}x${size}/apps/mosaicshell.png" \
		"${DATA_HOME}/icons/hicolor/${size}x${size}/apps/mosaicshell.png"
done
mkdir -p "$(dirname "${BIN_LINK}")"
ln -sfn "${HOST_EXE}" "${BIN_LINK}"
refresh_user_caches

# The default modules, as the Windows installer adds them; reinstalling one keeps its settings.
for module in Tessera Mixdeck; do
	"${APP_DIR}/Mosaicist/Mosaicist" install-module "${module}" >/dev/null ||
		say "==> Could not install the ${module} module; add it later from the Hub."
done

case ":${PATH}:" in
*":${PREFIX}/bin:"*) ;;
*) say "==> Add ${PREFIX}/bin to your PATH to run mosaicshell from a terminal." ;;
esac
say "==> MosaicShell ${TAG} is installed. Remove it later with: mosaicist.sh --uninstall"

# ---- restart a running copy ------------------------------------------------

if [[ ${was_running} -eq 1 ]]; then
	if [[ ${RESTART} -eq 0 ]]; then
		say "==> A running MosaicShell keeps the old version until you quit and start it again."
	elif [[ -z ${WAYLAND_DISPLAY-} && -z ${DISPLAY-} ]]; then
		say "==> MosaicShell was running; this shell has no display to start the new version on."
		say "    Start it from your application menu."
	else
		# Detached, so it outlives this script and the terminal.
		setsid -f "${HOST_EXE}" </dev/null >/dev/null 2>&1 || say "==> Could not start ${HOST_EXE}; start MosaicShell from your application menu."
		say "==> Restarted MosaicShell on the new version."
	fi
fi
