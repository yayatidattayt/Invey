# Developer scripts

- `Preflight-Windows.ps1` — read-only Windows development-environment report; does **not** test the app or request camera permission. Run with `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Preflight-Windows.ps1` in PowerShell 5.1, or use `pwsh -File .\scripts\Preflight-Windows.ps1` with PowerShell 7.
- `Sync-Research.ps1` — copy the canonical `docs/research.md` into the required root-level `research.md` mirror after an update. This overwrites **only** the root mirror; edit `docs/research.md` first.
- `Install-Dotnet.ps1` — install the exact Windows x64 SDK from `global.json` into `.tools/dotnet` after verifying the Microsoft archive's SHA-512. No administrator install is needed.
- `dotnet.ps1` — run the project-local SDK from the repository root. Pass arguments as an array from PowerShell, for example `& ./scripts/dotnet.ps1 -Arguments @('--version')`.
- `Run-AutomatedAcceptance.ps1` — run the native isolated-desktop harness on an authorized local Windows test session. Use `-TrackingOnly` for recorded tracking, calibration and controlled file-reader integration. The harness temporarily switches desktops. See the [runner guide](../tests/Pocket.Acceptance.Runner/README.md) and [evidence categories](../VERIFICATION.md).
- `verify_kit.py` and `validate-spec.mjs` — validate documentation and design references.

Tracking integration requires the project-local Python 3.12.10 / MediaPipe 0.10.32 environment under `.tools/exec05-inference` and the checksum-pinned model and qualification still under `.tools/exec05-model`. The [Execution 05 inventory](../docs/EXECUTION_05_REPORT.md) records the upstream assets. Generate the optional local MP4 with `tests/tracking/prepare_native_fixture.py` and FFmpeg. The worker unit tests run independently with NumPy and skip the real file-reader test when `GHOST_NATIVE_VIDEO_FIXTURE` is absent.
