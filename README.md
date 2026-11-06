# Invey

**invey.** (pronounced **in-vay**) is a Windows application bringing together three focused subsystems:

- **Pocket** — a quiet local workspace for useful screen references.
- **Dual** — an interaction model that keeps the ordinary physical mouse independent from the logical hand-controlled channel.
- **Ghost** — local hand tracking using MediaPipe with an explicit recorded or live camera source.

GHOST was the former product name; the public product name is now Invey. Open **Tracking lab** to select a source, explicitly enable tracking, and inspect the provisional calibration preview.

## Current status — Execution 10

The ASUS FHD laptop webcam has delivered real frames through Tracking lab; explicit Enable, Stop, and same-device Restart were verified. The latest visible calibration check verified live acquisition and status transitions, but did not reach a Ready calibration state. Calibration remains provisional: display mapping, numeric preferences, rectangle routing, multi-hand behavior, and Move/Pinch/Release practice are not yet accepted.

The isolated acceptance runner and Debug/Release builds pass their current deterministic checks. A1 physical acceptance remains blocked; A2 is fixture-only and provisional, with no general external control; B1 and full F07 acceptance remain open. This is a development snapshot, not release-ready.

See the [Execution 10 report](docs/EXECUTION_10_REPORT.md), [hardware evidence](docs/EXECUTION_09_REPORT.md), [automated acceptance evidence](docs/AUTOMATED_ACCEPTANCE.md), and [current handoff](codex/HANDOFF.md).

## Build and development

1. Install the pinned .NET SDK described by `global.json`.
2. Run `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Run-AutomatedAcceptance.ps1 -Build` for deterministic acceptance checks.
3. Run `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dotnet.ps1 -Arguments @('build','Pocket.Dual.Ghost.sln','-c','Debug')` for a normal build.
4. Launch the app from `src/Pocket.App` and open **Tracking lab**. The camera is off at startup and requires explicit selection and enablement.

The source includes the Windows application, local tracking and automation workers, tests, design references, architecture documents, and engineering reports. It does not require a cloud service or paid AI API, and camera frames are not uploaded.

## Key files

- `AGENTS.md` — permanent project instructions.
- `docs/MASTER_SPEC.md` and `docs/IMPLEMENTATION_RFC.md` — product and implementation contracts.
- `docs/QA_MATRIX.md` and `docs/TEST_RESULTS.md` — verification status.
- `design/DESIGN_TOKENS.json` — visual design tokens.
- `VERIFICATION.md` — verified and unverified status.
