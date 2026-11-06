
# Keep root-level research.md synchronized with canonical docs/research.md.
# Use: powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Sync-Research.ps1
$ErrorActionPreference='Stop'
$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root 'docs/research.md'
$dest = Join-Path $root 'research.md'
if (-not (Test-Path $source)) { throw 'Missing docs/research.md' }
Copy-Item -LiteralPath $source -Destination $dest -Force
Write-Host 'Updated research.md from canonical docs/research.md.' -ForegroundColor Green
