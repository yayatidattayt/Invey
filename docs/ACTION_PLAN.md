# Product action plan — what to tell Codex to do, in what order
**Recommended mode:** High reasoning for OS-input and COM isolation tasks. All steps should be executed on a real Windows 11 computer. One phase at a time, with QA evidence and no untested feature claims.

## Before first code
- Extract the v2.1 ZIP and open the extracted `POCKET_DUAL_GHOST_V2` folder directly as the Codex workspace; its root `AGENTS.md` and active `docs/` are already in place.
- Read `START_HERE.md` and paste `CODEX_START_PROMPT.md` into Codex to start the scoped Gate A1 implementation. The optional MASTER META-PROMPT in `docs/CODEX_PHASES.md` is for generating additional prompts, not needed for a direct build.
- Have Codex detect actual installed Windows/.NET/Python capabilities rather than assuming packages/versions.
- Update the included `docs/TEST_RESULTS.md` with real evidence; its native Windows gates start as NOT RUN.

## Gate A: the hardest thing first
**A1, desktop:** build WPF/Win32 layered pass-through overlay, simulated GHOST, internal Pocket target, overlay never grabs real mouse. **A2, external controls:** separate COM MTA UIA worker and deterministic accessible test app; verify overlay does not intercept automation hit target or fail gracefully. No webcam before this.

Success requires a video/log showing one physical mouse drag continuing while simulated GHOST manipulates Pocket and keyboard focus remaining in underlying window, plus independent UIA capability report with observed app compatibility.

## Gate B: real hand
Integrate local MediaPipe sidecar with explicit opt-in, monitor calibration and safe state machine. Require real pinch/drag/release, tracking-loss cancellation, no cursor teleportation and basic performance measurements.

## Gate C: actual consumer utility
Implement Snapshot capture and the hidden shelf fully; use native screenshot pipeline, local save/load, pinch-operated panels, pin/unpin, reorder, scroll, rename, delete, keyboard fallback, workspace dual interactions, empty/error states. Remove unsupported feature buttons from shipping UI.

## Gate D: the high-quality product
Apply design system to Settings, shelf, cards, Ghost; use polished motion with reduced-motion support. Measure frame pacing, load times, camera latency. Package offline as a portable app or installer with licensed assets; validate in a clean Windows user session and fault-test tracker/UIA process crashes.

## Optional Gate E
One at a time: live crop, supported video, browser adapter, broader UIA actions, multi-monitor. Each has its own RFC, compatibility/permission tests and feature flag.

## Suggested sequence over the development window (not delivery guarantees)
| Engineering period | Focus | Ship gate |
|---|---|---|
| Early | A1 click-through/focus/dual input, A2 external UIA spike | Decide whether external control is real or disabled |
| Next | B1 tracker supervision, B2 gesture calibration/stability | No accidental cursor movement or ghost drag lock |
| Middle | C Snapshot / shelf / persistence / shortcuts | First genuinely useful offline utility |
| Later | D visuals, animations, performance, installer and privacy | Stable demo and user-tested product |
| Remaining capacity | E a single best-value extension after evidence | No unvalidated feature avalanche |

## Stop/go criteria
- **Stop** if true native mouse pass-through isn't shown on Windows, not merely mocked.
- **Stop** if tracker can leave stuck interactions or grab focus.
- **Stop** if a picture-only mock is being presented as real application behavior.
- **Go** when Gate A proves independence, Gate B proves gesture control and Gate C proves the full capture→store→recall user journey.

## Final user-facing demonstration script
1. Open Notepad/test editor and POCKET on a Windows 11 laptop.
2. Create Snapshot Pocket from a known test diagram using explicit shortcut.
3. Return to typing / select text with real mouse; do not move native cursor via GHOST.
4. Activate hand tracking, move GHOST while still using mouse, reveal shelf by deliberate edge pull.
5. Pin/drag/scroll snapshot with GHOST, hide shelf; verify the editor continues working.
6. In Pocket Workspace, manipulate two separate cards simultaneously using mouse + GHOST.
7. Turn webcam off and repeat capture/reveal with keyboard shortcuts.
8. Optional: exercise tested UIA `Invoke` on a benign test button and show honest unsupported state in a noncompatible app.

If the product can't convincingly do steps 2–7, it isn't ready for the first public demo even if the interface looks beautiful.
