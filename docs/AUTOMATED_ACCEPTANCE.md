# GHOST — automated acceptance evidence

Execution 04 final short profile recorded 22 passes and one blocked Explorer shell case. The controlled WPF provider committed mutations after worker exit, and reconciliation preserved uncertainty. The earlier typing mismatch and inconclusive cursor trials remain in their original run folders. Final results (`artifacts/automated-acceptance/20261105T094547971-b1f74b41ae6441a3aa7c94fcf9b32a20/results.json`, local only) · [Engineering report](EXECUTION_04_REPORT.md)

## Execution03 reliability checkpoint — 2026-11-05

**A2-PROVISIONAL — A1 PHYSICAL ACCEPTANCE PENDING.** Controlled automated subset verified; full A2 NOT ACCEPTED. Current evidence: artifacts/automated-acceptance/20261105T022024024-0e7c9ffb23d94a3bb30d9700ac6caf54/results.json; complete report docs/EXECUTION_03_REPORT.md. Earlier sections below are historical.

Scoped root-tree underlay lookup now Confirmed at a physical point inside the opaque panel. Duplicate/disabled/missing/replaced elements reject. Client binds process start time across restarts, rejects concurrent work as Busy and exact-correlates replies; request IDs are deduplicated and capabilities are single-use/300ms with root/element identity and bounds checks. Uncertain Execute outcomes are flagged and never automatically retried.

Real custom-provider GetAutomationIdCore callback blocked in a separate process; final timeout1826.28ms, independent primary-host status0.59ms, worker exit and fresh-worker recovery verified. Cancellation, provider exception/termination, old capability rejection and duplicate/malformed/partial/oversized/incorrect IPC responses passed. Actual provider mutation rollback is not guaranteed; hang proof is a read query.

Four Execution03 runs (short/standard/extended/final short), three seeds:200 cycles,7152 exact characters,200 digest matches,0 mismatches/recoveries. Standard60 cycles2141 chars and extended100 cycles3523 chars belong to preceding binary versions; final short20 cycles744 chars covers final readback safeguards. Both WPF and standard WinForms/Win32 controlled fixtures independently confirm four patterns. Ignored range write Attempted; rejected write Unsupported; read-only Denied.

Locked restore PASS; final Debug/Release builds0 warnings/errors;22 deterministic tests/config PASS. Evidence artifacts/execution-03 has final binlogs, TRX, summary and cleanup audit. Final native20 cases:19 PASS,1 shell BLOCKED. Warm probe p9531.12ms (20 samples), working set47259648 bytes; spot sample only. Desktop Default restored,0 remaining scoped processes. Product has no native input injection, camera/capture/B1 or consumer external-action flow.

**Next: A2 review.** Full A1 physical/shell/extra-display and A2 ordinary-app/cross-app physical focus/drag evidence remain open; no release merge or SHIPPED claim. No need to repeat the old manual-only deadlock to perform separately authorized engineering.

Date2026-11-05, Asia/Calcutta. Workstream: **A2-PROVISIONAL — A1 PHYSICAL ACCEPTANCE PENDING**.

## Actual outcome
Executable isolated A1 cases: PASS on final run. Entire original A1 gate: BLOCKED (human/device provenance, shell and additional display setups missing). Full automated coverage: BLOCKED for shell Alt+Tab/tray and additional physical configurations. These are independent outcomes, not a gate waiver.
Provisional A2: fixture-only contracts/worker/client/pattern tests implemented; full A2 NOT ACCEPTED.

Latest actual evidence: results JSON (`artifacts/automated-acceptance/20261104T202754048-1b98c61d379f495cacb5c733e79e906b/results.json`, local only), per-case report (`artifacts/automated-acceptance/20261104T202754048-1b98c61d379f495cacb5c733e79e906b/results.md`, local only). Historical run directories remain intact. First corrected GUI pass was20261104T201715492-31d5077caa174000a31350a18e7ef887; subsequent A2 run20261104T202104577-faa6c50809d5430695f58b43260f1f11 passed patterns. Run20261104T202546669-31edc4ed07c5494f96445395be3cd92c had typing mismatch and is preserved. Final20261104T202754048-1b98c61d379f495cacb5c733e79e906b passed executable cases after stronger per-chunk digest acknowledgement.

## Environment and safeguards
Windows26200 x64; SDK10.0.403/runtime10.0.14, primary1920x1200 at120DPI. Existing Computer Use package was initialized; it returned4 targetable windows and no GHOST window, so existing session was not an isolated fixture environment. Chosen approach: repeatable dedicated Win32 test desktop with separate runner/App processes; original desktop restored. No arbitrary window/key content inspected, no default-desktop injection or scaling change. Test-only injection code is in tests, not a product dependency. Permission escalations were for SDK execution/GUI test launch, not application elevation to bypass integrity.

## Acceptance matrix
| Case | Expected | Final observed | Class | Result |
|---|---|---|---|---|
| Cursor closed |120 samples/no generated movement|0 changed|NATIVE_SOFTWARE_TEST|PASS|
| Cursor idle |same sampling|0 changed|NATIVE_SOFTWARE_TEST|PASS|
| Cursor replay |same sampling|0 changed|NATIVE_SOFTWARE_TEST|PASS|
| No-overlay routing control|1 click|1 received|INJECTED_GUI_TEST|PASS|
| A02 opaque-panel click-through|100|100, no duplicates|INJECTED_GUI_TEST|PASS|
| A02 drag-start/release|100 starts/ends|100/100,0 canceled|INJECTED_GUI_TEST|PASS|
| A03 typing|20 cycles/exact match|900 Ghost events,560 chars, exact digest|INJECTED_GUI_TEST|PASS|
| A04 native hold/Ghost advancement|20 trials|20 starts/ends,0 canceled|INJECTED_GUI_TEST|PASS|
| Simulator stop/restart|release and restart|verified|NATIVE_SOFTWARE_TEST|PASS|
| Overlay close during press|claim/timer/HWND gone|all true|NATIVE_SOFTWARE_TEST|PASS|
| Exit cleanup|process and fixture/overlay HWNDs gone|exit0, handles gone|NATIVE_SOFTWARE_TEST|PASS|
| A10 shell Alt+Tab/tray|real shell switching|private desktop lacks Explorer; default desktop not targeted|INJECTED_GUI_TEST|BLOCKED|
| A11 current display|actual DPI/bounds|125%,1920x1200|NATIVE_SOFTWARE_TEST|PASS metadata only|
| Other DPI / mixed monitor|actual display setups|not changed, mixed-monitor deferred|NATIVE_SOFTWARE_TEST|NOT RUN|
| DUAL / protocol|20 tests per config|20 passed,0 failed/skipped|LOGIC_TEST|PASS|
| Full original human/device acceptance|genuine origin|not provided|HUMAN_PHYSICAL_TEST / DEVICE_ORIGIN_TEST|NOT RUN|

Closed/idle/active cursor controls found no movement here; they do not establish the cause of prior unattended changed coordinates. No reset was used to force a stationary result.

## A2 provisional traceability
F05/AC-F05, M02, A06–A09/S04:
- Contracts: strict operations/known fixture targets, finite coordinates/values,16KiB bounds.
- Worker: separate process/windowless MTA; registered PID, HWND marker, target identity/pattern validation;300ms single-use capability tied to runtime ID; Invoke/Toggle/RangeValue/Scroll.
- Client: serialized bounded RPC,2000ms default deadline, cancellation/kill, fresh worker on independent next request, concurrent stderr drain; no automatic action replay.
- Native fixture: all4 patterns executed and independently confirmed; Invoke correctly returns Attempted until fixture postcondition confirms it. Consumed tokens Stale and wrong PID Denied.
- Under-overlay point probe: Unsupported, with no fallback action/injection. This is an explicit capability limitation; no generic underlay resolver or compatibility claim.
- Watchdog:2 intentionally stalled-worker-double requests timed out,2 process starts, roughly0.6s combined; genuine third-party COM-provider hang NOT RUN.
- Normal consumer App references neither runner injection nor WorkerClient. A2 interface exists only via guarded acceptance mode; consumer external controls remain unavailable.
- Real app capability allowlists, compatibility/focus/drag evidence and polished experimental UI remain BLOCKED/NOT RUN. A2 is partial/provisional, not complete/shipped.

## Recovery and unresolved risks
Native runner failures recorded without overwriting history. Unicode desktop name marshalling and DPI coordinate-launch issues were harness defects. Typing handshake tightened after a single intermittent mismatch; final pass is not a stability benchmark. Closure-to-timer cancellation was explicitly wired and native-tested. Parent watchdog covers child timeout; forced supervisor death/power-loss restoration, real UIA provider hang, long soak and shell integration still need validation. No known reproduced high-severity product input defect from final cases; keep provisional boundaries until missing cases resolved.

## Reproduce
From this project root:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Run-AutomatedAcceptance.ps1 -Build
& .\scripts\dotnet.ps1 -Arguments @('build','Pocket.Dual.Ghost.sln','-c','Debug','--no-restore','-p:Platform=x64')
& .\scripts\dotnet.ps1 -Arguments @('test','tests/Pocket.Core.Tests/Pocket.Core.Tests.csproj','-c','Debug','--no-build','--no-restore')
```
Final Debug/Release binaries compile with0 warnings/errors,20 tests per config. Build evidence artifacts/automation-final-{Debug,Release}.binlog; tests artifacts/automation-tests/automation-final-*.trx. Script requires interactive unlocked default desktop and briefly switches to private test desktop; no security bypass or virtual driver.

Next engineering work: stabilize runner timing/typing with a repeatable soak and expand controlled underlay/provider-stall compatibility tests. Keep A2 fixture-only/provisional; no B1 or release. No repeated100-click human checklist is required to make this next engineering progress.

## Sources checked
Microsoft [Desktops](https://learn.microsoft.com/en-us/windows/win32/winstation/desktops), [SwitchDesktop](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-switchdesktop), [SendInput](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput), [UIA threading](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-threading), accessed2026-11-05. API availability is separate from the actual measured results above.
