# GHOST Execution 04 engineering report

## A. Starting state

The checkout was on `codex/a1-automation-a2-provisional` with an unborn HEAD. There is no commit SHA for the existing source. The provisional files and prior evidence are preserved. The Execution 03 baseline had clean builds, 22 deterministic tests per configuration, and a final native run with 19 passes and one blocked shell case. The A2 review found that cleanup could lose the worker handle when termination failed. [Execution 03 report](EXECUTION_03_REPORT.md)

## B. Mutation uncertainty resolution

| Outcome | Evidence required |
|---|---|
| Applied | The worker directly verified the operation's documented postcondition. |
| Rejected | The client knows it did not send the operation, or the worker positively reports rejection before calling the mutation pattern. |
| Unknown | The request may have reached the provider and the result is unconfirmed. An Invoke call without an independently verifiable postcondition falls here. |
| Conflict | The original capability, identity or state changed before dispatch. |

The client owns request IDs, the active worker, and at most one unresolved operation per fixture session. The worker owns the single-use capability and marks the dispatch boundary immediately before calling the provider. A write interruption is conservative because even a partial transport failure can leave dispatch uncertain. Dismissal releases the session hold. Any subsequent action needs a new deliberate request and a fresh probe. Neither restart nor observation resends a mutation.

A user can request at most three observations of the original operation. Each uses the existing worker deadline and revalidates fixture process lifetime, HWND, root and element IDs, bounds and point. The worker reads only the pattern state. RangeValue checks the requested absolute value within tolerance and reports limit changes. Toggle compares the observed binary state with the earlier state. Scroll compares the observed direction with the earlier position. Generic Invoke has no readback contract. All reconciliations keep the original result Unknown and report observations separately, including expected state, prior state, conflict, unavailable target and inconclusive evidence. Runtime IDs are compared as opaque values and can be reused over time. [UI Automation runtime identity](https://learn.microsoft.com/en-us/dotnet/api/system.windows.automation.automationelement.getruntimeid?view=windowsdesktop-10.0) · [Invoke provider contract](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-implementinginvoke)

## C. Worker and IPC reliability

Timed-out or invalid exchanges quarantine the worker and invalidate its capabilities. Process termination and the one-second exit wait run off the WPF thread. A failed termination leaves the process under supervision and prevents a replacement from starting. Explicit subsequent requests or disposal can check again. Reply IDs and mutation outcome fields are validated on the pipe of the active generation. The retired pipe cannot supply evidence to the replacement. [Process termination and exit observation](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.kill?view=net-10.0)

## D. Failure testing

| Controlled case | Observed result |
|---|---|
| Pre-cancel and stale capability | Pre-cancel returned Rejected. Slow validation expired a capability before provider dispatch and returned Conflict with zero mutation calls. |
| Delayed RangeValue, Toggle and Scroll | Each worker timed out and exited before the provider applied the change. The provider committed after release and received exactly one mutation call. Subsequent readback observed 42, On and 10% respectively while each mutation stayed Unknown. |
| Commit before acknowledgement loss | The range provider had already applied 42 when its worker exited. A later fixture write set 33, which reconciliation reported as StateConflict. Target process exit subsequently reported TargetUnavailable. |
| Generic Invoke and original identity | Independent fixture counter verified the benign effect. Generic reconciliation stayed inconclusive. Replacing the element produced TargetChanged. |
| Ignored and rejected range writes | An ignored write left the earlier value and retained Unknown. The provider exception also retained Unknown because the mutation call was entered. Read-only input was rejected before dispatch. |
| Failed termination and late acknowledgement | The fault double denied termination and then wrote a late Applied reply. The original process stayed quarantined. The replacement started only after exit and its observation EOF stayed inconclusive. |
| IPC and repeated operations | Duplicate IDs and stale tokens were rejected. Malformed Execute reply remained Unknown. Duplicate, partial, oversized and miscorrelated IPC replies failed closed. A second mutation stayed blocked while uncertainty was pending. |
| Observation and UI lifecycle | Timed-out and canceled reads were contained. The three-check budget was enforced. WPF handlers presented Unknown, checked state and dismissed it. The independent host responded in under 0.6 ms in all four gated mutation trials, with no Ghost claim. |

## E. User-facing recovery

The private-desktop fixture has a WPF recovery window with keyboard access keys and polite automation live-region announcements. Unknown holds the action button and exposes Check current state and Dismiss. Conflict explains that the fixture changed before dispatch. Dismiss warns about possible duplication before allowing a separate new action. The harness exercised the actual WPF handlers and control state. Assistive-technology announcements and physical keyboard traversal remain manual checks.

## F. Builds and tests

From the project root, executed locked restore, Debug and Release solution builds, deterministic tests in both configurations, and the short native profile. The initial Release build caught a duplicate primary-constructor capture in the adverse provider peer, which was corrected by using the inherited owner. Final build logs and TRX files are under `artifacts/execution-04`.

```powershell
& .\scripts\dotnet.ps1 -Arguments @('restore','Pocket.Dual.Ghost.sln','--locked-mode')
& .\scripts\dotnet.ps1 -Arguments @('build','Pocket.Dual.Ghost.sln','-c','Debug','-p:Platform=x64','--no-restore')
& .\scripts\dotnet.ps1 -Arguments @('build','Pocket.Dual.Ghost.sln','-c','Release','-p:Platform=x64','--no-restore')
& .\scripts\dotnet.ps1 -Arguments @('test','tests/Pocket.Core.Tests/Pocket.Core.Tests.csproj','-c','Debug','--no-build','--no-restore')
& .\scripts\dotnet.ps1 -Arguments @('test','tests/Pocket.Core.Tests/Pocket.Core.Tests.csproj','-c','Release','--no-build','--no-restore')
& .\scripts\Run-AutomatedAcceptance.ps1 -Profile short -Seed 73021
```

Both configurations built with zero warnings and errors. All 22 deterministic tests passed per configuration. The first short run passed the mutation suite but recorded three inconclusive cursor samples, including movement with GHOST closed. The second recorded 713 characters against 707 expected at the failing chunk. Its cause remains unverified. The typing harness now submits one Windows input batch per chunk, verifies exact foreground and editor focus, and retains the expected digest on timeout. Windows serializes the events within one SendInput call. Exact digest and length checks are still required. First run (`artifacts/automated-acceptance/20261105T093947879-8ed4bc34d55f411b94ac54203c2ddde7/results.json`, local only) · Typing failure (`artifacts/automated-acceptance/20261105T094322729-e2a2862ca73d46d984cf6998f95da784/results.json`, local only) · [SendInput contract](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput)

The final short run passed 22 cases with one blocked shell case and zero failures. It recorded 20 exact typing matches across 744 characters, 100 click-through trials, 100 drag starts/releases, 20 held drags and three stationary cursor samples. On exit, the runner returned 0 and the cleanup audit observed the Default input desktop and zero scoped processes. Warm probe p95 was 27.11 ms across 20 samples. Final native evidence (`artifacts/automated-acceptance/20261105T094547971-b1f74b41ae6441a3aa7c94fcf9b32a20/results.json`, local only) · Cleanup audit (`artifacts/execution-04/cleanup.json`, local only)

## G. A1 status

Full A1 physical acceptance remains blocked. The automated cases use a private desktop and the recorded DPI applies to the tested display. Explorer Alt+Tab and tray behavior, physical mouse and keyboard trials, and additional display configurations still need evidence. [Physical checklist](A1_NATIVE_CHECKLIST.md)

## H. A2 readiness

A2 mutation recovery is provisionally implemented and verified on controlled fixtures. Full acceptance still requires the A1 gate and A09 ordinary-app evidence with physical focus and drag. F05 permits core progression after a formal deferral of optional external integration, subject to the foundation gate.

## I. B1 scope and dependencies

| Canonical contract | Scope | Dependency assessment |
|---|---|---|
| F06 / M03 | Opt-in, local Hand Landmarker sidecar, bounded observations, health and shutdown, visible states, stop/restart, camera-off shortcuts | Schema, clock and synthetic replay are independent engineering pieces. A complete vertical slice requires Gate A/M02 disposition, a pinned and audited Windows runtime, wheel and model, and opted-in camera verification. |
| F07 / M03 | Calibration for one display, preview on consent, mirror, hand and comfort region, sensitivity, practice and cancel | Numeric transforms are testable offline. The live slice requires F06, a bounded preview route, and actual display alignment and camera checks. |

The Gate A sequencing decision is ADR-05. Windows lists two camera devices and Python 3.12.10 is installed. MediaPipe and OpenCV were absent from that interpreter, and the workspace had no pinned tracker model bundle. The documented Windows/Python support covers the broad platform, while exact wheel, backend and model-license verification remains open. No B1 task depends on an A2 consumer adapter if the optional capability receives a formal deferred disposition. [Roadmap](../codex/EXECUTION-PHASES.md) · [ADR-05](DECISIONS.md) · [MediaPipe setup](https://developers.google.com/edge/mediapipe/solutions/setup_python) · [Hand Landmarker](https://developers.google.com/edge/mediapipe/solutions/vision/hand_landmarker/python)

## J. B1 implementation

B1 remains at dependency review. Execution 04 conditions require canonical eligibility and a dependency-ready complete slice. Gate A remains open and the exact F06 runtime, model and opt-in hardware path remain unverified. The audit found independently testable pieces but no complete B1 slice eligible under those conditions.

## K. Architecture review

External actions remain scoped to the existing fixtures and run in the isolated MTA worker. The new WPF view awaits IPC asynchronously. Reconciliation holds no GHOST pointer claim and calls no mutation pattern. The native cursor and the DUAL input path continue to use the existing boundaries. The only synthetic-input change is in the guarded acceptance runner.

## L. Remaining issues

- Full acceptance still depends on A1 physical, shell and additional DPI evidence and an A09 ordinary-app decision.
- Provider actions can complete after worker exit. Readback can establish a state snapshot while attribution remains uncertain.
- The tests exercise timeout after IPC dispatch and cancellation before dispatch. They do not force an OS process-start timeout before IPC or exhaust all UIA identity reuse cases.
- F06 runtime/model pinning, asset licensing and real opt-in hardware lifecycle remain open.

## M. Next execution

Resolve the outstanding Gate A evidence and record the M02 disposition. The first eligible B1 slice is F06 camera enable, observation, stop and restart with a pinned local tracker. Verify it on an explicitly enabled camera before extending to F07 calibration.
