# Gate A2 handoff

## Execution 04 — mutation recovery verified in fixtures

The worker supervisor retains a failed termination until exit is confirmed. Explicit mutation outcomes and bounded read-only observations are separate. The controlled tests verified late provider commits, acknowledgement loss, a late response after quarantine and the recovery controls. A2 remains provisional pending A1 and A09. [Execution 04 report](../docs/EXECUTION_04_REPORT.md)

## Execution03 reliability checkpoint — 2026-11-05

**A2-PROVISIONAL — A1 PHYSICAL ACCEPTANCE PENDING.** Controlled automated subset verified; full A2 NOT ACCEPTED. Current evidence: artifacts/automated-acceptance/20261105T022024024-0e7c9ffb23d94a3bb30d9700ac6caf54/results.json; complete report docs/EXECUTION_03_REPORT.md. Earlier sections below are historical.

Scoped root-tree underlay lookup now Confirmed at a physical point inside the opaque panel. Duplicate/disabled/missing/replaced elements reject. Client binds process start time across restarts, rejects concurrent work as Busy and exact-correlates replies; request IDs are deduplicated and capabilities are single-use/300ms with root/element identity and bounds checks. Uncertain Execute outcomes are flagged and never automatically retried.

Real custom-provider GetAutomationIdCore callback blocked in a separate process; final timeout1826.28ms, independent primary-host status0.59ms, worker exit and fresh-worker recovery verified. Cancellation, provider exception/termination, old capability rejection and duplicate/malformed/partial/oversized/incorrect IPC responses passed. Actual provider mutation rollback is not guaranteed; hang proof is a read query.

Four Execution03 runs (short/standard/extended/final short), three seeds:200 cycles,7152 exact characters,200 digest matches,0 mismatches/recoveries. Standard60 cycles2141 chars and extended100 cycles3523 chars belong to preceding binary versions; final short20 cycles744 chars covers final readback safeguards. Both WPF and standard WinForms/Win32 controlled fixtures independently confirm four patterns. Ignored range write Attempted; rejected write Unsupported; read-only Denied.

Locked restore PASS; final Debug/Release builds0 warnings/errors;22 deterministic tests/config PASS. Evidence artifacts/execution-03 has final binlogs, TRX, summary and cleanup audit. Final native20 cases:19 PASS,1 shell BLOCKED. Warm probe p9531.12ms (20 samples), working set47259648 bytes; spot sample only. Desktop Default restored,0 remaining scoped processes. Product has no native input injection, camera/capture/B1 or consumer external-action flow.

**Next: A2 review.** Full A1 physical/shell/extra-display and A2 ordinary-app/cross-app physical focus/drag evidence remain open; no release merge or SHIPPED claim. No need to repeat the old manual-only deadlock to perform separately authorized engineering.


## Autonomous Execution02 — provisional implementation now exists

Latest user authorization explicitly permits isolated provisional A2 after executable A1 readiness despite remaining physical/environment gaps. Eight-project build and20 tests/config pass; isolated GUI cases pass. Source/contracts/client/MTA worker and benign fixture-native4-pattern tests now implemented on codex/a1-automation-a2-provisional. Label A2-PROVISIONAL — A1 PHYSICAL ACCEPTANCE PENDING. Older “not started” instructions below are historical.

F05 partial: fixed fixture-only allowlist, bounded IPC/supervision, fresh single-use runtime-identity tokens and guarded actions; native postconditions4/4, wrongPID denied/reused tokens stale. Under-overlay point lookup Unsupported, so no generic underlay control. Watchdog tested with explicit stalled-worker double; real provider hang and real-app A06–A09 compatibility unverified. No consumer external-control UI/activation. Keep A2 full gate BLOCKED; no B1.

Next: controlled provider-hang/underlay/allowed-app compatibility and runner typing soak, maintaining existing regressions. See docs/AUTOMATED_ACCEPTANCE.md for evidence/commands. Do not merge/distribute provisional work as release.

Execution02 update (2026-11-05): latest user directive includes conditional A2 implementation authority after legitimate A1 GO, subject to required gate review. A1 currently remains NO-GO/BLOCKED because mandatory human physical observations are missing. Conditional authority has not activated; this handoff remains preparation-only. Do not interpret the earlier request for a separate instruction as permission to bypass A1 evidence/sign-off, or automatically request redundant authorization after a qualifying later instruction is established.

2026-11-05. **Do not execute yet. A1 remains BLOCKED by human physical checks.** Next permitted action is the A1 checkpoint in docs/A1_PHYSICAL_ACCEPTANCE_RESULTS.md. Separate user execution instruction and reviewed A1 evidence are required before A2.

Sources: docs/CODEX_PHASES.md “PHASE A2”, MASTER-IMPLEMENTATION-PROMPT F05/§13/M02, IMPLEMENTATION_RFC §3 and QA_MATRIX A06–A09. Requirement F05/AC-F05; tests A06,A07,A08,A09,S04.

Exact slice:
- Separate Pocket.Automation.Worker process, no owned UI, COM MTA; typed bounded local IPC in Pocket.Automation.Contracts; host watchdog terminates/restarts on hung provider. Caller timeout cannot cancel a blocked COM call.
- Dedicated deterministic target fixture exposes benign Invoke, Toggle, Scroll and RangeValue with observable counters/state. Existing A1 physical fixture is not an implemented UIA integration.
- Throttled probe at typed physical screen coordinate; evaluate whether passive overlay masks underlying target. Reject own process/ambiguous targets. Underlay heuristics require evidence, else Unsupported.
- Explicit deliberately activated per-app/control allowlist, revalidation of PID/HWND/identity/bounds/enabled/pattern/freshness before action. No hover invocation, typing, generic drag, sensitive/destructive actions, elevation or pointer injection.
- Test own fixture plus at least one ordinary real app (Notepad/Explorer/Edge). Record focus and mouse-drag continuity separately, and postcondition success distinct from attempted call. Hang/flood/malformed/stale cases leave host responsive and tokens invalidated.
- A07 historical matrix mentions ExpandCollapse, while canonical phase/F05 minimum is the four patterns above. Keep ExpandCollapse unavailable unless explicitly specified/tested; record its disposition rather than claiming it passed.

Expected modules only after authorization: Automation.Contracts, Automation.Worker, deterministic automation test fixture, process/IPC tests and small App experimental capability/supervision wiring. Keep Core/Overlay independence regression intact. User controls remain experimental/off by default; poor UIA compatibility can be explicitly deferred/disabled after the A2 research gate.

Risks: under-overlay resolution, stale virtualized elements, COM apartment affinity, provider hangs, action side effects/focus theft, integrity/security boundaries and uncertain outcomes. A06 verifies own-overlay exclusion; A07 benign pattern outcomes; A08 hang/watchdog/nonblocking host; A09 separate external focus/physical drag evidence. S04 validates bounded IPC and wrong/stale/unknown targets, no unsafe automatic retry.

Acceptance: actual native fixture and real-app results, no host stall, no cursor movement, truthful Unsupported/Denied/Stale/Timeout/Attempted/Confirmed outcomes and app/action compatibility rows. Optional external controls cannot weaken Pocket-owned independence. Explicit unsupported disposition is preferable to fallback injection.

Deferred: B1 camera/MediaPipe/calibration, B2 gestures/hand ownership hardware trials, C capture/storage/library/full dual Workspace, D packaging/polish. No new modules or source for A2 were created during this handoff.
