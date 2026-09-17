#!/bin/sh
# Installed launcher (/usr/bin/buran). Catalog DB stays in ~/.local/share/Buran.
# LD_LIBRARY_PATH must be set before exec — setenv from C# is too late for dlopen.
APP="/usr/lib/buran"
LIBS="$APP"
if [ -d "$APP/vlc" ]; then
  LIBS="$APP:$APP/vlc"
fi
export LD_LIBRARY_PATH="$LIBS${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
if [ -d "$APP/vlc/plugins" ]; then
  export VLC_PLUGIN_PATH="$APP/vlc/plugins"
fi
exec "$APP/BuranUI" "$@"
