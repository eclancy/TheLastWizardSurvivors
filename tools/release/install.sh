#!/bin/sh
# The Last Wizard: Survivors for Linux: puts the game in your home folder and adds it to your app menu.
#
#   ./install.sh               install, or update an installed copy
#   ./install.sh --uninstall   take it out again (your save is kept)
#
# It installs to ~/.local/share/TheLastWizardSurvivors, which you own, so the game can update itself
# there - the same reason the Windows installer installs per user. No sudo, nothing outside your
# home. The save lives in ~/.local/share/godot/app_userdata/The One He Missed and is never touched.
set -e

# The program's name, package.py's NAME (which checks that this says the same), and the install folder:
# TheLastWizardSurvivors since 0.24.0 (in-flight 65), TheOneHeMissed before it.
APP=TheLastWizardSurvivors
TITLE="The Last Wizard: Survivors"
# KEPT ON PURPOSE: the save folder is named after project.godot's config/name, which stayed The One
# He Missed when the game was renamed again on 2026-11-08 so that no save would move.
SAVE_NAME="The One He Missed"
DATA="${XDG_DATA_HOME:-$HOME/.local/share}"
HERE=$(cd "$(dirname "$0")" && pwd)
DEST="$DATA/$APP"
# KEPT ON PURPOSE, the second name: every install since 0.12.0 wrote its menu entry here, so a new
# install replaces that entry rather than adding a second one beside it.
MENU="$DATA/applications/theonehemissed.desktop"

# KEPT ON PURPOSE, both earlier names. An install keeps updating itself where it is, under the
# program name it was installed with (UpdateScreen installs every update under the running program's
# name, so its menu entry keeps working - see .ai/releasing.md, "The rename" and "The third rename"):
# ~/.local/share/TheOneHeMissed from 0.12.0 to 0.23.x, and ~/.local/share/WizardSurvivors before
# that. This script retires such an install once a new one replaces it, refreshes it in place when
# run from inside it, and an uninstall takes out all of them. The save is never in any of them.
PREV_DEST="$DATA/TheOneHeMissed"
OLD_DEST="$DATA/WizardSurvivors"
OLD_MENU="$DATA/applications/wizardsurvivors.desktop"

if [ "$1" = "--uninstall" ]; then
	rm -rf "$DEST"
	rm -f "$MENU"
	for old in "$PREV_DEST" "$OLD_DEST"; do
		if [ -f "$old/install.sh" ]; then rm -rf "$old"; fi
	done
	rm -f "$OLD_MENU"
	echo "$TITLE is uninstalled. Your save is still in ~/.local/share/godot/app_userdata/$SAVE_NAME."
	exit 0
fi

if [ "$HERE" = "$PREV_DEST" ] || [ "$HERE" = "$OLD_DEST" ]; then
	# Run from inside an install of an earlier name: refresh it where it is.
	DEST="$HERE"
elif [ "$HERE" != "$DEST" ]; then
	mkdir -p "$DEST"
	cp -Rf "$HERE/." "$DEST/"
	for old in "$PREV_DEST" "$OLD_DEST"; do
		if [ -f "$old/install.sh" ]; then
			rm -rf "$old"
			echo "Removed the copy installed under an earlier name of the game ($old)."
		fi
	done
fi

# The program: TheLastWizardSurvivors.x86_64, or in an install of an earlier name the program name it
# kept (TheOneHeMissed.x86_64, WizardSurvivors.x86_64).
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
