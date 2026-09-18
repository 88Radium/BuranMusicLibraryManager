#!/usr/bin/env bash
# Pack the Linux RPM, then replace any layered buran via rpm-ostree.
set -uo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

echo "========================================"
echo " Buran: Linux-RPM packen und installieren"
echo "========================================"
echo "1/2  Packen …"
echo
bash "$ROOT/packaging/pack.sh" linux
pack_rc=$?
if [[ $pack_rc -ne 0 ]]; then
  echo "Packen fehlgeschlagen (Exit $pack_rc)." >&2
  exit "$pack_rc"
fi

echo
echo "2/2  Installieren (ersetzt vorhandenes buran, falls vorhanden) …"
echo
bash "$ROOT/packaging/rider-install-linux.sh"
