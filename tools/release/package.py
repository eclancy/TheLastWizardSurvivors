# -*- coding: utf-8 -*-
"""Turn the exports in build/windows/ and build/linux/ into the files a GitHub Release carries.

    python tools/release/package.py 0.2.0 [--progress-epoch=N]

--progress-epoch is the story epoch version.json carries as "progressEpoch" (release.sh passes
SaveData.CurrentProgressEpoch); left out, it is read from scripts/SaveData.cs the same way.

Writes build/release/ (the downloads named TheLastWizardSurvivors-*, DOWNLOAD below; the program inside
them is still TheOneHeMissed, NAME):

  TheLastWizardSurvivors-windows.zip   everything, in a TheOneHeMissed/ folder - the portable
                               version (the installer is what a new player downloads)
  TheLastWizardSurvivors-patch.zip     just the game: the .pck and the game's own assembly. A fraction
                               of the full zip, because the engine and the .NET runtime only
                               change when Godot is upgraded.
  TheLastWizardSurvivors-linux.tar.gz  the Linux version, in a TheOneHeMissed/ folder with install.sh
                               (which puts it in ~/.local/share and on the app menu) - from
                               build/linux/
  TheLastWizardSurvivors-linux-patch.zip  the Linux patch: its .pck and the game's assembly
  TheLastWizardSurvivors-windows-bridge.zip    the bridge: the whole game again, under the file names a
  TheLastWizardSurvivors-linux-bridge.tar.gz   copy from before the rename looks for (see BRIDGE below)
  version.json                 what the game's update check reads (see scripts/UpdateScreen.cs)

The installer, TheLastWizardSurvivors-Setup.exe, is compiled from the same export by release.sh
(tools/release/installer.iss); version.json carries its URL. The full zip is the portable version.
The Android APK, TheLastWizardSurvivors-android.apk, is exported and signature-checked by release.sh too;
version.json carries its URL as "android".

Normally run by tools/release/release.sh rather than by hand. See .ai/releasing.md.
Python 2.7, like every other tool in this repo - it is the only Python on the machine.
"""
from __future__ import print_function

import io
import json
import os
import re
import sys
import tarfile
import time
import zipfile

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
EXPORT = os.path.join(ROOT, "build", "windows")
LINUX = os.path.join(ROOT, "build", "linux")
OUT = os.path.join(ROOT, "build", "release")
REPO = "eclancy/TheLastWizardSurvivors"
# The program, its .pck and the .NET folder (the assembly is named the same), inside every download, and the
# Linux install folder, so install.sh's APP must stay equal to it. An installed copy's updater keeps its own
# program name, so changing this is the harder half of in-flight 65 and is not done.
NAME = "TheOneHeMissed"
# What the DOWNLOADS are called (in-flight 65, Eric 2026-11-10: "the downloaded file is still created as 'the one
# he missed-android'"): the game's name since 2026-11-08, with no colon, which Windows forbids. Safe apart from
# NAME because every updater follows version.json's addresses rather than looking for a file name, and ecec.dev
# finds each download by how its name ends ("-Setup.exe", "-windows.zip", "-android.apk", "-linux.tar.gz"),
# which is unchanged. release.sh's list and installer.iss's OutputBaseFilename say the same prefix.
DOWNLOAD = "TheLastWizardSurvivors"
DATA = "data_%s_windows_x86_64" % NAME
FULL_ZIP = DOWNLOAD + "-windows.zip"
PATCH_ZIP = DOWNLOAD + "-patch.zip"
INSTALLER = DOWNLOAD + "-Setup.exe"
APK = DOWNLOAD + "-android.apk"
LINUX_DATA = "data_%s_linuxbsd_x86_64" % NAME
LINUX_TAR = DOWNLOAD + "-linux.tar.gz"
LINUX_PATCH = DOWNLOAD + "-linux-patch.zip"

# BRIDGE. The game was called Wizard Survivors until 0.12.0, and every copy installed before then
# runs a build whose updater is fixed in it: it reads version.json's top-level "patch" and "full"
# (Windows) or "linux" pair (Linux), looks for a .pck called exactly OLD_NAME + ".pck" in what it
# downloads, copies whatever sits beside that .pck over its own folder, and starts OLD_NAME.exe (or
# .x86_64) again. Given the normal downloads it would find no such .pck and quietly stay on its
# version for good; given the patch alone, the renamed assembly's .NET folder would have no runtime
# in it. So those fields point at a bridge instead: the whole export, flat, with the program and the
# .pck renamed to OLD_NAME. The old program then starts the new game, and from then on the new
# updater (which reads "builds" and keeps an install's own program name) takes over.
#
# Kept on purpose, and in every release: a copy from before the rename may not be started again for
# months, and whichever release is newest when it is must still have a bridge for it. Without one it
# keeps playing its own version and never updates. The ends of the bridges' names ("-bridge.zip",
# "-bridge.tar.gz") are chosen so ecec.dev's download page, which finds files by how their names end
# ("-windows.zip", "-linux.tar.gz"), never offers one. See .ai/releasing.md, "The rename".
OLD_NAME = "WizardSurvivors"
BRIDGE_ZIP = DOWNLOAD + "-windows-bridge.zip"
BRIDGE_TAR = DOWNLOAD + "-linux-bridge.tar.gz"

# The engine the export was made with. A patch is only offered to a copy on the same engine;
# anything else downloads the full zip. Read from the editor binary's name rather than
# hard-coded, so upgrading Godot cannot leave this behind.
GODOT = os.environ.get("GODOT_BIN", "")

# What the patch carries: everything that changes when the game changes and the engine does
# not. The .pck is every scene, image, sound and setting; the TheOneHeMissed.* files are the C#.
PATCH = [NAME + ".pck", DATA + "/" + NAME + ".dll", DATA + "/" + NAME + ".deps.json",
         DATA + "/" + NAME + ".runtimeconfig.json"]

# The controls are the input map in project.godot (move_*, ui_*, pause). The README has no
# controls section, so this is the one place they are written down for a player.
HOW_TO_PLAY = u"""The Last Wizard: Survivors
==========================

You are a lone wizard in a haunted wood, and the dead keep coming. You only move - your spells
cast themselves. Gather what the fallen leave behind to level up, choose new spells and
upgrades, and survive each chapter long enough to face the boss at its end.

1. Double-click TheOneHeMissed.exe.

2. The first time, Windows may say "Windows protected your PC". That is because this game
   is not from a big company that paid for a signature. Click "More info", then "Run anyway".

3. Controls (keyboard or an Xbox controller):

     Move            WASD or the arrow keys, or the left stick
     Attack          nothing to press - your spells fire on their own
     Menus           arrows / WASD / d-pad to choose, Enter or Space or A to pick,
                     Esc or B to go back (the mouse works in menus too)
     Pause           Esc, or Start on a controller

The game updates itself: every time it starts, it checks for a new version and installs it.
Keep this folder somewhere you can write to, like Documents or the Desktop, so it can.

Made at https://ecec.dev - download page: https://ecec.dev/the-last-wizard-survivors/download
"""


HOW_TO_PLAY_LINUX = u"""The Last Wizard: Survivors
==========================

You are a lone wizard in a haunted wood, and the dead keep coming. You only move - your spells
cast themselves. Gather what the fallen leave behind to level up, choose new spells and
upgrades, and survive each chapter long enough to face the boss at its end.

1. Open a terminal in this folder and run:  ./install.sh
   That puts the game in ~/.local/share/TheOneHeMissed and adds it to your app menu.
   (Or just run ./TheOneHeMissed.x86_64 from here - keep the folder somewhere you can write to.)

2. Controls (keyboard or an Xbox controller):

     Move            WASD or the arrow keys, or the left stick
     Attack          nothing to press - your spells fire on their own
     Menus           arrows / WASD / d-pad to choose, Enter or Space or A to pick,
                     Esc or B to go back (the mouse works in menus too)
     Pause           Esc, or Start on a controller

The game updates itself: every time it starts, it checks for a new version and installs it.
To remove it: ~/.local/share/TheOneHeMissed/install.sh --uninstall  (your save is kept)

Made at https://ecec.dev - download page: https://ecec.dev/the-last-wizard-survivors/download
"""


def add_tar_file(t, data, name, executable=False):
    info = tarfile.TarInfo(name)
    info.size = len(data)
    info.mode = 0o755 if executable else 0o644
    info.mtime = int(time.time())
    t.addfile(info, io.BytesIO(data))


def export_files(src):
    """Every file of an export as (path on disk, path inside it with forward slashes), sorted."""
    found = []
    for dirpath, _dirs, files in os.walk(src):
        for f in sorted(files):
            full = os.path.join(dirpath, f)
            found.append((full, os.path.relpath(full, src).replace(os.sep, "/")))
    return found


def as_old_name(rel):
    """A bridge's name for an export file: the program and the .pck - the top-level files named
    NAME, whatever their extension - take OLD_NAME; everything else, the .NET folder included
    (it is named for the assembly, which the new .pck asks for), keeps its own."""
    if "/" not in rel:
        stem, ext = os.path.splitext(rel)
        if stem == NAME:
            return OLD_NAME + ext
    return rel


def read_install_sh():
    with open(os.path.join(ROOT, "tools", "release", "install.sh"), "rb") as fh:
        return fh.read().replace(b"\r\n", b"\n")


def linux_package():
    """The Linux download, its patch and its bridge. Every file gets its permissions set here, on
    Windows: the game binary and install.sh executable, the rest readable. Tarballs rather than
    zips, so those permissions travel with them."""
    patch_files = [NAME + ".pck", LINUX_DATA + "/" + NAME + ".dll", LINUX_DATA + "/" + NAME + ".deps.json",
                   LINUX_DATA + "/" + NAME + ".runtimeconfig.json"]
    for rel in patch_files:
        if not os.path.isfile(os.path.join(LINUX, rel.replace("/", os.sep))):
            raise SystemExit("the Linux export has no %s" % rel)
    files = export_files(LINUX)
    with open(os.path.join(ROOT, "icon.png"), "rb") as fh:
        icon = fh.read()

    t = tarfile.open(os.path.join(OUT, LINUX_TAR), "w:gz")
    for full, rel in files:
        with open(full, "rb") as fh:
            add_tar_file(t, fh.read(), NAME + "/" + rel, executable=rel == NAME + ".x86_64")
    add_tar_file(t, read_install_sh(), NAME + "/install.sh", executable=True)
    add_tar_file(t, icon, NAME + "/icon.png")
    add_tar_file(t, HOW_TO_PLAY_LINUX.encode("utf-8"), NAME + "/How to play.txt")
    t.close()

    z = zipfile.ZipFile(os.path.join(OUT, LINUX_PATCH), "w", zipfile.ZIP_DEFLATED)
    for rel in patch_files:
        z.write(os.path.join(LINUX, rel.replace("/", os.sep)), rel)
    z.close()

    # The bridge, flat: an old Linux copy copies what sits beside the .pck over its install folder.
    # install.sh and icon.png come too, so an old install's own install.sh learns the new names
    # (its --uninstall then takes out both). See BRIDGE.
    t = tarfile.open(os.path.join(OUT, BRIDGE_TAR), "w:gz")
    for full, rel in files:
        with open(full, "rb") as fh:
            add_tar_file(t, fh.read(), as_old_name(rel), executable=rel == NAME + ".x86_64")
    add_tar_file(t, read_install_sh(), "install.sh", executable=True)
    add_tar_file(t, icon, "icon.png")
    t.close()


def engine_version():
    name = os.path.basename(GODOT)
    # Godot_v4.5.1-stable_mono_win64_console.exe -> 4.5.1
    if name.startswith("Godot_v"):
        return name[len("Godot_v"):].split("-")[0]
    raise SystemExit("set GODOT_BIN to the Godot editor, so the engine version is known")


def progress_epoch():
    """SaveData.CurrentProgressEpoch: which telling of the story this build is. A copy that finds a
    higher one in version.json warns before the download, because installing it resets progress
    (.ai/releasing.md, "Releasing a story reset")."""
    path = os.path.join(ROOT, "scripts", "SaveData.cs")
    with io.open(path, encoding="utf-8") as f:
        for line in f:
            m = re.match(r"\s*public const int CurrentProgressEpoch = (\d+);", line)
            if m:
                return int(m.group(1))
    raise SystemExit("scripts/SaveData.cs has no 'public const int CurrentProgressEpoch = N;' line")


def main(argv):
    if not argv:
        print(__doc__)
        return 1
    version = argv[0]
    epoch = None
    for arg in argv[1:]:
        if arg.startswith("--progress-epoch="):
            epoch = int(arg[len("--progress-epoch="):])
        else:
            raise SystemExit("unknown argument: %s" % arg)
    if epoch is None:
        epoch = progress_epoch()
    # Before writing anything, so a missing GODOT_BIN cannot leave half a release behind.
    engine = engine_version()
    if not os.path.isfile(os.path.join(EXPORT, NAME + ".pck")):
        raise SystemExit("no export in build/windows - run the Godot export first")
    for rel in PATCH + [NAME + ".exe"]:
        if not os.path.isfile(os.path.join(EXPORT, rel.replace("/", os.sep))):
            raise SystemExit("the export has no %s - is the preset still embedding nothing?" % rel)
    if not os.path.isdir(OUT):
        os.makedirs(OUT)

    files = export_files(EXPORT)
    full = os.path.join(OUT, FULL_ZIP)
    z = zipfile.ZipFile(full, "w", zipfile.ZIP_DEFLATED, allowZip64=True)
    for path, rel in files:
        z.write(path, NAME + "/" + rel)
    z.writestr(NAME + "/How to play.txt", HOW_TO_PLAY.replace(u"\n", u"\r\n").encode("utf-8"))
    z.close()

    patch = os.path.join(OUT, PATCH_ZIP)
    z = zipfile.ZipFile(patch, "w", zipfile.ZIP_DEFLATED)
    for rel in PATCH:
        z.write(os.path.join(EXPORT, rel.replace("/", os.sep)), rel)
    z.close()

    # The Windows bridge, flat, the program included so a bridge across an engine upgrade works
    # too. See BRIDGE.
    z = zipfile.ZipFile(os.path.join(OUT, BRIDGE_ZIP), "w", zipfile.ZIP_DEFLATED, allowZip64=True)
    for path, rel in files:
        z.write(path, as_old_name(rel))
    z.close()

    linux = os.path.isfile(os.path.join(LINUX, NAME + ".pck"))
    if linux:
        linux_package()

    base = "https://github.com/%s/releases/download/v%s/" % (REPO, version)
    builds = {"windows": {"patch": base + PATCH_ZIP, "full": base + FULL_ZIP}}
    if linux:
        builds["linux"] = {"patch": base + LINUX_PATCH, "full": base + LINUX_TAR}
    info = {
        "version": version,
        "engine": engine,
        # What every copy since the rename updates from (UpdateScreen): its platform's patch, on the
        # same engine, or its full download. A copy from before Linux existed never looks for one.
        "builds": builds,
        # The bridge, for copies from before the rename only - see BRIDGE. Their updater reads these
        # three and nothing else of the above; this build's never reads them. Both of a pair are the
        # bridge: an old copy on this engine takes "patch", one on another engine "full", and either
        # way it needs the whole game under the old names.
        "patch": base + BRIDGE_ZIP,
        "full": base + BRIDGE_ZIP,
        # Built by release.sh with Inno Setup after this script runs. The game does not read it;
        # it is there for anything that wants this release's installer by URL.
        "installer": base + INSTALLER,
        # The release-signed APK, exported and verified by release.sh after this script runs.
        # The Android copy reads it (UpdateBanner) and offers it in the browser - copies from before
        # the rename too: the APK keeps the app's package id, so it installs over them, save and all.
        "android": base + APK,
        # The story epoch (SaveData.CurrentProgressEpoch). A copy built at a lower one learns from
        # this that installing the release resets its progress, and asks (Windows, Linux) or warns
        # on its strip (Android) first. Copies from before epochs never read it, like any added field.
        "progressEpoch": epoch,
    }
    if linux:
        info["linux"] = {"patch": base + BRIDGE_TAR, "full": base + BRIDGE_TAR}
    with io.open(os.path.join(OUT, "version.json"), "w", encoding="utf-8") as f:
        f.write(unicode(json.dumps(info, indent=2, sort_keys=True)) + u"\n")

    names = [FULL_ZIP, PATCH_ZIP, BRIDGE_ZIP, "version.json"]
    if linux:
        names += [LINUX_TAR, LINUX_PATCH, BRIDGE_TAR]
    for name in names:
        print("%-36s %6.1f MB" % (name, os.path.getsize(os.path.join(OUT, name)) / 1048576.0))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
