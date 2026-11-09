param([switch]$UseNormalProfile,[switch]$Rebuild,[switch]$CheckOnly)
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$exe=Join-Path $root 'src/Pocket.App/bin/Release/net10.0-windows/Invey.exe'
$demoLauncher=Join-Path $PSScriptRoot 'Start-Invey-DualCursor.ps1'
$logDir=Join-Path $env:LOCALAPPDATA 'Invey/Beta'
$log=Join-Path $logDir 'launch.log'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
function Write-BetaLog([string]$line){
    if((Test-Path -LiteralPath $log) -and (Get-Item -LiteralPath $log).Length -gt 48KB){
        $tail=Get-Content -LiteralPath $log -Tail 120
        Set-Content -LiteralPath $log -Value $tail -Encoding utf8
    }
    Add-Content -LiteralPath $log -Value ("{0} {1}" -f [DateTime]::UtcNow.ToString('O'),$line) -Encoding utf8
}
Push-Location $root
try {
    $commit=(& git -C $root rev-parse HEAD 2>$null | Select-Object -First 1)
    if(-not $commit){throw 'Cannot identify the source commit. Open this launcher from the Invey repository.'}
    $dirty=@(& git -C $root status --porcelain --untracked-files=normal)
    if($Rebuild){
        & (Join-Path $PSScriptRoot 'dotnet.ps1') -Arguments @('restore','Invey.sln','--locked-mode','-p:Platform=x64','-m:1')
        if($LASTEXITCODE -ne 0){throw 'Locked restore failed. The beta app was not launched.'}
        & (Join-Path $PSScriptRoot 'dotnet.ps1') -Arguments @('build','Invey.sln','-c','Release','-p:Platform=x64','--no-restore','-m:1')
        if($LASTEXITCODE -ne 0){throw 'Release build failed. The beta app was not launched.'}
    }
    if(-not(Test-Path -LiteralPath $exe)){throw "Release app is missing: $exe`nFrom the repository folder, run .\scripts\Start-Invey-Beta.ps1 -Rebuild."}
    if($dirty.Count){throw 'Source checkout has uncommitted changes. Commit or preserve them, then launch from a clean verified build.'}
    $hash=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash
    Write-Host "Source commit: $commit"
    Write-Host "Executable:    $exe"
    Write-Host "SHA-256:       $hash"
    Write-Host ("Profile:       {0}" -f $(if($UseNormalProfile){'normal Invey workspace'}else{'isolated Invey Beta profile'}))
    Write-BetaLog "commit=$commit sha256=$hash profile=$(if($UseNormalProfile){'normal'}else{'beta'})"
    if($UseNormalProfile){& $demoLauncher -NormalProfile -CheckOnly}
    else {& $demoLauncher -CheckOnly}
    if($CheckOnly){Write-Host 'Preflight passed. Camera remains off; no app was started.';return}
    if($UseNormalProfile){& $demoLauncher -NormalProfile}
    else {& $demoLauncher}
    Write-BetaLog 'launcher returned after handing off startup to Invey'
}
catch {
    Write-BetaLog ("preflight-or-launch-error="+$_.Exception.Message)
    Write-Error $_
    exit 1
}
finally {Pop-Location}
