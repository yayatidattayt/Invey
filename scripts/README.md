# Developer scripts

- `Preflight-Windows.ps1` — read-only Windows development-environment report; does **not** test the app or request camera permission. Run with `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Preflight-Windows.ps1` in PowerShell 5.1, or use `pwsh -File .\scripts\Preflight-Windows.ps1` with PowerShell 7.
- `Sync-Research.ps1` — copy the canonical `docs/research.md` into the required root-level `research.md` mirror after an update. This overwrites **only** the root mirror; edit `docs/research.md` first.
- `verify_kit.py` — optional Python 3 standard-library integrity checks for the spec pack. Python is **not required** for Gate A1 if this script isn't used.

**None of these scripts installs dependencies or changes Windows system configuration.**
