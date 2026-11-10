param([ValidateSet('Baseline','Hybrid')][string]$Mode='Baseline',[switch]$CheckOnly)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$names=@('INVEY_CAMERA_EXECUTION','INVEY_CAMERA_BUDGET','INVEY_CAMERA_DIAGNOSTICS','INVEY_VISUAL_BASELINE')
$previous=@{}
foreach($name in $names){$previous[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}
try {
    $env:INVEY_CAMERA_EXECUTION='execution-27'
    $env:INVEY_CAMERA_BUDGET=Join-Path $root 'artifacts/execution-16/camera-budget.json'
    $env:INVEY_CAMERA_DIAGNOSTICS=Join-Path $root ('artifacts/execution-27/live-'+$Mode.ToLowerInvariant())
    [Environment]::SetEnvironmentVariable('INVEY_VISUAL_BASELINE',$(if($Mode -eq 'Baseline'){'1'}else{$null}),'Process')
    New-Item -ItemType Directory -Force -Path $env:INVEY_CAMERA_DIAGNOSTICS|Out-Null
    # Never applies an amendment, selects a device or presses Enable. Guardian enforces the staged backend.
    # Beta validates commit/hash/runtime, but its normal launch delegates to the older
    # DualCursor wrapper, which would overwrite this execution's diagnostic directory.
    & "$PSScriptRoot/Start-Invey-Beta.ps1" -CheckOnly
    if(-not $CheckOnly){& "$PSScriptRoot/Start-Invey-Demo.ps1" -DemoProfile}
} finally {
    foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$previous[$name],'Process')}
}
