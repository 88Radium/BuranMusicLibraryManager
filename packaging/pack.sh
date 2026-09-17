#!/usr/bin/env bash
# Build distributable packages (WiX-equivalent: publish folder, then wrap it):
#   Linux:   dist/buran_<ver>_amd64.deb
#            dist/buran-<ver>-1.x86_64.rpm
#   Optional: dist/Buran-<ver>-x86_64.AppImage   (pack.sh appimage)
#   Windows: dist/Buran-<ver>-win-x64.zip
#            dist/Buran-<ver>-win-x64-setup.exe  (when iscc is on PATH)
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

export PATH="${HOME}/.dotnet:/usr/share/dotnet:${PATH}"
if [[ -z "${DOTNET:-}" ]]; then
  if command -v dotnet >/dev/null 2>&1; then
    DOTNET=dotnet
  elif [[ -x "${HOME}/.dotnet/dotnet" ]]; then
    DOTNET="${HOME}/.dotnet/dotnet"
  else
    echo "dotnet SDK nicht gefunden." >&2
    exit 1
  fi
fi

if command -v python3 >/dev/null 2>&1; then
  PYTHON=python3
elif command -v python >/dev/null 2>&1; then
  PYTHON=python
else
  echo "python3 nicht gefunden." >&2
  exit 1
fi

VERSION="$("$PYTHON" - <<'PY'
import re, pathlib
text = pathlib.Path("Directory.Build.props").read_text()
m = re.search(r"<Version>([^<]+)</Version>", text)
print(m.group(1).strip() if m else "0.1")
PY
)"

TARGET="${1:-all}"
CONFIG="${CONFIG:-Release}"
DIST="$ROOT/dist"
PACK="$ROOT/packaging"
BUILD="$PACK/build"

mkdir -p "$DIST" "$BUILD"

# Git Bash paths like /d/a/foo are not valid for Windows Python or iscc.
native_path() {
  if command -v cygpath >/dev/null 2>&1; then
    cygpath -w "$1"
  else
    printf '%s' "$1"
  fi
}

publish_host() {
  local rid="$1" out="$2"
  rm -rf "$out"
  mkdir -p "$out"
  "$DOTNET" publish "$ROOT/BuranUI/BuranUI.csproj" \
    -c "$CONFIG" \
    -r "$rid" \
    --self-contained true \
    -p:PublishSingleFile=false \
    -p:PublishTrimmed=false \
    -p:DebugType=portable \
    -o "$out"
}

ensure_plugins() {
  local rid="$1" out="$2"
  local missing=0
  local p
  for p in Buran.Player Buran.ID3Editor Buran.DBEditor; do
    if [[ ! -f "$out/$p.dll" ]]; then
      missing=1
      break
    fi
  done
  if [[ "$missing" -eq 0 ]]; then
    return 0
  fi
  echo "MEF-Plugins nachpublishen nach $out"
  for p in Buran.Player Buran.ID3Editor Buran.DBEditor; do
    "$DOTNET" publish "$ROOT/$p/$p.csproj" \
      -c "$CONFIG" \
      -r "$rid" \
      --self-contained false \
      -p:PublishSingleFile=false \
      -p:PublishTrimmed=false \
      -o "$out"
  done
}

copy_linux_vlc() {
  local out="$1"
  local libdir="" plug=""
  if [[ -e /usr/lib64/libvlc.so.5 ]]; then
    libdir=/usr/lib64
  elif [[ -e /usr/lib/x86_64-linux-gnu/libvlc.so.5 ]]; then
    libdir=/usr/lib/x86_64-linux-gnu
  elif [[ -e /usr/lib/libvlc.so.5 ]]; then
    libdir=/usr/lib
  fi
  if [[ -d /usr/lib64/vlc/plugins ]]; then
    plug=/usr/lib64/vlc
  elif [[ -d /usr/lib/x86_64-linux-gnu/vlc/plugins ]]; then
    plug=/usr/lib/x86_64-linux-gnu/vlc
  elif [[ -d /usr/lib/vlc/plugins ]]; then
    plug=/usr/lib/vlc
  fi

  if [[ ! -e "$out/libvlc.so" && ! -e "$out/libvlc.so.5" ]]; then
    if [[ -z "$libdir" ]]; then
      echo "Warnung: libvlc nicht gefunden. Player braucht vlc-libs auf der Build-Maschine." >&2
      return 0
    fi
    echo "libvlc aus $libdir nach $out kopieren"
    cp -a "$libdir"/libvlc.so* "$out/" 2>/dev/null || true
    cp -a "$libdir"/libvlccore.so* "$out/" 2>/dev/null || true
  fi
  if [[ ! -e "$out/libvlc.so" && -e "$out/libvlc.so.5" ]]; then
    cp -L "$out/libvlc.so.5" "$out/libvlc.so" 2>/dev/null || cp -a "$out/libvlc.so.5" "$out/libvlc.so"
  fi
  if [[ ! -e "$out/libvlccore.so" && -e "$out/libvlccore.so.9" ]]; then
    cp -L "$out/libvlccore.so.9" "$out/libvlccore.so" 2>/dev/null || cp -a "$out/libvlccore.so.9" "$out/libvlccore.so"
  fi
  if [[ ! -d "$out/vlc/plugins" && -n "$plug" ]]; then
    echo "VLC-Plugins aus $plug nach $out/vlc kopieren"
    mkdir -p "$out/vlc"
    cp -a "$plug/plugins" "$out/vlc/"
    cp -a "$plug"/libvlc_*.so* "$out/vlc/" 2>/dev/null || true
  fi
}

require_bundled_vlc() {
  local dir="$1"
  if [[ ! -e "$dir/libvlc.so" && ! -e "$dir/libvlc.so.5" ]]; then
    echo "Gebündeltes libvlc fehlt in $dir. Auf der Build-Maschine vlc-libs / vlc-plugins-base installieren." >&2
    exit 1
  fi
  if [[ ! -d "$dir/vlc/plugins" ]]; then
    echo "Gebündelte VLC-Plugins fehlen in $dir/vlc/plugins." >&2
    exit 1
  fi
}

copy_windows_vlc() {
  local out="$1"
  if [[ -f "$out/libvlc/win-x64/libvlc.dll" && -d "$out/libvlc/win-x64/plugins" ]]; then
    echo "Windows libvlc liegt in $out/libvlc/win-x64"
    return 0
  fi
  if [[ -f "$out/libvlc.dll" && -d "$out/plugins" ]]; then
    echo "Windows libvlc liegt neben der App in $out"
    return 0
  fi

  local root="${NUGET_PACKAGES:-${HOME}/.nuget/packages}/videolan.libvlc.windows"
  local src=""
  if [[ -d "$root" ]]; then
    src="$(find "$root" -type d -path '*/build/x64' 2>/dev/null | sort | tail -1)"
  fi
  if [[ -z "$src" || ! -f "$src/libvlc.dll" ]]; then
    echo "VideoLAN.LibVLC.Windows nicht im NuGet-Cache ($root)." >&2
    return 1
  fi
  echo "libvlc aus $src nach $out/libvlc/win-x64 kopieren"
  mkdir -p "$out/libvlc/win-x64"
  cp -a "$src"/. "$out/libvlc/win-x64/"
}

require_windows_vlc() {
  local out="$1"
  if [[ -f "$out/libvlc/win-x64/libvlc.dll" && -d "$out/libvlc/win-x64/plugins" ]]; then
    return 0
  fi
  if [[ -f "$out/libvlc.dll" && -d "$out/plugins" ]]; then
    return 0
  fi
  echo "Windows-libvlc fehlt in $out (libvlc.dll + plugins)." >&2
  ls -la "$out" >&2 || true
  ls -la "$out/libvlc" >&2 || true
  exit 1
}

find_windows_ffmpeg() {
  local hit choc
  if [[ -d /c/ProgramData/chocolatey/lib/ffmpeg ]]; then
    hit="$(find /c/ProgramData/chocolatey/lib/ffmpeg -type f -name ffmpeg.exe -path '*/bin/ffmpeg.exe' 2>/dev/null | head -1)"
    if [[ -n "$hit" ]]; then
      printf '%s' "$hit"
      return 0
    fi
  fi
  if [[ -n "${ChocolateyInstall:-}" ]]; then
    choc="$(cygpath -u "$ChocolateyInstall" 2>/dev/null || true)"
    if [[ -d "$choc/lib/ffmpeg" ]]; then
      hit="$(find "$choc/lib/ffmpeg" -type f -name ffmpeg.exe -path '*/bin/ffmpeg.exe' 2>/dev/null | head -1)"
      if [[ -n "$hit" ]]; then
        printf '%s' "$hit"
        return 0
      fi
    fi
  fi
  return 1
}

download_windows_ffmpeg() {
  local tmp="$BUILD/ffmpeg-win"
  mkdir -p "$tmp"
  local zip="$tmp/ffmpeg.zip"
  echo "ffmpeg Windows-Build wird geladen …" >&2
  if ! curl -fL --retry 3 -o "$zip" "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"; then
    curl -fL --retry 3 -o "$zip" \
      "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip"
  fi
  if [[ ! -s "$zip" ]]; then
    return 1
  fi
  if command -v unzip >/dev/null 2>&1; then
    unzip -qo "$zip" -d "$tmp"
  else
    "$PYTHON" - "$zip" "$tmp" <<'PY'
import sys, zipfile
zip_path, dest = sys.argv[1], sys.argv[2]
with zipfile.ZipFile(zip_path) as z:
    z.extractall(dest)
PY
  fi
  find "$tmp" -type f -name ffmpeg.exe | head -1
}

copy_windows_ffmpeg() {
  local out="$1"
  if [[ -f "$out/ffmpeg.exe" ]]; then
    echo "ffmpeg.exe liegt in $out"
    return 0
  fi
  local src
  src="$(find_windows_ffmpeg || true)"
  if [[ -z "$src" || ! -f "$src" ]]; then
    src="$(download_windows_ffmpeg || true)"
  fi
  if [[ -z "$src" || ! -f "$src" ]]; then
    echo "ffmpeg.exe nicht gefunden." >&2
    return 1
  fi
  echo "ffmpeg aus $src nach $out kopieren"
  cp "$src" "$out/ffmpeg.exe"
}

require_windows_ffmpeg() {
  if [[ ! -f "$1/ffmpeg.exe" ]]; then
    echo "ffmpeg.exe fehlt in $1 — Spektrogramm auf Windows braucht das." >&2
    exit 1
  fi
}

ensure_appimagetool() {
  local tool="$BUILD/appimagetool-x86_64.AppImage"
  if [[ -x "$tool" ]]; then
    return 0
  fi
  echo "appimagetool wird geladen …" >&2
  curl -fsSL -o "$tool" \
    "https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage"
  chmod +x "$tool" 2>/dev/null || true
}

NFPM_VERSION_PIN="2.47.0"

ensure_nfpm() {
  local bin="$BUILD/nfpm"
  if [[ -x "$bin" ]]; then
    return 0
  fi
  echo "nFPM ${NFPM_VERSION_PIN} wird geladen …" >&2
  local archive="$BUILD/nfpm.tgz"
  curl -fsSL -o "$archive" \
    "https://github.com/goreleaser/nfpm/releases/download/v${NFPM_VERSION_PIN}/nfpm_${NFPM_VERSION_PIN}_Linux_x86_64.tar.gz"
  tar -xzf "$archive" -C "$BUILD" nfpm
  chmod +x "$bin" 2>/dev/null || true
  rm -f "$archive"
}

publish_linux() {
  local out="$DIST/linux-x64"
  echo "==> Linux publish (linux-x64, self-contained)"
  publish_host linux-x64 "$out"
  ensure_plugins linux-x64 "$out"

  if [[ ! -f "$out/BuranUI" ]]; then
    echo "BuranUI fehlt in $out" >&2
    exit 1
  fi
  if [[ ! -f "$out/Buran.Player.dll" || ! -f "$out/Buran.ID3Editor.dll" || ! -f "$out/Buran.DBEditor.dll" ]]; then
    echo "MEF-Plugins fehlen in $out" >&2
    ls -1 "$out"/Buran.*.dll >&2 || true
    exit 1
  fi
  chmod +x "$out/BuranUI" 2>/dev/null || true
  # VideoLAN.LibVLC.Windows native assets follow the host publish even on linux-x64.
  rm -rf "$out/libvlc"
  copy_linux_vlc "$out"
  require_bundled_vlc "$out"
}

stage_linux_root() {
  local out="$1"
  local root="$BUILD/linux-root"
  rm -rf "$root"
  mkdir -p \
    "$root/usr/lib/buran" \
    "$root/usr/bin" \
    "$root/usr/share/applications" \
    "$root/usr/share/icons/hicolor/256x256/apps"
  cp -a "$out"/. "$root/usr/lib/buran/"
  chmod +x "$root/usr/lib/buran/BuranUI" 2>/dev/null || true
  cp "$PACK/linux/buran.sh" "$root/usr/bin/buran"
  chmod +x "$root/usr/bin/buran" 2>/dev/null || true
  cp "$PACK/linux/buran.desktop" "$root/usr/share/applications/buran.desktop"
  cp "$PACK/linux/buran.png" "$root/usr/share/icons/hicolor/256x256/apps/buran.png"
  require_bundled_vlc "$root/usr/lib/buran"
}

pack_linux_native() {
  local out="$DIST/linux-x64"
  local root="$BUILD/linux-root"
  local nfpm="$BUILD/nfpm"
  publish_linux
  stage_linux_root "$out"
  ensure_nfpm

  export NFPM_VERSION="$VERSION"
  export LINUX_ROOT="$root"
  local nfpm_yaml="$BUILD/nfpm.resolved.yaml"
  sed \
    -e "s|\${NFPM_VERSION}|${VERSION}|g" \
    -e "s|\${LINUX_ROOT}|${root}|g" \
    "$PACK/linux/nfpm.yaml" > "$nfpm_yaml"

  echo "==> .rpm und .deb (nFPM)"
  "$nfpm" package -f "$nfpm_yaml" --packager rpm --target "$DIST"
  "$nfpm" package -f "$nfpm_yaml" --packager deb --target "$DIST"
  if ! ls "$DIST"/*.rpm >/dev/null 2>&1; then
    echo "RPM fehlt in $DIST" >&2
    ls -lh "$DIST" >&2 || true
    exit 1
  fi
  echo "Linux-Pakete in $DIST :"
  ls -lh "$DIST"/*.rpm "$DIST"/*.deb
}

pack_linux_appimage() {
  local out="$DIST/linux-x64"
  local appdir="$BUILD/Buran.AppDir"
  local tool="$BUILD/appimagetool-x86_64.AppImage"
  local image="$DIST/Buran-${VERSION}-x86_64.AppImage"
  publish_linux
  copy_linux_vlc "$out"

  rm -rf "$appdir"
  mkdir -p \
    "$appdir/usr/lib/buran" \
    "$appdir/usr/bin" \
    "$appdir/usr/share/applications" \
    "$appdir/usr/share/icons/hicolor/256x256/apps"

  cp -a "$out"/. "$appdir/usr/lib/buran/"
  chmod +x "$appdir/usr/lib/buran/BuranUI" 2>/dev/null || true
  ln -s ../lib/buran/BuranUI "$appdir/usr/bin/buran"

  cp "$PACK/linux/AppRun" "$appdir/AppRun"
  chmod +x "$appdir/AppRun" 2>/dev/null || true
  cp "$PACK/linux/buran.desktop" "$appdir/buran.desktop"
  cp "$PACK/linux/buran.desktop" "$appdir/usr/share/applications/buran.desktop"
  cp "$PACK/linux/buran.png" "$appdir/buran.png"
  cp "$PACK/linux/buran.png" "$appdir/usr/share/icons/hicolor/256x256/apps/buran.png"

  ensure_appimagetool
  rm -f "$image"
  echo "==> AppImage $image"
  export ARCH=x86_64
  export APPIMAGE_EXTRACT_AND_RUN=1
  export VERSION
  if ! "$tool" --appimage-extract-and-run "$appdir" "$image"; then
    "$tool" "$appdir" "$image"
  fi
  chmod +x "$image" 2>/dev/null || true
  echo "Linux-Paket: $image"
}

pack_windows() {
  local out="$DIST/win-x64"
  echo "==> Windows publish (win-x64, self-contained, von $(uname -s))"
  publish_host win-x64 "$out"
  ensure_plugins win-x64 "$out"

  if [[ ! -f "$out/BuranUI.exe" ]]; then
    echo "BuranUI.exe fehlt in $out" >&2
    exit 1
  fi
  if [[ ! -f "$out/Buran.Player.dll" || ! -f "$out/Buran.ID3Editor.dll" || ! -f "$out/Buran.DBEditor.dll" ]]; then
    echo "MEF-Plugins fehlen in $out" >&2
    exit 1
  fi
  copy_windows_vlc "$out" || true
  require_windows_vlc "$out"
  copy_windows_ffmpeg "$out" || true
  require_windows_ffmpeg "$out"

  cp "$PACK/windows/install.ps1"   "$out/install.ps1"
  cp "$PACK/windows/install.bat"   "$out/install.bat"
  cp "$PACK/windows/uninstall.ps1" "$out/uninstall.ps1"
  cp "$PACK/windows/uninstall.bat" "$out/uninstall.bat"
  cp "$PACK/windows/Liesmich.txt"  "$out/Liesmich.txt"

  local zip="$DIST/Buran-${VERSION}-win-x64.zip"
  local staged="$DIST/Buran-${VERSION}-win-x64"
  rm -f "$zip"
  rm -rf "$staged"
  mkdir "$staged"
  cp -a "$out"/. "$staged/"
  echo "==> ZIP $zip"
  local zip_name staged_name
  zip_name="$(basename "$zip")"
  staged_name="$(basename "$staged")"
  (
    cd "$DIST"
    if command -v zip >/dev/null 2>&1; then
      zip -r -q "$zip_name" "$staged_name"
    else
      "$PYTHON" - "$staged_name" "$zip_name" <<'PY'
import pathlib, sys, zipfile
root = pathlib.Path(sys.argv[1])
zip_path = pathlib.Path(sys.argv[2])
if not root.is_dir():
    raise SystemExit(f"staged folder missing: {root.resolve()}")
with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as z:
    for p in root.rglob("*"):
        if p.is_file():
            z.write(p, p.relative_to(root.parent).as_posix())
PY
    fi
  )
  if [[ ! -f "$zip" ]]; then
    echo "ZIP fehlt: $zip" >&2
    ls -lh "$DIST" >&2 || true
    exit 1
  fi
  rm -rf "$staged"

  if command -v iscc >/dev/null 2>&1; then
    echo "==> Inno Setup"
    # MSYS would rewrite /DMyAppVersion=... into a drive path.
    MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' \
      iscc "$(native_path "$PACK/windows/buran.iss")" \
        "/DMyAppVersion=${VERSION}" \
        "/DMySourceDir=$(native_path "$out")" \
        "/DMyOutputDir=$(native_path "$DIST")"
  else
    echo "Inno Setup (iscc) nicht vorhanden — nur ZIP. Setup.exe entsteht in GitHub Actions auf windows-latest."
    if [[ "${REQUIRE_INNO:-}" == "1" ]]; then
      echo "REQUIRE_INNO=1: iscc ist Pflicht." >&2
      exit 1
    fi
  fi

  if [[ "${REQUIRE_INNO:-}" == "1" ]]; then
    local setup="$DIST/Buran-${VERSION}-win-x64-setup.exe"
    if [[ ! -f "$setup" ]]; then
      echo "Windows-Installer fehlt: $setup" >&2
      ls -lh "$DIST" >&2 || true
      exit 1
    fi
  fi
  echo "Windows-Paket: $zip"
}

case "$TARGET" in
  linux|rpm|deb) pack_linux_native ;;
  appimage)      pack_linux_appimage ;;
  windows)       pack_windows ;;
  all)           pack_linux_native; pack_windows ;;
  *)
    echo "Nutzung: $0 [linux|appimage|windows|all]" >&2
    exit 1
    ;;
esac

echo
echo "Fertig. Dateien in $DIST :"
ls -lh "$DIST"/*.{rpm,deb,AppImage,zip,exe} 2>/dev/null || ls -lh "$DIST"
