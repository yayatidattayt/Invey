# Codex repository rules — POCKET × DUAL × GHOST v2

Execution18 adds opt-in local supervised mapping personalization, preview only. Read docs/EXECUTION_18_REPORT.md and docs/PERSONAL_LEARNING_GUIDE.md. Preserve every existing feature and gesture safety reducer. No B2 or ordinary-desktop automated input. The user explicitly authorized an Execution18 amendment carrying forward all five existing camera charges, at most three additional user-enabled sessions≤180s, with the original12-session/1,800-second caps unchanged. The guardian ledger is artifacts/execution-16/camera-budget.json; never reset it. Camera activation remains a deliberate user action. Synthetic/WPF learning improvement is not physical accuracy evidence.

Execution17 authorizes camera-free demo stabilization, explicit isolated demo data and bounded isolated-desktop verification. Do not reuse Execution16 camera-test permission. No ordinary-desktop input injection, remote-session disruption or automatic PR merge. Read the current handoff and docs/EXECUTION_17_DEMO_REPORT.md. A foreground-ownership guard has blocked native remote rehearsal; do not weaken it or modify the system input service. Supplemental WPF control-route checks must never be described as native input acceptance.


Execution 10 names the public product Invey while preserving Pocket, Dual, and Ghost and the existing Windows identities. F07 starts with a provisional, landmark-only calibration preview. The single authorized ASUS FHD test was specific to this execution. Gate A, M02 and full B1 acceptance remain separate.

Execution 09 authorized bounded, visible testing of the selected laptop webcam. ASUS FHD capture, inference, Stop and same-device reacquisition were verified. Hardware authorization was specific to this execution. Gate A, M02 and release acceptance remain separate. See `docs/EXECUTION_09_REPORT.md`.

Execution 08 explicitly authorizes provisional F06 camera code, UI, and controlled verification before the canonical reviews. Gate A, M02 review, device consent and release evidence remain mandatory for their respective decisions. A physical camera may open only on deliberate user Enable or separate test-specific authorization. See the leading handoff and `docs/EXECUTION_08_REPORT.md`.

Execution 07 clarified the M02 optional ordinary-app UIA deferral and the prerequisite A1 review. The recorded lab now shares a frame pipeline and separates missing fresh results from no-hand inference. Live adapter integration remains behind the canonical Gate A and M02 disposition. See the leading handoff and `docs/EXECUTION_07_REPORT.md`.

Execution 06 added the camera-free Tracking lab to the existing app. It uses real local inference and the F06 observation lifecycle under controlled input. Gate A and M02 disposition still gate the webcam adapter. See the leading handoff and `docs/EXECUTION_06_REPORT.md`.

Execution 05 authorized independent B1 observation foundations and camera-free model qualification while the canonical webcam gate remains open. See `codex/HANDOFF.md` and `docs/EXECUTION_05_REPORT.md` for the tested scope and prerequisites for a live camera adapter.

Execution 04 authorizes controlled A2 mutation recovery and B1 implementation only after its eligibility conditions are met. A2 recovery has controlled evidence. A1 physical acceptance and the A09 ordinary-app disposition remain open. The B1 dependency audit and current boundary are in `codex/HANDOFF.md` and `docs/EXECUTION_04_REPORT.md`.

Execution03 (2026-11-05) explicitly authorizes A2 reliability hardening, controlled adverse-provider processes, target-scoped UIA, longer injected-input profiles and regression evidence. Current boundary is A2 REVIEW, not B1. Full A1 physical acceptance stays BLOCKED; A2 remains PROVISIONAL. Read the leading Execution03 handoff and docs/EXECUTION_03_REPORT.md; older stop/preparation-only entries are historical. Do not merge or ship provisional external control.


Execution02 automation directive (2026-11-05) authorizes a separate isolated-desktop acceptance runner with explicitly INJECTED_GUI_TEST input, and eligible isolated A2-PROVISIONAL engineering after automated readiness. This overrides older instructions to stop all work solely for absent human evidence, but never changes full A1 physical acceptance to PASS. GHOST product code still must not inject native input. Harness injection stays in tests/Pocket.Acceptance.Runner and refuses the normal desktop. No B1/camera work.

Latest execution2026-11-05: user resumed M00/M01. Local SDK/build/core tests/native smoke verified; full A1 physical acceptance BLOCKED. Read leading current section in codex/HANDOFF.md before older historical instructions. Use scripts/dotnet.ps1 -Arguments arrays. Stop at A1 review.

Read `docs/research.md` (living technical research and risk register), `docs/MASTER_SPEC.md`, `docs/IMPLEMENTATION_RFC.md`, `docs/DESIGN_SYSTEM.md` and `docs/QA_MATRIX.md` for the active specification. Any old `archive/v1/` material is historical and intentionally excluded from this ZIP; current v2 docs are authoritative.

## Non-negotiable invariants
- The native Windows physical mouse must always remain OS-owned and unaffected by GHOST. Do not teleport it (`SetCursorPos`), hijack its drag state, or use global `SendInput` mouse movement/click injection as a GHOST fallback.
- GHOST is an independent logical pointer/object, not a second globally native Windows mouse. Full simultaneous interactions are guaranteed only within POCKET-owned controls if demonstrated. External Windows UIA support is an opt-in, per-target capability, not an implicit promise.
- Always develop a **Windows 11 native application** first: WPF/.NET + documented Win32 integration. No finished website pretending to be the app.
- Camera is opt-in and processed locally. Do not upload or persist frames. Capture only on explicit user action; store only confirmed cropped screenshots.
- No cloud API, paid inference, administrator bypass, kernel driver, code injection or protected-content workaround.
- External UIA must not run on UI thread. Blocking third-party providers require process isolation and restartable failure recovery.
- All user-facing features must actually respond to user actions, with real error/disabled states; no static-looking fake buttons or decorative hover reactions that claim success.
- A phase is NOT finished merely because its code compiles. Run unit/integration tests and document Windows manual evidence. Never fabricate PASS or performance data.
- UI has to follow "Quiet Instrument": precise typography, restrained neutral palette, a single muted accent, 8px grid, no AI-looking gradients/orbs/oversized glass cards, compositor-friendly smooth motion and reduced-motion support.
- Gesture actions must be deliberate and recover from tracking loss. Don't require an open-palm hold and a pinch at the same time; Interactive Peek latches temporarily.
- Every feature must have normal mouse/keyboard fallback. Webcam OFF is a valid normal usage mode.
- If a Windows API limitation is discovered, log it, provide a safe fallback, and mark feature experimental/deferred rather than silently replacing it with an incorrect workaround.

## Development workflow
- Work one `docs/CODEX_PHASES.md` phase at a time, in order.
- Before code, inspect installed Windows/.NET/Python versions and official docs for APIs touched in the phase.
- Keep Pure Core, Windows interop, WPF visuals, UIA worker, tracker sidecar and persistence as separate modules.
- Prefer deterministic repeatable tests with synthetic landmark samples before live webcam tests.
- Profile perf/frame pacing on target laptop; all FPS/latency budgets are targets until measured.
- Update `docs/research.md` with **dated new findings and confirmed test outcomes**, and update `docs/DECISIONS.md`, `docs/COMPATIBILITY_MATRIX.md` and `docs/TEST_RESULTS.md` with true evidence as work progresses. Keep unresolved limits explicit; do not overwrite failed experiments.
- Default to clear functional software over feature bloat. Respect `SHIPPED/EXPERIMENTAL/DEFERRED/UNSUPPORTED` labels.

## First Codex session
Begin with `CODEX_START_PROMPT.md` after unzipping the project folder. `docs/research.md` is canonical; after edits, run `scripts/Sync-Research.ps1` to refresh root `research.md`. Do not mark a gate passed without evidence in `docs/TEST_RESULTS.md`.

## Current resume procedure — 2026-11-04
Read `codex/HANDOFF.md`, `docs/SPECIFICATION_REVIEW.md`, `docs/TRACEABILITY.md` and `codex/MASTER-IMPLEMENTATION-PROMPT.md` for the latest source audit, conflict resolutions and standalone execution contract. The latest task generated specifications only; it did not authorize application-code changes. A1 remains unpassed; restore the approved .NET 10 target during future implementation and verify the actual nested solution directory. Read evidence/decisions, inspect scoped changes, validate current state, then do the next unblocked current-gate task. Stop at A1 review before webcam/UIA work. Preserve original docs/history; specification gate ratings are separate from native implementation results.
