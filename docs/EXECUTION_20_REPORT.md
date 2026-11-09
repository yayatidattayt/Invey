# Execution20 recovery — controlled dual-pointer product

The previous incomplete report is preserved as [EXECUTION_20_CHECKPOINT_REPORT.md](EXECUTION_20_CHECKPOINT_REPORT.md). All earlier failed logs remain in their original artifact directories.

**Verdict at this report checkpoint: controlled product route PASS; final regression/publication verification is recorded in the delivery addendum. Physical camera/hand actuation remains BLOCKED.**

## 1. Previous checkpoint
Recovered clean PR9 branch codex/execution-20-interaction-engine at84b8102275292a2b3ecf79ccf4728768cbccd76f. Fetched remote refs; the same head remained current. No reset, clean, force push, parent merge or profile migration. The old private Workspace coordinator was the only connected interaction surface.

## 2. Resize/focus root cause
Native failure snapshots place the pinned card at(610,320),700x500, covering the Workspace Enlarge mouse point around(719,367). Pinning was behaving legitimately. The attempted Alt+L repair also collided with Move left’s accelerator. Added selected slot, command-enabled state and keyboard-focus diagnostics confirm B selected and the resize command enabled. After resize was repaired, the new visibility-aware router correctly rejected a covered B grip; this is distinct from the resize failure.

## 3. Reliability repair
Enlarge now has unique Alt+E. The native demo moves the pinned card using its real native title bar to a clear area before Workspace interaction; it keeps the pin and real resize postcondition. No dimensions are written through a test-only shortcut. One early two-seed attempt passed the complete route; the other failed at initial foreground setup, before resize. Subsequent full native D17 passes are recorded below. Compiler/fixture attempts are preserved.

## 4. Architecture before and after
Before: Tracking/practice, private Workspace simulation and ordinary Pocket commands were disconnected. After: one MainWindow-owned GhostDesktopHost owns one Core GhostDesktop session, one passive pointer window and a registry of real surfaces. Workspace callbacks still use its existing InteractionState reducer for Mouse/Ghost claims. The old GhostInteraction class remains for compatibility tests and is no longer the product coordinator.

## 5. Shared pointer
The source session has a generation token, source kind, sequence, timestamp, physical desktop position, provenance, mode and active target. Events from old sessions, invalid ordering and expired data fail closed. A controlled-source heartbeat refreshes only its explicitly started session. A replaced Workspace simulation checks its own generation before stopping, so its old timer cannot stop a new source. Window closure cancels only associated claims.

## 6. Passive display
The new pointer window reuses NativeWindow.ConfigurePassive and native positioning. It is layered, topmost, non-activating, hidden from task switching, WPF-hit-test-disabled and mouse-pass-through. It shows a small ring and a deliberate Pocket affordance. Ring fill changes while holding. Its arranged physical point and the router position are independently compared in the native test; this is not optical scanout latency. Legacy diagnostic replay and the desktop pointer are mutually stopped/hidden when switching modes.

## 7. Tracking connection
RecordedTrackingWindow’s explicit Desktop Ghost preview control initializes calibration setup and feeds qualified snapshots to the MainWindow adapter. Recorded inference and the separately enabled camera use the same mapper. Calibration and selected-hand settings are required. Recorded MediaPipe inference actually moves the desktop pointer in the new WPF/native route. No camera was activated.

## 8. Intent boundary
Typed source kinds distinguish Controlled, Recorded and Camera; position provenance distinguishes Controlled, FullHand, PartialHand and FingerVisual. Only the explicitly controlled source can arm or submit actions. Recorded/camera preview cannot arm, even with full-hand position evidence. No caller-supplied full-hand boolean authorizes production actions. Partial/visual evidence can move preview and cancels an active hold. No live gesture classifier or dwell threshold was promoted.

## 9. Registry
Real registrations include stable IDs, lifetime tokens, current visibility/enabled geometry, owning window, native stacking rank, within-window priority, supported commands, drag policy and lifecycle callbacks. Windows/rows unregister on closure/replacement. Geometry is queried from actual WPF elements; no stale HWND lookup dispatches actions externally. Duplicate registration rejects.

## 10. Hit testing and stacking
Native top-level z-order and visible window rectangles gate reachable targets, skipping transparent passive windows. A covering unrelated window blocks action. Within Workspace, current Canvas z-order controls overlapping targets. The sole windowless affordance is the explicitly drawn Pocket tab; it can only invoke the registered internal shelf reveal command. It does not act on the underlying external window. Unsupported commands are rejected and disabled in the controller.

## 11. Coordinates
The shared logical pointer’s coordinate contract is physical virtual-desktop pixels. Raw validated index → working-region/mirror calibration → compatible accepted personal correction → one precision filter → display origin/extent → physical pointer. WPF PointToScreen/PointFromScreen translate actual elements and board positions, accounting for DPI. Native card movement uses physical window bounds and the current usable work area. Deterministic tests cover96/120DPI, negative origins, mirroring and incompatible correction fallback. Physical mixed-DPI acceptance remains unavailable.

## 12. Workspace
The old simulated route and explicit controlled movement both feed the shared pointer. Actual A/B PNG views use the existing reducer, preserving grab offsets, Mouse ownership and owner-specific release. The native product route holds A with injected native mouse input while shared Ghost acquires/moves/releases B, verifies actual canvas positions, then tests loss. Closing Workspace cannot cancel a separately owned floating card. Moving/changing DPI/scroll mapping disarms a Workspace claim.

## 13. Shelf
Ghost explicitly activates the passive Pocket affordance to reveal the real library without activation. Actual saved-reference rows register as targets; activation opens the real card using existing storage/decode logic. Dismiss tucks the shelf and disarms, preserving pinned cards. Empty libraries register no invented reference. Existing capture, shortcut, rename, order, delete and Undo remain intact.

## 14. Floating cards
The actual existing card layout registers a draggable target. Commands preserve the grab offset and clamp movement to the usable work area. Mouse capture/native move-size activity blocks conflicting acquisition. Native user movement of the same card cancels its Ghost claim. Hide uses the existing close path. No Ghost deletion, unrestricted resize, zoom or scrolling is advertised; ordinary controls remain available.

## 15. Pinning and persistence
Pin/Unpin uses the existing checkbox/state handler, native Topmost and existing debounced/queued writer. Native styles are read independently. Final placement is flushed through existing close/restart behavior. Reopening through Ghost reloads current reference metadata rather than using stale shelf placement. The first persistence test mistakenly expected the full monitor’s bottom instead of the taskbar-excluding work area; the actual safe clamped position was retained, and the expectation now derives from the independently exposed work area.

## 16. Mouse independence
Native cursor samples before/after Ghost commands remain equal, and native mouse movement moves A independently of Ghost-owned B. No product SetCursorPos/SendInput was added. Test injection remains confined to the established isolated desktop. These are software/native observations, not physical human acceptance.

## 17. Focus
A separate editor-like WPF fixture retains its foreground HWND and keyboard focus while Ghost reveals/opens/moves/pins/tucks supported Pocket surfaces. Exact known typing is independently checked by digest. Workspace remains an ordinary focusable window. This evidence is from an isolated desktop and does not establish universal normal-desktop behavior in arbitrary external apps.

## 18. Interruption
Core tests cover duplicate/order/future/stale/nonfinite events, source replacement, partial/visual evidence, target disposal/visibility, expiry and idempotent Stop/Pause. The actual product route tests recognized loss during a hold, late-intent denial, replacing a running simulation, unrelated Workspace closure and closing a Ghost-owned card through its real Hide control. Rendering failures produce a visible released state and a trace warning. Worker shutdown remains supervised.

## 19. Personal learning
Opt-in lessons, raw mapped labels, independent validation, persistence, rollback/reset and context mismatch remain preserved. Compatible accepted coefficients may affect desktop preview before temporal filtering; they do not grant action authority. Incompatible or invalid models fall back to the original map. Teaching never consumes filtered or visual-only positions. Physical improvement remains unvalidated.

## 20. Smoothing
FingerPointer can provide validated unsmoothed eligibility for the desktop adapter. The mapper applies only one PrecisionPointFilter after geometric/personal correction. Camera image/model overlays and the old calibration practice visualization remain diagnostic views, not separate product pointer authorities. The controlled button source uses exact explicit coordinates, with no hidden second smoothing layer.

## 21. Performance
The first complete source-connected product route measured median2.17ms/p9538.42ms for IPC command round trips including WPF state observation. These are not optical pointer latency. Host diagnostics separately expose mapping, movement and intent-dispatch timings, maximum movement cost and bounded update counts. No unbounded history or per-frame storage write was added. A sustained CPU/thermal/leak qualification remains separate; inherited resource/worker checks are run in the full regression.

## 22. Core
150 tests pass per Debug/Release at the recovery validation checkpoint. New tests enforce source capability, preview-only behavior, loss cancellation, generation rejection, stacking/disposal, expiry and compatible/incompatible model mapping. Prior137 tests remain. The historical migration access fallback remains documented; no storage safeguard was suppressed.

## 23. Worker
29 tests pass, no skips, including native reading of the existing generated MP4. No new production package/model or camera dependency was installed. Camera budgets and v1/v2/v3 tracking protocols remain unchanged.

## 24. Native
E20 controlled route29fbda09 and controller-button route31a22749 pass. Each drives actual shelf/card/Workspace content, reads native styles and geometry, checks native cursor/focus, connects recorded inference, restarts and verifies persistence. All failed runs remain preserved. The new route uses its own clean isolated dual-demo-profile, so an earlier demo’s additional pinned cards cannot make a LastWindow-based restart assertion select the wrong restored card.

## 25. Complete demo
The formerly failing D17 resize path passes in the full recovery run d8af3c64, with its original resize and pin expectations retained. That run’s new E20 stage failed only its shared-profile restart assertion; the old workflow’s extra pinned cards contaminated that fixture’s selected last card. The new route now starts/restarts its own clean profile, matching the acceptance contract. Final complete reruns are recorded in the delivery addendum.

## 26. Camera authorization
Ten released rows,809686ms, zero pending releases and zero remaining Execution18 starts. Ledger SHA25601A3B86D3121061FCDE4D4BC2903F1C0DADDF4B5D6F928F207BE7A937BE78D58 is unchanged. The new launcher preserves this guardian and starts camera off. No permission, cap or watchdog was extended.

## 27. Physical limitations
REAL CAMERA and PHYSICAL HUMAN TEST remain BLOCKED. Future newly authorized tests must measure selected-hand alignment, pinch false activations, acquisition/drag/release, loss/two-hand/Stop cancellation, real mouse concurrency, appropriate focus behavior and independent camera release. No synthetic/native pass is relabeled physical. Cold fingertip acquisition and sustained finger-only reliability remain open.

## 28. Preserved failures
Original checkpoint resize/focus failures remain. Recovery logs preserve: a missing System.IO diagnostic import compile error; nullable callback compile error; one card-close IPC timeout (subsequent cleanup now uses its cached original HwndSource rather than querying a destroyed handle); initial display-uninitialized preview fixture failure; incorrect full-monitor parking expectation; initial foreground-setup refusal; and the shared-profile restart mismatch. Exact causes are not inferred beyond the observed evidence. No tests were skipped, removed or weakened.

## 29. Stack and delivery
PR1–9 remain open/unmerged and mergeable at audit. Each parent’s current exact head is an ancestor of this branch and has passing Windows CI. No submitted reviews were returned. Review order remains1→9; CI and mergeability do not replace review of shared state, migrations or lifecycle. Continue PR9; no new milestone branch was created. Exact implementation commit, build hash, final CI and cleanup are recorded in the delivery addendum/local final-delivery.json.

## 30. Readiness
The supported target set is deliberately bounded to reveal/open/dismiss, Workspace drag/release, and floating-card move/release/pin/unpin/hide. Source and command gates remain explicit. Completion of controlled Checkpoints A–G is declared only after the final full regression and exact published-head CI succeed. Full live-hand release acceptance remains separate.

### References
Reviewed Microsoft’s [extended window styles](https://learn.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles) and existing native passive-window implementation. WPF’s actual screen/element transforms and the project’s existing calibration mapping are reused instead of guessing display scale.


## Historical pause

The user requested a smooth stop at10% remaining usage. At that point, source changes were uncommitted and final regression/publication had not run. The user later resumed this same checkpoint. See `codex/EXECUTION_20_RESUME.md`; all original pause artifacts and failed runs remain preserved.

## Final recovery and verification addendum — 2026-11-09

### Final camera-free verification

- Locked restore passed with the pinned .NET SDK10.0.403 using one MSBuild worker. Default parallel MSBuild worker startup was rejected in this managed Windows session; the initial failure is retained in `final-restore-diag2.log`.
- Full Debug and Release solution builds passed with zero warnings and zero errors (`final-Debug-build-after-recovery.log`, `final-Release-build-after-recovery.log`).
- Core tests:150/150 passed in Debug and150/150 in Release; zero skips (`final-recovery-Debug.trx`, `final-recovery-Release.trx`). VSTest needs localhost test-host communication. The first unapproved run could not establish its localhost socket and was preserved; reruns used the approved local test process boundary and completed normally.
- Tracking worker tests:29/29 passed, no skips, using the project’s pinned `.tools/exec05-inference` runtime and controlled video fixture (`artifacts/execution-20/final-worker.log`). An initial machine-global Python attempt failed because it lacked NumPy and could not write its default temp path. The final-worker log records the successful pinned-runtime run; the initial failure was observed during this resumed validation and is not represented as preserved in that log.
- Final full extended private-desktop run `20261109T151625931-104c0d76941e4388b6b1f0747fb5913c`:40 PASS,0 FAIL,1 BLOCKED. `A10_ALT_TAB_TRAY` remains blocked because the private desktop has no Explorer shell. All runnable cases passed, including Pocket capture/preview/save/resize/pin/library operations, 30 independent Workspace ownership trials, D17 complete demo, E20 shared-pointer workflow, recorded tracking/calibration practice, error controls, and owned-process cleanup.
- Final seeded E20 product routes passed with seeds73021 (`20261109T150506619-ac44f5a6660d4f779d52a0ffba7051cf`) and99174 (`20261109T150545644-4c9546353fa849b2985cb38b4f3a4f64`). They confirmed actual shelf/card/Workspace UI, mouse focus and cursor independence, stop/loss cancellation, persistence, and recorded-preview command denial. `CameraOpened=false` in both.
- Focused Pocket run `20261109T151529075-c2533875641d47d0a39c0ae120cd53a2` passed C01/C04 capture and resize, restart/library, 30-trial C09 Workspace control, shortcut/shelf, D17 and E20. The earlier extended run `20261109T150618623-687c8dbdb3bb45abb46183e4daacf57d` preserved a Pocket resize failure; the test now waits for native sizing to register and uses four bounded drag increments. The subsequent focused run and final full extended run pass. The intervening C09 failure `20261109T151252563-e8df508f4f054e57a342602151687e18` is also preserved; its targeted 30-trial run and final full extended run passed. No failed run was deleted or converted to skipped.
- The E20 harness previously exposed its full-screen synthetic Pocket capture fixture over the editor used for the focus check. The E20 test now hides that test-only fixture before the editor interaction; actual Pocket capture remains exercised separately and passed.

### Cleanup, privacy and launcher

- Isolated acceptance restored the input desktop to `Default`; read-only environment audit confirmed it. No Invey, worker, test or dotnet process remained after the final run.
- Camera ledger SHA-256 remained `01A3B86D3121061FCDE4D4BC2903F1C0DADDF4B5D6F928F207BE7A937BE78D58`; ten historical released entries remain, with no reservation or new session. No webcam was opened.
- `scripts/Start-Invey-DualCursor.ps1 -CheckOnly` passed. It identifies `src/Pocket.App/bin/Release/net10.0-windows/Invey.exe`, the isolated demo profile, all optional recorded-tracking prerequisites, camera-off startup, and zero running instances. Normal profile remains separately selected.
- Release executable SHA-256: `7BAF442E92DD7A795D1BA05CB2FB38F2EC07A545BD141BBC6CD439F1F63ABE05`. Acceptance runner SHA-256: `63D44EB5EFAC58A467C4644B112779BD226A5CAB8406A7A65A2786FE046D1C57`. Worker SHA-256: `41B6F6E72D71C30D623608635E7B256B740EAFE320363B75F980E1731704F5A0`.

### Preserved regressions and current delivery gate

Several early acceptance attempts failed before fixes or under setup constraints. They remain in `artifacts/automated-acceptance/`, including the source-replacement/persistence issues, foreground ownership, control-window overlap, the synthetic capture fixture covering focus, Pocket resize timing, and one C09 Workspace trial. The final complete run reports every runnable case PASS, with only the explicit Explorer block.

At this addendum’s creation, PR#9 still points at published baseline `84b8102275292a2b3ecf79ccf4728768cbccd76f`; its description still calls Execution20 incomplete. Delivery remains pending until the reviewed local changes are committed and pushed to PR#9 and GitHub Actions passes on that exact commit. Physical hand actuation remains blocked and is not part of this camera-free result.

**Controlled product verification: PASS.** The implementation commit `fc1d83125ef48d972cc9aeac5b484a00429ffcaf` is published on PR#9 and its exact-head GitHub Actions run `42645452357` completed successfully. **Physical hand acceptance: BLOCKED.** The GitHub connector rejected the attempted PR title/body update with HTTP403; PR#9 remains open and unmerged with its pre-existing description. No parent PR was merged.
