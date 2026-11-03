#!/bin/sh
# The One He Missed for Linux: puts the game in your home folder and adds it to your app menu.
#
#   ./install.sh               install, or update an installed copy
#   ./install.sh --uninstall   take it out again (your save is kept)
#
# It installs to ~/.local/share/TheOneHeMissed, which you own, so the game can update itself
# there - the same reason the Windows installer installs per user. No sudo, nothing outside your
# home. The save lives in ~/.local/share/godot/app_userdata/The One He Missed and is never touched.
set -e

# The same as the start of every release file's name (TheOneHeMissed-linux.tar.gz): ecec.dev's
# download page tells people the game installs itself to ~/.local/share/<that name>.
APP=TheOneHeMissed
TITLE="The One He Missed"
DATA="${XDG_DATA_HOME:-$HOME/.local/share}"
HERE=$(cd "$(dirname "$0")" && pwd)
DEST="$DATA/$APP"
MENU="$DATA/applications/theonehemissed.desktop"

# KEPT ON PURPOSE: the game was called Wizard Survivors until 0.12.0, and installed itself under
# that name. Such an install keeps updating itself where it is, under its old program name (see
# .ai/releasing.md, "The rename"); this script retires it once a new install replaces it, and an
# uninstall takes out both. Its save is in its own folder and is never touched either.
OLD_DEST="$DATA/WizardSurvivors"
OLD_MENU="$DATA/applications/wizardsurvivors.desktop"

if [ "$1" = "--uninstall" ]; then
	rm -rf "$DEST"
	rm -f "$MENU"
	if [ -f "$OLD_DEST/install.sh" ]; then rm -rf "$OLD_DEST"; fi
	rm -f "$OLD_MENU"
	echo "$TITLE is uninstalled. Your save is still in ~/.local/share/godot/app_userdata/$TITLE."
	exit 0
fi

if [ "$HERE" = "$OLD_DEST" ]; then
	# Run from inside an install from before the rename: refresh it where it is.
	DEST="$OLD_DEST"
elif [ "$HERE" != "$DEST" ]; then
	mkdir -p "$DEST"
	cp -Rf "$HERE/." "$DEST/"
	if [ -f "$OLD_DEST/install.sh" ]; then
		rm -rf "$OLD_DEST"
		echo "Removed the copy installed under the game's old name ($OLD_DEST)."
	fi
fi

# The program: TheOneHeMissed.x86_64, or in an install from before the rename the old name it kept.
PROGRAM=""
for candidate in "$DEST/$APP.x86_64" "$DEST"/*.x86_64; do
	if [ -f "$candidate" ]; then PROGRAM="$candidate"; break; fi
done
[ -n "$PROGRAM" ] || { echo "no game program (*.x86_64) in $DEST" >&2; exit 1; }
chmod +x "$PROGRAM" "$DEST/install.sh"

mkdir -p "$(dirname "$MENU")"
cat > "$MENU" <<DESKTOP
[Desktop Entry]
Type=Application
Name=$TITLE
Comment=A lone wizard against the dead in a haunted wood
Exec="$PROGRAM"
Path=$DEST
Icon=$DEST/icon.png
Terminal=false
Categories=Game;
DESKTOP
chmod +x "$MENU"
rm -f "$OLD_MENU"
command -v update-desktop-database >/dev/null 2>&1 && update-desktop-database "$(dirname "$MENU")" >/dev/null 2>&1 || true

echo "$TITLE is installed. Open it from your app menu, or run $PROGRAM"
echo "It keeps itself up to date."
