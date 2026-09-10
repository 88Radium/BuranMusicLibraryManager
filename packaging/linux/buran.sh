#!/bin/sh
# Installed launcher (/usr/bin/buran). Catalog DB stays in ~/.local/share/Buran.
APP="/usr/lib/buran"
export LD_LIBRARY_PATH="$APP${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
if [ -d "$APP/vlc/plugins" ]; then
  export VLC_PLUGIN_PATH="$APP/vlc/plugins"
fi
exec "$APP/BuranUI" "$@"
