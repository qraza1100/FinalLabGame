<#
.SYNOPSIS
    Rebuilds Build\DrownedSanctum.exe automatically when the project's source changes.

.DESCRIPTION
    One-shot (default): compares every file under Assets\, Packages\ and ProjectSettings\ with the
    snapshot taken after the last successful build. If anything changed, Unity runs in batch mode
    and builds the Windows exe. Otherwise it does nothing.

    -Watch keeps running and rebuilds a few seconds after you save a change.

    Changes to Assets\Level\level_map.txt or Assets\Editor\* regenerate the scenes and prefabs first
    (the same as "Drowned Sanctum > 1. Build Everything"), because those only take effect that way.
    Any other change builds the exe from the scenes as they are, so hand edits are kept.

    The Unity Editor must be closed while this runs, because Unity locks an open project.
    Unity Hub must be signed in, because batch mode needs the license.

.EXAMPLE
    .\build.ps1                 # build only if something changed
.EXAMPLE
    .\build.ps1 -Watch          # keep watching and rebuild on every change
.EXAMPLE
    .\build.ps1 -Force -Regenerate   # full rebuild: regenerate scenes, then build the exe
#>
param(
    [switch]$Watch,
    [switch]$Force,
    [switch]$Regenerate,
    [string]$UnityPath,
    [int]$Interval = 3,
    [int]$Debounce = 4
)

$ErrorActionPreference = 'Stop'
$Root = $PSScriptRoot
$StateDir = Join-Path $Root 'Library\AutoBuild'
$ManifestPath = Join-Path $StateDir 'last-build.json'
$LogDir = Join-Path $Root 'Logs'
$ExePath = Join-Path $Root 'Build\DrownedSanctum.exe'
$Watched = @('Assets', 'Packages', 'ProjectSettings')

function Write-Stamp([string]$msg, [string]$color = 'Gray') {
    Write-Host ("[{0}] {1}" -f (Get-Date -Format 'HH:mm:ss'), $msg) -ForegroundColor $color
}

function Find-Unity {
    if ($UnityPath) {
        if (Test-Path $UnityPath) { return $UnityPath }
        throw "Unity not found at $UnityPath"
    }
    $verFile = Join-Path $Root 'ProjectSettings\ProjectVersion.txt'
    $ver = (Select-String -Path $verFile -Pattern 'm_EditorVersion:\s*(\S+)').Matches[0].Groups[1].Value
    $candidates = @(
        "$env:ProgramFiles\Unity\Hub\Editor\$ver\Editor\Unity.exe",
        "${env:ProgramFiles(x86)}\Unity\Hub\Editor\$ver\Editor\Unity.exe"
    )
    $secondary = Join-Path $env:APPDATA 'UnityHub\secondaryInstallPath.json'
    if (Test-Path $secondary) {
        $dir = (Get-Content $secondary -Raw).Trim().Trim('"')
        if ($dir) { $candidates += (Join-Path $dir "$ver\Editor\Unity.exe") }
    }
    foreach ($c in $candidates) { if (Test-Path $c) { return $c } }
    throw "Unity $ver is not installed (looked in: $($candidates -join '; ')). Install it with Unity Hub or pass -UnityPath."
}

# path -> "size:lastWriteTicks" for every source file
function Get-Snapshot {
    $map = @{}
    foreach ($d in $Watched) {
        $full = Join-Path $Root $d
        if (-not (Test-Path $full)) { continue }
        Get-ChildItem $full -Recurse -File -Force -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -notmatch '(\.tmp|\.swp|~)$' } |
            ForEach-Object {
                $rel = $_.FullName.Substring($Root.Length + 1)
                $map[$rel] = "$($_.Length):$($_.LastWriteTimeUtc.Ticks)"
            }
    }
    return $map
}

function Get-Manifest {
    if (-not (Test-Path $ManifestPath)) { return $null }
    $obj = Get-Content $ManifestPath -Raw | ConvertFrom-Json
    $map = @{}
    foreach ($p in $obj.PSObject.Properties) { $map[$p.Name] = $p.Value }
    return $map
}

function Save-Manifest($snap) {
    New-Item -ItemType Directory -Force -Path $StateDir | Out-Null
    $snap | ConvertTo-Json -Compress | Set-Content -Path $ManifestPath -Encoding UTF8
}

function Get-Changes($old, $new) {
    $changed = New-Object System.Collections.Generic.List[string]
    foreach ($k in $new.Keys) { if (-not $old.ContainsKey($k) -or $old[$k] -ne $new[$k]) { $changed.Add($k) } }
    foreach ($k in $old.Keys) { if (-not $new.ContainsKey($k)) { $changed.Add($k) } }
    return ,$changed
}

function Test-NeedsRegenerate($changes) {
    foreach ($c in $changes) {
        if ($c -like 'Assets\Level\*' -or $c -like 'Assets\Editor\*') { return $true }
    }
    return $false
}

function Test-ProjectOpen {
    $lock = Join-Path $Root 'Temp\UnityLockfile'
    if (-not (Test-Path $lock)) { return $false }
    try {
        $fs = [System.IO.File]::Open($lock, 'Open', 'ReadWrite', 'None')
        $fs.Close()
        return $false
    } catch { return $true }
}

function Invoke-UnityBuild([bool]$regen) {
    if (Test-ProjectOpen) {
        Write-Stamp "The project is open in the Unity Editor. Close it first, or build from inside Unity with 'Drowned Sanctum > 2. Build Windows EXE'." 'Yellow'
        return $false
    }
    $unity = Find-Unity
    New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
    $log = Join-Path $LogDir ("autobuild-{0}.log" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
    $method = if ($regen) { 'DrownedSanctum.EditorTools.SanctumBuilder.BatchBuild' } else { 'DrownedSanctum.EditorTools.SanctumBuilder.BatchBuildExe' }
    $mode = if ($regen) { 'regenerate scenes + build exe' } else { 'build exe' }
    Write-Stamp "Building ($mode)... log: $log" 'Cyan'
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $unityArgs = @('-batchmode', '-projectPath', "`"$Root`"", '-executeMethod', $method, '-logFile', "`"$log`"")
    # Wait for Unity itself only: Unity leaves helper processes (licensing client etc.) running,
    # and Start-Process -Wait would wait for those too.
    $p = Start-Process -FilePath $unity -ArgumentList $unityArgs -PassThru -NoNewWindow
    $null = $p.Handle   # keeps the handle so ExitCode is readable afterwards
    $p.WaitForExit()
    if ($p.ExitCode -eq 0) {
        Write-Stamp ("Build succeeded in {0}s -> Build\DrownedSanctum.exe" -f [int]$sw.Elapsed.TotalSeconds) 'Green'
        return $true
    }
    Write-Stamp "Build FAILED (exit code $($p.ExitCode)). First errors:" 'Red'
    Select-String -Path $log -Pattern 'error CS|\[Builder\] FAILED|Scripts have compiler errors|Build Failed' |
        Select-Object -First 15 | ForEach-Object { Write-Host ("    " + $_.Line) -ForegroundColor Red }
    if (Select-String -Path $log -Pattern 'No valid Unity Editor license' -Quiet) {
        Write-Stamp "No Unity license: open Unity Hub, sign in, then run this again." 'Yellow'
    }
    return $false
}

function Invoke-BuildIfNeeded([bool]$quietWhenClean) {
    $snap = Get-Snapshot
    $old = Get-Manifest
    $changes = if ($old) { Get-Changes $old $snap } else { $null }
    $clean = $old -and $changes.Count -eq 0 -and (Test-Path $ExePath)
    if ($clean -and -not $Force) {
        if (-not $quietWhenClean) { Write-Stamp 'No source changes since the last build. Nothing to do (use -Force to rebuild anyway).' }
        return
    }
    if (-not $old) {
        Write-Stamp 'No previous build snapshot found; building once to create one.'
    } elseif ($changes.Count -gt 0) {
        Write-Stamp "$($changes.Count) changed file(s):"
        $changes | Select-Object -First 12 | ForEach-Object { Write-Host "    $_" }
        if ($changes.Count -gt 12) { Write-Host '    ...' }
    }
    $regen = $Regenerate -or ($old -and (Test-NeedsRegenerate $changes))
    if (Invoke-UnityBuild $regen) { Save-Manifest (Get-Snapshot) }
}

if ($Watch) {
    Write-Stamp "Watching Assets\, Packages\ and ProjectSettings\ for changes (Ctrl+C to stop)." 'Cyan'
    Invoke-BuildIfNeeded $true
    $last = Get-Snapshot
    $pendingSince = $null
    while ($true) {
        Start-Sleep -Seconds $Interval
        $now = Get-Snapshot
        if ((Get-Changes $last $now).Count -gt 0) {
            # still changing: wait until files have been quiet for $Debounce seconds
            $last = $now
            $pendingSince = Get-Date
            continue
        }
        if ($pendingSince -and ((Get-Date) - $pendingSince).TotalSeconds -ge $Debounce) {
            $pendingSince = $null
            Invoke-BuildIfNeeded $false
            $last = Get-Snapshot
        }
    }
} else {
    Invoke-BuildIfNeeded $false
}
