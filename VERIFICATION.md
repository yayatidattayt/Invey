# Invey verification

Execution 13 adds camera-off Pocket capture, local crop persistence and retrieval. The synthetic native capture harness exercises actual pixels in an isolated desktop. CI also tests Media Foundation reading and reopening a generated, non-private video fixture. [Execution 13 report](docs/EXECUTION_13_REPORT.md)

Execution 10 established the published Windows baseline. It recorded 54 core passes per configuration, 14 worker passes, and live acquisition and Stop on the laptop webcam. The live calibration session ended without observing Ready. [Execution 10 report](docs/EXECUTION_10_REPORT.md)

Execution 11 covers selected-display calibration, numeric preferences, practice and Windows CI. The combined local run passed 29 cases with Explorer blocked. Hosted Windows verification also passed for the implementation commit. Physical input, Explorer, F06 device loss and denial, physical F07 routes and full B1 remain open. [Execution 11 report](docs/EXECUTION_11_REPORT.md) · [Hosted verification](https://github.com/yayatidattayt/Invey/actions/runs/42174465421)

## Where evidence lives

- Public source and Markdown reports are included in the repository.
- Inline paths labeled **local only** refer to ignored files under `artifacts/` in the original workspace. The reports record the historical claims and their limitations.
- GitHub Actions publishes non-sensitive core TRX results with each workflow run when available. Hosted checks cover source builds and deterministic tests. Native recorded inference and file-reader fixtures run in the local isolated harness, with physical device and shell checks classified separately.

## Historical project-kit checkpoints

## Latest autonomous Execution02 —2026-11-05

Both configurations build cleanly;20 tests/config pass. Real Windows private-desktop injected acceptance runs now exercise100 clicks,100 drags,20 typing cycles and20 held-drag trials, plus no-injection three-scenario cursor controls. Final executable cases pass; shell/other scales/physical provenance remain gaps. UIA4-pattern benign fixture tests and watchdog test double pass provisionally. Full A1 BLOCKED, full A2 NOT ACCEPTED, no B1. See docs/AUTOMATED_ACCEPTANCE.md; histories preserved.

## Latest — Execution 01.5, 2026-11-05

Independent baseline reproduced; final Debug/Release zero-warning builds and17/17 tests pass per configuration. Native20-cycle run:900 observations; foreground fixture/styles/move/release/cleanup/stop-restart/single-instance checks pass, native cursor changed so status INCONCLUSIVE. A1 BLOCKED pending human physical trials. See docs/A1_PHYSICAL_ACCEPTANCE_RESULTS.md. Earlier observed pass remains historical; no OS input injection or later-gate implementation.

## Latest — 2026-11-05

Windows Debug/Release builds PASS, zero warnings/errors;15 deterministic tests pass per configuration. Native smoke passes actual style bits, panel drag/release, sampled fixture foreground/cursor retention, second-instance exit and HWND cleanup at125%. Full Gate A1 BLOCKED pending counted physical mouse/typing/drag and remaining DPI/task-switching tests. See docs/TEST_RESULTS.md and docs/A1_NATIVE_CHECKLIST.md. Earlier Linux/uncompiled descriptions below are retained history.

## Verified in this build environment (Linux)
- Required Markdown, JSON, screenshot references, PowerShell and startup files are present.
- Both copies of the 28-risk research log are byte-identical.
- ZIP archive integrity is checked after packaging.
- Offline HTML UI mockup is bundled and its scripts can be syntax-checked using Node.js.
- `scripts/verify_kit.py` can validate file presence, JSON syntax, research synchronization, Markdown fenced blocks, and relative Markdown links.

## Previously reported mockup checks (not rerun as Windows app tests)
- Browser-based mockup was reported to load at multiple desktop resolutions and basic interactions were exercised. Those are browser-only visual tests, not native Windows runtime evidence.

## NOT RUN: Windows native capability
- No WPF/.NET application compiled or executed in this Linux environment.
- Overlay mouse passthrough, foreground focus, active mouse-drag retention, UI Automation provider resolution, webcam hand tracking, gestures and capture are **not verified**.
- Clean-machine packaging, 60fps animation, latency targets and compatibility with third-party apps are **not verified**.

**Developers:** Gate A1 must be run on an actual Windows 11 x64 machine. Record real results (including FAIL/BLOCKED) in `docs/TEST_RESULTS.md`. No shipped claims before gates pass.

## 2026-11-04 supplement
The earlier Linux kit history is preserved above. A Windows source scaffold now exists but remains uncompiled/unverified. Windows x64 dotnet inspection still reports no SDK. The latest stage created a research/specification/directive/diagram package only; see `docs/SPECIFICATION_REVIEW.md` for scope and actual documentation validation. A1 source defects and net8 drift are recorded; no native PASS, camera test or packaging test follows from this specification work.
