#!/usr/bin/env bash
# Pack the Linux RPM, then layer it with rpm-ostree.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
bash "$ROOT/packaging/pack.sh" linux
bash "$ROOT/packaging/rider-install-linux.sh"
