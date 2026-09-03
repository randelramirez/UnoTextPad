#!/usr/bin/env bash

# Builds UnoTextPad.app and the UnoTextPad.dmg disk image that carries it.
#
# The packaging itself belongs to the Uno SDK: the osx-* publish profiles set
# PackageFormat, and the SDK then generates the icns, the Info.plist, the app
# bundle and the disk image after Publish. This script only picks an
# architecture and, for `universal`, merges the two single-architecture bundles
# into one before wrapping that in a disk image.

set -euo pipefail

readonly root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly project="${root}/UnoTextPad/UnoTextPad.csproj"
readonly framework="net10.0-desktop"
readonly configuration="Release"

target=${1-arm64}

publish() {
  local architecture=$1
  local package_format=$2

  dotnet publish "${project}" \
    --framework "${framework}" \
    --configuration "${configuration}" \
    --runtime "osx-${architecture}" \
    -p:PublishProfile="osx-${architecture}" \
    -p:PackageFormat="${package_format}"
}

publish_dir() {
  printf '%s/UnoTextPad/bin/%s/%s/osx-%s/publish' "${root}" "${configuration}" "${framework}" "$1"
}

# The Uno SDK only imports its packaging targets once a publish profile has been
# imported, so the two targets called by hand below need one named as well. Paths
# have to be absolute: MSBuild resolves relative ones against the project folder.
run_packaging_target() {
  local target_name=$1
  shift

  dotnet msbuild "${project}" \
    -target:"${target_name}" \
    -p:TargetFramework="${framework}" \
    -p:Configuration="${configuration}" \
    -p:PublishProfile=osx-arm64 \
    "$@"
}

case "${target}" in
arm64 | x64)
  publish "${target}" dmg
  disk_image="$(publish_dir "${target}")/UnoTextPad.dmg"
  ;;
universal)
  # A fat bundle is merged from two thin ones, so the disk image has to be built
  # from the merged bundle rather than by the publish that produced either half.
  publish x64 app
  publish arm64 app

  fat_bundle="${root}/UnoTextPad/bin/${configuration}/osx-universal/UnoTextPad.app"
  rm -rf "${fat_bundle}"
  mkdir -p "$(dirname "${fat_bundle}")"

  run_packaging_target UnoMergeBundles \
    -p:UnoX64Bundle="$(publish_dir x64)/UnoTextPad.app" \
    -p:UnoArm64Bundle="$(publish_dir arm64)/UnoTextPad.app" \
    -p:UnoFatBundle="${fat_bundle}"

  run_packaging_target UnoCreateDiskImage -p:AppBundlePath="${fat_bundle}"

  disk_image="${root}/UnoTextPad/bin/${configuration}/osx-universal/UnoTextPad.dmg"
  ;;
*)
  echo "Unknown target '${target}'." >&2
  cat <<'EOF' >&2

Usage:
  ./publish-macos.sh [arm64|x64|universal]

  arm64      Apple Silicon only (default).
  x64        Intel only.
  universal  One bundle that runs on both, merged from the two above.
EOF
  exit 1
  ;;
esac

echo
echo "Disk image: ${disk_image#"${root}/"}"
cat <<'EOF'

The bundle inside it is ad-hoc signed, which is enough to run on this machine.
Copying the disk image through a browser, a mail client or AirDrop marks it as
quarantined, and Gatekeeper then refuses to open an app that is not signed with a
Developer ID certificate and notarized. The Publishing section of README.md covers
what that takes; to clear the flag on a machine you control:

  xattr -d -r com.apple.quarantine /Applications/UnoTextPad.app
EOF
