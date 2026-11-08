# Invey

**invey.** (pronounced **in-vay**) is a Windows application bringing together three focused subsystems:

- **Pocket** — capture a region of your screen and reopen it later as a local snapshot.
- **Dual** — an interaction model that keeps the ordinary physical mouse independent from the logical hand-controlled channel.
- **Ghost** — local hand tracking using MediaPipe with an explicit recorded or live camera source.

GHOST was the former product name; the public product name is now Invey. Open **Tracking and calibration** to select a source, explicitly enable tracking, and inspect the calibration preview.

## Demonstration

For the camera-free demo workflow, start with [Demo quickstart](docs/DEMO_QUICKSTART.md), the [short demonstration script](docs/DEMO_SCRIPT.md) and the [Execution17 report](docs/EXECUTION_17_DEMO_REPORT.md). The optional demo profile contains two labeled synthetic references and is separate from your normal library. Native remote-session rehearsal still requires manual confirmation; the report distinguishes it from passing control-route checks.

## Current status

The Tracking lab includes an optional memory-only live camera image with clean, fingertip and full-landmark overlays, plus a selected-display mapping, local numeric preferences and a Move/Pinch/Release practice target. The camera starts off and requires explicit Enable. Authorized hardware sessions have displayed live Ready and verified camera release. Physical rectangle routing, selected-hand behavior and physical two-hand cancellation remain open.

The isolated acceptance runner verifies the camera-off shelf, shortcuts and Workspace on controlled content. A1 physical acceptance remains blocked. A2 remains provisional and fixture-scoped. B1 and release acceptance remain open.

See [verification and evidence availability](VERIFICATION.md), the [Execution 15 report](docs/EXECUTION_15_REPORT.md) and the [current handoff](codex/HANDOFF.md).

## See your hand locally

Open **Tracking and calibration**, choose **Live camera**, find and select the webcam, then select **Show local camera view and finger landmarks** before **Enable**. Overlay mode can change while running. **Mirror camera image** mirrors the video and its matching landmarks together; **Mirror horizontal pointer mapping** changes only the calibration pointer direction. **Stop** releases the worker and clears the image. No video is recorded or uploaded.

The experimental finger pointer can retain an in-frame model index point when the wrist is outside. This does not guarantee isolated-finger cold acquisition or grant partial observations permission to pinch. See [Execution16 evidence](docs/EXECUTION_16_REPORT.md) for measured scope and open acceptance.

## Build and development

From the repository root on Windows x64, run these commands in PowerShell.

```powershell
& ./scripts/Install-Dotnet.ps1
& ./scripts/dotnet.ps1 -Arguments @('restore','Invey.sln','--locked-mode','-p:Platform=x64')
& ./scripts/dotnet.ps1 -Arguments @('build','Invey.sln','-c','Debug','-p:Platform=x64','--no-restore')
& ./scripts/dotnet.ps1 -Arguments @('test','tests/Pocket.Core.Tests','-c','Debug','--no-build')
& ./scripts/dotnet.ps1 -Arguments @('run','--project','src/Pocket.App','-c','Debug','--no-build')
```

The [developer scripts](scripts/README.md) describe local tracking runtime and native test prerequisites.

The source includes the Windows application, local tracking and automation workers, tests, design references, architecture documents, and engineering reports. It does not require a cloud service or paid AI API, and camera frames are not uploaded.

## Save a Pocket reference

1. Open **Pocket shelf** in Invey or toggle it from the tray.
2. Choose the display and select **Capture region**. Invey hides its windows while taking the frozen snapshot.
3. Drag a rectangle or use **Keyboard region**. Arrow keys move the selection and Shift with an arrow resizes it.
4. Choose **Preview**, then **Save to Pocket**. Escape cancels before saving.
5. Select the saved reference and choose **Open reference**. **Hide reference** returns you to the library.

Pocket works with the camera off. Confirmed crops are stored as local PNGs in your Windows profile, where account permissions apply. Screen content can be sensitive. Review the crop before saving. See the [Execution 13 report](docs/EXECUTION_13_REPORT.md) for scope and verification.

The desktop executable is `Invey.exe`. Invey uses `%LOCALAPPDATA%\Invey` and preserves the legacy profile when it migrates verified references and calibration settings. Opened snapshots are independent cards. Pin a card to keep it above other windows and restore it at restart. The library supports title changes, explicit ordering, and Delete with a 10-minute Undo window. [Execution 14 report](docs/EXECUTION_14_REPORT.md)

## Tuck the shelf and use Workspace

Use **Tuck shelf** or Escape to hide Pocket and keep pinned reference cards available. The Pocket button reveals the same shelf with its current selection. Open **Workspace** to load two saved snapshots, select either card, and move, resize or zoom it.

| Default shortcut | Action |
| --- | --- |
| Ctrl+Alt+G | Capture region |
| Ctrl+Alt+P | Toggle Pocket shelf |
| Ctrl+Alt+Space | Pause Ghost |
| Ctrl+Alt+W | Open Workspace |

Change or disable bindings in **Settings**. If Windows reports a conflict, use the main window or tray while choosing a new shortcut.

## Key files

- `AGENTS.md` — permanent project instructions.
- `docs/MASTER_SPEC.md` and `docs/IMPLEMENTATION_RFC.md` — product and implementation contracts.
- `docs/QA_MATRIX.md` and `docs/TEST_RESULTS.md` — verification status.
- `design/DESIGN_TOKENS.json` — visual design tokens.
- `VERIFICATION.md` — verified and unverified status.
