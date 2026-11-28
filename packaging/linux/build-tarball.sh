#!/usr/bin/env bash
# Build the Linux release tarball: MosaicShell-<version>-linux-x86_64.tar.gz
#
#   packaging/linux/build-tarball.sh [VERSION] [OUT_DIR]
#
# VERSION is the date-build tag (yyyy.M.d-bN, as the Windows release uses) and
# defaults to 0.0.0-dev. OUT_DIR defaults to packaging/output. The tarball holds
# one top-level folder, the same layout as the Windows stage plus a private .NET
# runtime, so nothing has to be installed system-wide:
#
#   mosaicshell-<version>/
#     Host/        MosaicShell.Host and the browser relay (framework-dependent)
#     Mosaicist/   Mosaicist (framework-dependent)
#     Tiles/       bundled modules
#     dotnet/      the .NET runtime every executable above runs on
#     share/       desktop entry template and icons
#     VERSION.txt
#
# Each executable finds dotnet/ relative to itself (AppHostDotNetSearch), so it
# also runs when a browser starts the relay or the session starts the Host at
# login, with no DOTNET_ROOT set. packaging/linux/mosaicist.sh installs it.
set -euo pipefail

here="$(cd "$(dirname "$0")" && pwd)"
repo="$(cd "${here}/../.." && pwd)"
version="${1:-0.0.0-dev}"
out="${2:-${repo}/packaging/output}"
mkdir -p "${out}"
out="$(cd "${out}" && pwd)"

machine="$(uname -m)"
[[ ${machine} == x86_64 ]] || {
	echo "error: release builds are x86_64 only" >&2
	exit 1
}
name="MosaicShell-${version}-linux-${machine}"

work="$(mktemp -d)"
trap 'rm -rf "${work}"' EXIT
stage="${work}/mosaicshell-${version}"
mkdir -p "${stage}"

# Every apphost looks for the runtime at ../dotnet from its own folder.
publish() { # project, folder
	dotnet publish "${repo}/host/$1" \
		-c Release -r linux-x64 --self-contained false \
		-p:Version="${version}" \
		-p:AppHostDotNetSearch=AppRelative \
		-p:AppHostRelativeDotNet=../dotnet \
		-o "${stage}/$2"
}
echo "==> Publishing Host"
publish MosaicShell.Host/MosaicShell.Host.csproj Host
# Publish copies a referenced program's assembly but not its apphost, and the
# browsers start the relay by its apphost next to the Host.
echo "==> Publishing the browser relay"
publish MosaicShell.BrowserRelay/MosaicShell.BrowserRelay.csproj Host
echo "==> Publishing Mosaicist"
publish Mosaicist/Mosaicist.csproj Mosaicist

# The runtime the SDK in use ships: the newest Microsoft.NETCore.App of the
# major version the projects target, with its matching host resolver.
echo "==> Copying the .NET runtime"
runtime_line="$(dotnet --list-runtimes | grep '^Microsoft.NETCore.App 11\.' | sort -V | tail -n1)"
[[ -n ${runtime_line} ]] || {
	echo "error: no Microsoft.NETCore.App 11.x runtime next to this SDK" >&2
	exit 1
}
runtime_version="$(awk '{print $2}' <<<"${runtime_line}")"
shared_dir="$(sed -n 's/.*\[\(.*\)\]$/\1/p' <<<"${runtime_line}")"
dotnet_root="$(cd "${shared_dir}/../.." && pwd)"
fxr="$(find "${dotnet_root}/host/fxr" -mindepth 1 -maxdepth 1 -type d -printf '%f\n' | sort -V | tail -n1)"
mkdir -p "${stage}/dotnet/shared/Microsoft.NETCore.App" "${stage}/dotnet/host/fxr"
cp -a "${shared_dir}/${runtime_version}" "${stage}/dotnet/shared/Microsoft.NETCore.App/"
cp -a "${dotnet_root}/host/fxr/${fxr}" "${stage}/dotnet/host/fxr/"
for f in LICENSE.txt ThirdPartyNotices.txt; do
	if [[ -f "${dotnet_root}/${f}" ]]; then cp -a "${dotnet_root}/${f}" "${stage}/dotnet/"; fi
done
echo "    Microsoft.NETCore.App ${runtime_version}, hostfxr ${fxr}"

echo "==> Copying Tiles, desktop entry and icons"
cp -a "${repo}/Tiles" "${stage}/Tiles"
install -Dm644 "${here}/mosaicshell.desktop" "${stage}/share/applications/mosaicshell.desktop"
for size in 32 64 128 256 512; do
	install -Dm644 "${repo}/.github/res/logo-variants/compact-${size}.png" \
		"${stage}/share/icons/hicolor/${size}x${size}/apps/mosaicshell.png"
done
install -Dm644 "${repo}/LICENSE" "${stage}/share/licenses/mosaicshell/LICENSE"
printf '%s\n' "${version}" >"${stage}/VERSION.txt"

# A quick proof before anything ships: the executables are there, and one of
# them starts on the bundled runtime with no DOTNET_ROOT to help it.
for exe in Host/MosaicShell.Host Host/MosaicShell.BrowserRelay Mosaicist/Mosaicist; do
	[[ -x "${stage}/${exe}" ]] || {
		echo "error: the stage has no ${exe}" >&2
		exit 1
	}
done
env -u DOTNET_ROOT "${stage}/Mosaicist/Mosaicist" --help >/dev/null 2>&1 || {
	echo "error: the staged Mosaicist does not start on the bundled runtime" >&2
	exit 1
}

tar -C "${work}" -czf "${out}/${name}.tar.gz" "mosaicshell-${version}"
echo "${out}/${name}.tar.gz"
