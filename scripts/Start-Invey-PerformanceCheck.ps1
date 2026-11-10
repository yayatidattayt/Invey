param([switch]$CheckOnly)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$names=@('INVEY_CAMERA_EXECUTION','INVEY_CAMERA_BUDGET','INVEY_CAMERA_DIAGNOSTICS')
$previous=@{}
foreach($name in $names){$previous[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}
try {
    $env:INVEY_CAMERA_EXECUTION='execution-25'
    $env:INVEY_CAMERA_BUDGET=Join-Path $root 'artifacts/execution-16/camera-budget.json'
    $env:INVEY_CAMERA_DIAGNOSTICS=Join-Path $root 'artifacts/execution-25/live'
    New-Item -ItemType Directory -Force -Path $env:INVEY_CAMERA_DIAGNOSTICS|Out-Null
    # Camera OFF. Never amends the exhausted ledger, selects a device, or presses Enable.
    & "$PSScriptRoot/Start-Invey-Demo.ps1" -DemoProfile -CheckOnly:$CheckOnly
} finally {
    foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$previous[$name],'Process')}
}
