#!/usr/bin/env bash
# Install the RPM that matches Directory.Build.props <Version> (Bazzite / rpm-ostree).
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

VERSION="$(python3 -c 'import re, pathlib; t=pathlib.Path("Directory.Build.props").read_text(); m=re.search(r"<Version>([^<]+)</Version>", t); print((m.group(1) if m else "").strip())')"
if [[ -z "$VERSION" ]]; then
  echo "Version in Directory.Build.props nicht lesbar." >&2
  exit 1
fi

shopt -s nullglob
matches=("$ROOT/dist/buran-${VERSION}-"*.x86_64.rpm)
if [[ ${#matches[@]} -eq 0 ]]; then
  echo "Kein RPM für Version ${VERSION} in dist/." >&2
  echo "Zuerst „Buran: Linux-RPM packen“ ausführen." >&2
  exit 1
fi
rpm="$(ls -t "${matches[@]}" | head -1)"
echo "Installiere $rpm"

elevate() {
  if [[ "$(id -u)" -eq 0 ]]; then
    "$@"
    return
  fi
  if command -v pkexec >/dev/null 2>&1; then
    pkexec "$@"
    return
  fi
  sudo "$@"
}

RPM_OSTREE="$(command -v rpm-ostree)"
if [[ -z "$RPM_OSTREE" ]]; then
  echo "rpm-ostree nicht gefunden. Dieses Skript ist für Bazzite / Fedora Atomic." >&2
  exit 1
fi

if rpm -q buran >/dev/null 2>&1; then
  elevate "$RPM_OSTREE" uninstall buran --install "$rpm"
else
  elevate "$RPM_OSTREE" install "$rpm"
fi

echo
echo "rpm-ostree ist durch. Die neue Version gilt nach einem Neustart:"
echo "  sudo systemctl reboot"
