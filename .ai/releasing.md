# Releasing

Decided 2026-10-12, ported from DrawFight (whose `.ai/releasing.md` is where this design was
first argued and tested). How a downloadable copy of The One He Missed gets to people, and how it
keeps itself up to date. The game was renamed on 2026-11-02; how copies and saves from before then
get across is "The rename" below.

**In one line:** `bash tools/release/release.sh X.Y.Z "What changed"` makes a GitHub Release. The
next time a Windows copy starts, it finds the release and updates itself; an Android copy says on
its title screen that the new version is out and offers the APK.

## Where things live

| What | Where | Why |
|---|---|---|
| The download page | `ecec.dev/the-one-he-missed/download` (in the EricClancyEngineeringAndCrafts repo) | the page people are sent; it offers the installer, with the zip as the portable fallback |
| The game files | GitHub Releases on `eclancy/TheOneHeMissed` | GitHub serves release downloads free with no bandwidth limit, so the droplet's 1 TB a month is left alone |
| The update check | `ecec.dev/the-one-he-missed/version.json` | an nginx 302 to `github.com/eclancy/TheOneHeMissed/releases/latest/download/version.json` |
| The old update check | the game's old address, `/wizard-survivors/version.json` | the same 302, kept forever by the website for copies from before the rename (see "The rename") |
| "What's new" | `ecec.dev/games/the-one-he-missed` | shows the newest release's notes, minus the download paragraph |

The update address is on ecec.dev, not GitHub, because it is the one address compiled into every
copy ever downloaded (`UpdateScreen.VersionUrl`). Behind a redirect, the releases can move
somewhere else later without breaking old copies. The download button links to GitHub's
`releases/latest/download/...`, which always points at the newest release, so **neither the page
nor ecec.dev needs a deploy for a release**.

## What a release contains

Nine files: six made from the export in `build/windows/`, three from the export in
`build/linux/` (see "Linux" below), and the Android APK.

- **`TheOneHeMissed-Setup.exe`**: the installer, and the file a new player downloads. Compiled
  by `release.sh` with Inno Setup from `tools/release/installer.iss`. See "The installer" below.
- **`TheOneHeMissed-windows.zip`** (about 89 MB): the **portable version**, for anyone who would
  rather not run an installer. Everything, in a `TheOneHeMissed/` folder, with a
  `How to play.txt` (SmartScreen, where to keep the folder, the controls). The controls in it are
  written from the input map in `project.godot`; change one and change the other.
- **`TheOneHeMissed-patch.zip`** (about 19 MB): `TheOneHeMissed.pck` (every image, sound,
  setting and scene) and `TheOneHeMissed.dll` with its two JSON files (all the C#). These are the
  only files that change when the game changes and Godot does not. The rest of the full zip is
  the Godot engine and the .NET runtime.
- **`TheOneHeMissed-windows-bridge.zip`** and **`TheOneHeMissed-linux-bridge.tar.gz`** (about the
  size of the full downloads): the whole game again, under the file names a copy from before the
  rename looks for. Only those copies ever fetch them. See "The rename".
- **`TheOneHeMissed-android.apk`** (about 110 MB): the Android game, release-signed. See
  "Android" below.
- **`version.json`**: the version, the Godot version it was built with, under `"builds"` the
  Windows and Linux pairs (`"patch"` and `"full"` each) that every copy since the rename reads, the
  installer (`"installer"`; the game itself does not read that one), the APK (`"android"`, read
  only by the Android copy), `"progressEpoch"`, the story epoch the release was built at (see
  "Releasing a story reset"), and at the top level `"patch"`, `"full"` and `"linux"`, which since
  the rename all point at the bridges. Adding a field is safe: `UpdateScreen` reads the fields it
  names with `GetProperty`/`TryGetProperty` and ignores the rest, so every copy already out there
  carries on as before.

`tools/release/package.py` makes the zips, the bridges and `version.json`; ISCC makes the
installer; `release.sh` exports, checks and copies the APK.

The export preset is `Windows Desktop` in `export_presets.cfg`: x86_64, `.pck` beside the exe
rather than embedded (the patch depends on that), `tools/`, `docs/`, `.ai/` and `build/`
excluded. Everything under `build/` is gitignored.

## The installer

`tools/release/installer.iss`, Inno Setup 6. `release.sh` compiles it after `package.py` with
`/DAppVersion=X.Y.Z`, into `build/release/TheOneHeMissed-Setup.exe`.

- **It installs per user, to `%LOCALAPPDATA%\Programs\TheOneHeMissed`**, with no admin
  prompt (`PrivilegesRequired=lowest`). Two reasons. The self-updater copies new files into the
  game's own folder, so that folder has to stay writable by the player - Program Files would
  leave every installed copy stuck on its first version. And no UAC prompt means one less
  scary dialog after SmartScreen's.
- It adds a Start menu shortcut and, checked by default, a desktop one, and offers "Play The One
  He Missed now" at the end. Uninstalling (Settings > Apps) removes the whole folder, including
  anything the updater added, but **not the save** in `%APPDATA%\Godot\app_userdata\`.
- **The `AppId` GUID in `installer.iss` must never change.** It is how Windows recognises a newer
  setup as the same program, so running a new installer upgrades in place (into the same folder,
  `UsePreviousAppDir`) instead of installing a second copy. It closes a running game first. It is
  also why a copy installed before the rename upgrades in place: see "The rename".
- An installed copy updates itself exactly like an unzipped one; the installer is only how the
  first copy arrives. Installing a newer setup over an older copy also works.
- The setup is not code-signed either, so SmartScreen warns about it the same way.

**A releasing machine needs Inno Setup:** `winget install JRSoftware.InnoSetup` (per user, to
`%LOCALAPPDATA%\Programs\Inno Setup 6\`). `release.sh` looks there, or wherever the `ISCC`
environment variable points, and stops before doing anything if it is missing.

Tested 2026-10-18 (0.2.1): a silent install to a scratch folder and a default install both
worked without an admin prompt, the installed game started, and the uninstaller left no folder
or shortcuts behind.

## How a copy updates

`scripts/UpdateScreen.cs` runs first in an exported Windows build. It never runs in the editor,
from `godot --path .`, or in any harness, because it checks `OS.HasFeature("template")`.

It is not a scene. `TitleScreen.tscn` stays the main scene, and `TitleScreen._Ready` lays
`UpdateScreen` over the artwork before the prompt, the music or the menu start; input to the title
is ignored until it is done. It runs once per launch, so returning to the title after a run does
not check again.

1. It fetches `version.json` (5-second timeout) and compares versions number by number, so
   0.10.0 beats 0.9.0. The game's own version is `application/config/version` in
   `project.godot`, which the release script sets.
2. If the release restarts the story (its `progressEpoch` is above this copy's), it asks first -
   Update or Not now; see "Releasing a story reset". Otherwise it never asks anything.
   If the release was built with the same Godot version as this copy, it downloads the patch;
   otherwise the full zip - the pair under `"builds"."windows"` (or `"linux"`). It unpacks to
   `user://update/`.
3. It installs the new files **under the running program's name**, not the release's: the
   release's `.pck` (and in a full download its program) is renamed to match the exe this copy
   was started as (`UpdateScreen.ProgramName`). Every install since the rename already agrees, so
   nothing happens; a copy from before the rename that crossed on the bridge is still
   `WizardSurvivors.exe`, its shortcuts name that file, and Godot loads the `.pck` named after the
   program. See "The rename".
4. Windows will not let a running program overwrite its own files. So the game writes
   `apply.cmd`, starts it, and quits. The script waits for the game to close, `robocopy`s the new
   files over the old ones, and starts the game again.

Anything that goes wrong, including no internet, GitHub being down, a web page where the JSON
should be, or a folder the game cannot write to, just goes on to the title screen. **An update is never worth not being able to
play.** The current version shows small in the bottom-left corner of the title screen.

Tested end to end on 2026-10-12: a 0.0.9 export in `build/test/` updated itself to 0.1.0 from a
local server in about 8 seconds and restarted, and its `.pck` and DLL came out byte-identical to
the 0.1.0 export. Relaunched, it said it was up to date and went to the title. With the server
down, and against the real address before it existed, it went straight to the title.

## Linux

Added 2026-10-26, on Eric's call, for every game on ecec.dev (DrawFight did it first, the same
way). `release.sh` exports the `Linux` preset (x86_64, `.pck` beside the binary) to `build/linux/`,
and `package.py` makes three more files from it:

- **`TheOneHeMissed-linux.tar.gz`** (about 80 MB): the game in a `TheOneHeMissed/` folder - the
  `TheOneHeMissed.x86_64` binary, the `.pck`, and `data_TheOneHeMissed_linuxbsd_x86_64/` with
  the .NET runtime, so nothing has to be installed first - plus `install.sh`, `icon.png` and
  `How to play.txt`. A tarball rather than a zip, because a tarball carries the "can be run"
  permission on the binary and the script; `package.py` sets it, which it can do from Windows.
- **`TheOneHeMissed-linux-patch.zip`** (about 19 MB): the Linux export's `.pck` and the game's
  DLL with its two JSON files.
- **`TheOneHeMissed-linux-bridge.tar.gz`**: for Linux copies from before the rename; see "The
  rename".

**`install.sh`** (`tools/release/install.sh`) is the Linux installer. It copies the game to
`~/.local/share/TheOneHeMissed` and writes `theonehemissed.desktop` into
`~/.local/share/applications`, so it is on the app menu. No sudo and nothing outside the home
folder, for the same reason the Windows installer installs per user: the game updates itself, so
its folder must stay writable. `./install.sh --uninstall` takes it out again; the save (in
`~/.local/share/godot/app_userdata/The One He Missed`) is never touched. Running
`./TheOneHeMissed.x86_64` straight from the unpacked folder works too. **The folder's name must
stay the name the release files start with** (`APP` in `install.sh`, `NAME` in `package.py`):
ecec.dev's download page reads it off the files and tells people that is where the game goes.

**Updating** works as on Windows, from `version.json`'s `"builds"."linux"` pair: the patch on the
same engine, the tarball otherwise (unpacked with `System.Formats.Tar`, so the binary stays
runnable). The game writes `apply.sh`, starts it with `/bin/sh` and quits; the script waits for the
game to close, copies the new files over, `chmod +x`es the binary and starts it again. A release
with no Linux pair leaves a Linux copy on the title.

**Tested so far:** the export, the packaging and the permissions in the tarball, on Windows. Not
yet run on Linux - Eric is testing it on a Linux machine.

## Android

Added 2026-10-21. Every release carries `TheOneHeMissed-android.apk`, a **release-signed** APK
made from the `Android` preset in `export_presets.cfg` (arm64, portrait, a Gradle build for the
GL surface watchdog - see CLAUDE.md). It is not on the Play Store; players sideload it from the
download page.

### How the APK is built

`release.sh`, after the Windows export and the installer:

1. Restores `android/build/libs/` from the installed templates' `android_source.zip` if it is
   missing (git does not track the 264 MB of AARs). The patterns are per directory
   (`'libs/debug/*' 'libs/release/*'`): Git Bash's unzip silently extracts nothing for `'libs/*'`.
2. Runs `--export-release "Android" build/android/TheOneHeMissed.apk` under a 30-minute timeout,
   with the signing key in the environment (below). A cold Gradle build takes several minutes;
   the first one on this machine took 11. The export can write the APK and then never exit, so it
   is judged by the file, not the exit code.
3. Puts back anything the export rewrote under `android/` (the manifest, launcher icons and
   app-name strings are committed as the export writes them, so normally nothing changes). The
   release commit holds only `project.godot` and `export_presets.cfg`.
4. Checks the APK: `apksigner verify --print-certs` must pass, its certificate's SHA-256 must equal
   the release keystore's (read with `keytool`), it must not carry a debug certificate
   (`Android Debug` or Godot's `Godot Engine`), and `aapt dump badging` must show this release's
   versionCode and versionName and the package id `com.ecec.wizardsurvivors` (the game's old name,
   kept on purpose - see "The rename"). Any failure stops the release before anything is committed.
5. Copies it to `build/release/TheOneHeMissed-android.apk`.

apksigner, aapt and keytool are found where the Godot editor settings point
(`export/android/android_sdk_path`, `export/android/java_sdk_path`), or through `ANDROID_SDK_ROOT`
and `JAVA_HOME`. The script checks all of them, the keystore and the templates **before** it
touches anything.

### The signing key

| File | What |
|---|---|
| `%APPDATA%\Godot\keystores\wizardsurvivors-release.keystore` | the release keystore, alias `wizardsurvivors` |
| `%APPDATA%\Godot\keystores\wizardsurvivors-release.secret` | two lines, `alias=...` and `password=...` |

Both keep the game's old name **on purpose**: they were made before the rename, the key inside is
what Android checks, and the alias cannot be renamed without rewriting the keystore - the one file
that must never be put at risk. Nothing reads the file names but `release.sh`.

**This repo is public, so the password is never in it.** The preset's `keystore/release` fields
are left empty; Godot then reads `GODOT_ANDROID_KEYSTORE_RELEASE_PATH`, `..._USER` and
`..._PASSWORD` from the environment. `release.sh` reads the secret file and exports those three
inside a subshell around the Godot export (and around `keytool`), so the password exists only in
those two processes. It is never on a command line, never in `export_presets.cfg`, and the
script searches the export log for it and deletes the log and stops if it is there (it was not,
in testing). `THEONEHEMISSED_KEYSTORE` points the script at a keystore elsewhere; the secret is
the same path with `.secret` for `.keystore`.

> **Back up both files somewhere safe, off this machine** - a password manager or an encrypted
> drive, not this repo and not a public cloud folder. **If the keystore or its password is lost,
> no Android copy already installed can ever be updated again**: Android installs an update only
> if it is signed with the same key as the installed app. Every player would have to uninstall -
> losing their save - and install a new APK signed with a new key. There is no recovery.

### Version codes

Android refuses to install an APK over one with an equal or higher `versionCode`, so it has to
climb with every release. It is derived from the version: `major*10000 + minor*100 + patch`
(0.3.1 is 301, 1.2.0 is 10200). That stays monotonic only while minor and patch are under 100;
the script refuses a version that breaks it, and one whose code is not above the last.

The **version name is not stored**: the preset's `version/name` is empty, so Godot uses
`application/config/version` from `project.godot`. The **version code is stored**, in
`export_presets.cfg`, and the release commit carries it beside the version bump. Kept rather
than set-and-restored because it is not secret, it makes the committed tree describe exactly
what was published, it is what the monotonic check compares against, and
`tools/android-deploy.sh`'s debug builds then carry the same number.

### How players install it

On the phone: open `ecec.dev/the-one-he-missed/download`, tap **Download for Android**, open the
downloaded `TheOneHeMissed-android.apk`, and when Android asks, allow the browser to install
unknown apps (Settings > *Allow from this source*), then **Install**. Play Protect may warn that
it does not recognise the app, because it is not from the Play Store: *More details*, *Install
anyway*. The download page says all of this.

### How updates reach Android players

An Android copy cannot replace itself the way the Windows one does, and asking for
`REQUEST_INSTALL_PACKAGES` to do it would make a sideloaded game look like malware. So it only
tells the player. `scripts/UpdateBanner.cs`, laid over the title screen by `TitleScreen._Ready`:

- runs only in an exported Android build (`template` feature and `OS.GetName() == "Android"`),
  once per launch, and `--no-update` turns it off;
- fetches the same `version.json` as Windows (5-second timeout) on a worker thread, **without
  holding the title up** - the title starts at once and the strip appears whenever the answer
  arrives;
- if the version is newer *and* the release has an `"android"` URL, shows a strip across the top:
  "Version X is available", **Download** (opens the APK URL in the browser with `OS.ShellOpen`)
  and **Later** (hides it until the next launch), and under them, when the release restarts the
  story, the warning line (see "Releasing a story reset"). Every failure, and a release with no APK,
  shows nothing;
- never installs anything itself. The browser downloads, Android's installer asks, and because
  the new APK has the same signature and a higher version code, it installs over the old one and
  **the save survives**.

The strip is a CanvasLayer at 6, inside the safe area, and hides while a full-screen menu panel
(Spell Index, Unlocks, Arcane Upgrades, Options) is open, because it would sit over that panel's heading. Its
buttons take taps and clicks only and attach no `MenuNavigator`, on purpose: on the title A means
"start", and a strip that took the selection would turn a pad player's A into "open the browser".
A tap on the strip does not count as the title's "press any button".

To look at it on a desktop: `--update-banner=9.9.9` shows it with that version, no network, in
any build; `--update-banner=9.9.9:2` makes that release story epoch 2, which adds the warning. With `_UiShot`: `"$GODOT_BIN" --path . --resolution 540x1212 scenes/_UiShot.tscn --
--portrait --safe-area --scene=res://scenes/TitleScreen.tscn --update-banner=0.9.9 --shot=banner`
(add `--open-menu` for the menu over it). `THEONEHEMISSED_UPDATE_URL` points the real check at a
test server, as on Windows.

### Switching from the debug build: uninstall first, and the save is lost

The builds `tools/android-deploy.sh` installs are signed with Godot's **debug** key, the release
APK with the **release** key. Android will not install one over the other ("App not installed as
package conflicts with an existing package"). **Anyone with the debug build - the development
phone included - must uninstall it before installing the release APK, and uninstalling deletes
the app's data, which is where the save (`user://savegame.json`) lives on Android. Their progress
is gone.** Nothing in the game can carry it across.

The same wall stands the other way: once a phone has the release build, `android-deploy.sh` cannot
install its debug build over it without an uninstall either. For day-to-day phone debugging,
keep the debug build on the development phone and judge the release APK by its signature check
(or on a second device), unless losing that phone's save is fine.

### Rehearsing it: `--dry-run`

**Phone-free startup checks (2026-11-06):** `pwsh -File tools/android-startup-check.ps1` extracts
the real APK's assets, exports the current code with the Windows release engine, and checks a
portrait/safe-area menu with desktop display controls and debug-only options absent. It covers
cold preparation with forced garbage collection, return-to-menu, a missing scene, and an injected
menu-construction exception. Both failure paths must display a cause, copy a complete report
(including native loader errors), and recover through Retry. Profiles and logs stay under
`build/android-startup-check/`; save writes and update checks are disabled. `-SkipExport` reuses
the fixture when its code is unchanged. This is asset/UI regression coverage, **not an Android
runtime or ARM64/GPU test**. An Android 0.14.0 startup failure remains unconfirmed by these checks.
The loading error screen now includes its stage and Copy error details; native errors are captured
only during startup, in a bounded thread-safe buffer, so a phone can report failures without adb.

2026-11-06: the **0.14.0 rehearsal passed** from `af0b8330`: all nine artifacts built, including
the release-signed Android APK (versionCode 1400, package identity and release certificate verified).
Story epoch remains 2. The packaged Windows executable passed keyboard/mouse/controller handoff,
all 22 Powers entries and category selection, live single-hound/Littermates attacks, native fullscreen,
saved window sizes, F11 and footer checks. Title preparation took 2,154.7 ms; Enter-to-menu 35.3 ms.
Source phone/touch/safe-area layouts, Familiar cards, defeat navigation and full content validation
also passed (0 content errors, one existing Test Wizard warning; C# build 0 errors, 45 existing warnings).
Package tests used isolated temporary APPDATA with save writes and updates disabled. Windows patch
contents match the full download, and the Linux binary/installer have executable permissions.

2026-11-06: the **0.13.2 rehearsal passed** from `79cda2d0`: all nine artifacts built, including
the 109.5 MB signed APK (`versionCode` 1302, package identity and release certificate verified).
Story epoch stays 2. The packaged Windows executable passed title preparation (1,115.7 ms,
32.9 ms Enter-to-menu in this run), crowded/compact results and reward inspection, rendered
quit-run freeing + lesson, and tall level-up card scrolling. Tests used isolated temporary
`APPDATA` and disabled save writes/updates. Source portrait/touch/safe-area and full content
validation also passed; there was no connected Android device for an on-device playtest.

```sh
bash tools/release/release.sh --dry-run 0.3.1
```

Does everything a release does - version bump, Windows export, both zips, `version.json`, the
installer, the Android export and every APK check - into `build/release/`, then puts
`project.godot` and `export_presets.cfg` back. No commit, no tag, no push, no GitHub Release. It
still needs a clean tree, because it builds what is committed. Run it after any change to the
preset, the keystore, the Android template or the engine.

Tested 2026-10-21: `--dry-run 0.3.1` took 10.5 minutes end to end and produced a 91 MB APK,
versionCode 301, versionName 0.3.1 (so the empty `version/name` does fall back to
`config/version`), minSdk 24, not debuggable, `INTERNET` its only permission, signed (v2 scheme)
by `CN=Eric Clancy, O=ECEC, C=US`, certificate SHA-256
`8052adfcf2f544073f05085b41fa0d025a10d4a9b9ce193af18a86ce48bddd47`. Godot's debug certificate is
`970b6d17...452c5b`, `CN=Godot, OU=Godot Engine`. The export left `android/` untouched and the
tree was clean afterwards. It has not yet been installed on a phone.

**One-time setup for Android on a releasing machine:** the **full** Godot export templates
(`android_source.zip` comes only from the whole `.tpz`, not the Windows subset), the Android SDK
with build-tools, a JDK 17, both set in the Godot editor settings, and the keystore and secret
above.

## Making a release

```sh
bash tools/release/release.sh 0.2.0 "Two new spells. The Gaoler hits less hard."
```

The quoted text is optional: it is what players see under "What's new" on
`ecec.dev/games/the-one-he-missed`, so write it for them. Leave it out and the release just says
how to download. The script always appends a paragraph starting `Download **` below it; that page
strips the paragraph that starts that way, so **keep that wording when editing the script**.

This needs a clean working tree, Inno Setup, and everything the Android build needs (see
"Android" above). The script sets the version and the Android version code, builds, exports for
Windows, packages, compiles the installer, exports and checks the APK, commits
"Release v0.2.0" (`project.godot` and `export_presets.cfg`), tags it, pushes, and runs
`gh release create`. The Windows half takes about a minute; the Android export adds several more.
Rehearse it with `--dry-run` first. It
reads `GODOT_BIN` from `.claude/settings.json` when the shell does not have it.

Version numbers: bump the last number for fixes and balance changes, and the middle one for a new
chapter, wizard or spell set.

**One-time setup on a new machine:** install Godot's export templates for the exact editor
version. They come from the `godotengine/godot-builds` GitHub release, file
`Godot_v<version>-stable_mono_export_templates.tpz`, about 1.2 GB. The `windows_*` files and
`linux_release.x86_64` / `linux_debug.x86_64` are needed, unpacked into
`%APPDATA%\Godot\export_templates\<version>.stable.mono\`. The export fails with "No export
template found" until they are there.

**Testing an update without publishing:**

1. Export the current version to `build/windows/` and run `package.py` (or let a dry run of the
   steps in `release.sh` do it).
2. Copy `build/release/` somewhere, and edit its `version.json` so its URLs (the `"builds"`
   pairs, and the top-level bridge fields for an old copy) point at a local server
   (`http://127.0.0.1:8765/...`). Serve it: `python -m SimpleHTTPServer 8765`.
3. Temporarily set `config/version` lower, export to `build/test/`, and put it back. (Or skip the
   export: an `override.cfg` beside an exported exe with `[application]` `config/version="0.11.9"`
   makes it report that version - put the file back as soon as the first run exits, or the
   restarted copy updates again.)
4. Run `build/test/TheOneHeMissed.exe` with `THEONEHEMISSED_UPDATE_URL` set to the local
   `version.json` (`WIZARDSURVIVORS_UPDATE_URL` for a copy from before the rename). It should
   update, restart, and on the next launch say it is up to date. The relaunched copy inherits the
   variable.

Run any build with `--no-update` to skip the check entirely. The exported exe prints to stdout
when started from a shell; `user://logs/` is shared with every other copy and editor session on
the machine, so do not trust it for this - and point `APPDATA` at a scratch folder, so the test
never touches a real save. To keep a test copy off the screen, an `override.cfg` beside its exe
with `[display]` `display_server/driver.windows="headless"` and `[audio]` `driver/driver="Dummy"`
makes it, and the copy it restarts, headless; `[application]` `run/flush_stdout_on_print=true`
makes the restarted copy's `user://logs/godot.log` readable while it runs.

## Releasing a story reset

**The next release is one** (Eric, 2026-11-02: "The next release will reset game progress for
everyone"): raise the epoch to 2 in the release commit itself, so no branch run restarts the
developer's save before then. It carries the slow start (`.ai/slow-start.md`). Copies of 0.10.0
predate the epoch, so their updater fetches it without the warning; the new build still keeps their
old save beside the new one on first launch.

Decided 2026-11-02 (`.ai/decisions/2026-11-02-story-resets.md`): a release that changes the story
restarts every player's progress, and they are warned before they download it. The game is in
active development, and a save earned under a story that no longer exists is better started again
than migrated into states nobody designed.

**How to do it.** Raise `SaveData.CurrentProgressEpoch` in `scripts/SaveData.cs` by one, commit it
with the story change, and release as usual. That one constant is the whole switch. `release.sh`
reads it, compares it with the last tag's, and when it has risen prints a `STORY RESET` banner
before the export (Ctrl+C is still free then) and again after packaging; it writes it into
`version.json` as `"progressEpoch"` and puts a line saying so at the head of the release notes. It
refuses an epoch lower than the last release's. Rehearse with `--dry-run` - the banner and the
`version.json` it built are both printed. **Never raise it to avoid writing a migration:** a change
to how progress is stored still needs a schema step in `SaveData.Migrate`.

**What players see.**

- *Before downloading.* A Windows or Linux copy asks before it installs: "This update restarts the
  story: your progress will be reset. Your current save is kept as a backup." **Update** (selected,
  A or Enter) installs it; **Not now** (B or Escape too) plays the installed version and asks again
  at the next launch. An Android copy shows the same line under its update strip; Download still
  opens the APK. A release with no epoch change looks exactly as before.
- *The first time the new version runs.* The save from the earlier epoch is copied, unchanged, to
  `savegame.before-epoch-<n>.json` beside it (`%APPDATA%\Godot\app_userdata\The One He Missed\` on
  Windows; `-2`, `-3` if the name is taken), then replaced by a new save that keeps only the
  settings (`SaveData.SettingsKeys`: the Options screen's sound, damage numbers, tips switch,
  playtest toolkit and difficulty preset). Everything else - Arcane Energy, unlocks, chapters,
  achievements, shop and found levels, secrets, quests, seen flags, the prologue - starts again. If
  the backup cannot be written, nothing is reset and the next launch tries again.
- *On the title,* once: "The story has been rebuilt. Your progress starts again from the beginning.
  Your old save is kept." with Continue. It is cleared when Continue is pressed, so quitting before
  reading it shows it again.

**Only a copy that knows about epochs can ask.** A copy older than the epoch machinery (0.10.0 and
before) updates itself straight into a reset with no question - it still keeps the backup and shows
the title's notice afterwards, because that half runs in the new version. So the first story reset
should come at least one ordinary release after the one that first carried `progressEpoch`, giving
copies time to pick the question up.

**Not covered by the game:** a player who downloads the new APK or installer from the download page
rather than through the update check sees no warning before it. That page (ecec.dev, in the
EricClancyEngineeringAndCrafts repository) should read `progressEpoch` from `version.json` and say it
beside the buttons when it has risen since the previous release.

**Looking at it without a release** (no network, nothing downloaded, writes off):

```sh
# the Windows question - Update and Not now; drop ":2" for an ordinary update, which asks nothing
"$GODOT_BIN" --path . --resolution 1280x720 scenes/_UiShot.tscn -- --scene=res://scenes/TitleScreen.tscn --update-screen=0.11.0:2 --frames=90 --shot=question
# the Android strip with the warning
"$GODOT_BIN" --path . --resolution 540x1212 scenes/_UiShot.tscn -- --portrait --safe-area --scene=res://scenes/TitleScreen.tscn --update-banner=0.11.0:2 --shot=strip
# the title's notice, over an in-memory reset of the save
"$GODOT_BIN" --path . --resolution 1280x720 scenes/_UiShot.tscn -- --story-reset --scene=res://scenes/TitleScreen.tscn --frames=150 --shot=notice
```

`--update-screen` works in any build, so a whole exported copy can be walked through the question
too; Update then goes to the title instead of downloading. Add `--pad=a` or `--pad=b` to answer it.
The reset itself is checked at every startup by `RegressionChecks.ValidateStoryReset`, in debug
builds against a scratch file in `user://story-reset-check-<pid>/`, never the real save.

## Bug reports

"Report a bug" sits beside Options on the main menu and in the pause screen's bottom row
(`scripts/ReportBugScreen.cs`). It is DrawFight's bug report screen, ported, and it posts to the
same server: `https://ecec.dev/api/draw/suggest` (EricClancyEngineeringAndCrafts,
`intake/server.js`), which saves each report under `/data/suggestions/<id>/` and emails it to Eric.
The server picks the email subject from the `game` field through an allowlist, so this game must
send exactly `The One He Missed`; anything else is filed as a DrawFight suggestion. (Copies from
before the rename send the game's old name, which the server files under the new one.) The server's
rate limit (a few posts an hour per address) is shared with DrawFight.

**What is sent**, as multipart/form-data with a `TheOneHeMissed/<version>` User-Agent
(`UpdateScreen.UserAgent`, which the update checks send too):

| Field | Value |
|---|---|
| `game` | `The One He Missed` |
| `kind` | `bug` |
| `name` | what the player typed, or empty (40 characters at most) |
| `version` | `application/config/version` |
| `suggestion` | the context lines below, `---`, then what the player typed (2000 characters at most) |
| `screenshot` | a file part, `screenshot.jpg`, `image/jpeg`; only while "Include a screenshot" is on |

The context lines are the ones the screen shows under "Sent with it", from one method, so what is
shown is what is sent: `[The One He Missed 0.2.1 | Windows | PC canvas 1280x720]`, the window and
screen size, from the pause screen the chapter, wizard, time in run, player level and every
spell held with its level, and while the screenshot is on, `Screenshot: attached (WxH, the picture
shown)`. **Nothing else**: no save file, no log, no device or account identifier. Keep it that
way, and if a line is added, the screen shows it automatically.

**The screenshot.** Beside those lines is a thumbnail and an "Include a screenshot" switch, on by
default. The thumbnail is decoded from the JPEG bytes that are uploaded, so it is exactly what
arrives; switched off, it fades, its line goes, and no file part is sent at all. The picture is
the game just before the report screen covered it. From the pause screen that is **the run
without the pause screen**: `Node2DGame` draws the pause screen transparent on the frame it opens,
reads the viewport once that frame is drawn (`RenderingServer.FramePostDraw` - before it the
texture holds nothing; `_UiShot`'s header), shows the pause screen the next frame, and hands the
picture to the report screen. It is taken on every pause and dropped on resume. From the main menu
the report screen photographs the menu the same way, drawn transparent itself for that one frame.
It is scaled so its long side is at most 1600 px and encoded at JPEG quality 85 off the main
thread: about 150-200 KB at 1280x720. The server keeps the first image (JPEG or PNG, under 12 MB),
saves it beside the report and attaches it to the email; a server from before screenshots
ignores the part. If the picture could not be taken, the switch is not shown and the report goes
as text, as before.

The typed text survives Back, Escape, a failed send and closing the pause screen; only a report
the server accepted clears it. A failure shows the server's own message (429 gets ours, since the
server's speaks of fighters) and keeps everything. The request times out after 15 seconds, or 45
with a screenshot, which on a weak phone uplink can take most of 15 by itself.

**Testing without emailing Eric:** set `THEONEHEMISSED_INTAKE_URL=http://127.0.0.1:8799` and run
any local server that answers `POST /api/draw/suggest`. `_UiShot` drives the screen:
`--press-late=ReportBugButton` (with `--open-menu --scene=res://scenes/TitleScreen.tscn`) or
`--open-pause --press-late=EscapeReportBug` in a run opens it, `--report-name=`, `--report-text=`
fill it, `--report-no-shot` switches the screenshot off, `--report-send` presses Send and holds
the shutter for the answer, and `--report-back` checks where focus lands afterwards. The local
server needs to accept a file part; logging each part's headers is enough to see the picture
arrive.

**On Android** the export preset carries `permissions/internet=true`; a debug export adds the
permission by itself only when remote debugging, so without it the phone build could not send.

## The rename

Eric, 2026-11-02: *"remove all references to wizard survivors from the repo, from the website and
from the game itself and replace it with the new name of the game"* - The One He Missed
(`.ai/game-name.md`). Every copy installed before then is 0.11.0 or older under the old name, and
its updater, its file names and its save folder are fixed in it. This section is how those copies
and their saves get across, and the list of what keeps the old name **because changing it would
strand someone**. Do not tidy any of it away.

### What keeps the old name, on purpose

| What | Where | Why it cannot change |
|---|---|---|
| The Android package id, `com.ecec.wizardsurvivors` | `export_presets.cfg` `package/unique_name`; `release.sh` refuses an APK with any other; the `adb` lines in `CLAUDE.md`, `tools/android-deploy.sh` and a few comments | the app's identity: a new id is a second app that cannot update the first, and every installed copy's save is filed under it. Only the label (`package/name`) changed |
| The release keystore's file name and alias | `%APPDATA%\Godot\keystores\`, `release.sh` | made before the rename; the key is what Android checks, and the alias lives inside the keystore |
| The installer's `AppId` | `installer.iss` | never changes by design - and it is what makes the new setup upgrade an old install in place |
| The old ecec.dev addresses, `/wizard-survivors/version.json` and `/wizard-survivors/download` | the website's `nginx.conf` | baked into every copy up to 0.11.0: Windows and Linux ask the first, Android opens the second. They answer exactly as the new ones do, forever |
| The old `user://` folder's name | `UserDataCarryOver.OldFolderName` | where every desktop save from before the rename is |
| `WizardSurvivors.exe`, `.pck`, `.x86_64` in the bridges | `package.py` `OLD_NAME` | what a 0.11.0 updater looks for in a download and restarts |
| The old program, `.pck`, .NET folder, shortcuts, install folder and menu entry | `installer.iss` `[InstallDelete]`, `install.sh` `OLD_DEST` | so a new install can retire one of the old name |
| The checkout folder (`WizardSurvivors/`), the Desktop art folder, the pre-filter backup | outside the repo | not part of the game; named in docs as they are on disk |

Everything else - the game's name in every window, label and line of text, the export and release
file names, the C# namespace, solution, project and assembly (`TheOneHeMissed`), the update and
intake variables (`THEONEHEMISSED_UPDATE_URL`, `..._INTAKE_URL`, `..._KEYSTORE`), the User-Agent,
the bug report's `game`, the URLs and the repo slug (`eclancy/TheOneHeMissed`) - took the new name.
The title's lettering is redrawn on its own branch (`.ai/title-screen.md`).

### Saves: copied once, the old folder left alone

On a desktop Godot names the `user://` folder after `application/config/name`, so the rename moved
it: `%APPDATA%\Godot\app_userdata\Wizard Survivors\` became `...\The One He Missed\` (on Linux,
under `~/.local/share/godot/app_userdata/`). `scripts/UserDataCarryOver.cs`, which
`SaveManager._Ready` runs before it loads the save, copies the old folder into the new one the
first time the game runs under the new name:

- everything - the save, its `before-epoch` and `before-reset` backups, settings, logs, shots -
  except `update/`, the updater's scratch folder (an old copy's apply script may still be deleting
  it while the updated game starts);
- never over a file the new folder already has (this launch's own log), and never into a folder
  that already holds a save of its own;
- the save last, so a copy cut short is finished on the next launch rather than trusted, and then
  a marker, `carried-over.txt`, so it never happens twice - a player who later deletes their save is
  not handed the old one back;
- **the old folder is never moved, deleted or written.** It stays as a backup, and as the live
  folder of any copy not yet updated;
- never from a launch that may not write the save (every `_` tool scene, `--no-save-writes`): a
  tool run that copied would leave a stale save behind for the real first launch to trust while the
  player's live save moved on. So until the first real launch, tool runs see an empty save.

**Why a copy and not `use_custom_user_dir`.** A custom user dir is placed straight under
`%APPDATA%` - not under `Godot\app_userdata\` - so pointing it back at the old folder takes a whole
path, and a different one per platform (`Godot` on Windows and macOS, `godot` on Linux), and it
keeps the old name in `project.godot` for good. (This page's predecessor, `game-name.md`, assumed
`custom_user_dir_name` alone would keep the folder. It would not.) Copying gives the folder the
game's name, costs one copy on one launch, and keeps the old folder untouched.

`RegressionChecks.ValidateUserDataCarryOver` runs the real copy against scratch folders at every
startup: the save copied word for word, a nested file copied, an existing file not overwritten,
`update/` skipped, the old folder byte for byte unchanged, a second launch and a folder with a save
of its own both left alone.

Android is not touched by any of it: there `user://` is the app's private folder, named by the
package id, which did not change.

### Updates: what a copy from before the rename does with a renamed release

A 0.11.0 copy on Windows, step by step, with the first release that carries the new name:

1. It asks the old address; ecec.dev answers with a 302 to the newest release's `version.json` - the
   same file the renamed copies read.
2. The version is newer. If `progressEpoch` has risen, it asks first (0.11.0 knows epochs).
3. It reads the top-level `"patch"` (same engine) or `"full"` (another engine). **Both are the
   Windows bridge.** A Linux copy reads `"linux"`, both of which are the Linux bridge.
4. It unpacks the bridge and looks for `WizardSurvivors.pck` at the top or one folder down. The
   bridge is flat and has it at the top.
5. Its `apply.cmd` robocopies everything beside that `.pck` over its folder - `WizardSurvivors.exe`
   (this release's engine binary, with the new product name), `WizardSurvivors.pck` (the renamed
   game) and `data_TheOneHeMissed_windows_x86_64\` (the renamed assembly with the whole .NET
   runtime) - and starts `WizardSurvivors.exe` again.
6. Godot loads the `.pck` named after the program, so the new game starts. It asks for the
   assembly its `.pck` names, `TheOneHeMissed`, from `data_TheOneHeMissed_windows_x86_64\`, which
   is there in full. `UserDataCarryOver` copies the save across, the window says The One He
   Missed, and its own update check now uses the new address and reads `"builds"`.

The old `data_WizardSurvivors_windows_x86_64\` stays behind, unused (about 86 MB), until a setup is
run over it.

**Why a bridge, and in every release.** The normal patch has no `WizardSurvivors.pck`, so a 0.11.0
copy given it would find nothing to install, log "no update" and go to the title - playing 0.11.0
for good. A patch under the old names would not do either: the renamed assembly's .NET folder would
hold only the game's DLL and no runtime, and the game would not start. So the bridge is the whole
export, and it ships with **every** release: a copy from before the rename that is not started
again for months still needs one from whichever release is newest then. It costs about 170 MB of
upload per release. ecec.dev's download page never offers a bridge - it finds files by how their
names end (`-windows.zip`, `-linux.tar.gz`), and theirs end `-bridge.zip` and `-bridge.tar.gz`.

**From then on** a copy updates from `"builds"` - the 19 MB patch - and installs it **under the
running program's name** (`UpdateScreen.ProgramName`, step 3 of "How a copy updates"): a copy that
crossed on the bridge is `WizardSurvivors.exe`, its Start menu and desktop shortcuts name that
file, and Godot loads the `.pck` named after it, so the release's `TheOneHeMissed.pck` is installed
as `WizardSurvivors.pck` (and in a full download `TheOneHeMissed.exe` as `WizardSurvivors.exe`).
Every install made since the rename has matching names and nothing is renamed.

**Running a new setup over an old install** upgrades it in place (same `AppId`, so the same folder,
`...\Programs\WizardSurvivors\`): `[InstallDelete]` removes the old program, its `.pck`, its .NET
folder and its "Wizard Survivors" shortcuts, and the new program and "The One He Missed" shortcuts
take their place. **On Linux**, `install.sh` installs to `~/.local/share/TheOneHeMissed`, and if an
install of the old name is there (`~/.local/share/WizardSurvivors` with an `install.sh` in it) it
removes it and its menu entry; run from inside an old install - which the bridge gives the new
`install.sh` - it refreshes that one where it is; `--uninstall` takes out both.

**Android** needs nothing: the APK keeps the package id and the signing key, so it installs over an
old copy, save and all, and only the label under the icon changes. An old copy's update strip opens
the old `/download` address, which ecec.dev serves as the new page.

### How it was tested

2026-11-02, on the Windows PC, all in scratch folders. Every game run had `APPDATA` pointed at a
scratch folder, and the old-name user folder it used was a **copy** of the developer's real one; the
real `%APPDATA%\Godot\app_userdata\` folders were only ever read.

- **`release.sh 0.12.0 "rename" --dry-run`** completed: every file built, the installer compiled,
  the APK release-signed (certificate `8052adfc...bddd47`), versionCode 1200, package
  `com.ecec.wizardsurvivors`, label "The One He Missed"; `android/` came out of the export exactly
  as committed, and the tree was clean afterwards. The archives were read back: the Windows bridge
  is flat - `WizardSurvivors.exe`, `WizardSurvivors.pck`, `data_TheOneHeMissed_windows_x86_64\` (188
  files); the Linux bridge the same with `WizardSurvivors.x86_64` (mode 755), `install.sh` and
  `icon.png`; `version.json` has `"builds"` and its top-level `"patch"`, `"full"` and `"linux"`
  name the bridges. It took 29 minutes: the Windows export sat 15 minutes after its DONE line (the
  hang in "Things to know"), and the Android export wrote its APK and then did not exit, so that
  process was stopped by hand rather than waiting out its 30-minute timeout. A first attempt lost
  its Android export to another session's `taskkill` of Godot processes.
- **A 0.11.0 copy across the rename.** The real v0.11.0 `WizardSurvivors-windows.zip` from GitHub,
  unpacked; a copy of the developer's old-name user folder (752 MB, 5,656 files, the save at schema
  18); the dry run's files on a local server (`python -m SimpleHTTPServer 8765`) with
  `version.json`'s URLs pointed at it and `WIZARDSURVIVORS_UPDATE_URL` set; an `override.cfg`
  beside the exe making it headless (`display/display_server/driver.windows="headless"` - the plain
  key is shadowed by the engine's `.windows` one). 0.11.0 took the bridge (its `"patch"`, same
  engine), wrote `apply.cmd` and quit in 3 s; the script robocopied and restarted
  `WizardSurvivors.exe`, which ran the renamed game: its exe and `.pck` byte-identical to the
  bridge's, `data_TheOneHeMissed_windows_x86_64\` present and the old .NET folder left beside it.
  The restarted game copied the user folder in about 8 s - all 5,649 files other than the save
  byte-identical, `update\` not copied, the marker written - then loaded the carried save and
  migrated it to schema 22 in the new folder (Arcane Energy 603 before and after). The old folder
  was unchanged outside `logs\` and `update\`. Launched again it said "0.12.0 is up to date" and
  copied nothing.
- **A renamed copy updating.** The 0.12.0 export, made to report itself as 0.11.9
  (`config/version` in `override.cfg`, put back as soon as the first run exited) and with its
  `.pck` altered so a replacement would show: as `TheOneHeMissed.exe` it took the patch and its
  `.pck` became the release's; as `WizardSurvivors.exe` (a copy that crossed on the bridge) it took
  the patch, logged "TheOneHeMissed.pck installs as WizardSurvivors.pck" and left nothing of the new
  name behind; the same with the release's `"engine"` made to differ, it took the full zip and
  renamed the exe too, replacing the old one. Every restarted copy said "0.12.0 is up to date".
- **`install.sh`**, in Git Bash against a scratch `XDG_DATA_HOME`: run from inside an install of
  the old name it refreshed it in place and replaced its menu entry; installed from a new download
  it retired the old install and entry; `--uninstall` removed both.
- **The game:** `dotnet build TheOneHeMissed.sln`, the headless import, the startup validator (0
  errors, `ValidateUserDataCarryOver` among the checks), the title headless, and `_UiShot` of the
  main menu and of the bug report screen, which shows `[The One He Missed 0.11.0 | Windows | PC
  canvas 1280x720]`. (`_UiShot` over `TitleScreen` with a late press crashed in the C# finalizer,
  `gchandle.is_released()`, as the released 0.11.0 did at its own title in a windowed GL run on
  this machine; the bug report screen was photographed over `MainMenu.tscn` instead.)

**Not tested:** anything on Linux itself (no Linux machine here - the bridge, the patch and
`install.sh` were checked from Windows); the installer run over an old install (on this machine
that would rewrite the developer's own install's uninstall entry - `[InstallDelete]` was only
compiled); the APK on a phone (the dry run checks its signature, version, package id and label);
and the real ecec.dev, which is not deployed yet. A copy restarted by `apply.cmd` writes its log
(`user://logs/godot.log`) only as it exits unless `application/run/flush_stdout_on_print` is on:
the 0.11.0 run's restarted copy was stopped unflushed, so it is judged by what it left on disk and
by its second launch; the renamed-copy runs had the flush in their `override.cfg`.

### Merging and releasing it, in order

1. **Rename the GitHub repo** `eclancy/WizardSurvivors` to `eclancy/TheOneHeMissed`. GitHub then
   redirects the old slug, release downloads included, so links to 0.11.0 and earlier keep working.
   Point each checkout at it: `git remote set-url origin https://github.com/eclancy/TheOneHeMissed.git`.
2. **Deploy the website.** Its `version.json` redirects name `TheOneHeMissed`; before the repo
   rename they would point at nothing (an old copy would just find no update, but do it in order).
   Check: `curl -sI https://ecec.dev/wizard-survivors/version.json` - a 302 to
   `github.com/eclancy/TheOneHeMissed/releases/latest/download/version.json`.
3. **Merge this branch.** Every branch that lands afterwards still saying `WizardSurvivors.scripts`
   needs the namespace command from the mechanical commit's message (`Rename: namespace, solution,
   project and assembly ...`), then `dotnet build TheOneHeMissed.sln`.
4. **The first release from the renamed code** (0.12.0 or later): `--dry-run` first, then for real.
   It is the first with bridges and `"builds"`; nothing else about releasing changed. It can be the
   story reset too - the carry-over runs before the load, so the reset's backup lands in the new
   folder beside the carried save.
5. **After it is out:** fetch `version.json` from both addresses (the same file), and if a 0.11.0
   copy is to hand, start it - it should update, restart as The One He Missed, and keep its save.

## Things to know

- **A desktop export can finish and not exit.** On 2026-11-02 the Linux export wrote its .pck,
  printed its `savepack` DONE line and then sat until the 900 s timeout, which failed the dry run.
  `release.sh` (`export_desktop`) now treats a timeout after that line as a finished export, judged
  by the log and the .pck as the Android export always was. It costs the full timeout when it
  happens, so a release can take 15 minutes longer than it should.
- **Windows SmartScreen** warns "Windows protected your PC" the first time, because neither the
  setup nor the exe is code-signed. `How to play.txt` and the download page tell people to click "More info", then
  "Run anyway".
- **Upgrading Godot** changes the engine version, so every copy downloads the full zip once.
  Install the new export templates before releasing.
- **The game must be somewhere it can write to.** The installer sees to that. Unzipped into Program Files, it cannot update
  itself and silently stays on its version. `How to play.txt` says to keep it in Documents or on
  the Desktop.
- **Windows, Linux and Android are exported.** A Mac build needs Apple notarization to open without a
  fight; iOS needs the App Store.
- The save (`user://savegame.json`) lives in `%APPDATA%\Godot\app_userdata\The One He Missed\`
  (on Linux, `~/.local/share/godot/app_userdata/The One He Missed/`; before the rename, in the
  folder of the old name, which the first launch copies from - see "The rename"), not in the
  game folder, so an update never touches it. On Android it is in the app's private data: an
  update installed over the top keeps it, an uninstall deletes it.
