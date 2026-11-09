# Windows test evidence log

Execution18 UI repair:125 core tests/configuration and20 workers pass. Real-WPF guided lessons and demo routes pass both seeds; budget preflight denies pending/exhausted tests without spawning a worker. Native tracking suite20261109T075027413-36306822786542fcbc1291b166103ef2 passes11 cases. Preserved nullable-build, queued-Cancel, keyboard-focus and intermittent safe migration-fallback failures are described in EXECUTION_18_REPORT.md. Separate cursor controls with physical motion remain INCONCLUSIVE. Missing fourth-session native release acknowledgement is preserved; explicit independent review charged the full reservation and granted no new session.

Execution18: baseline100 core/configuration; delivery121/121 Debug and Release,20/20 worker tests. Actual-WPF synthetic learning/new-process reload/rollback/reset and demo controls pass both seeds. Native standard run20261108T201144706-ac276dd29a4e402abfea11f0a4b71894:38 PASS, zero FAIL, Explorer BLOCKED. Physical target improvement remains NOT RUN. All earlier failures are preserved and described in [Execution18 report](EXECUTION_18_REPORT.md); a clean final run does not erase them.

Execution 11 passed 60 core tests per configuration, 14 local worker tests and eight targeted isolated cases. The latter cover recorded Ready and display mapping, camera-off settings reload across app processes, keyboard focus, and practice through release and ambiguity cancellation. Physical and Explorer cases remain separate. [Report](EXECUTION_11_REPORT.md)

Execution 10 verified the Invey name, existing single-instance behavior, camera accounting and provisional F07 calibration. Debug and Release passed 54 core tests each, and 14 worker tests passed. The first combined run exposed a synthetic typing mismatch. After the injector repair both seeds acknowledged all 20 chunks exactly. Their results were 28 PASS and 27 PASS with one blocked Explorer case each and a prelaunch cursor control inconclusive on the second. The authorized webcam run confirmed 247 conversions, 246 accepted results, one cancellation and clean exit. [Report](EXECUTION_10_REPORT.md) · First seed (`artifacts/automated-acceptance/20261106T054037373-b6b311ce355949f289adb1729be27892/results.json`, local only) · Second seed (`artifacts/automated-acceptance/20261106T054236093-f0c297e9822048e2a024b185800ecc9c/results.json`, local only)

Execution 09 verified real ASUS FHD acquisition, inference, Stop and same-device Restart in the visible app. Three bounded sessions converted 379 frames, accepted 378 observations and confirmed every worker exited. Both builds passed 51 core tests each and the worker passed 14 tests. The isolated regression had 27 passes, zero failures and one blocked Explorer shell case. The three cursor controls passed in their software scopes. [Execution 09 hardware report](EXECUTION_09_REPORT.md) · Final native evidence (`artifacts/automated-acceptance/20261105T190951613-7c4125fc0c0d4975af1bb96265e3d743/results.json`, local only)

Execution 08 locked restore and both builds passed. Each configuration passed 49 core tests and all 13 Python tests passed. A reversible vertical-flip fault failed both native and deterministic orientation tests. The final private-desktop run had 26 passes, zero failures, one blocked Explorer case and one inconclusive closed-app cursor baseline. Native file inference, permission feedback, recorded tracking and A2 late mutation passed. The normal desktop was restored with zero scoped workers. Camera-device validation remains blocked. Final results (`artifacts/automated-acceptance/20261105T173443873-72db4162abd54d28bd1f6125873e0f20/results.json`, local only) · [Execution 08 report](EXECUTION_08_REPORT.md)

Execution 07 locked restore and both builds passed. Core passed 47 tests per configuration and the Python worker passed eight. A disposable ordering fault failed its selected test and passed after restoration. The final native run recorded 24 passes, one blocked Explorer check, one inconclusive prelaunch cursor control and zero failures. All three recorded cases passed, including stale-result recovery. The Default desktop was restored with zero scoped workers. Native results (`artifacts/automated-acceptance/20261105T165845687-97103b0ee2674f8c9169b75edce63b76/results.json`, local only) · [Execution 07 report](EXECUTION_07_REPORT.md)

Execution 06 Debug and Release builds had zero warnings/errors. Each passed 47 .NET tests. Four worker tests passed and a reversible negative control exposed two failing bounds subcases. The final native run had 23 PASS, one blocked Explorer check, one inconclusive cursor baseline and zero failures. Both recorded-source cases and active worker shutdown passed. No device frames were acquired. Final native results (`artifacts/automated-acceptance/20261105T162333916-e5faee384bda46c081ce462be083b822/results.json`, local only) · [Execution 06 report](EXECUTION_06_REPORT.md)

Execution 05 final Debug/Release builds passed with zero warnings/errors and 47 tests each. Two isolated deliberate defects failed their selected tests and passed after restoration. CPU still-image inference passed. The short A1/A2 regression recorded 21 passes, one closed-app cursor case inconclusive and one Explorer shell case blocked. Native evidence (`artifacts/automated-acceptance/20261105T101056505-0f438a56911d4afaa59f0f4467003b7e/results.json`, local only) · [Engineering report](EXECUTION_05_REPORT.md)

Execution 04 locked restore passed. Both configurations built with zero warnings/errors and passed 22 deterministic tests. The final native run passed 22 cases with one blocked shell case. A scoped audit observed the Default input desktop with zero test processes remaining. Native results (`artifacts/automated-acceptance/20261105T094547971-b1f74b41ae6441a3aa7c94fcf9b32a20/results.json`, local only) · [Engineering report](EXECUTION_04_REPORT.md)

## Execution03 reliability checkpoint — 2026-11-05

**A2-PROVISIONAL — A1 PHYSICAL ACCEPTANCE PENDING.** Controlled automated subset verified; full A2 NOT ACCEPTED. Current evidence: artifacts/automated-acceptance/20261105T022024024-0e7c9ffb23d94a3bb30d9700ac6caf54/results.json; complete report docs/EXECUTION_03_REPORT.md. Earlier sections below are historical.

Scoped root-tree underlay lookup now Confirmed at a physical point inside the opaque panel. Duplicate/disabled/missing/replaced elements reject. Client binds process start time across restarts, rejects concurrent work as Busy and exact-correlates replies; request IDs are deduplicated and capabilities are single-use/300ms with root/element identity and bounds checks. Uncertain Execute outcomes are flagged and never automatically retried.

Real custom-provider GetAutomationIdCore callback blocked in a separate process; final timeout1826.28ms, independent primary-host status0.59ms, worker exit and fresh-worker recovery verified. Cancellation, provider exception/termination, old capability rejection and duplicate/malformed/partial/oversized/incorrect IPC responses passed. Actual provider mutation rollback is not guaranteed; hang proof is a read query.

Four Execution03 runs (short/standard/extended/final short), three seeds:200 cycles,7152 exact characters,200 digest matches,0 mismatches/recoveries. Standard60 cycles2141 chars and extended100 cycles3523 chars belong to preceding binary versions; final short20 cycles744 chars covers final readback safeguards. Both WPF and standard WinForms/Win32 controlled fixtures independently confirm four patterns. Ignored range write Attempted; rejected write Unsupported; read-only Denied.

Locked restore PASS; final Debug/Release builds0 warnings/errors;22 deterministic tests/config PASS. Evidence artifacts/execution-03 has final binlogs, TRX, summary and cleanup audit. Final native20 cases:19 PASS,1 shell BLOCKED. Warm probe p9531.12ms (20 samples), working set47259648 bytes; spot sample only. Desktop Default restored,0 remaining scoped processes. Product has no native input injection, camera/capture/B1 or consumer external-action flow.

**Next: A2 review.** Full A1 physical/shell/extra-display and A2 ordinary-app/cross-app physical focus/drag evidence remain open; no release merge or SHIPPED claim. No need to repeat the old manual-only deadlock to perform separately authorized engineering.


## Autonomous Execution02 —2026-11-05

Executable isolated A1 subset PASS; full automation matrix BLOCKED for unavailable shell/other configurations; full original A1 BLOCKED (no human/device provenance). No physical evidence relabeling. Latest artifacts/automated-acceptance/20261104T202754048-1b98c61d379f495cacb5c733e79e906b/results.json, per-case Markdown alongside it; details docs/AUTOMATED_ACCEPTANCE.md.

Actual injected results100 clicks,100 drag starts/ends0 cancellations,20-cycle exact560-character text,20 held native drags while Ghost progresses. Three120-sample cursor controls (closed/idle/active) had0 changes, no synthetic reposition/reset in those experiments. Current125% only. Stop/restart/close-during-press/exit cleanup native checks pass. Shell Alt+Tab/tray marked BLOCKED in isolated desktop with no Explorer; no default desktop automation or scaling changes.

Debug/Release8-project builds0 warnings/errors.20 deterministic tests/config pass; artifacts/automation-final-*.binlog and artifacts/automation-tests/automation-final-*.trx. Previous failed/inconclusive native runs untouched. New unique run JSON preserves failed desktop Unicode and DPI routing controls, intermediate typing mismatch and passing repairs.

A2-PROVISIONAL fixture-native tests pass Invoke/Toggle/RangeValue/Scroll with independently observed postconditions, consumed-token Stale and wrong-PID Denied. Under-overlay point probe Unsupported. Watchdog2 timeouts/restarts using explicit hung-worker test double passed; genuine provider hang/real app/focus-drag compatibility still NOT RUN. No A2 release acceptance/B1.

Run: powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Run-AutomatedAcceptance.ps1 -Build. It uses a temporary private input desktop and restores normal session; see runner README for guard/timeout limits. Product code has no SendInput; only isolated test runner does.

## Execution 02 — 2026-11-05 — A1 evidence review only

Reviewed current native JSON and project export inventory; no newly supplied human physical results. Existing regression-Debug.trx and regression-Release.trx each show17 passed/0 failed. No test/build/native run repeated. Latest900-event native record remains INCONCLUSIVE for cursor retention, with physical/Alt+Tab NOT RUN and current DPI only. No cursor-writing/synthetic-input/hook call sites matched application/test source scan; causation unresolved. Decision NO-GO/A1 BLOCKED, A2 not started. Detailed missing cases and next human action recorded in A1_PHYSICAL_ACCEPTANCE_RESULTS.md; prior evidence preserved.

## Latest — Execution 01.5 / 2026-11-05

Independent baseline restore/build zero warnings/errors,15/15 tests in Debug and Release. After A1 diagnostic repairs: both builds zero warnings/errors,17/17 tests per configuration, zero skips/failures. Build/trace evidence: artifacts/execution-01-5/baseline-{Debug,Release}.binlog, regression-{Debug,Release}.binlog and tests/*.trx.

Native20-cycle run: artifacts/a1-native/20261104T193854053-0b5a195da0b64c1b92166e8b8dbb3394/native-smoke.json; Status INCONCLUSIVE because cursorUnchanged=false.900 observations retained fixture foreground; overlay never foreground; actual styles/claim release/panel return/second instance/duplicate-start rejection/stop-restart/handle destruction true. No physical acceptance supplied. Historical favorable and inconclusive runs preserved, no repeated run to seek PASS.

Detailed mandatory matrix, environment, defect IDs and human-results section: docs/A1_PHYSICAL_ACCEPTANCE_RESULTS.md. Gate A1 BLOCKED. No display settings/camera/UIA/capture activity. A2 planning only in codex/A2_HANDOFF.md; no A2 source added.

## Latest results — 2026-11-05, M00/M01 resumed

**Gate A1 BLOCKED for full acceptance; build/core/native smoke verified.** Earlier entries below are historical.

Environment: Windows NT10.0.26200 x64, local SDK10.0.403/runtime10.0.14, primary1920x1200/120DPI. App manifest asInvoker; actual token integrity not separately measured.

| Check | Observed result | Status / evidence |
|---|---|---|
| SDK | Interrupted extraction missing analyzer/build-task files repaired after official SHA512 validation | PASS; Install-Dotnet.ps1 session output/global.json |
| Restore | All four projects, package locks generated | PASS exit0 |
| Debug and Release builds | All four projects compiled, zero warnings/errors | PASS; artifacts/build-debug.binlog, build-release.binlog |
| Core tests |15 discovered/passed per configuration, zero failures/skips | PASS; artifacts/tests/a1-core.trx and a1-core-release.trx |
| Initial native smoke | Styles/move/release/cleanup true, focus/cursor changed; insufficient window identity to attribute | INCONCLUSIVE independence; artifacts/native-smoke-01.json |
| Follow-up native smoke |90 events; actual styles true; moved/returned panel; Ghost released; fixture foreground5507164 retained; cursor1487,643 unchanged; overlay never foreground; HWND destroyed; second instance exit0 | PASS for bounded check; artifacts/native-smoke.json |
| Full physical input trials |100-click/drag-start,100 stationary movements,20 typing/reveal and20 physical-drag trials unperformed | NOT RUN |
| A10/A11 | Automated startup/second-instance/shutdown and125% DPI exercised; Alt+Tab/manual tray and DPI100/150 untested | PARTIAL; remaining NOT RUN |

Intermediate failures retained: initial SDK import/build-task missing files; external powershell -File colon arguments misparsed MSBuild switches (fixed by explicit -Arguments array); sandbox csc canceled (outside-sandbox build succeeded); Release CS8602 nullable-path diagnostic fixed by null-safe string.Equals. No tests suppressed. Initial native observation did not pass independence; subsequent better-instrumented run does not erase it.

Executed commands via wrapper: restore Pocket.Dual.Ghost.sln; build solution -c Debug and Release --no-restore -p:Platform=x64 with binary logs; test Core.Tests -c Debug and Release --no-build --no-restore with TRX; run Release Pocket.App.dll --native-smoke. Wrapper uses project-local SDK/cache; scripts/Install-Dotnet.ps1 repairs/validates SDK. Exact reproducible PowerShell array examples in docs/A1_NATIVE_CHECKLIST.md.

No camera, UIA or screen acquisition implemented/run. Next: physical checklist at A1 review boundary; no later gate.

## 2026-11-04 — Execution 01 paused by user

M00/M01 implementation began and was explicitly stopped after reported battery loss. Preflight and dotnet inspection executed; system SDK still absent. Project-local SDK download/setup was interrupted with Ctrl+C, exit1; successful installation/hash verification NOT confirmed. Partial source changes are saved but lifecycle/solution/tests are unfinished. Restore/build/unit tests/native smoke/physical mouse/focus trials: NOT RUN. Gate A1 remains BLOCKED pending completed implementation and verification. See codex/HANDOFF.md interruption checkpoint; resume only on user instruction. No webcam/UIA/capture activity occurred.

**Current status: ALL NATIVE WINDOWS TESTS NOT RUN.** This package contains specifications, research and a browser-only visual concept. It has never been validated as a real Windows application in this environment.

## How to record evidence
For each gate, append a dated entry with: Windows build and architecture; process integrity level (ordinary user/admin); screen resolution and scale; affected app name/version; hardware, camera and lighting where relevant; exact reproduction steps; expected result; observed behavior; `PASS | FAIL | NOT RUN | BLOCKED`; relevant logs/screenshot filenames (use approved dummy data only); bug issue or fix commit.

**Do not commit personal screenshots, raw webcam frames or secret data.** Store private evidence outside the repository. Any screenshots shared publicly must show dummy content.

## Gate summary
| Gate | Purpose | Evidence status | Blocker |
|---|---|---|---|
| A1 | Pass-through, no focus theft, simulated GHOST and physical mouse independence | NOT RUN | Windows native build and QA required |
| A2 | UI Automation compatibility fixture + real apps | NOT RUN | A1 first |
| B1 | Webcam sidecar, calibrated tracking, correct DPI mapping | NOT RUN | A1 first |
| B2 | Gesture FSM, loss handling, concurrent owned drags | NOT RUN | B1 first |
| C | Pocket snapshot workflow, shelf, persistence, no-camera fallback | NOT RUN | A/B first |
| D1 | Visual polish, frame-pacing and latency measurements | NOT RUN | Core functionality first |
| D2 | Clean install, privacy and offline release | NOT RUN | Stable feature set first |
| E | Optional live/video/browser integrations | NOT RUN | Deferred |

## Windows test entries

<!-- Append entries using this template. Keep failures, don't rewrite history. -->

### YYYY-MM-DD — Gate X — TEST-ID
- **OS/build and app version:**
- **Machine/monitor/DPI:**
- **Setup and steps:**
- **Expected:**
- **Observed:**
- **Result:** NOT RUN
- **Evidence:**
- **Issue/decision:**

### 2026-11-04 — Gate A1 — PREFLIGHT
- **OS/build and app version:** Windows NT 10.0.26200.0; x64; app not yet built
- **Machine/monitor/DPI:** Not collected by read-only preflight
- **Setup and steps:** Ran `scripts/Preflight-Windows.ps1`; inspected `dotnet --info`.
- **Expected:** Windows x64 and development tooling reported without camera/screen access.
- **Observed:** Windows x64 detected. .NET host 8.0.22 and WindowsDesktop runtime 8.0.22 present; no .NET SDK installed.
- **Result:** BLOCKED
- **Evidence:** Console output from preflight and `dotnet --info` in session log.
- **Issue/decision:** Install a supported .NET SDK before compiling. Native overlay, focus, drag and keyboard tests remain NOT RUN.

### 2026-11-04 — Gate A1 — IMPLEMENTATION
- **OS/build and app version:** Source scaffold only; no binary
- **Setup and steps:** Added `src/Pocket.Core`, `src/Pocket.App`, and deterministic core tests.
- **Expected:** A1 source boundaries exist without webcam, UIA, SetCursorPos or SendInput.
- **Observed:** Pure ownership state and WPF overlay source added; compile not possible without SDK.
- **Result:** NOT RUN
- **Evidence:** Repository source files.
- **Issue/decision:** Human Windows verification is required after SDK installation.

### 2026-11-04 — Specification package — DOC01–DOC03
- **Environment:** Windows x64; node/Python available; dotnet repeated inspection still has no SDK.
- **Scope:** Documentation-only; no application implementation, camera, UIA, screen capture or native compatibility test.
- **DOC01:** `python scripts/verify_kit.py` exit0, 25 kit assets/JSON/links/fences/research mirror PASS.
- **DOC02:** `node scripts/validate-spec.mjs` exit0, six docs/17 local links/19 contracts and traceability/14 report parts/13 Mermaid sources+3 inline parsed PASS. Initial attempt failed due missing DOM; jsdom setup corrected validation harness and rerun passed. This was tooling failure, not diagram/app runtime failure.
- **DOC03:** `node scripts/render-spec-diagrams.mjs` exit0, 16 SVG+PNG diagrams rendered via headless installed Edge; representative architecture/state/journey visually inspected. PASS for documentation rendering only.
- **Evidence:** scripts/validate-spec.mjs; scripts/render-spec-diagrams.mjs; actual generated `artifacts/spec-diagrams/` images; command outputs in session; documentation details in SPECIFICATION_REVIEW Part M.
- **Runtime gate status:** A1 build BLOCKED/NOT RUN; all native/camera/UIA/performance/packaging compatibility still NOT RUN. Static audit defects recorded for future A1 repair.


## Execution 15 — 2026-11-07

The pinned Debug and Release builds passed 82 core cases each, and the worker suite passed all 15 including the Media Foundation file reader. The final isolated Windows suite passed 36 cases, with zero failures and one blocked Explorer case. This includes 30 dual-target Workspace trials with native fixture mouse input and a separate logical Ghost stream. The camera diagnostic scope failure, retained attempts, and remaining physical gates are documented separately. [Execution 15 report](EXECUTION_15_REPORT.md)

## Execution 16 continuation — 2026-11-08

See [Execution 16 evidence](EXECUTION_16_REPORT.md) and [finger research](FINGER_TRACKING_RESEARCH.md). The real camera preview and aligned finger dots have user confirmation. Bounded capture, sparse model points, explicit per-finger states and an independently supervised versioned diagnostic budget are implemented. Full physical B1, Gate A/M02 and sustained finger-accuracy acceptance remain open. All failed runs and repairs are preserved. The final standard regression passed37 cases with zero failures and Explorer blocked; both configurations passed100 core tests and all20 worker tests passed. Draft PR5 is published and implementation455abf4 passed Windows CI. The user reported wrist-out index loss. Complete continuation acceptance remains CHANGES REQUIRED.

## Execution17 — camera-free demo stabilization (2026-11-08)

Main navigation, explicit isolated sample profile, launcher and labeled recorded/simulated entry points reuse existing implementations. Pinning now handles checked-state changes, including accessibility Toggle. Two seeded supplemental WPF-control/GDI/model/restart rehearsals passed. They do not replace native input acceptance: the final standard run was refused at foreground ownership by the existing safety guard. Debug/Release100 core tests and20 worker tests pass; readiness is PARTIAL with a manual remote-session checklist. No webcam acquisition or normal/legacy library migration was performed. See docs/EXECUTION_17_DEMO_REPORT.md and docs/DEMO_FEATURE_INVENTORY.md.
# Execution19 precision continuation — 2026-11-09

132 core tests per Debug/Release and29 workers pass. Full isolated Windows run20261109T101753949-5972d4d5d1bf410aa3e0d3a866bdc0da:39 PASS, Explorer BLOCKED, zero FAIL. Includes actual WPF generated-image visual movement/reacquisition/loss/Stop, sparse model preview, Pocket capture/restart, Workspace, shelf, shortcuts and native cursor controls. See EXECUTION_19_REPORT.md for source categories, earlier failures and final delivery evidence. Physical wrist-out/cold-start and personalized accuracy remain unverified; camera allowance exhausted, no camera started.

## Execution20 recovery — 2026-11-09

Final locked restore and Debug/Release solution builds passed with zero warnings/errors. Core suite:150/150 each configuration, zero skips; worker suite:29/29, zero skips. Isolated Windows suite:40 PASS,0 FAIL,1 BLOCKED (Explorer unavailable on private desktop). Seeded E20 routes and complete D17/E20 workflows passed. Detailed run identifiers, preserved failures, executable hashes and camera/cleanup evidence: [Execution20 report](EXECUTION_20_REPORT.md). Physical hand validation is blocked; camera remained off.

## Execution21 hands-on beta continuation — 2026-11-09

Pinned Debug/Release solution builds passed with 0 warnings/errors. Core:155/155 per configuration, zero skips. Worker:29/29, zero skips, with generated controlled video fixture. Full extended isolated Windows run `20261109T163409321-5611ac9b719c4475a7d7ee4700a732ba`:39 PASS,0 FAIL,1 INCONCLUSIVE (`CURSOR_CONTROL_GHOST_CLOSED`),1 BLOCKED (`A10_ALT_TAB_TRAY`, no Explorer on private desktop). D17/E20 both pass; E20 includes actual isolated keyboard steering and Escape Stop. Earlier run `20261109T162626252-f833bc11ac0c4196b6549652d9cf4666` preserved D17/E20 Alt+T failures caused by the new beta access-key collision; fixed to Alt+B. Initial Release `File.Replace` fixture failure and initial worker temp cleanup failure are preserved and pass on project-local TEMP/TMP reruns. Human ordinary-desktop behavior and physical hand control are not verified. Camera off; ledger unchanged. See [Execution21 report](EXECUTION_21_REPORT.md).

## Execution22 — 2026-11-10

Typed dispatcher gesture coordinator, neutral-before-pinch, Pause/gap/context/target binding, bounded open/pinch/release lesson and controlled mapped WPF gesture replay added. Final core167/167 Debug and Release; builds0 warnings/errors; worker29/29. Actual WPF E20 gesture/card route passed seed22003; final native regression BLOCKED by GameInputServiceWindow foreground, failure runs retained. Live authorization absent; camera ledger unchanged; no production action issuer. Physical/DPI/shell and final native/lesson acceptance remain open. See EXECUTION_22_REPORT.md and LIVE_GHOST_TEST_GUIDE.md; do not infer physical success or complete software acceptance.

## Execution23 — physical evidence and camera-free quality repair,2026-11-10

Full preparation native39 PASS/0 FAIL/1 INCONCLUSIVE/1 Explorer BLOCKED. Actual guided lesson and Camera-protocol routing passed. Creator confirmed usable scrollbar repair. One authorized physical session117094ms:1141 results,full1033/partial63/empty45;native/guardian release and worker exit confirmed. Real desktop Ghost moved correctly, but user reports jitter/choppiness/disappearance; quality NOT ACCEPTED and fingers-only NOT TESTED. Ledger11 released/926780ms/version5, historical ten rows unchanged. Camera-free cadence repairs pass172 Core tests/configuration and focused WPF seed23009. Preview250ms lifetime is separate from unchanged150ms gesture readiness;50ms interpolation is visual-only, not new observations or higher camera FPS. Raw source rate9.76 results/s unresolved; separate native Tracking run blocked again by service-window foreground on private desktop. Fresh physical retest approval pending; never infer it from prepared version6 code. See EXECUTION_23_REPORT.md.

## Execution23 final retest and recovery —2026-11-10

Second separately approved session127297ms released; ledger12 released,total1054077ms,0 pending,original session cap exhausted. Creator reports worse/jittery/glitching pointer and missing preview; physical quality FAILED, wrist-out NOT TESTED. Removed candidate interpolation and its feature-specific test, preserved inherited suite. Deduplicated unchanged WPF preview frames/landmarks/transforms; placed image before calibration and made Live selection default local preview without activation; exposed mapping rejection reasons. Final171 Core tests/configuration,0 build warnings/errors, worker29/29; final focused WPF/native image+product route seed23010 passes. Raw source about9.8/s unresolved; final physical benefit unverified. No release/live gesture qualification. Full native preparation39pass with cursor/shell gaps remains historical source evidence. See EXECUTION_23_REPORT and leading handoff; no further camera starts without new explicit policy decision.

## Execution24 camera-free capture candidate —2026-11-10

Direct RGB copy synthetic1080p7.90→6.16ms, identical pixels; offered-only30Hz format policy with explicit size/rate negotiation and legacy fallback; bounded PTS/delivery diagnostics. No inference resizing adopted: larger still fixtures lost detection, retained as negative evidence. Core171/config,workers32,native focused product PASS; broader Pocket native blocked by service foreground,3 failures preserved. No physical30FPS/live improvement claim, no camera or ledger change. Presentation wrapper and guide supplied. See EXECUTION_24_REPORT.md; camera cap remains exhausted.
