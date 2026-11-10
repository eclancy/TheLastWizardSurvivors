#requires -Version 7.0
param(
    [string]$Apk = "build/release/TheLastWizardSurvivors-android.apk",
    [switch]$SkipExport
)
# Runs Windows release-engine UI checks against the real APK assets.
# This does not emulate Android's native runtime, JNI, GPU driver or ARM64 execution.
$ErrorActionPreference = "Stop"
$repo = Split-Path $PSScriptRoot -Parent
Set-Location -LiteralPath $repo
$godot = $env:GODOT_BIN
if (-not $godot) { $godot = (Get-Content .claude/settings.json -Raw | ConvertFrom-Json).env.GODOT_BIN }
if (-not (Test-Path -LiteralPath $godot)) { throw "GODOT_BIN must point to the configured Godot Mono executable" }
$apkPath = (Resolve-Path -LiteralPath $Apk).Path
$work = Join-Path $repo "build/android-startup-check"
New-Item -ItemType Directory -Path $work -Force | Out-Null
$assetPack = Join-Path $work "android-assets.zip"
$sourceZip = [System.IO.Compression.ZipFile]::OpenRead($apkPath)
$targetStream = [System.IO.File]::Open($assetPack, [System.IO.FileMode]::Create)
$targetZip = [System.IO.Compression.ZipArchive]::new($targetStream, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($entry in $sourceZip.Entries) {
        if (-not $entry.FullName.StartsWith("assets/") -or $entry.FullName.EndsWith("/") -or $entry.FullName.StartsWith("assets/.godot/mono/")) { continue }
        $outEntry = $targetZip.CreateEntry($entry.FullName.Substring(7), [System.IO.Compression.CompressionLevel]::NoCompression)
        $inputStream = $entry.Open()
        $outputStream = $outEntry.Open()
        try { $inputStream.CopyTo($outputStream) } finally { $outputStream.Dispose(); $inputStream.Dispose() }
    }
} finally { $targetZip.Dispose(); $sourceZip.Dispose() }

function Invoke-CheckProcess([string]$Exe, [string[]]$Arguments, [string]$Log, [int]$TimeoutSeconds = 180) {
    # Git Bash waits for Windows GUI executables and gives headless exports a bounded lifetime.
    # Pass executable/arguments as argv, never interpolate them into shell code.
    $bash = Join-Path $env:ProgramFiles "Git/bin/bash.exe"
    if (-not (Test-Path -LiteralPath $bash)) { throw "Git Bash is required, as for the release script" }
    $launcher = 'task_exe=$(cygpath -u "$1"); shift; task_limit=$1; shift; timeout "$task_limit" "$task_exe" "$@"'
    & $bash -c $launcher -- $Exe $TimeoutSeconds @Arguments *> ($Log + ".process.txt")
    if ($LASTEXITCODE -ne 0) { throw "Startup check failed ($LASTEXITCODE): $Log" }
}
# The program's name since 0.24.0 (package.py's NAME); Godot loads the .pck named after it. A fixture
# exported before then is TheOneHeMissed.exe, which -SkipExport does not find: run once without it.
$exe = Join-Path $work "TheLastWizardSurvivors.exe"
if (-not $SkipExport) {
    Invoke-CheckProcess $godot @("--headless", "--path", $repo, "--export-release", "Windows Desktop", $exe, "--", "--no-save-writes") (Join-Path $work "export.log") 900
}
if (-not (Test-Path -LiteralPath $exe)) { throw "The release-engine fixture is missing; run without -SkipExport" }
$scratchProfile = Join-Path $work "profile"
New-Item -ItemType Directory -Path $scratchProfile -Force | Out-Null
$priorAppData = $env:APPDATA
try {
    $env:APPDATA = $scratchProfile
    # MainLoop probes have no SaveManager/autoloads, so each fresh process truly starts cold.
    foreach ($order in @("unlocks", "achievements")) {
        $log = Join-Path $work ("catalog-" + $order + ".log")
        $arguments = @("--headless", "--log-file", $log, "--script", "res://scripts/_UnlockStartupProbe.cs",
            "--", "--no-save-writes")
        if ($order -eq "achievements") { $arguments += "--achievements-first" }
        Invoke-CheckProcess $exe $arguments $log
        if (-not (Select-String -LiteralPath $log -Pattern "UNLOCKPROBE $order first: PASS" -Quiet)) {
            throw "Cold catalog check did not complete: $log"
        }
        Write-Output "catalog-${order}: PASS"
    }
    $cases = @(
        @{ Name = "apk-assets"; Extra = @("--stress-gc", "--asset-pack=$assetPack") },
        @{ Name = "resource-failure"; Extra = @("--failed-load") },
        @{ Name = "preparation-failure"; Extra = @("--failed-preparation") },
        @{ Name = "returning"; Extra = @("--returning", "--asset-pack=$assetPack") }
    )
    foreach ($case in $cases) {
        $log = Join-Path $work ($case.Name + ".log")
        $arguments = @("--resolution", "540x1212", "--log-file", $log, "res://scenes/_TitleLoadProbe.tscn",
            "--", "--no-save-writes", "--no-update", "--portrait", "--safe-area", "--release-portrait", "--no-early-input") + $case.Extra
        Invoke-CheckProcess $exe $arguments $log
        Write-Output "$($case.Name): PASS"
    }
} finally { $env:APPDATA = $priorAppData }
Write-Output "Phone-free checks passed. Logs: $work"
