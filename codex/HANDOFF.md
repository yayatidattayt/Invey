# Invey — Current handoff

## Execution 14 — Invey desktop identity and Pocket cards

Build and launch from the current checkout with `Invey.sln` and `Invey.exe`. Invey resolves the verified local profile at `%LOCALAPPDATA%\Invey`. Pocket can open independent cards, persist pinned placement and manage the library with Rename, ordering, Delete and Undo. Consult the [Execution 14 report](../docs/EXECUTION_14_REPORT.md) for the migration policy, exact verification and remaining gates.

## Execution 13 — first persistent Pocket references

Open Pocket from the main window or tray to capture, preview, save and retrieve a local snapshot without a camera. Read the [Execution 13 report](../docs/EXECUTION_13_REPORT.md) for the tested slice, stacked review and remaining hardware gates.

## Execution 12 — B1 reliability review

PR #1 gains session-correlated calibration diagnostics, stricter settings parsing and controlled camera failure and display-loss coverage. Physical camera activation requires a new explicit Enable or test-specific authorization. [Execution 12 report](../docs/EXECUTION_12_REPORT.md)

## Execution 11 — calibration and Windows verification

Invey now maps the calibration region to a selected display, saves numeric preferences, and provides a local practice target with timed pinch and release. The camera starts off and the live Ready cause remains unresolved. Review the [Execution 11 report](../docs/EXECUTION_11_REPORT.md) before the historical checkpoints below.

## Invey Execution 10 checkpoint — 2026-11-06

Invey is the public product name. Ghost remains the hand-tracking subsystem. Existing assemblies, executables, mutexes and pipes retain compatibility. Tracking lab includes a session-local calibration preview with a working region, mirror, hand choice and readiness checks. Cancel stops the active worker. [Execution 10 report](../docs/EXECUTION_10_REPORT.md)

The single authorized ASUS FHD check converted 247 frames and accepted 246 results. One post-inference cancellation accounted for the difference. The preview showed live waiting and working-region feedback. Full F07 display mapping, numeric settings and practice remain next, subject to B1 review and applicable hardware authorization.

## Execution 09 checkpoint — 2026-11-06

Real ASUS FHD webcam acquisition passed through the visible Tracking lab. Three bounded sessions converted 379 frames and delivered 378 accepted observations. Explicit Stop completed source shutdown and observed worker exit. Restart opened the same selected device in a fresh generation. The final 25-second diagnostic limit also released it. [Hardware report and evidence](../docs/EXECUTION_09_REPORT.md)

Keep physical Gate A and Explorer, A2 scope and M02 review separate. The next useful B1 slice is F07 calibration on this verified camera after F06 review. Real device loss and OS-denial acceptance remain open.

## Execution 08 checkpoint — 2026-11-05

The creator authorized provisional F06 implementation before acceptance. Tracking lab now offers **Live camera**, explicit metadata discovery and device selection, Enable, visible lifecycle, Stop and Restart. The Windows Media Foundation adapter runs inside the existing local MediaPipe worker. The worker requires deliberate start and observed exit before replacement. Hand geometry near an edge can report a partial result with no interaction geometry. [Execution 08 report](../docs/EXECUTION_08_REPORT.md)

The final native run had 26 passes, zero failures, one blocked Explorer case and one inconclusive closed-app cursor baseline. It verified the actual native reader through WPF on a controlled file. An extended native file run completed 720 inferences. The camera metadata probe opened zero devices and the final cleanup audit found zero scoped test processes. Final native run (`artifacts/automated-acceptance/20261105T173443873-72db4162abd54d28bd1f6125873e0f20/results.json`, local only)

Next, verify the live path on an explicitly authorized device or virtual source and close F06 review before F07 calibration. Gate A physical and Explorer evidence remain open. M02 optional ordinary-app UIA deferral remains proposed. No ordinary-app control or release merge was made.

## Execution 07 checkpoint — 2026-11-05

The existing recorded Tracking lab uses a shared, validated single-frame VIDEO path. A distinct stale-result state clears an expired hand and recovers on a fresh result in the same session. Live camera is explicitly unavailable. The Windows regression recorded 24 passes, one blocked Explorer check, one inconclusive prelaunch cursor baseline and zero failures. All three recorded cases and active-worker shutdown passed. [Execution 07 report](../docs/EXECUTION_07_REPORT.md) · Native results (`artifacts/automated-acceptance/20261105T165845687-97103b0ee2674f8c9169b75edce63b76/results.json`, local only)

M02 is the disposition of optional ordinary-app UIA. Recommend formal deferral with consumer access kept hidden. A1 physical and shell evidence still precedes formal A2/M02 review. Once eligible, wire one opt-in Windows Media Foundation source into the existing worker, validate an authorized device and finish F06 before F07. The current checkout remains uncommitted.

## Execution 06 checkpoint — 2026-11-05

The existing app's **Tracking lab** connects controlled frames to real MediaPipe CPU VIDEO inference, a PID-validated named pipe and WPF. Enable, hand detection, movement, loss, return, Stop and Restart work on this source. Exit is required before Restart. Tested worker faults clear observations and contain the process. The lab says **Recorded source · Camera off**. Its local fixture and runtime come from Execution 05. [Execution 06 report](../docs/EXECUTION_06_REPORT.md)

Both builds succeeded and 47 deterministic tests passed per configuration. The final private-desktop run had 23 passes, one blocked Explorer check and one inconclusive closed-app cursor baseline. The two recorded tracking cases and active-worker shutdown passed. Full A1 and A2 disposition remain open. Next, finish those prerequisites and complete M03/B1 F06 live capture and authorized device verification. F07 calibration follows F06. The checkout still has no commit. Native results (`artifacts/automated-acceptance/20261105T162333916-e5faee384bda46c081ce462be083b822/results.json`, local only)

## Execution 05 checkpoint — 2026-11-05

Runtime-neutral F06 foundations are implemented in Pocket.Core/Tracking. Tests exercise session identity, bounded framing, one latest observation, tracking loss, worker fault signals and restart only after teardown. MediaPipe Tasks 0.10.32 and the version 1 float16 model passed local CPU qualification on still inputs. The model, wheel set and attributed fixture are in ignored project tools, with hashes in the report and evidence.

Both configurations passed 47 tests. The private-desktop A1/A2 run recorded 21 passes, one closed-app cursor control inconclusive and one Explorer case blocked. Default was restored with no scoped processes. [Execution 05 report](../docs/EXECUTION_05_REPORT.md)

Gate A physical/shell and M02 disposition still gate the product webcam worker. Next, close those prerequisites, then wire the first F06 explicit Enable → Observe → Stop → Restart slice with an authorized camera. Source remains on the unborn A2 checkout. The negative-control copy was restored and verified. Earlier sections preserve prior checkpoints.

## Execution 04 checkpoint — 2026-11-05

A2 now reports Applied, Rejected, Unknown and Conflict. A session with an unresolved mutation holds further actions until explicit dismissal. Up to three read-only checks preserve the original result. Failed teardown quarantines the worker until observed exit. The recovery window is available in the guarded fixture. [Engineering report](../docs/EXECUTION_04_REPORT.md)

Clean Debug and Release builds and 22 deterministic passes each. The final private-desktop run had 22 passes and one blocked Explorer shell case. Evidence is `artifacts/automated-acceptance/20261105T094547971-b1f74b41ae6441a3aa7c94fcf9b32a20/results.json`. The report retains both the earlier inconclusive cursor evidence and the typing failure preceding the harness correction. The Default desktop was restored and no scoped processes remained.

The checkout is still on the unborn `codex/a1-automation-a2-provisional` branch. The next checkpoint is Gate A physical/shell evidence and M02 disposition. F06 camera enable/observe/stop/restart is the earliest B1 slice after eligibility, followed by F07 calibration. Older entries below record previous checkpoints.

## Resumed at A2 acceptance review — 2026-11-05

The user resumed from the battery-stop checkpoint. The saved final run identifies the current Release app, worker, client and runner binaries by matching SHA-256 hashes. The run recorded 19 passes and one shell case blocked by the private desktop. Its display evidence covers 125% scaling. The real provider stall was a read query. This review used the saved run and current source.

**Review disposition: A2 remains provisional and full A1 physical acceptance remains open.** The four fixture actions and worker recovery are verified in their controlled scope. Ordinary-app compatibility and simultaneous physical drag and focus under A09 still need evidence.

Keep the current fixture allowlist for external actions. A timed-out or interrupted mutation can still take effect in the provider. Treat the outcome as unknown, require independent readback when available, and require a fresh, deliberate action before dispatching another mutation. Generic Invoke remains an attempt until the application supplies an observable postcondition.

Review finding from source inspection: `WorkerClient.Stop()` can throw while handling a timed-out or canceled request. The `finally` block then releases the process handle and clears the reference even if worker exit was never observed. This can bypass the structured uncertainty reply and permit a fresh worker to start while the previous worker's state is unresolved. The saved run verified successful teardown. Before using the supervisor with ordinary applications, cover termination failure and retain supervision until exit is confirmed.

Next evidence: use the existing `docs/A1_NATIVE_CHECKLIST.md` for the physical and shell cases. For A09, scope a disposable ordinary application and one benign action with independent readback, then record focus and physical drag continuity in a separate window. The review findings and the pending evidence above define the next A2 checkpoint.

## PAUSED — user battery stop, 2026-11-05

User explicitly requested a smooth stop because charge was running out. **Do not resume until asked.** All native tests finished before this stop; original input desktop Default restored and zero project test/provider/worker processes remained (artifacts/execution-03/cleanup.json). Final code builds Debug/Release with zero warnings/errors;22 deterministic tests/config passed. Final native regression19 PASS/1 shell BLOCKED. Four runs total200 typing cycles/7152 exact characters,0 mismatches. A1 physical remains BLOCKED; A2 remains PROVISIONAL and awaits review; no B1 started.

Resume from docs/EXECUTION_03_REPORT.md and artifacts/execution-03/summary.json, then the Execution03 checkpoint below. Final results: artifacts/automated-acceptance/20261105T022024024-0e7c9ffb23d94a3bb30d9700ac6caf54/results.json. No pending native run or uncompleted code edit. Source remains uncommitted on codex/a1-automation-a2-provisional; preserve existing files/history.


## Execution03 reliability checkpoint — 2026-11-05

**A2-PROVISIONAL — A1 PHYSICAL ACCEPTANCE PENDING.** Controlled automated subset verified; full A2 NOT ACCEPTED. Current evidence: artifacts/automated-acceptance/20261105T022024024-0e7c9ffb23d94a3bb30d9700ac6caf54/results.json; complete report docs/EXECUTION_03_REPORT.md. Earlier sections below are historical.

Scoped root-tree underlay lookup now Confirmed at a physical point inside the opaque panel. Duplicate/disabled/missing/replaced elements reject. Client binds process start time across restarts, rejects concurrent work as Busy and exact-correlates replies; request IDs are deduplicated and capabilities are single-use/300ms with root/element identity and bounds checks. Uncertain Execute outcomes are flagged and never automatically retried.

Real custom-provider GetAutomationIdCore callback blocked in a separate process; final timeout1826.28ms, independent primary-host status0.59ms, worker exit and fresh-worker recovery verified. Cancellation, provider exception/termination, old capability rejection and duplicate/malformed/partial/oversized/incorrect IPC responses passed. Actual provider mutation rollback is not guaranteed; hang proof is a read query.

Four Execution03 runs (short/standard/extended/final short), three seeds:200 cycles,7152 exact characters,200 digest matches,0 mismatches/recoveries. Standard60 cycles2141 chars and extended100 cycles3523 chars belong to preceding binary versions; final short20 cycles744 chars covers final readback safeguards. Both WPF and standard WinForms/Win32 controlled fixtures independently confirm four patterns. Ignored range write Attempted; rejected write Unsupported; read-only Denied.

Locked restore PASS; final Debug/Release builds0 warnings/errors;22 deterministic tests/config PASS. Evidence artifacts/execution-03 has final binlogs, TRX, summary and cleanup audit. Final native20 cases:19 PASS,1 shell BLOCKED. Warm probe p9531.12ms (20 samples), working set47259648 bytes; spot sample only. Desktop Default restored,0 remaining scoped processes. Product has no native input injection, camera/capture/B1 or consumer external-action flow.

**Next: A2 review.** Full A1 physical/shell/extra-display and A2 ordinary-app/cross-app physical focus/drag evidence remain open; no release merge or SHIPPED claim. No need to repeat the old manual-only deadlock to perform separately authorized engineering.

Resume by reading this checkpoint, docs/EXECUTION_03_REPORT.md, docs/TRACEABILITY.md and actual current source. Branch remains codex/a1-automation-a2-provisional, all source uncommitted/untracked. Preserve artifacts and pre-A2 snapshot. Scripts accept -Profile short|standard|extended and -Seed; private desktop only, bounded150/240/360s. No ongoing test process remains. Do not start B1.


## Latest checkpoint — autonomous Execution02,2026-11-05

Workstream **A2-PROVISIONAL — A1 PHYSICAL ACCEPTANCE PENDING**, local codex/a1-automation-a2-provisional repository/branch (uncommitted; no release merge). Existing files preserved; pre-A2 snapshot artifacts/a1-before-provisional-a2.zip SHA25604DC284BD52923A8DB072AB5B5A95C3F29492F06E9336C68390E285B1F53BDF3. Per-command trust used only for this initialized root; no global safe.directory change.

Read docs/AUTOMATED_ACCEPTANCE.md and tests/Pocket.Acceptance.Runner/README.md. Final actual evidence artifacts/automated-acceptance/20261104T202754048-1b98c61d379f495cacb5c733e79e906b/results.json. Dedicated private desktop with supervisory restore/timeout/job cleanup; default desktop never receives injected test input. Native and INJECTED_GUI_TEST classes separated. Actual final:100/100 clicks through rendered panel,100 starts/ends with0 canceled,560-char exact typing over20 cycles,20 held-drag/Ghost advancement trials, closed/idle/active cursor controls0 changes, stop/restart/closure/exit cleanup pass. Shell Alt+Tab/tray BLOCKED (no Explorer on test desktop); other scales not changed. Full A1 still BLOCKED for original human/device/environment evidence.

Eight-project Debug/Release build0 warnings/errors;20 deterministic tests/config pass. Failures retained: Unicode desktop-name marshalling, DPI virtual-coordinate routing mismatches (no-overlay control also failed), one intermittent text mismatch; corrected launch/DPI and strengthened per-chunk digest acknowledgment, final run passed. Typing soak remains next reliability task; no causal claim about prior unattended cursor change.

Actual provisional A2 source: Automation.Contracts, Client, Worker; fixed fixture PID/window/control allowlist, bounded JSON, windowless MTA,300ms single-use runtime-ID capabilities,4 patterns, no uncertain action retry. Real native fixture postconditions pass; consumed token Stale/wrong PID Denied. Under-overlay point lookup returns Unsupported, no unsafe fallback. Watchdog tested using explicitly stalled-worker double (2 timeouts/restarts), not real provider hang. Consumer App does not enable these external actions; only guarded test mode opens fixture. Full A2 not accepted; no B1/camera/capture work.

Next engineering action: soak/exact-text synchronization regression, then expand controlled underlay/native-provider hang and real-app allowlist compatibility. Keep shell/DPI/hardware evidence pending; do not repeat lengthy manual checklist as prerequisite for safe independent work. All test processes exited after final run. Earlier checkpoints below are historical.

## Latest checkpoint — Execution 02 / 2026-11-05

**A1 NO-GO / BLOCKED; A2 NOT STARTED.** New directive conditionally authorizes A2 only after legitimate A1 closure; no new human physical observations or exports were supplied/found in project evidence. Current acceptance matrix and existing JSON/TRX were reviewed. Existing17/17 per-config tests remain historical verified evidence, not rerun in this execution. Latest900-event native run still cursor INCONCLUSIVE; no cause inferred or fresh failure diagnosed. Application code/build/evidence unchanged.

Next required action is user physical input: follow docs/A1_NATIVE_CHECKLIST.md and return A02 counts, A03 exact match over20 cycles, A04 continuous drag counts, A05 controlled stationary trial, A01/A10 switching/tray/cleanup and A11 tested/unavailable DPI setups plus named numeric exports. Record provenance as user-reported physical observations; do not use software automation as substitute. Once mandatory evidence and required review pass, reevaluate the conditional A2 instruction against codex/A2_HANDOFF.md and current scope. No camera/B1 authorized.

## Latest checkpoint — Execution 01.5 / 2026-11-05

**A1 BLOCKED at human physical-input checkpoint.** Read docs/A1_PHYSICAL_ACCEPTANCE_RESULTS.md; no user physical results supplied. Do not start A2. Its preparation-only scope is codex/A2_HANDOFF.md.

Independently rebuilt baseline Debug/Release with restore: zero warnings/errors and15 tests passed each. Repaired diagnostic evidence gaps: native smoke output/error files unique per-run, explicit PASS/FAIL/INCONCLUSIVE with foreground-overlay detection a failure; fixture tracks canceled/active drag; lifecycle observer verifies actual press->stop->restart. Added2 ownership regressions. Final Debug/Release builds and17 tests each PASS. Evidence: artifacts/execution-01-5 baseline/regression build binlogs and test TRX.

Latest native software evidence: artifacts/a1-native/20261104T193854053-0b5a195da0b64c1b92166e8b8dbb3394/native-smoke.json.20 cycles/900 observations, styles/move/release/return/stop/duplicate-start rejection/fixture foreground/cleanup/second instance passed. Native cursor changed, so overall independence observation INCONCLUSIVE; no cause attributed. Old native-smoke.json/native-smoke-01.json left intact. No app process left running. No physical clicks/keys or synthetic OS input supplied; no display settings changed.

Next: person launches Release controller, executes numbered A1 checklist with real devices, reports counts/exact match/focus/drag/Alt+Tab/125% and available100/150% cases and JSON filenames. See acceptance record for user-result form. Manual canceled-drag counter behavior awaits real trials. Unit/native software evidence cannot close the human requirements. Later sections are historical.

## Current checkpoint — 2026-11-05: resumed, A1 review boundary

User explicitly resumed. Source now builds; **Gate A1 BLOCKED pending full physical acceptance**. No A2/webcam work authorized.

Local SDK10.0.403/runtime10.0.14 repaired from official ZIP, SHA512 verified: 7A114C318FA7F1D9822991B534B2814D0E78386CEDA816B371F5141ACEFE741D87CCB606EDE06CADAC8F9AC5070E32B708A67BBD1619FF5E5A7858A27874EF48. global.json pins SDK; scripts/dotnet.ps1 uses project-local CLI/NuGet and disables telemetry. No machine PATH change.

Completed: .NET10 Core/App/Overlay/test projects and valid Debug/Release solution mappings; pure independent pointer/target claims and typed geometry; internal Stopwatch-timed replay; checked Win32 passive styles/primary display; controller, native fixture, settings toggle, numeric export, single-instance/reopen event, tray Open/Settings/Stop/Exit and shutdown cleanup.

Restore PASS; Debug/Release builds PASS with zero warnings/errors.15/15 deterministic tests PASS per configuration. Native follow-up smoke:90 events, required style bits true, panel moved/returned, claim released, fixture foreground retained, cursor unchanged, overlay never foreground, second instance exited and HWND destroyed. Environment Windows26200 x64,1920x1200,120DPI. First native run changed focus/cursor with insufficient identity to attribute: retained as artifacts/native-smoke-01.json. Follow-up artifacts/native-smoke.json includes exact baseline/fixture/overlay handles. These are sampled checks, not physical acceptance.

Evidence: artifacts/build-debug.binlog, build-release.binlog; artifacts/tests/a1-core.trx and a1-core-release.trx; native-smoke.json. No app process left running.

Next legitimate action: docs/A1_NATIVE_CHECKLIST.md, actual100-click/drag-start,100 stationary-cursor movements,20 typing/reveal,20 drag continuity, Alt+Tab/manual tray/stop/exit and DPI100/125/150 trials. All remaining physical trials NOT RUN. Do not advance beyond A1. Full dual Workspace, camera/UIA/capture/library remain later gates. Earlier paused/uncompiled text below is historical and superseded here.

Updated 2026-11-04. **PAUSED at the user's explicit request after battery loss.** Current implementation gate: M00/M01 / A1, unpassed. Partial application implementation is saved; do not treat the working tree as buildable or tested.

## Execution 01 interruption checkpoint

- User requested: “my battery ran out, cleanly stop this process.” Stop request supersedes ongoing implementation. Do not resume until user asks.
- Re-read current directive/handoff/source and independently verified no system .NET SDK. Windows preflight ran; CIM metadata still unavailable in sandbox. Git still resolves to unrelated home repository with ownership warning; no Git trust/configuration change made.
- Began official project-local .NET10 SDK acquisition using scripts/Install-Dotnet.ps1, which resolves Microsoft release metadata, downloads win-x64 ZIP, checks SHA512, then expands under ignored .tools. Metadata selected 10.0.403. The active setup session was interrupted with Ctrl+C and returned exit1. Installation and hash verification were NOT confirmed. Retain any partial download; inspect before retrying. No machine-wide install or PATH change.
- Saved partial source: .NET10 Core/App targets, Directory.Build.props, pure typed geometry/per-target ownership, deterministic ReplaySession, new Pocket.Overlay project with checked Win32 styles/primary-monitor geometry, GhostOverlay, timed SimulationController, controller UI, ordinary test FixtureWindow, manifest and global imports. Old App/GhostOverlay.cs moved conceptually into the new overlay module (old file removed, replacement saved).
- **Still pending:** App startup/single-instance/tray/cleanup wiring (App.xaml.cs still old); solution references/configuration; SDK/global.json; updated tests (old tests reference removed API); native smoke/evidence harness; build/restore, compiler fixes, deterministic tests and native verification. No build or test has been run on the partial implementation. App.xaml now uses explicit shutdown, so do not launch before wiring the lifecycle.
- No application, webcam, UIA, capture workflow, or native input test launched in this implementation session. No later gate started.
- Post-stop inspection found the downloaded ZIP (300745255 bytes) and .tools/dotnet/dotnet.exe present; extraction completeness and SDK usability remain unverified. No Pocket.App or dotnet process was listed by the post-stop process check.
- On explicit resume: inspect partial files and .tools first, complete/verify SDK setup, finish lifecycle/solution/tests, then build and fix before running. Preserve all original specs and prior evidence. Stop at A1 review; physical mouse/focus acceptance remains NOT RUN.

Actual solution root is this file's parent directory's parent (`GHOST_CODEX_PROJECT_READY`). Parent Documents/ghost holds original ZIP and project folder. Existing application code is unverified and has compile/configuration/state/simulator defects recorded in the directive §3. Machine is Windows x64 NT10.0.26200.0; latest observed dotnet has runtimes only, no SDK. No source repair was performed in the specification stage.

Read order: AGENTS -> MASTER_SPEC/DECISIONS -> SPECIFICATION_REVIEW -> MASTER-IMPLEMENTATION-PROMPT -> TEST_RESULTS/COMPATIBILITY_MATRIX -> source and scoped status. Preserve unrelated edits and historical evidence. Git currently walks to home directory and reports dubious ownership; do not change global trust or commit home-directory files.

Next actionable task when implementation is requested: establish supported .NET10 x64 SDK through approved flow, repair net8 targets/imports/solution build mappings, build app/core directly, then implement timed internal replay, HWND style assertions, selected-display typed geometry, lifecycle and native fixture. Camera/UIA are outside A1.

Required review: A01–A05/A10 plus coordinate/ownership tests; record actual app/build/monitor/DPI, cursor/focus/counters, continuous physical drag. Native checks remain NOT RUN until observed. Stop at A1 review. If SDK/human hardware checks remain unavailable, retain blocker and complete safe current-gate work without claiming pass.

Canonical research: docs/research.md, root mirror via scripts/Sync-Research.ps1. Stable feature/test traceability: docs/TRACEABILITY.md. Runtime evidence: docs/TEST_RESULTS.md. No SHIPPED features yet.
