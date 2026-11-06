
# Safe, read-only environment check for POCKET x DUAL x GHOST.
# Use: powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Preflight-Windows.ps1
# Does not access the camera, capture screen, modify system settings, or require admin.
$ErrorActionPreference = 'Continue'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
Write-Host '=== POCKET DUAL GHOST / Windows preflight ===' -ForegroundColor Cyan
$onWindows = ($env:OS -eq 'Windows_NT')
if (-not $onWindows) {
    Write-Warning 'This implementation target requires Windows 11 x64. Run preflight on the actual Windows development machine.'
    exit 2
}
try {
    $os = Get-CimInstance Win32_OperatingSystem -ErrorAction Stop
    Write-Host ('OS: ' + $os.Caption + ' / ' + $os.Version + ' / build ' + $os.BuildNumber)
    if ([int]$os.BuildNumber -lt 22000) { Write-Warning 'Target is Windows 11; older builds are not validated.' }
} catch {
    Write-Warning ('Cannot read full OS metadata: ' + $_.Exception.Message)
    Write-Host ('Fallback OS: ' + [System.Environment]::OSVersion.VersionString)
}
Write-Host ('Process architecture: ' + [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture)
Write-Host ('64-bit OS: ' + [System.Environment]::Is64BitOperatingSystem)

foreach ($name in @('dotnet','git','python','py')) {
    $cmd = Get-Command $name -ErrorAction SilentlyContinue
    if ($null -eq $cmd) { Write-Warning ($name + ': NOT FOUND (Python is optional for Gate A1)') }
    else { Write-Host ($name + ': ' + $cmd.Source) }
}
$dotnet = Get-Command 'dotnet' -ErrorAction SilentlyContinue
if ($dotnet) {
    try {
        Write-Host '.NET SDKs:'
        & dotnet --list-sdks
    } catch { Write-Warning ('dotnet --list-sdks failed: ' + $_.Exception.Message) }
} else {
    Write-Warning 'Gate A1 cannot compile until a suitable .NET SDK is installed.'
}
$files = @('AGENTS.md','CODEX_START_PROMPT.md','docs/research.md', 'research.md', 'docs/MASTER_SPEC.md', 'docs/IMPLEMENTATION_RFC.md', 'docs/DESIGN_SYSTEM.md', 'docs/QA_MATRIX.md', 'docs/CODEX_PHASES.md', 'docs/TEST_RESULTS.md', 'design/UX_CONCEPT.html')
$missing = @()
foreach ($f in $files) {
    if (-not (Test-Path (Join-Path $root $f))) { $missing += $f }
}
if ($missing.Count -gt 0) { Write-Error ('Missing project files: ' + ($missing -join ', ')); exit 1 }
$hashA = (Get-FileHash (Join-Path $root 'research.md') -Algorithm SHA256).Hash
$hashB = (Get-FileHash (Join-Path $root 'docs/research.md') -Algorithm SHA256).Hash
if ($hashA -ne $hashB) {
    Write-Warning 'research.md differs from docs/research.md. See scripts/Sync-Research.ps1.'
} else { Write-Host 'Research mirror: IN SYNC' -ForegroundColor Green }
Write-Host 'Preflight complete. This does NOT test mouse passthrough, camera or UI Automation.' -ForegroundColor Cyan
