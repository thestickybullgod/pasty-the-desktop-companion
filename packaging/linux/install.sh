#!/bin/sh
# Installs Pasty for the current user. No root needed.
#   sh install.sh            install (or upgrade)
#   sh install.sh --uninstall
set -eu

APP_DIR="${XDG_DATA_HOME:-$HOME/.local/share}/pasty"
BIN_DIR="$HOME/.local/bin"
DESKTOP_DIR="${XDG_DATA_HOME:-$HOME/.local/share}/applications"
ICON_DIR="${XDG_DATA_HOME:-$HOME/.local/share}/icons/hicolor/256x256/apps"
HERE="$(cd "$(dirname "$0")" && pwd)"

if [ "${1:-}" = "--uninstall" ]; then
    rm -rf "$APP_DIR"
    rm -f "$BIN_DIR/pasty" "$DESKTOP_DIR/pasty.desktop" "$ICON_DIR/pasty.png"
    rm -f "${XDG_CONFIG_HOME:-$HOME/.config}/autostart/pasty.desktop"
    echo "Pasty has left the chat. (Your settings are still in ~/.config/Pasty if you come back.)"
    exit 0
fi

mkdir -p "$APP_DIR" "$BIN_DIR" "$DESKTOP_DIR" "$ICON_DIR"
cp "$HERE/Pasty" "$APP_DIR/Pasty"
chmod +x "$APP_DIR/Pasty"
ln -sf "$APP_DIR/Pasty" "$BIN_DIR/pasty"
cp "$HERE/pasty.png" "$ICON_DIR/pasty.png"

cat > "$DESKTOP_DIR/pasty.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=Pasty the Desktop Companion
GenericName=Desktop Companion
Comment=Pasty, your desktop companion. Born on Halloween 2026.
Exec="$APP_DIR/Pasty"
Icon=pasty
Terminal=false
Categories=Utility;
Actions=Settings;

[Desktop Action Settings]
Name=Pasty Settings
Exec="$APP_DIR/Pasty" --settings
EOF

command -v update-desktop-database >/dev/null 2>&1 && update-desktop-database "$DESKTOP_DIR" >/dev/null 2>&1 || true

echo "Pasty is installed. Find it in your app menu, or run: pasty"
case ":$PATH:" in
    *":$BIN_DIR:"*) ;;
    *) echo "(Note: $BIN_DIR isn't on your PATH, so use the app menu or run $APP_DIR/Pasty.)" ;;
esac
