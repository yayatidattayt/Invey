# GHOST — Execution03 A2 reliability review
Date: 2026-11-05, Asia/Calcutta. **A2-PROVISIONAL — A1 PHYSICAL ACCEPTANCE PENDING. Stop at A2 review.**

## A. Starting state

Actual branch: codex/a1-automation-a2-provisional, all source still uncommitted/untracked in the existing local repository; no release merge. Execution02 baseline had20 deterministic tests per configuration and a passing isolated native/injected subset, but underlay point resolution was Unsupported and its watchdog used a hung-worker double. Full A1 was BLOCKED, A2 provisional. Original snapshots, failures and run directories remain intact.

Environment: Windows NT10.0.26200 x64, SDK10.0.403/runtime10.0.14, primary1920x1200 at120DPI (125%). No scaling, security policy, camera, capture or unrelated application state was changed.

## B. Underlay targeting

The old point path used desktop-global AutomationElement.FromPoint. That API finds a desktop hit, which is not equivalent to finding the requested descendant of an authorized HWND. Earlier Unsupported records did not retain the rejected hit's identity, so claiming a specific overlay/ancestor result in that historical run would overstate the evidence. Source inspection establishes the unsafe assumption; the new controlled result establishes the improvement.

The worker now validates HWND ownership, Pocket.App process name and captured process start time, and the fixture root marker before searching that root's descendant tree for the fixed target ID. More than one match returns AmbiguousTarget. One candidate must be enabled, non-password, on screen, in the expected process and contained in the root's physical bounds. Point requests additionally require physical-point containment in that candidate.

Capabilities bind root and element runtime IDs, exact bounds, HWND, process, target, point and a300ms single-use lifetime. Execution revalidates those identities and uses the same resolved element rather than resolving again immediately before the call.

**Observed final:** UnderOverlayPointProbe=Confirmed, Resolution=ScopedTree; the test asserts the physical point lies inside the visible opaque panel over a partially covered fixture button. Invalid/outside/wrong-window targets, duplicates, replacement and destroyed-window capabilities reject. No system-pointer movement or global discovery fallback exists in product code.

This is a controlled fixture resolver, not a universal obscured-control adapter. FindAll runs only inside the authorized small fixture; the process deadline bounds provider stalls. Virtualized controls, overlapping unrelated windows, irregular clickable shapes and large provider trees need their own adapter/evidence.

## C. Provider-hang recovery

[Final native results](../artifacts/automated-acceptance/20261105T022024024-0e7c9ffb23d94a3bb30d9700ac6caf54/results.json), [provider cleanup](../artifacts/automated-acceptance/20261105T022024024-0e7c9ffb23d94a3bb30d9700ac6caf54/adverse-provider/cleanup.json).

A separate guarded Pocket.App process exposes a custom WPF AutomationPeer. It writes an entry marker from GetAutomationIdCore, then blocks the real provider callback until an external release marker or a30-second fixture safety limit. GHOST's primary acceptance process is separate and serves independent status requests.

Final observed cases:

| Case | Observation |
|---|---|
| Slow query | Confirmed in315.20ms |
| Real blocked query | Callback entry proved; Timeout in1826.28ms against1800ms request deadline |
| Main GHOST responsiveness | Independent status reply0.59ms while provider callback was blocked |
| Concurrent request | Busy immediately; no unbounded queue |
| Timeout cleanup | Worker exit observed; no automatic retry |
| Fresh request after release | New worker Confirmed; old worker's capability Stale |
| Explicit cancellation while blocked | Canceled; worker exit observed |
| Provider callback exception | Unsupported; subsequent healthy query Confirmed |
| Provider terminated during query | Unsupported; next request Denied; provider gone |

The separate explicit hung-worker double still produced two bounded timeouts/restarts, about556ms combined at250ms deadlines. IPC action-timeout uncertainty uses that double and is labelled separately.

**Limit:** the genuine hang test blocks a read query. It does not establish rollback of a dispatched Invoke or other mutation. An Execute transport failure reports OutcomeUnknown and is never automatically retried. A provider can have accepted an action before the worker dies. Generic transactional rollback is unavailable and remains a release restriction, not a claimed guarantee. Logical action ownership is not connected to consumer UI; dead-worker capability state is discarded with the process.

## D. Typing stability

All four Execution03 runs preserved below passed exact seeded per-chunk and batch digests against the controlled editor while GHOST replay progressed. Batches reset the fixture and stop/restart replay every20 cycles. Foreground and editor focus are checked. Profiles have bounded private-desktop budgets: short150s, standard240s, extended360s, with restoration watchdog5s later.

| Profile | Seed | Cycles | Characters | Matches / mismatches / recoveries | Evidence |
|---|---|---|---|---|---|
| short | 73021 | 20 | 744 | 20 / 0 / 0 | [JSON](../artifacts/automated-acceptance/20261105T021101703-c62ffc7bfd8549ec95e34db0f7964f9e/results.json) |
| standard | 99173 | 60 | 2141 | 60 / 0 / 0 | [JSON](../artifacts/automated-acceptance/20261105T021406895-9bb7faac4f5d40e087e7fb07fe3b3f82/results.json) |
| extended | 41007 | 100 | 3523 | 100 / 0 / 0 | [JSON](../artifacts/automated-acceptance/20261105T021655335-e4bd8a304114423e9eb0fbc00667fed7/results.json) |
| short (final) | 73021 | 20 | 744 | 20 / 0 / 0 | [JSON](../artifacts/automated-acceptance/20261105T022024024-0e7c9ffb23d94a3bb30d9700ac6caf54/results.json) |

Total: **200 cycles,7152 characters,200 exact chunk matches,0 observed mismatches,0 recoveries** across three seeds and incremental code versions. The final short run validates the final product changes; standard/extended evidence belongs to the immediately preceding versions, not silently relabelled final binaries. Final results include app, worker, client and runner hashes.

Extended run:139.45s typing, max acknowledgement62.51ms, mean33.24ms. Final short:27.87s typing, max63.48ms, mean37.49ms. Extended/final audit files record each expected/acknowledged length and digest, seed/batch/cycle/event position/latency; no raw editor text. Successful runs had no missing acknowledgement, extra characters or timeouts. Acknowledgements are serial state snapshots, not an event stream; duplicate acknowledgement-event counts are inapplicable. Exact length/digest catches duplicated or dropped input.

Earlier Execution02 intermittent mismatch remains preserved; these bounded passing soaks do not establish its root cause or multi-hour stability. No controlled CPU-load sweep or hardware-origin typing was performed.

## E. UIA pattern reliability

| Pattern | Positive proof | Adverse observations |
|---|---|---|
| Invoke | Benign counter independently increments in WPF and WinForms; worker says Attempted | Disabled/missing/no-pattern unsupported; replacement/expiry/consumed token stale; duplicate action ID denied, counter increments once; pre-cancel prevented dispatch |
| Toggle | Four repeated WPF transitions; independent state verification in WPF and WinForms | Disabled target unsupported; no promise about arbitrary invalid provider state or target replacement mid-call |
| RangeValue | WPF0,100,42.125 and standard fixture42 independently observed | -1/101 denied; read-only denied; ignored write yields Attempted with actual13; rejecting provider yields Unsupported |
| Scroll | Vertical small increment produces independent WPF and WinForms position change | No-scroll and bottom boundary unsupported; other directions/ExpandCollapse unavailable |

Toggle confirms only an observed valid state change. Range confirms only finite readback within relative1e-9 tolerance of the requested value. Scroll confirms only an observed valid increase. Missing readback change yields Attempted rather than false success. Invoke cannot generically verify a business outcome; fixture state supplies independent confirmation.

Invalid toggle-state callbacks, adversarial toggle postcondition mismatch, every pattern's mid-call replacement/rejection, and all direction combinations were not exhaustively tested.

## F. Security and identity

- Target process start time is captured by the client at construction and passed unchanged to every worker restart; the worker compares it before authorizing a request. PID-only identity is insufficient.
- HWND existence/owner/root marker and root/element runtime identities are checked. Bounds, point and freshness are capability-bound. Reused and consumed capabilities reject.
- Exact reply correlation replaces the earlier allowance for unknown IDs. Malformed, partial, oversized, EOF/exit, mismatched and leftover duplicate replies fail closed and terminate that worker.
- Canonical GUID dedup blocks alternate formatting and duplicate action IDs. Bounded4096-entry request history fails closed when full instead of forgetting old IDs; client history persists across worker restart. Tokens are capped32 per worker.
- One active request; Busy for additional callers. Deadline covers pipe write and read; cancellation terminates the worker and observes exit. A new caller explicitly requests restart; no infinite automatic restart loop.
- JSON lines are limited to16384 characters, operations/targets are fixed, coordinates/values must be finite. No typing, arbitrary drag, elevation or generic desktop action path.
- Native wrong-PID/HWND/invalid HWND, expiry, duplicate ID/action, pre-cancel, replacement, window recreation and worker-restart capability cases passed. The actual OS reusing a PID or HWND was not forced; lifetime binding is implemented, not empirically certified against real reuse.

## G. Compatibility

Tested only controlled same-user fixtures on this Windows build/DPI:

| Provider class | Observed scope |
|---|---|
| WPF standard controls + controlled Slider peer | Four positive patterns, under-overlay point, selected negative states |
| WinForms/Win32 Button, CheckBox, HScrollBar, scrolling Panel | All four capabilities and actions with independent counter/state verification |
| Custom WPF AutomationPeer in separate process | Slow/blocked/exception/terminated provider and fresh-worker recovery |

No Notepad, Explorer, browser, private document, elevated target, secure desktop or game was exercised. Ordinary third-party application compatibility and simultaneous cross-app physical drag/focus acceptance remain unverified. Additional real DPI/monitor configurations remain NOT RUN. No universal compatibility claim.

## H. Build and automated tests

Executed from GHOST_CODEX_PROJECT_READY using the project-local wrapper:

```powershell
& .\scripts\dotnet.ps1 -Arguments @('restore','Pocket.Dual.Ghost.sln','--locked-mode')
& .\scripts\dotnet.ps1 -Arguments @('build','Pocket.Dual.Ghost.sln','-c','Debug','-p:Platform=x64','--no-restore')
& .\scripts\dotnet.ps1 -Arguments @('build','Pocket.Dual.Ghost.sln','-c','Release','-p:Platform=x64','--no-restore')
& .\scripts\dotnet.ps1 -Arguments @('test','tests/Pocket.Core.Tests/Pocket.Core.Tests.csproj','-c','Debug','--no-build','--no-restore')
& .\scripts\dotnet.ps1 -Arguments @('test','tests/Pocket.Core.Tests/Pocket.Core.Tests.csproj','-c','Release','--no-build','--no-restore')
& .\scripts\Run-AutomatedAcceptance.ps1 -Profile short -Seed 73021
& .\scripts\Run-AutomatedAcceptance.ps1 -Profile standard -Seed 99173
& .\scripts\Run-AutomatedAcceptance.ps1 -Profile extended -Seed 41007
```

Locked restore passed. Final Debug/Release builds:0 warnings,0 errors.22 deterministic tests per configuration passed,0 failures/skips. TRX and binlogs are in [Execution03 evidence](../artifacts/execution-03/). Core/contracts test code was unchanged by the final worker readback refinement; its final native regression passed.

Final native suite:20 cases,19 PASS and1 BLOCKED (shell Alt+Tab/tray). Overall PASS WITH EXPLICIT ENVIRONMENT GAPS. Four preserved Execution03 run directories have zero FAIL cases. One intermediate compilation failed because the new harness file lacked a System.IO import; fixed before execution. No failures suppressed or gates lowered. Documentation validators are separate from Windows acceptance.

## I. Performance and cleanup

Final21-probe spot sample: cold182.84ms;20 warm queries mean27.95ms, p9531.12ms. Worker working set47259648 bytes, private bytes12079104,298 handles, cumulative CPU171.875ms at sampling; disposal observed. Extended-version warm p9525.90ms. These are small controlled samples, not frame, battery, sustained-load or leak certification.

Final cursor controls:120 samples each with GHOST closed/idle/active, no generated input during sampling; no coordinate changes. Final input checks:100/100 panel clicks,100/100 drag starts/ends with0 canceled,20 held drags while GHOST progresses. Stop/restart and close-during-press cancel timer/claim; exit0 and HWND disappearance verified.

[Post-run cleanup audit](../artifacts/execution-03/cleanup.json): input desktop Default, zero remaining project acceptance host/provider/worker processes. Provider cleanup markers also prove worker/provider exit. The parent job owns only the disposable acceptance tree. Abrupt supervisor death/power-loss desktop restoration and long-duration resource trends are not certified.

## J. Code changes

Significant modules:
- src/Pocket.Automation.Worker/Program.cs: scoped resolution, lifetime/identity checks, dedup, token consumption and truthful pattern readback.
- src/Pocket.Automation.Client/WorkerClient.cs: nonqueued RPC, strict correlation, deadline/cancellation, request history, process lifetime and observed exit.
- src/Pocket.Automation.Contracts/Protocol.cs: resolution/uncertainty fields and bounded canonical request ledger.
- src/Pocket.App/AutomationFixtureWindow.cs, AdverseProviderWindow.cs, StandardProviderFixture.cs, AcceptanceHost.cs and App.xaml.cs: guarded disposable providers and adverse state controls.
- tests/Pocket.Acceptance.Runner/Program.cs, Reliability.cs, Desktop.cs: seeded profiles, provider/IPC/identity/compatibility/resource evidence and bounded desktop lifetime.
- tests/Pocket.Core.Tests/RequestLedgerTests.cs: duplicate GUID formatting and capacity fail-closed regressions.
- scripts/Run-AutomatedAcceptance.ps1: profile/seed parameters with environment restoration; handoff/research/decisions/traceability/compatibility/evidence docs updated.

No consumer external-control workflow, B1, webcam or capture implementation was introduced.

## K. A1 acceptance

Executable automated/injected subset passed with explicit shell/display gaps. **Full A1 physical acceptance remains BLOCKED.** No human/device-origin evidence was supplied or relabelled. A10 shell and additional display setups remain open independently of engineering progress.

## L. A2 readiness

**PROVISIONALLY IMPLEMENTED; CONTROLLED AUTOMATED SUBSET VERIFIED; FULL A2 NOT ACCEPTED.** A06/A07/A08/S04 gained native evidence. A09 real-app physical drag/focus compatibility, consumer per-app authorization/disposition, full A1 prerequisites and release review are still open. Nothing SHIPPED or merged.

## M. Remaining defects and limits

1. **P1 release restriction:** uncertain provider mutations cannot be rolled back by killing the client. Preserve OutcomeUnknown, no retries and narrow verified adapters; consumer external controls stay unavailable.
2. **P1 gate gaps:** A1 physical/shell/display and A2 ordinary-app/cross-app drag/focus evidence missing. Passing controlled fixtures does not close these.
3. **P2 compatibility scope:** virtualized/irregular/large or obscured foreign trees, actual PID/HWND reuse and further pattern adverse states unverified. No global fallback.
4. **P2 coverage limits:** bounded soak is under five minutes of typing combined, not hours; no controlled load sweep, long leak/battery/frame test, abrupt-supervisor recovery or genuine stalled mutation test.
5. Native IUIAutomation2 connection/transaction timeout controls were researched but not configured through this managed client. The process watchdog remains the verified outer containment boundary.

No reproduced unresolved wrong-target action, missed character, leaked owned process or primary-host freeze occurred in these four runs. That statement is limited to their evidence.

## N. Exact next milestone

**A2 acceptance review**, using this report, current traceability and preserved evidence. Resolve external-action uncertainty policy and consumer per-target capability scope, then plan the outstanding permitted compatibility/physical evidence. Full release gates remain unchanged. Do not begin B1, camera tracking or capture.

## Sources checked

Microsoft documentation opened2026-11-05: [FromPoint](https://learn.microsoft.com/en-us/dotnet/api/system.windows.automation.automationelement.frompoint?view=windowsdesktop-10.0) documents physical coordinates and warns that bounds do not prove clickability; [FindAll](https://learn.microsoft.com/en-us/dotnet/api/system.windows.automation.automationelement.findall?view=windowsdesktop-10.0) documents relative tree scope; [UIA threading](https://learn.microsoft.com/en-us/dotnet/framework/ui-automation/ui-automation-threading-issues) supports separate-thread discovery/actions; [Invoke provider](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-implementinginvoke) distinguishes invocation from downstream completion; [IUIAutomation2](https://learn.microsoft.com/en-us/windows/win32/api/uiautomationclient/nn-uiautomationclient-iuiautomation2) exposes native connection/transaction timeout controls. API documentation supports design decisions; measured claims above come from local artifacts.
