$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$version = (Get-Content -Raw -LiteralPath (Join-Path $root 'global.json') | ConvertFrom-Json).sdk.version
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Expected an exact stable SDK version in global.json.' }
$channel = ($version.Split('.')[0..1] -join '.')
$toolsDir = Join-Path $root '.tools'
New-Item -ItemType Directory -Force $toolsDir | Out-Null
$metadata = Invoke-RestMethod "https://builds.dotnet.microsoft.com/dotnet/release-metadata/$channel/releases.json"
$sdk = $metadata.releases.sdks | Where-Object version -eq $version | Select-Object -First 1
if (-not $sdk) { throw "Pinned SDK $version is missing from official release metadata." }
$asset = $sdk.files | Where-Object { $_.rid -eq 'win-x64' -and $_.url.EndsWith('.zip') } | Select-Object -First 1
$expectedUrl = "https://builds.dotnet.microsoft.com/dotnet/Sdk/$version/dotnet-sdk-$version-win-x64.zip"
if (-not $asset -or $asset.url -cne $expectedUrl -or $asset.hash -notmatch '^[a-fA-F0-9]{128}$') {
    throw 'Unexpected SDK download identity or checksum.'
}
$zip = Join-Path $toolsDir "dotnet-sdk-$($sdk.version)-win-x64.zip"
Write-Output "Downloading official SDK $($sdk.version) to project-local tools"
if (-not (Test-Path -LiteralPath $zip) -or (Get-FileHash -LiteralPath $zip -Algorithm SHA512).Hash -ne $asset.hash) {
    Invoke-WebRequest $asset.url -MaximumRedirection 0 -UseBasicParsing -OutFile $zip
}
$actual = (Get-FileHash -LiteralPath $zip -Algorithm SHA512).Hash
if ($actual -ne $asset.hash) { throw 'SDK SHA512 verification failed.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$destination = [IO.Path]::GetFullPath((Join-Path $toolsDir 'dotnet'))
$archive = [IO.Compression.ZipFile]::OpenRead($zip)
try {
    foreach ($entry in $archive.Entries) {
        $target = [IO.Path]::GetFullPath((Join-Path $destination $entry.FullName))
        if (-not $target.StartsWith($destination + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid archive path.' }
        if ($entry.Name -eq '') { [IO.Directory]::CreateDirectory($target) | Out-Null; continue }
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $target, $true)
    }
} finally { $archive.Dispose() }
Write-Output "SDK $($sdk.version); verified SHA512 $actual"
& (Join-Path $PSScriptRoot 'dotnet.ps1') -Arguments @('--version')
if ($LASTEXITCODE -ne 0) { throw "Pinned SDK failed to start ($LASTEXITCODE)." }
