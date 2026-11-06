param([switch]$Build,[ValidateSet('short','standard','extended')][string]$Profile='standard',[int]$Seed=73021,[switch]$TrackingOnly)
$previousProfile=$env:GHOST_ACCEPTANCE_PROFILE
$previousSeed=$env:GHOST_ACCEPTANCE_SEED
$previousTracking=$env:GHOST_ACCEPTANCE_F06_ONLY
$env:GHOST_ACCEPTANCE_PROFILE=$Profile
$env:GHOST_ACCEPTANCE_SEED=$Seed.ToString()
$env:GHOST_ACCEPTANCE_F06_ONLY=if($TrackingOnly){'1'}else{'0'}
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    if($Build) {
        & "$PSScriptRoot/dotnet.ps1" -Arguments @('build','Pocket.Dual.Ghost.sln','-c','Release','-p:Platform=x64')
        if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
    }
    Write-Host 'Starting bounded isolated-desktop tests. The original desktop will be restored. No normal-desktop input injection.'
    & "$PSScriptRoot/dotnet.ps1" -Arguments @('tests/Pocket.Acceptance.Runner/bin/Release/net10.0-windows/Pocket.Acceptance.Runner.dll')
    $result=$LASTEXITCODE
    Get-ChildItem artifacts/automated-acceptance -Directory | Sort-Object Name -Descending | Select-Object -First 1 -ExpandProperty FullName
    exit $result
} finally {Pop-Location;$env:GHOST_ACCEPTANCE_PROFILE=$previousProfile;$env:GHOST_ACCEPTANCE_SEED=$previousSeed;$env:GHOST_ACCEPTANCE_F06_ONLY=$previousTracking}
