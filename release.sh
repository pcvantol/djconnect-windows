#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_PATH="$ROOT_DIR/src/DJConnect.Windows/DJConnect.Windows.csproj"
MACCATALYST_TFM="net10.0-maccatalyst"
WINDOWS_TFM="net10.0-windows10.0.19041.0"
RELEASE_DIR="$ROOT_DIR/.public-release"
ARTIFACT_DIR="$RELEASE_DIR/artifacts"
CONFIGURATION="Release"

BUILD_MAC=true
BUILD_WINDOWS=false
RUN_TESTS=true
RUN_WORKLOAD_RESTORE=true
START_MAC=false
CLEAN=true
VERSION=""

usage() {
  cat <<'USAGE'
Usage: ./release.sh [options]

Build local unsigned DJConnect release artifacts.

Options:
  --mac                 Build Mac Catalyst artifact (default)
  --windows             Build Windows x64 and arm64 artifacts
  --all                 Build Mac Catalyst and Windows artifacts
  --start-mac           Open the published Mac Catalyst app after release
  --version X.Y.Z       Override artifact version label
  --skip-tests          Do not run ./run_tests.sh
  --skip-workload       Do not run dotnet workload restore
  --no-clean            Keep existing .public-release before building
  -h, --help            Show this help

Environment:
  DEVELOPER_DIR         Optional Xcode path for Mac Catalyst builds
  MD_APPLE_SDK_ROOT     Optional Xcode app root for Mac Catalyst builds

Examples:
  ./release.sh --start-mac
  ./release.sh --all
  DEVELOPER_DIR=/Applications/Xcode_26.4.1.app/Contents/Developer ./release.sh --mac
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --mac)
      BUILD_MAC=true
      ;;
    --windows)
      BUILD_MAC=false
      BUILD_WINDOWS=true
      ;;
    --all)
      BUILD_MAC=true
      BUILD_WINDOWS=true
      ;;
    --start-mac)
      START_MAC=true
      BUILD_MAC=true
      ;;
    --version)
      if [[ $# -lt 2 ]]; then
        echo "--version requires a value" >&2
        exit 2
      fi
      VERSION="$2"
      shift
      ;;
    --skip-tests)
      RUN_TESTS=false
      ;;
    --skip-workload)
      RUN_WORKLOAD_RESTORE=false
      ;;
    --no-clean)
      CLEAN=false
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown option: $1" >&2
      usage >&2
      exit 2
      ;;
  esac
  shift
done

cd "$ROOT_DIR"

if [[ -z "$VERSION" ]]; then
  VERSION="$(sed -n 's:.*<ApplicationDisplayVersion>\(.*\)</ApplicationDisplayVersion>.*:\1:p' "$PROJECT_PATH" | head -n 1)"
fi

if [[ -z "$VERSION" ]]; then
  echo "Could not resolve release version from $PROJECT_PATH" >&2
  exit 1
fi

select_maccatalyst_xcode() {
  if [[ -n "${DEVELOPER_DIR:-}" ]]; then
    return
  fi

  for candidate in /Applications/Xcode_26.4.1.app /Applications/Xcode_26.4.app /Applications/Xcode.app; do
    if [[ -d "$candidate/Contents/Developer" ]]; then
      local version
      local xcode_output
      xcode_output="$(DEVELOPER_DIR="$candidate/Contents/Developer" xcodebuild -version 2>/dev/null || true)"
      version="$(awk '/Xcode/{print $2; exit}' <<< "$xcode_output")"
      if [[ "$version" == 26.4* ]]; then
        export DEVELOPER_DIR="$candidate/Contents/Developer"
        export MD_APPLE_SDK_ROOT="$candidate"
        echo "Using Xcode $version at $candidate"
        return
      fi
    fi
  done

  echo "No Xcode 26.4.x installation found. Set DEVELOPER_DIR and MD_APPLE_SDK_ROOT for Mac Catalyst release builds." >&2
  exit 1
}

maccatalyst_runtime_identifier() {
  case "$(uname -m)" in
    arm64)
      echo "maccatalyst-arm64"
      ;;
    x86_64|amd64)
      echo "maccatalyst-x64"
      ;;
    *)
      echo "Unsupported Mac architecture: $(uname -m)" >&2
      exit 1
      ;;
  esac
}

sha256_file() {
  local file="$1"
  local output="$2"
  shasum -a 256 "$file" > "$output"
}

if [[ "$RUN_TESTS" == true ]]; then
  ./run_tests.sh
fi

if [[ "$CLEAN" == true ]]; then
  rm -rf "$RELEASE_DIR"
fi

mkdir -p "$ARTIFACT_DIR"

if [[ "$BUILD_MAC" == true ]]; then
  select_maccatalyst_xcode
  mac_rid="$(maccatalyst_runtime_identifier)"

  if [[ "$RUN_WORKLOAD_RESTORE" == true ]]; then
    dotnet workload restore "$PROJECT_PATH" -p:TargetFramework="$MACCATALYST_TFM"
  fi

  dotnet restore "$PROJECT_PATH" \
    -p:TargetFramework="$MACCATALYST_TFM" \
    -p:RuntimeIdentifier="$mac_rid" \
    -r "$mac_rid"

  mac_output="$RELEASE_DIR/maccatalyst"
  mkdir -p "$mac_output"
  dotnet publish "$PROJECT_PATH" \
    -c "$CONFIGURATION" \
    -f "$MACCATALYST_TFM" \
    -r "$mac_rid" \
    --no-restore \
    -p:RuntimeIdentifier="$mac_rid" \
    -p:BuildIpa=False \
    -p:CodesignKey= \
    -p:CodesignProvision= \
    -o "$mac_output"

  mac_app_path="$(find "$mac_output" -maxdepth 3 -type d -name 'DJConnect.app' | head -n 1)"
  if [[ -z "$mac_app_path" ]]; then
    mac_app_path="$(find "$ROOT_DIR/src/DJConnect.Windows/bin/$CONFIGURATION/$MACCATALYST_TFM/$mac_rid" -maxdepth 3 -type d -name 'DJConnect.app' | head -n 1)"
  fi
  if [[ -z "$mac_app_path" ]]; then
    echo "Could not find DJConnect.app in $mac_output" >&2
    echo "Also checked src/DJConnect.Windows/bin/$CONFIGURATION/$MACCATALYST_TFM/$mac_rid" >&2
    find "$mac_output" -maxdepth 3 -print >&2
    exit 1
  fi

  mac_zip="$ARTIFACT_DIR/DJConnect-MacCatalyst-${VERSION}-unsigned.zip"
  ditto -c -k --keepParent "$mac_app_path" "$mac_zip"
  sha256_file "$mac_zip" "$ARTIFACT_DIR/DJConnect-MacCatalyst-${VERSION}-SHA256SUMS.txt"
  echo "Mac Catalyst artifact: $mac_zip"

  mac_pkg="$(find "$mac_output" -maxdepth 2 -type f -name 'DJConnect-*.pkg' | head -n 1)"
  if [[ -n "$mac_pkg" ]]; then
    pkg_artifact="$ARTIFACT_DIR/$(basename "$mac_pkg")"
    cp "$mac_pkg" "$pkg_artifact"
    sha256_file "$pkg_artifact" "$ARTIFACT_DIR/$(basename "$mac_pkg" .pkg)-PKG-SHA256SUMS.txt"
    echo "Mac Catalyst package: $pkg_artifact"
  fi

  if [[ "$START_MAC" == true ]]; then
    open "$mac_app_path"
  fi
fi

if [[ "$BUILD_WINDOWS" == true ]]; then
  dotnet restore "$PROJECT_PATH" -p:TargetFramework="$WINDOWS_TFM"

  for rid in win-x64 win-arm64; do
    output_dir="$RELEASE_DIR/windows/$rid"
    dotnet restore "$PROJECT_PATH" \
      -p:TargetFramework="$WINDOWS_TFM" \
      -p:RuntimeIdentifier="$rid" \
      -r "$rid"

    dotnet publish "$PROJECT_PATH" \
      -c "$CONFIGURATION" \
      -f "$WINDOWS_TFM" \
      -r "$rid" \
      --no-restore \
      -p:RuntimeIdentifier="$rid" \
      -p:PublishReadyToRun=false \
      -p:WindowsPackageType=None \
      -o "$output_dir"

    arch="x64"
    if [[ "$rid" == "win-arm64" ]]; then
      arch="arm64"
    fi

    windows_zip="$ARTIFACT_DIR/DJConnect-Windows-${arch}-${VERSION}-unsigned.zip"
    ditto -c -k "$output_dir" "$windows_zip"
    sha256_file "$windows_zip" "$ARTIFACT_DIR/DJConnect-Windows-${arch}-${VERSION}-SHA256SUMS.txt"
    echo "Windows ${arch} artifact: $windows_zip"
  done
fi

if compgen -G "$ARTIFACT_DIR/*.zip" > /dev/null; then
  (
    cd "$ARTIFACT_DIR"
    shasum -a 256 *.zip > SHA256SUMS.txt
  )
fi

echo "Release artifacts written to $ARTIFACT_DIR"
