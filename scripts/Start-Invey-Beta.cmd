@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Start-Invey-Beta.ps1" %*
if errorlevel 1 (
  echo Invey Beta did not start. Check the local Invey Beta launch log or rerun the PowerShell command for details.
  pause
)
endlocal
