# Invey

**invey.** (pronounced **in-vay**) is a Windows application bringing together three focused subsystems:

- **Pocket** — capture a region of your screen and reopen it later as a local snapshot.
- **Dual** — an interaction model that keeps the ordinary physical mouse independent from the logical hand-controlled channel.
- **Ghost** — local hand tracking using MediaPipe with an explicit recorded or live camera source.

GHOST was the former product name; the public product name is now Invey. Open **Tracking lab** to select a source, explicitly enable tracking, and inspect the provisional calibration preview.

## Current status

The Tracking lab includes a landmark-only calibration preview, selected-display mapping, local numeric preferences and a Move/Pinch/Release practice target. The camera starts off and requires explicit Enable. Execution 10 verified webcam acquisition and release. Live Ready, physical rectangle routing and physical two-hand behavior remain open.

The isolated acceptance runner and Debug/Release builds pass their current deterministic checks. A1 physical acceptance remains blocked; A2 is fixture-only and provisional, with no general external control; B1 and full F07 acceptance remain open. This is a development snapshot, not release-ready.

See [verification and evidence availability](VERIFICATION.md), the [Execution 11 report](docs/EXECUTION_11_REPORT.md) and the [current handoff](codex/HANDOFF.md).

## Build and development

From the repository root on Windows x64, run these commands in PowerShell.

```powershell
& ./scripts/Install-Dotnet.ps1
& ./scripts/dotnet.ps1 -Arguments @('restore','Pocket.Dual.Ghost.sln','--locked-mode','-p:Platform=x64')
& ./scripts/dotnet.ps1 -Arguments @('build','Pocket.Dual.Ghost.sln','-c','Debug','-p:Platform=x64','--no-restore')
& ./scripts/dotnet.ps1 -Arguments @('test','tests/Pocket.Core.Tests','-c','Debug','--no-build')
& ./scripts/dotnet.ps1 -Arguments @('run','--project','src/Pocket.App','-c','Debug','--no-build')
```

The [developer scripts](scripts/README.md) describe local tracking runtime and native test prerequisites.

The source includes the Windows application, local tracking and automation workers, tests, design references, architecture documents, and engineering reports. It does not require a cloud service or paid AI API, and camera frames are not uploaded.

## Save a Pocket reference

1. Open **Pocket · capture and references** in Invey or from the tray.
2. Choose the display and select **Capture region**. Invey hides its windows while taking the frozen snapshot.
3. Drag a rectangle or use **Keyboard region**. Arrow keys move the selection and Shift with an arrow resizes it.
4. Choose **Preview**, then **Save to Pocket**. Escape cancels before saving.
5. Select the saved reference and choose **Open reference**. **Hide reference** returns you to the library.

Pocket works with the camera off. Confirmed crops are stored as local PNGs in your Windows profile, where account permissions apply. Screen content can be sensitive. Review the crop before saving. See the [Execution 13 report](docs/EXECUTION_13_REPORT.md) for scope and verification.

## Key files

- `AGENTS.md` — permanent project instructions.
- `docs/MASTER_SPEC.md` and `docs/IMPLEMENTATION_RFC.md` — product and implementation contracts.
- `docs/QA_MATRIX.md` and `docs/TEST_RESULTS.md` — verification status.
- `design/DESIGN_TOKENS.json` — visual design tokens.
- `VERIFICATION.md` — verified and unverified status.
