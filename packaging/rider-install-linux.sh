#!/usr/bin/env bash
# Install the RPM that matches Directory.Build.props <Version> (Bazzite / rpm-ostree).
# Replaces an already layered buran in the same transaction.
# No `set -e`: cancelled auth must print a reason, not vanish.
set -uo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

log() { printf '%s\n' "$*"; }
err() { printf '%s\n' "$*" >&2; }

# Bazzite exports alias sudo='sudo -A' and SUDO_ASKPASS=ksshaskpass.
# That askpass (and pkexec's polkit dialog) appear behind Rider on Wayland.
unalias sudo 2>/dev/null || true
unset SUDO_ASKPASS SSH_ASKPASS

ASKPASS_TMP=""
PKTTY_PID=""
elevate=()

cleanup() {
  if [[ -n "${PKTTY_PID}" ]]; then
    kill "${PKTTY_PID}" 2>/dev/null || true
  fi
  if [[ -n "${ASKPASS_TMP}" && -f "${ASKPASS_TMP}" ]]; then
    rm -f "${ASKPASS_TMP}"
  fi
}

pause_if_tty() {
  echo
  if [[ -t 0 && -t 1 ]]; then
    read -r -p "Enter zum Schließen … " _ || true
  else
    log "(Kein interaktives Terminal — Fenster bleibt über Rider offen.)"
  fi
}

trap 'status=$?
cleanup
echo
if [[ $status -eq 0 ]]; then
  log "Fertig (Exit 0)."
else
  err "Abbruch (Exit $status)."
fi
pause_if_tty' EXIT

import_graphical_env() {
  command -v systemctl >/dev/null 2>&1 || return 0
  local line
  while IFS= read -r line; do
    case "$line" in
      DISPLAY=*|WAYLAND_DISPLAY=*|XDG_RUNTIME_DIR=*|DBUS_SESSION_BUS_ADDRESS=*|XAUTHORITY=*|XDG_SESSION_TYPE=*|XDG_CURRENT_DESKTOP=*)
        export "$line"
        ;;
    esac
  done < <(systemctl --user show-environment 2>/dev/null || true)
}

make_kdialog_askpass() {
  ASKPASS_TMP="$(mktemp "${TMPDIR:-/tmp}/buran-askpass.XXXXXX")"
  cat > "$ASKPASS_TMP" << 'ASK'
#!/bin/bash
prompt="${1:-Administratorpasswort für Buran (rpm-ostree):}"
if command -v kdialog >/dev/null 2>&1; then
  exec kdialog --title "Buran — Installation" --password "$prompt"
fi
if command -v zenity >/dev/null 2>&1; then
  exec zenity --title="Buran — Installation" --password --timeout=120
fi
printf '%s\n' "Kein Passwort-Dialog (kdialog/zenity) gefunden." >&2
exit 1
ASK
  chmod 700 "$ASKPASS_TMP"
}

have_tty() { [[ -t 0 && -t 1 ]]; }

setup_elevate() {
  if [[ "$(id -u)" -eq 0 ]]; then
    elevate=()
    log "Rechte: bereits root."
    return
  fi

  if [[ -x /usr/bin/sudo ]]; then
    if have_tty; then
      elevate=(/usr/bin/sudo -p "Buran (rpm-ostree) Passwort für %p: ")
      log "Rechte: sudo — Passwort JETZT in DIESEM Terminal eingeben."
      log "Es kommt kein Extra-Fenster: pkexec/ksshaskpass wären hinter Rider verschwunden."
      return
    fi
    make_kdialog_askpass
    export SUDO_ASKPASS="$ASKPASS_TMP"
    elevate=(/usr/bin/sudo -A)
    log "Rechte: kein TTY — KDE-Dialog „Buran — Installation“."
    log "Falls unsichtbar: Taskleiste, andere Arbeitsfläche, hinter Rider."
    return
  fi

  if command -v pkexec >/dev/null 2>&1; then
    if command -v pkttyagent >/dev/null 2>&1; then
      pkttyagent --process $$ >/dev/null 2>&1 &
      PKTTY_PID=$!
      log "Rechte: pkexec + pkttyagent — Passwort in DIESEM Terminal."
    else
      log "Rechte: pkexec — grafischer Polkit-Dialog (oft hinter Rider)."
    fi
    elevate=(pkexec)
    return
  fi

  err "Weder sudo noch pkexec gefunden, und nicht als root."
  exit 1
}

buran_is_layered() {
  if command -v rpm >/dev/null 2>&1 && rpm -q buran >/dev/null 2>&1; then
    return 0
  fi
  command -v rpm-ostree >/dev/null 2>&1 || return 1
  rpm-ostree status --json 2>/dev/null | "$PYTHON" -c '
import json, sys
try:
    data = json.load(sys.stdin)
except Exception:
    sys.exit(1)
for dep in data.get("deployments") or []:
    values = []
    for key, val in dep.items():
        lk = key.lower()
        if "package" not in lk and "local" not in lk:
            continue
        if isinstance(val, list):
            values.extend(val)
        elif val:
            values.append(val)
    for item in values:
        s = str(item)
        if s == "buran" or s.startswith("buran-"):
            sys.exit(0)
sys.exit(1)
'
}

import_graphical_env

log "========================================"
log " Buran: Linux-RPM installieren"
log "========================================"
log "Projekt: $ROOT"
log "Zeit:    $(date '+%Y-%m-%d %H:%M:%S')"
log

PYTHON=""
if command -v python3 >/dev/null 2>&1; then
  PYTHON=python3
elif command -v python >/dev/null 2>&1; then
  PYTHON=python
else
  err "python3 nicht gefunden."
  exit 1
fi

VERSION="$("$PYTHON" -c 'import re, pathlib; t=pathlib.Path("Directory.Build.props").read_text(); m=re.search(r"<Version>([^<]+)</Version>", t); print((m.group(1) if m else "").strip())')"
if [[ -z "$VERSION" ]]; then
  err "Version in Directory.Build.props nicht lesbar."
  exit 1
fi
log "Zielversion aus Directory.Build.props: $VERSION"

shopt -s nullglob
matches=("$ROOT/dist/buran-${VERSION}-"*.x86_64.rpm)
if [[ ${#matches[@]} -eq 0 ]]; then
  err "Kein RPM für Version ${VERSION} in dist/."
  err "Zuerst die Run-Konfiguration „Pack Linux“ ausführen."
  log
  log "RPMs in dist/:"
  local_rpms=("$ROOT/dist"/*.rpm)
  if [[ ${#local_rpms[@]} -eq 0 ]]; then
    log "  (keine .rpm-Dateien)"
  else
    ls -lh "$ROOT/dist"/*.rpm
  fi
  exit 1
fi

rpm="${matches[0]}"
for f in "${matches[@]}"; do
  [[ "$f" -nt "$rpm" ]] && rpm="$f"
done
log "RPM:    $rpm"
log "Größe:  $(du -h "$rpm" | cut -f1)"
log

installed="(nicht installiert)"
if command -v rpm >/dev/null 2>&1 && rpm -q buran >/dev/null 2>&1; then
  installed="$(rpm -q --qf '%{VERSION}-%{RELEASE}' buran 2>/dev/null || rpm -q buran)"
fi
log "Aktuell installiert: $installed"
log

RPM_OSTREE="$(command -v rpm-ostree || true)"
if [[ -z "$RPM_OSTREE" ]]; then
  err "rpm-ostree nicht gefunden. Dieses Skript ist für Bazzite / Fedora Atomic."
  err "PATH=$PATH"
  exit 1
fi
log "rpm-ostree: $RPM_OSTREE"

setup_elevate
log
log "Das kann ein bis mehrere Minuten dauern. Ausgabe von rpm-ostree folgt."
log "----------------------------------------"

if buran_is_layered; then
  log "Vorhandenes buran wird in derselben Transaktion entfernt und durch ${VERSION} ersetzt."
  log "Befehl: ${elevate[*]} $RPM_OSTREE install -y --uninstall=buran $rpm"
  "${elevate[@]}" "$RPM_OSTREE" install -y --uninstall=buran "$rpm"
  rc=$?
else
  log "Kein layered buran gefunden — Neuinstallation."
  log "Befehl: ${elevate[*]} $RPM_OSTREE install -y $rpm"
  "${elevate[@]}" "$RPM_OSTREE" install -y "$rpm"
  rc=$?
fi

log "----------------------------------------"
if [[ $rc -ne 0 ]]; then
  if [[ $rc -eq 126 || $rc -eq 127 ]]; then
    err "Passwort abgebrochen, oder sudo/pkexec nicht erlaubt."
  else
    err "rpm-ostree ist mit Exit $rc fehlgeschlagen."
  fi
  exit "$rc"
fi

log
log "rpm-ostree ist durch. Altes buran (falls vorhanden) ist aus der neuen Deployment-Schicht raus."
log "Die neue Version gilt erst nach einem Neustart:"
log "  sudo systemctl reboot"
