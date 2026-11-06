# POCKET × DUAL × GHOST — Codex Prompt Pack
**Use with the included `MASTER_SPEC.md`, `ROADMAP_AND_TESTS.md` and `AGENTS.md`.**

## Fastest path

1. Create an empty Git repository (`pocket-dual-ghost`).
2. Copy `MASTER_SPEC.md` to `docs/MASTER_SPEC.md`; `ROADMAP_AND_TESTS.md` to `docs/ROADMAP_AND_TESTS.md`; `AGENTS.md` to repository root.
3. Commit those files before implementation.
4. Select your available Codex coding model, e.g. GPT-6 Astra with High reasoning if available in your client. The model and reasoning setting are not required for the program at runtime.
5. Use **Prompt 0 (read-only preflight)**, then **Prompt A (native feasibility)**. Do not launch everything from a single enormous prompt.
6. Give Codex one phase at a time. Run and inspect each Windows demonstration before asking for the next phase.

---

## PROMPT GENERATOR — A prompt that produces Codex implementation prompts

Use this if you want another GPT-6 model to generate and improve the step-by-step prompts. Attach or paste `MASTER_SPEC.md`, `ROADMAP_AND_TESTS.md` and `AGENTS.md`.

```text
You are a principal Windows desktop engineer, a human-computer interaction designer, and a senior technical product manager. I need you to produce an implementation PROMPT SERIES for Codex, NOT write the application yet.

Read the attached canonical project specifications for POCKET × DUAL × GHOST and treat them as requirements. The user's original vision is non-negotiable: a physical Windows mouse and a simultaneously usable independent hand-controlled GHOST object. POCKET is a Windows app with hidden references/snapshots and gesture recall. GHOST is NOT a native second mouse. It must never commandeer or teleport the Windows system cursor. External Windows actions are via actual supported automation adapters, not universal synthetic mouse clicks.

Deliver the following, in order:
1. A contradictions-and-unknowns review, prioritizing the technical impossibilities and Windows focus/capture limitations. Do not invent guarantees. Explicitly distinguish actions fully controlled by our app from third-party UIA actions that may change focus or fail.
2. One recommended Windows-native, zero-paid-service stack. Prefer .NET 10/WPF + Python MediaPipe sidecar initially; justify any change with exact Windows interoperability and distribution reasons.
3. A dependency and licensing verification checklist, with official documentation targets and a plan to pin exact package/model versions after live verification.
4. A phase map with prerequisites, acceptance tests, stop conditions, and what source files each phase will touch.
5. Seven or fewer SEPARATE, copy-pasteable Codex implementation prompts, beginning with a read-only preflight and a Windows desktop feasibility spike before camera integration. Each phase prompt must contain: exact goal; concrete deliverables; non-goals; target modules; interface contracts; Windows test fixture requirements; build and test commands to run; expected reporting format; and objective acceptance criteria.
6. A persistent short AGENTS.md draft that doesn't bloat every Codex interaction, and a decision-log template.
7. A comprehensive manual compatibility matrix for the overlay, mouse focus, UIA controls, camera, capture and Pocket interactions on Windows 11.
8. A final release checklist covering no-admin install, privacy, model/license notices, restart/recovery, camera off, delete snapshots and clean computer testing.

Quality constraints:
- Preserve genuine independent input streams. Do not implement a fake second cursor by sending `SetCursorPos` / global `SendInput` movements or briefly moving the native cursor back and forth.
- Don't claim that an intelligent overlay object can universally click/drag arbitrary Windows software.
- Do not conflate UIA Invoke/Scroll/RangeValue with native mouse buttons; measure focus consequences.
- The test suite must include a controlled WPF target app and two-app simultaneous activity tests.
- The default product must be free to run locally with a normal laptop, webcam and mouse; don't add APIs, servers, paid services or mandatory custom drivers.
- Keep screenshot capture user-initiated; no sending frames or images to third-party services.
- Use real documentation rather than relying on guessed API details. Mark uncertain API behavior as an experiment.
- Never mark a phase complete unless Codex can demonstrate it in an appropriate Windows environment.
- Prefer code that compiles and tests to speculative architecture and ornamental UI.

The final output should be ready-to-paste individual prompts and a precise suggested execution order, not a broad essay and not a single mega-prompt telling Codex to build everything at once.
```

---

## PROMPT 0 — Read-only feasibility review in Codex

```text
We are building POCKET × DUAL × GHOST, a free Windows-native desktop application.

Read `AGENTS.md`, the relevant sections of `docs/MASTER_SPEC.md`, and `docs/ROADMAP_AND_TESTS.md`. Do not modify or create source code in this turn. Inspect installed Windows OS, .NET SDK and Python environment if tools allow; if not, list exactly what cannot be verified.

Produce:
1. A concise architecture recommendation: native .NET 10 WPF application, transparent non-activating click-through overlay, independent Ghost event engine, isolated Windows UIA worker, Python/MediaPipe camera sidecar later.
2. The three highest-risk assumptions, including own-overlay vs UIA FromPoint hit testing, no-focus click-through behavior, and external-app UIA actions during simultaneous physical-mouse drags.
3. Concrete repo structure and phase order, with Phase A as a platform feasibility proof using simulated coordinates and no webcam.
4. An initial compatibility matrix design and deterministic WPF test fixture.
5. A list of exact commands and tests you can run in the current machine; distinguish desktop tests that require a real Windows user session.
6. A scope fence: no browser, screen capture, camera, video, installer, cloud or visual polish in Phase A.

Do not claim that Windows supports universal second native cursors. Do not substitute native mouse movement, mouse hooks or pointer teleportation. Report any contradiction and decision needed before implementation.
```

---

## PROMPT A — Build Phase A, native desktop feasibility

```text
Implement Phase A of POCKET × DUAL × GHOST according to `AGENTS.md`, `docs/MASTER_SPEC.md` and the Phase A acceptance matrix in `docs/ROADMAP_AND_TESTS.md`.

GOAL: A real Windows WPF desktop proof that displays a non-activating, click-through green Ghost object movable independently via SIMULATED hand coordinates. The physical Windows mouse must remain fully normal and unchanged. Build a test fixture app exposing predictable UI Automation patterns. Probe and invoke at least one fixture action through a dedicated UIA worker, with explicit unsupported feedback.

SCOPE:
- Create a .NET 10 WPF host, transparent layered overlay and pure C# input core.
- Implement simulated Ghost movement with keyboard inputs in a dedicated test mode (avoid focus-stealing global shortcuts unless explicit/configured).
- Use Win32 extended-window-style interop for click-through and no-activation behavior; verify actual mouse pass-through. Separate overlay from normal Settings/test-control window.
- Create a separate controlled WPF test target window with Invoke button, Toggle checkbox, RangeValue slider and scrollable container. Log each action visibly.
- Implement a UIA probe worker OFF the main WPF UI thread. Exclude own overlay from semantic hit test or explicitly report the unresolved limitation; do not fake working hit-testing.
- Dispatch one user-initiated supported UIA action. Add timeout/exception isolation as technically feasible and describe remaining provider-hang risk.
- Build a smoke-test diagnostics view for Ghost coordinates, native GetCursorPos coordinates, active window handle, target capabilities and action result.
- Add unit tests for logical Ghost state, pointer ownership, pause/cancel and unsupported actions.

NON-GOALS: Webcam, MediaPipe, screenshot, Pocket shelf, live windows, YouTube, driver, admin installer, universal Windows drag, polished graphics.

BANS: SetCursorPos used for Ghost; global SendInput for a normal click fallback; DLL/kernel injection; unsupported UIA claims.

RUN: `dotnet --info`, `dotnet restore`, `dotnet build`, `dotnet test` and available Windows/manual tests. If you cannot run the desktop, say so explicitly.

REPORT: Files changed; exact commands; builds/tests; A1-A6 test matrix with observed results; compatibility blockers; revised architecture decision. DO NOT proceed to Phase B automatically.
```

---

## PROMPT B — MediaPipe sidecar and reliable GHOST motion

```text
Continue POCKET × DUAL × GHOST. Phase A has been reviewed. Read relevant `docs/MASTER_SPEC.md` sections 5-6 and Phase B in `docs/ROADMAP_AND_TESTS.md`. Preserve proven Windows overlay/automation behavior. Do not rearchitect without explaining why.

GOAL: Make GHOST follow one webcam-tracked hand while remaining completely independent of the native mouse.

BUILD:
- Python local MediaPipe HandLandmarker live-stream tracker with pinned working dependencies and a configurable model asset path. Verify model asset redistribution license separately.
- Child-process lifecycle managed by WPF; initial IPC: newline-delimited JSON sample records on stdout, JSON commands via stdin, bounded stderr handling. No camera frames transmitted to main process or remotely.
- C# parser and latest-sample reader with bound checks, timestamps and stale-frame rejection. Python sends landmarks; C# computes gestures for deterministic unit testing.
- Selected hand tracking/mirror option; calibrated one-monitor coordinate mapping, One Euro motion smoothing, normalized pinch detection with enter/exit thresholds and debounce, armed state, loss cancellation, clutch and hotkey pause.
- Visual state ring for disabled, armed, press/drag and lost.
- Simulated/replay tracker feeding same engine for repeatable tests.
- First-run webcam activation with clear local processing/privacy indicator; camera off by default.

NON-GOALS: Snapshot capture, live video Pocket, universal Windows clicks, second physical mouse drivers.

TEST: Camera off fallback; native cursor untouched; 20-trial pinch/drag exercise; safe cancellation after tracking loss and Python crash; log actual frame rate/latency; run builds and unit tests. Explain any unverified human-camera tests.

REPORT: implementation summary, exact test results, sample diagnostics, unresolved performance risks. Stop after Phase B.
```

---

## PROMPT C — Snapshot capture and persistent POCKET shelf

```text
Implement Phase C of POCKET × DUAL × GHOST after Phase B passes.

GOAL: The first useful consumer experience: select a rectangle on the visible desktop, save a cropped snapshot locally, tuck it into a hidden edge shelf, recall it via deliberate hand gesture, and manipulate the saved card with GHOST while the physical mouse remains active elsewhere.

BUILD:
- User-initiated capture shortcut; temporary selection overlay that intentionally accepts normal mouse clicks. Cancel with Escape. Make clear capture mode may take focus; this is separate from the focus-free Peek overlay.
- Hide GHOST/POCKET overlay during capture, crop image and discard full-frame buffer. Save PNG and versioned local JSON manifest atomically.
- Hidden shelf with compact edge tab; Pocket item cards; pin/unpin, hide/show, move, resize, delete and basic reference scrolling.
- Explicit input event routing: physical mouse direct to Windows except in normal interactive Workspace/capture window; GHOST direct to Pocket owned controls. Per-pointer ownership and cancel handling.
- Deliberate edge gesture with start-zone + dwell + movement direction; keyboard toggle fallback.
- Normal POCKET workspace permitting simultaneous mouse drag of item A and GHOST drag of item B.
- Camera disabled still allows capture, normal workspace and hotkey reveal.

NON-GOALS: Live window crops, arbitrary external UI control automation beyond existing proven adapter, YouTube, cloud sync, protected screen scraping.

TEST: persistence after restart, mouse input pass-through while Ghost manipulates overlay, keyboard focus retention while typing, safe cancel on lost hand, no full-screen image persisted when cropped only, independent dual drag in Workspace. Log results and failing behaviors.

Do not auto-progress to live content or polish. Give a working demonstration procedure.
```

---

## PROMPT D — Hardening, UX and native installer

```text
Implement Phase D after the snapshot Pocket user journey works end to end. Preserve current architecture and documented invariants.

GOAL: A stable, clean-install Windows desktop MVP with no paid services and a useful mouse-only fallback.

TASKS: Add tray controls/settings, quick calibration, camera selection, mirror and sensitivity, pointer size, hotkey conflict handling, reduced motion, first-run onboarding, pin/peek state recovery, error messages, atomic local storage, delete-all local data, tracker process restart, bounded and privacy-conscious logs, deterministic startup/shutdown, security review, model and dependency license audit, installer or portable distribution. Handle per-monitor DPI correctly for selected first monitor and explicitly defer unsupported multimonitor cases. Conduct manual user tasks and document results.

TEST: clean Windows 11 machine/VM; runtime included; no Python developer installation required for distribution; offline after models provided; no admin by default; camera off by default; ordinary mouse fully usable; tracker crash doesn't affect Windows; no remote uploads; test each acceptance result with observed evidence.

Report binary/package path when created, installation runbook, test log and open issues. Do not claim all Windows apps compatible.
```

---

## PROMPT E — Capability extension (only if core product works)

```text
Investigate ONE Phase E integration at a time; do not implement every option at once. Read related sections of `docs/MASTER_SPEC.md`.

Choose one of:
(A) DWM live source thumbnail/crop;
(B) supported official embedded video Pocket;
(C) explicit-permission Chrome extension native messaging adapter;
(D) extra UI Automation control patterns;
(E) second-monitor physical pixel/DPI mapping.

First provide a comparative engineering feasibility note: official API support, expected user value, compatibility and licensing concerns, security permissions, user consent, test fixture, fallback, objective acceptance criteria, and reasons to defer. Then implement only the selected adapter with a feature flag and explicit unavailable state. Keep `SetCursorPos`/global native mouse fallback prohibited. Add reproducible Windows tests and update compatibility matrix.
```

---

## PROMPT REVIEW — Ask Codex to verify a completed phase

```text
Perform a skeptical verification of the current POCKET × DUAL × GHOST phase without expanding scope. Inspect the code and the relevant acceptance tests. Build and run the test suite and, on Windows, run actual manual UI test steps if tooling permits. Pay special attention to the true native mouse coordinates, focus retention, hit-testing behind transparent overlay, UIA worker blocking, drag ownership, camera-loss cancellation, private screen/camera data handling, and unsupported app behavior.

Return a table: Test ID / Expected / Observed / Evidence / Pass-Fail-Not tested. Identify architectural workarounds that violate the requirements (especially pointer teleportation or fake duplicate cursor) and propose the smallest repair. Do not mark unverified tests as passing.
```

---

## Recommended prompt cadence

- Codex interaction 1: Prompt 0, **read-only plan**.
- Interaction 2: Prompt A, native feasibility + actual tests. Fix until A1-A6 reported truthfully.
- Interaction 3: Prompt B, hand tracking.
- Interaction 4: Prompt C, consumer utility.
- Interaction 5: Prompt D, hardening and installer.
- Interaction 6+: Prompt E only after real user tests, one adapter at a time.
- Between each: use PROMPT REVIEW and inspect the output yourself.

**Model strategy:** Use a high-capability reasoning model for Phase A native Windows interop and architecture tradeoffs; routine bug fixes and styling do not necessarily need the highest-cost mode. Your Codex login usage may be subject to plan allowance; no API subscription or token billing is needed by the finished app itself.
