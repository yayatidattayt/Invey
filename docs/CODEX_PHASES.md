# CODEX Phase Pack v2 — Principal engineering instructions
Use with a capable coding model in High reasoning. This pack is designed for Codex **on a real Windows 11 x64 machine**. Do not paste every prompt simultaneously. Each phase ends with observable evidence, not a prose claim.

## Quick start
For direct implementation (rather than prompt generation), open `CODEX_START_PROMPT.md` from the repository root. It runs Gate A1 only. The meta-prompt below is optional and is for users who specifically want Codex to generate further prompts.

## MASTER META-PROMPT — generate the best task-specific coding prompts
```text
Act as principal Windows desktop engineer, UI designer and skeptical release manager for POCKET × DUAL × GHOST v2. Read AGENTS.md and docs/research.md, docs/MASTER_SPEC.md, docs/IMPLEMENTATION_RFC.md, docs/DESIGN_SYSTEM.md and docs/QA_MATRIX.md. Treat these as requirements. The archive/v1 content is obsolete.

My original product vision is nonnegotiable: the physical Windows mouse and a separately webcam-hand-controlled on-screen GHOST must operate simultaneously. GHOST never teleports or steals the native mouse. The actual product is POCKET, a beautiful hidden desktop shelf for captures and references, recalled through intentional hand gestures. External app actions are strictly capability-limited, not universal native cursor replacement.

Your assignment in this session is to PLAN AND GENERATE implementation prompts, not write the entire application yet. First inspect the actual Windows environment, SDKs and repository. Produce:
1) a requirements traceability matrix with feature IDs and test IDs;
2) a risk-first dependency graph, calling out OS overlay hit-testing and focus, UIA self-overlay target resolution, worker hangs, DPI, capture pipeline, webcam packaging, gesture loss and frame pacing;
3) a concise ADR/decision log comparing viable Windows UI stack choices and explaining why the recommended choice wins; do not over-engineer;
4) 6-10 incremental Codex prompts, each scoped to a demonstrable build artifact, files to edit, API assumptions to verify, exact commands, automated tests, manual Windows tests and stop conditions;
5) design implementation mapping from DESIGN_SYSTEM.md with accessible controls and measured animation performance;
6) a safe fallback for each uncertain Windows capability; and
7) a delivery checklist distinguishing implemented, tested, experimental and deferred features.

Plan the Windows feasibility experiment first (simulated hand, actual overlay and external UIA fixture). Do not add hand tracking or decorative UI before a real pass-through/focus/drag proof. Always separate P0/p0 risks from polish. All delivered artifacts must be offline-capable and require no paid APIs, admin bypass, unsigned kernel drivers or private Windows hooks. If Windows cannot be run in your environment, state tests NOT RUN and give exact instructions; don't fabricate results.

Return ready-to-paste prompts in execution order, and identify which ones require me to perform physical hand/camera tests. Ask no unnecessary clarification; make conservative, explicit assumptions.
```

## PHASE 0 — Read-only environment audit + scaffold plan
```text
Read all active v2 docs **including docs/research.md**; ignore archive/v1. Inspect Windows version/build, architecture, installed dotnet SDKs, Python version, available cameras (metadata only with opt-in), and repo structure. Read current official Windows documentation for layered windows, WPF/Win32, UIA COM MTA, and MediaPipe. Produce an ADR with candidate WPF + Win32 overlay / WinUI3 + composition; prioritize easiest proven Windows interoperability, not visual novelty. Identify licensing and packaging constraints.

Do not execute camera or screen capture. Do not yet implement the full app. Return a minimal solution structure, CLI build/test plan, unresolved research risks, feature scope baseline and Gate A acceptance test procedure. Mark not tested where appropriate. Update the included docs/DECISIONS.md and docs/COMPATIBILITY_MATRIX.md with evidence placeholders appropriate to the actual environment; preserve the original baseline. Do not claim native multicursor compatibility.
```

## PHASE A1 — Overlay, native-mouse pass-through and GHOST simulation
```text
Implement the smallest .NET 10 x64 Windows WPF app with:
- single instance and exit, simple normal Settings window;
- one optionally visible non-activating layered topmost GHOST overlay HWND on primary monitor; verify WS_EX_LAYERED, WS_EX_TRANSPARENT, WS_EX_NOACTIVATE and toolwindow behavior using native code and actual Win32 styles;
- simulated GHOST coordinates driven by a small developer controller or keyboard, with distinct on-screen visual in the Quiet Instrument style;
- normal physical mouse must pass through even *visible* GHOST / Pocket visual shapes, with foreground keyboard focus remaining in underlying test app;
- native cursor position logging to assert zero ghost-induced movement;
- a tiny Pocket panel controlled by simulated GHOST through direct internal hit-testing, with one real select/move operation;
- no Python, camera, screenshot or UIA yet.

Build real desktop binaries, no browser imitation. Add tests for pure pointer ownership; provide manual Windows verification A01-A05/A10 with reproducible click/drag/type tests. Use event log of GHOST actions, not OS SendInput. Stop at any P0 pass-through/focus failure and fix it before further phases. Record PASS/FAIL/NOT RUN with evidence.
```

## PHASE A2 — Independent UI Automation worker and real compatibility fixture
```text
Create Pocket.Automation.Worker.exe in a separate process, COM MTA, no UI; robust bounded IPC; watchdog/cancel by process kill on hang. Build a deterministic test application exposing Invoke, Toggle, Scroll and RangeValue controls. Implement a throttled target-probe method at physical screen coordinate; test whether our topmost layered overlay masks underlying UIA elements. If it does, investigate reliable limited target-resolution strategies without stealing mouse focus or using synthetic global input; if unresolved, return Unsupported.

Implement only explicit action allowlist with user deliberate activation, no password handling, delete/purchase/destructive controls, typing or generic drag. Test A06-A09 using own fixture and at least one real app (Notepad/Explorer/Edge); record focus and mouse-drag continuity distinctly. Unexpected external focus change is a compatibility outcome, not a hidden success. Never block WPF UI thread on UIA. Do not broaden beyond this research gate.
```

## PHASE B1 — Webcam sidecar, observability, calibration
```text
Add an isolated Python MediaPipe Tasks tracker. Pin compatible versions after checking the current official Python and MediaPipe docs and licensing of model assets. Use LIVE_STREAM inference or VIDEO mode depending on actual measurement. Camera frames stay local and never traverse IPC. Sidecar outputs validated bounded observations with seq/monotonic timestamps, plus health and shutdown messages. Include a simulator fixture for CI.

C# host starts/stops sidecar on explicit camera opt-in, reads IPC asynchronously, supervises process/heartbeat, logs only health metrics, and shows camera-off/tracking/lost states. Add calibration for selected monitor and mirror toggle; ensure correct physical screen coordinates at 100% and 125% DPI. Track GHOST visual smoothly without touching native cursor. Tests B01-B03,B08-B09. Stop at leaks/crashes/permission errors.
```

## PHASE B2 — Gesture FSM + owned interactions
```text
Implement timestamp-aware pinch enter/exit hysteresis, press and drag separation, edge-zone inward pull, armed/clutch/pause/lost states, One Euro position smoothing, stale-frame rejection, single-hand lock, zero accidental state resurrection after loss. Keep all interaction state in pure C# testable reducer; the tracker emits observations only. Implement GHOST grab/drag/release of Pocket target and distinct mouse-owned target in Workspace, with no cross-release.

Critical UX fix: a continuous open-palm hold cannot be required to keep Pocket visible while the same hand pinches. Implement Interactive Peek as a latched session after completed pull, closes only on explicit reverse gesture or ≥12s idle (configurable); separate passive glance mode optional. For scrolling v1, provide grab-to-scroll rail and explicit page controls, not unverified two-finger magic.

Write exhaustive unit tests for ownership/timeout/reacquisition; run B04-B12 on physical hardware. Default hand input paused on camera start; accidental gestures do nothing until armed. Log observed performance.
```

## PHASE C — Snapshot capture and full Pocket MVP
```text
Implement complete screenshot-based end-user flow: user action -> hide overlay -> capture selected monitor into RAM -> show frozen snapshot selection window accepting physical mouse -> rectangular crop -> preview/confirm -> atomic local PNG+manifest -> dispose uncropped buffer -> return to previous workspace when feasible -> shelf card. Esc cancels without storage. Only Snapshot is SHIPPED; no fake live/video/AI image processing.

Implement hidden shelf, edge tab, clean card thumbnails, rename, reorder, pin/unpin, resize, scroll inside Pocket, delete with undo, persistence/restart, on-screen bounds checks, keyboard/mouse fallback, normal Workspace with genuinely simultaneous different-target mouse+GHOST drags, settings and privacy controls. Preserve pass-through in Passive overlay. Use DESIGN_SYSTEM.md for exact geometry and visual tone; priority is functional correctness first then polish. Run Gate C complete matrix and fix failures before closing phase.
```

## PHASE D1 — Visual polish, smooth animation and latency engineering
```text
Use the supplied design/UX_CONCEPT.html only as visual direction; it is a non-native demo and must not be mistaken for product implementation. Implement Quiet Instrument visual system in WPF: muted ink/off-white/moss palette, native Segoe UI Variable typography, careful 8px grid, minimal surface hierarchy, compact floating shelf, recognizably small GHOST ring, contextual action states, high-contrast and reduced-motion alternatives.

Animate opacity/render transforms, not layout-heavy Width/Height on every frame. Build a developer-only perf panel with tracking Hz, render FPS, p95 frame time, end-to-end latency and dropped samples. Measure A/B: empty overlay, shelf closed, shelf animating, 3 snapshots visible, ghost dragging; collect results on actual target hardware. If WPF layered overlay frame pacing fails, propose a targeted Win32/DirectComposition overlay renderer ADR and benchmark a small spike; do not rewrite entire product speculatively.

Tests D01-D06; capture actual Windows screenshots with approved dummy content; ensure every interactive button has hover/pressed/disabled and keyboard focus states, no fake controls. Do not call it 60fps without measurements.
```

## PHASE D2 — Packaging, privacy/security and release readiness
```text
Produce portable Windows 11 x64 package with .NET runtime and tracker sidecar/model asset bundled and licensed appropriately. Clean machine with no Python installed must run. No admin or cloud account. Verify camera OFF by default, privacy indicator, no stored frames, no outgoing network from core app, local data deletion, safe error recovery, corrupt manifest recovery, reliable exit, missed webcam permissions, hotkey collisions. Audit binaries/licenses, privacy logging and unsigned installer warnings. Run D07-D11 and all remaining release blockers. Update README with installation, shortcuts, known limitations and troubleshooting. Do not mark tests passed unless observed.
```

## PHASE E — One optional adapter at a time
```text
After first release stable, choose exactly ONE extension from: source live preview; local/embeddable video; Chrome DOM adapter with explicit permissions; extra validated UIA patterns; multi-monitor mapping. Create an RFC with official APIs, limitations, security/legal constraints, user benefit, measured performance, implementation/testing plan, and feature flag. Only implement if it adds measurable value without violating no-native-cursor-stealing invariant. Never assert that snapshot and live crops are the same thing.
```

## REVIEW — Independent skeptical release review after each phase
```text
Be a skeptical tester, not a supportive narrator. Compare current code with docs/MASTER_SPEC.md, docs/IMPLEMENTATION_RFC.md, docs/QA_MATRIX.md, docs/DESIGN_SYSTEM.md. Run build and tests where possible; on Windows, perform actual UI verification where authorized. Produce a precise table: test ID, expected, actual, evidence, PASS/FAIL/NOT RUN, remediation owner. Identify any fake second mouse, `SetCursorPos`/SendInput misuse, UIA from UI thread, blocking COM call without process isolation, focus-stealing overlay, frame processing on UI thread, inaccessible UI, leftover full-screen screenshot, unlicensed model weight, incomplete pocket control, cosmetic-only 'feature'. Fix P0 issues before declaring done. Record measured latency and frame pacing; do not invent benchmarks. Summarize first unresolved release blocker.
```

## Prompt discipline
1. Give Codex a phase and test IDs, not a vague request to "make the whole app".
2. At phase end, run REVIEW; only proceed after critical blockers pass or are explicitly deferred with feature excluded from the shipped set.
3. An ADR change requires updating MASTER_SPEC/IMPLEMENTATION_RFC/AGENTS as appropriate, plus tests.
4. The final release must be a **Windows native binary**, not a browser mockup. UI preview in `/design` is illustrative.
5. High thinking effort is useful for Windows interop and architecture; don't spend expensive reasoning on routine styling refactors unnecessarily.
