param([switch]$DemoProfile, [switch]$CheckOnly, [switch]$Rebuild, [switch]$PrepareOnly)
$ErrorActionPreference='Stop'
if($PrepareOnly -and -not $DemoProfile){throw '-PrepareOnly requires explicit -DemoProfile.'}
$root=Split-Path -Parent $PSScriptRoot
$exe=Join-Path $root 'src/Pocket.App/bin/Release/net10.0-windows/Invey.exe'
$localDotnet=Join-Path $root '.tools/dotnet'
if($Rebuild) {
    & "$PSScriptRoot/dotnet.ps1" -Arguments @('restore','Invey.sln','--locked-mode','-p:Platform=x64')
    if($LASTEXITCODE -ne 0){throw 'Locked restore failed. No older build was launched.'}
    & "$PSScriptRoot/dotnet.ps1" -Arguments @('build','Invey.sln','-c','Release','-p:Platform=x64','--no-restore')
    if($LASTEXITCODE -ne 0){throw 'Release build failed. No older build was launched.'}
}
foreach($name in @('Invey.exe','Invey.dll','Invey.deps.json','Invey.runtimeconfig.json','Pocket.Core.dll','Pocket.Overlay.dll','Pocket.Automation.Client.dll','Pocket.Automation.Contracts.dll')) {
    if(-not(Test-Path -LiteralPath (Join-Path (Split-Path $exe) $name))){throw "Missing $name. Run this script with -Rebuild first."}
}
$runtimeConfig=Get-Content -LiteralPath (Join-Path (Split-Path $exe) 'Invey.runtimeconfig.json') -Raw | ConvertFrom-Json
$frameworks=@($runtimeConfig.runtimeOptions.frameworks)
if(-not $frameworks.Count -or $null -eq $frameworks[0]){$frameworks=@($runtimeConfig.runtimeOptions.framework)}
foreach($framework in $frameworks){
    $required=[version]$framework.version
    $shared=Join-Path $localDotnet ('shared/'+$framework.name)
    $compatible=@(Get-ChildItem -LiteralPath $shared -Directory -ErrorAction SilentlyContinue | Where-Object {
        $version=$null
        [version]::TryParse($_.Name,[ref]$version) -and $version.Major -eq $required.Major -and $version.Minor -ge $required.Minor
    })
    if(-not $compatible.Count){throw "Missing compatible project-local $($framework.name) $required runtime. Use the existing Install-Dotnet.ps1 setup before the demo."}
}
$built=(Get-Item -LiteralPath (Join-Path (Split-Path $exe) 'Invey.dll')).LastWriteTimeUtc
$newer=Get-ChildItem (Join-Path $root 'src') -File -Recurse |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in '.cs','.xaml','.csproj' -and $_.LastWriteTimeUtc -gt $built } |
    Select-Object -First 1
if($newer){throw 'Application source is newer than the Release build. Run with -Rebuild; an older executable was not launched.'}
$tracking=@('tests/tracking/recorded_worker.py','.tools/exec05-inference/pyvenv.cfg','.tools/exec05-model/hand_landmarker.task','.tools/exec05-model/public-palm.jpg')
$missing=@($tracking | Where-Object {-not(Test-Path -LiteralPath (Join-Path $root $_))})
$venv=Join-Path $root '.tools/exec05-inference/pyvenv.cfg'
if(Test-Path -LiteralPath $venv){
    $pythonHome=Get-Content -LiteralPath $venv | Where-Object {$_ -like 'home = *'} | Select-Object -First 1
    if(-not $pythonHome -or -not(Test-Path -LiteralPath (Join-Path $pythonHome.Substring(7).Trim() 'python.exe'))){$missing+= 'Python interpreter'}
}
foreach($module in @('mediapipe','numpy')){
    if(-not(Test-Path -LiteralPath (Join-Path $root ('.tools/exec05-inference/Lib/site-packages/'+$module)))){$missing+=$module}
}
if($missing.Count){Write-Warning 'Optional recorded tracking assets are incomplete. Pocket and Workspace remain usable. See DEMO_QUICKSTART.md.'}
if($env:SESSIONNAME -like 'RDP*'){Write-Output 'Remote session detected. Refresh displays if geometry changes; live camera availability may differ.'}
$running=@(Get-Process Invey -ErrorAction SilentlyContinue)
[pscustomobject]@{Executable=$exe; DemoProfile=[bool]$DemoProfile; Camera='Off at startup'; RunningInstances=$running.Count; OptionalTrackingAvailable=($missing.Count -eq 0)}
if($CheckOnly){return}
if($running.Count){throw 'Invey is already running. Use its Exit button before switching or relaunching profiles. No process was killed.'}
$oldRoot=$env:DOTNET_ROOT
$oldX64=$env:DOTNET_ROOT_X64
try {
    $env:DOTNET_ROOT=$localDotnet
    $env:DOTNET_ROOT_X64=$localDotnet
    if($PrepareOnly){
        $process=Start-Process -FilePath $exe -WorkingDirectory $root -ArgumentList @('--demo-profile','--prepare-only') -WindowStyle Hidden -PassThru
        if(-not $process.WaitForExit(15000)){
            $process.Kill()
            if(-not $process.WaitForExit(3000)){throw 'Demo preparation cleanup is unconfirmed. Stop and inspect the owned preparation process.'}
            throw 'Demo preparation exceeded15 seconds and its owned process was stopped. Demo files were preserved; no older build was launched.'
        }
        if($process.ExitCode -ne 0){throw "Demo preparation failed (exit $($process.ExitCode)). No older build was launched."}
        Write-Output 'Demo references prepared. Launch later with -DemoProfile.'
    }
    elseif($DemoProfile){Start-Process -FilePath $exe -WorkingDirectory $root -ArgumentList '--demo-profile'}
    else {Start-Process -FilePath $exe -WorkingDirectory $root}
} finally {$env:DOTNET_ROOT=$oldRoot; $env:DOTNET_ROOT_X64=$oldX64}
