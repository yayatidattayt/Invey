param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Arguments)
$root=Split-Path -Parent $PSScriptRoot
$env:DOTNET_CLI_HOME=Join-Path $root '.tools/cli'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false'
$env:MSBUILDDISABLENODEREUSE='1'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH='0'
$env:NUGET_PACKAGES=Join-Path $root '.tools/nuget'
$dotnet = Join-Path $root '.tools/dotnet/dotnet.exe'
$pin = (Get-Content -Raw -LiteralPath (Join-Path $root 'global.json') | ConvertFrom-Json).sdk.version
if (-not (Test-Path -LiteralPath (Join-Path $root ".tools/dotnet/sdk/$pin/dotnet.dll"))) {
    throw "Project-local SDK $pin is missing. Run scripts/Install-Dotnet.ps1 first."
}
Push-Location $root
try {
    & $dotnet @Arguments
    exit $LASTEXITCODE
} finally { Pop-Location }
