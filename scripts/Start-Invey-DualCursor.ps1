param([switch]$NormalProfile,[switch]$CheckOnly)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$names=@('INVEY_CAMERA_EXECUTION','INVEY_CAMERA_BUDGET','INVEY_CAMERA_DIAGNOSTICS')
$previous=@{}
foreach($name in $names){$previous[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}
try {
    $env:INVEY_CAMERA_EXECUTION='execution-18'
    $env:INVEY_CAMERA_BUDGET=Join-Path $root 'artifacts/execution-16/camera-budget.json'
    $env:INVEY_CAMERA_DIAGNOSTICS=Join-Path $root 'artifacts/execution-20/live'
    New-Item -ItemType Directory -Force -Path $env:INVEY_CAMERA_DIAGNOSTICS|Out-Null
    & "$PSScriptRoot/Start-Invey-Demo.ps1" -DemoProfile:(-not $NormalProfile) -CheckOnly:$CheckOnly
} finally {
    foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$previous[$name],'Process')}
}
