# A1 acceptance record — Execution 01.5

## Autonomous Execution02 update —2026-11-05

Original full A1 remains BLOCKED. New automated/native evidence exists; earlier “no progress/new exports” entries below are history. Executable injected cases now pass on an isolated desktop; see docs/AUTOMATED_ACCEPTANCE.md and artifacts/automated-acceptance/20261104T202754048-1b98c61d379f495cacb5c733e79e906b/results.json. Physical(H)/device-origin criteria below remain unpassed; injected(N/software) results never replace their provenance.

Actual100 clicks,100 drag starts/ends,560-char exact text over20 cycles and20 continuous native drag trials all pass. Three no-injection cursor controls each120 samples show no change. Native stop/restart and overlay-close timer/claim cleanup pass. Shell Alt+Tab/tray and other DPI setups remain unavailable/unattempted in safe isolation. No normal-desktop injection; no display changes. Final automation permits specifically authorized fixture-only provisional A2 while preserving full A1/release gates. No repeated human checklist is requested to continue independent engineering.

Intermittent injected typing mismatch preserved in run20261104T202546669-31edc4ed07c5494f96445395be3cd92c. Per-chunk digest acknowledgment strengthened; final20261104T202754048-1b98c61d379f495cacb5c733e79e906b passed. This does not prove the original mismatch was caused by GHOST or establish a long-soak stability claim.

## Execution 02 evidence review — 2026-11-05

Decision: **NO-GO / Gate A1 BLOCKED**. Execution02 supplies conditional A2 authorization, but its mandatory A1 prerequisite is not satisfied. No A2 implementation began.

Inspected the current checklist/matrix/handoff, project JSON evidence inventory and existing Debug/Release regression TRX records. No new user physical observations or physical-result exports were present in the supplied request or inspected project evidence. Existing TRX records contain17 passed/0 failed in each configuration; these records were inspected, not tests rerun. No builds or native replay repeated because they cannot supply missing human evidence.

The latest native file remains artifacts/a1-native/20261104T193854053-0b5a195da0b64c1b92166e8b8dbb3394/native-smoke.json:900 observations, EvidenceClass native software/no human trials, Status INCONCLUSIVE, focusRetained=true, cursorUnchanged=false, overlay activation=false. PhysicalMouseTrials and AltTab are explicitly NOT RUN; DPI is current setup only. No controlled Ghost-inactive versus active stationary physical baseline is supplied, so cursor movement causation remains unresolved. Source scan matched no SetCursorPos/SendInput/mouse_event/hooks/Cursor.Position/PostMessage/SendMessage call sites; read-only cursor sampling exists. This narrows source concerns but cannot prove physical independence or attribute device noise.

Outstanding mandatory observations remain exactly the matrix below: counted physical clicks/drag starts (A02), exact-match typing over20 cycles (A03),20 continuous drags (A04), controlled stationary-cursor trial over>=100 Ghost movements (A05), physical Alt+Tab/tray/manual lifecycle (A01/A10), and human DPI/alignment/display-change observations for supported/available setups (A11). No user-reported failure was supplied to reproduce or repair. No product code, native styles, acceptance threshold or prior evidence changed.

Next action: run the guided session in A1_NATIVE_CHECKLIST.md and provide physical-device provenance, attempted/passed counts, display/DPI, exact-text-match yes/no (not typed content), observed focus/drag/lifecycle outcome and JSON filenames. Unavailable/unattempted cases must be identified. Review user results as user-reported H evidence alongside N exports, resolve defects if any, then reevaluate GO. Explicit approval/sign-off cannot substitute for missing mandatory evidence.

Date: 2026-11-05 (Asia/Calcutta). Verdict: **BLOCKED — human physical evidence outstanding**. This file maps required observations; canonical requirements remain A1_NATIVE_CHECKLIST.md, MASTER-IMPLEMENTATION-PROMPT §§15/25 and QA_MATRIX A01–A05/A10/A11.

## Environment and independently confirmed baseline

Windows NT10.0.26200 x64; project-local SDK10.0.403/runtime10.0.14; primary1920x1200 at120DPI/125%; app manifest PerMonitorV2/asInvoker (actual token integrity not measured). Configuration Debug and Release. Release Pocket.App.dll SHA256 after diagnostics repairs: F1462DA7B5B49EF8B791E8D33F9F82229C3459E0B17607E326AE56B2A0B3E7D7.

Restored and rebuilt both configurations before edits: zero warnings/errors; original15/15 deterministic tests passed each. After edits: zero-warning Debug/Release builds and17/17 tests passed each, zero failed/skipped. Evidence directory: artifacts/execution-01-5; baseline/regression-{Debug,Release}.binlog and tests/baseline-*.trx / regression-*.trx. Git discovery still reaches unrelated home-level repository with dubious ownership; no project-local Git history/trust mutation or discarded changes.

## Evidence classes

- D: automated deterministic (Core/unit tests).
- N: native software (actual HWND/foreground/cursor/fixture/replay instrumentation).
- H: human physical (person using real devices). No H evidence supplied in this execution; never infer it from WPF routed events or a scripted replay.
- UI controls deliberately opened by the person may receive focus normally. Passive overlay must not.

## Mandatory acceptance matrix

Every row uses the environment above unless a new setup is recorded. “Retest” below concerns the current execution, not the earlier historical records.

| Test / requirement | Method and expected result | Actual observation / evidence | Status | Defect / retest |
|---|---|---|---|---|
| A01 / F01 | N startup, tray initialization, second instance and shutdown; H visible tray/reopen/manual Exit | Native run starts controller/fixture, second process exit0, returns and cleans up. H tray/menu observation absent | PASS for N startup/second-instance; NOT RUN for full H lifecycle | N regression passed; H pending |
| A02 / F02 | H100/100 clicks and drag starts through visible opaque panel and ring reach underlying fixture; no claim corruption | Actual required style bits observed (N); no counted physical clicks/drag starts supplied | NOT RUN physical acceptance | No product interference defect demonstrated |
| A03 / F02,F03 | H typing while20 show/hide cycles; exact sentence retained and no unwanted foreground changes | N20 cycles/900 observations retained fixture foreground; H text trial absent | NOT RUN physical typing | N foreground check passed; H pending |
| A04 / F03 | H20 continuous native drags while internal Ghost moves panel, independent release | D independent claims pass, N panel move/release pass; no H drag trial | NOT RUN physical drag | Canceled/active drag counters now distinguish interruption |
| A05 / F02 | H mouse stationary over>=100 Ghost movements; N coordinates exactly unchanged | Current N900 observations: cursor changed; caller/device origin cannot be attributed; prior bounded run retained cursor | INCONCLUSIVE current N; NOT RUN controlled H | Preserve evidence; repeat with person explicitly keeping device stationary |
| A10 / F01,F02 | H Alt+Tab/taskbar excludes overlay; settings/controller focus normal; manual start/stop/reopen/Exit | N overlay never foreground, duplicate Start rejected, Stop released, restart completed, overlay HWND destroyed; H Alt+Tab/menu test absent | PASS for N invariants; NOT RUN full H task-switch/lifecycle | N passed; H pending |
| A11 / F02 | H alignment/hit tests at100/125/150% if available, display-change cancellation and recreate | N125% styles/geometry/replay observed; D coordinate round-trips at96/120/144/192DPI pass. No H scale/topology change | NOT RUN H matrix | Actual100/150 setups not yet attempted; no silent system setting changes |
| Ownership / F03, foundational F04 | D separate nonconflicting claims, occupied target rejection, opposite release/cancel, no late movement after release |17 tests per config, including2 new independent cancellation regressions | PASS D only | Full dual Workspace remains later, not certified |
| Simulator cleanup / F01,F03,F19 | N duplicate activation rejected, stop during press releases only Ghost, restart completes, dispose/close removes HWND | duplicateStartRejected/stopReleased/claimReleased/panelReturned/HandleDestroyed true | PASS N exercised paths | Human Exit/Alt+Tab and display-change paths still pending |

Mandatory H movement/click/typing/drag/simultaneous-activity observations remain outstanding. Optional mixed-monitor movement is DEFERRED under v1 single-primary-display scope; it is not a new A1 blocker. Unavailable required scale setups must be reported explicitly, not assumed unavailable or passed.

## Native evidence and repairs

Current unique native evidence: artifacts/a1-native/20261104T193854053-0b5a195da0b64c1b92166e8b8dbb3394/native-smoke.json. It contains900 replay observations over20 cycles and Status=INCONCLUSIVE: styles/movement/release/return/foreground retention/no overlay activation/second-instance exit/handle cleanup all true; cursorUnchanged=false. Exit0 means native invariant checks did not fail, NOT that physical acceptance passed. No automatic retry to seek a favorable result.

Historical artifacts/native-smoke-01.json (earlier inconclusive) and artifacts/native-smoke.json (earlier bounded pass) were inspected and left intact. New output uses UTC timestamp+GUID directories and has a local-offset timestamp inside. Artifacts are numeric evidence, no screen pixels, typed text, clipboard or application titles logged.

- A1-D01: fixed per-run overwrite risk by unique native evidence/error directories.
- A1-D02: fixed false-success diagnostic path: overlay becoming foreground now fails native invariants. External foreground/cursor changes produce INCONCLUSIVE instead of a focus/cursor passing claim. Physical field remains NOT RUN.
- A1-D03: fixture now counts canceled drags separately and reports active state; a completed-event count no longer obscures cancellation.
- ObservationReceived is a dispatcher-local diagnostic event used to wait for actual Press before testing stop/restart; no system hook, artificial delay, global monitoring or OS input synthesis.

No observed pass-through product defect can be diagnosed without H trials. Changes address evidence reliability; no Win32 input styles were randomly changed. Confirmed no SetCursorPos/SendInput/mouse_event/hooks in source. Foreground, pointer and logical claim state are separate; conventional UI activation remains intentional.

## Human checkpoint — provide these results

Follow A1_NATIVE_CHECKLIST.md. For each test return: ID, human physical device used, display/DPI, number attempted/succeeded, PASS/FAIL/INCONCLUSIVE, evidence JSON filename, and brief failure timing/steps (if any). Do not send personal typed text; just report whether the controlled sentence matched exactly. Each scale/config gets its own record.

| User-reported entry | Source | Result | Trials / setup | Evidence / defect / retest |
|---|---|---|---|---|
| None yet | No human observations received | NOT RUN | — | — |

A1 closure requires all mandatory criteria, plus the repository review/sign-off boundary; native smoke and unit tests cannot close it. A2 handoff is prepared in codex/A2_HANDOFF.md and gated on reviewed A1. A2 implementation has not begun.
