$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$toolsDir = Join-Path $root '.tools'
New-Item -ItemType Directory -Force $toolsDir | Out-Null
$metadata = Invoke-RestMethod 'https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json'
$sdk = $metadata.releases.sdk | Where-Object version -eq $metadata.'latest-sdk' | Select-Object -First 1
if (-not $sdk) { throw 'No stable .NET 10 SDK metadata found.' }
$asset = $sdk.files | Where-Object { $_.rid -eq 'win-x64' -and $_.url.EndsWith('.zip') } | Select-Object -First 1
if (-not $asset -or ([uri]$asset.url).Host -notin @('builds.dotnet.microsoft.com','ci.dot.net')) { throw 'Unexpected SDK download source.' }
$zip = Join-Path $toolsDir "dotnet-sdk-$($sdk.version)-win-x64.zip"
Write-Output "Downloading official SDK $($sdk.version) to project-local tools"
if (-not (Test-Path -LiteralPath $zip) -or (Get-FileHash -LiteralPath $zip -Algorithm SHA512).Hash -ne $asset.hash) {
    Invoke-WebRequest $asset.url -OutFile $zip
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
& (Join-Path $toolsDir 'dotnet\dotnet.exe') --info
