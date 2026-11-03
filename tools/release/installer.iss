; The One He Missed Windows installer (Inno Setup 6). Built by tools/release/release.sh:
;
;   ISCC.exe -DAppVersion=0.2.1 tools\release\installer.iss
;
; Packs the export in build\windows\ into build\release\TheOneHeMissed-Setup.exe.
;
; Per-user, no admin prompt: it installs to %LOCALAPPDATA%\Programs\TheOneHeMissed, which the
; player can write to - and must, because the game updates itself by robocopying new files into
; its own folder (scripts/UpdateScreen.cs). Program Files would leave it stuck on its version.
; See .ai/releasing.md.

#ifndef AppVersion
  #error Pass the version: ISCC -DAppVersion=X.Y.Z tools\release\installer.iss
#endif

[Setup]
; Never change this GUID: it is how Windows knows a new setup is the same program, so it upgrades
; in place instead of installing a second copy. It is why the rename needed nothing more here than
; the [InstallDelete] below: a copy installed as Wizard Survivors is this program.
AppId={{B8EAA8FA-C3BC-4790-ACE3-438B4BC1F103}
AppName=The One He Missed
AppVersion={#AppVersion}
; Apps & features shows the name alone; the version has its own column there.
UninstallDisplayName=The One He Missed
AppPublisher=ECEC
AppPublisherURL=https://ecec.dev/games/the-one-he-missed
; Paths below are relative to the repo root.
SourceDir=..\..
PrivilegesRequired=lowest
; A new install. An upgrade keeps the folder it already has (UsePreviousAppDir) - for a copy installed
; before the rename, ...\Programs\WizardSurvivors, which nothing needs to move.
DefaultDirName={localappdata}\Programs\TheOneHeMissed
DisableProgramGroupPage=yes
DisableDirPage=auto
UsePreviousAppDir=yes
WizardStyle=modern
SetupIconFile=icon.ico
UninstallDisplayIcon={app}\TheOneHeMissed.exe
OutputDir=build\release
OutputBaseFilename=TheOneHeMissed-Setup
Compression=lzma2/max
SolidCompression=yes
CloseApplications=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[InstallDelete]
; KEPT ON PURPOSE, the game's old name: a copy installed before the rename (0.11.0 and earlier) ran as
; WizardSurvivors.exe, with shortcuts named Wizard Survivors, and kept that program name when it
; updated itself across the rename (UpdateScreen installs under the running program's name, so the
; shortcuts keep working). Installing this setup over it finishes the move: the old program, its .pck,
; its .NET folder and its shortcuts go, and the ones below take their place. The save is elsewhere
; (%APPDATA%\Godot\app_userdata\) and is never touched. See .ai/releasing.md, "The rename".
Type: files; Name: "{app}\WizardSurvivors.exe"
Type: files; Name: "{app}\WizardSurvivors.pck"
Type: filesandordirs; Name: "{app}\data_WizardSurvivors_windows_x86_64"
Type: files; Name: "{autoprograms}\Wizard Survivors.lnk"
Type: files; Name: "{autodesktop}\Wizard Survivors.lnk"

[Files]
; The export only. "How to play.txt" is written into the zip by package.py, never into
; build\windows, so it is not picked up here.
Source: "build\windows\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{autoprograms}\The One He Missed"; Filename: "{app}\TheOneHeMissed.exe"
Name: "{autodesktop}\The One He Missed"; Filename: "{app}\TheOneHeMissed.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\TheOneHeMissed.exe"; Description: "Play The One He Missed now"; Flags: postinstall nowait skipifsilent

[UninstallDelete]
; The whole folder, including files the self-updater added. The save lives in
; %APPDATA%\Godot\app_userdata\The One He Missed\ (and, from before the rename, ...\Wizard Survivors\)
; and is deliberately left alone.
Type: filesandordirs; Name: "{app}"
