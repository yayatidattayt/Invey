param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Arguments)
$root=Split-Path -Parent $PSScriptRoot
$env:DOTNET_CLI_HOME=Join-Path $root '.tools/cli'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false'
$env:MSBUILDDISABLENODEREUSE='1'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH='0'
$env:NUGET_PACKAGES=Join-Path $root '.tools/nuget'
& (Join-Path $root '.tools/dotnet/dotnet.exe') @Arguments
exit $LASTEXITCODE
